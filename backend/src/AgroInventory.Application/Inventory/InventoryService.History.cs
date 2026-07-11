using AgroInventory.Application.Common;
using AgroInventory.Application.History;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    private const string MovementEntityType = "InventoryMovement";

    /// <summary>Редактирование операции с пересчётом остатков (ТЗ §20).</summary>
    public async Task EditMovementAsync(Guid id, EditMovementRequest request, CancellationToken ct = default)
    {
        var movement = await _db.InventoryMovements
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct)
            ?? throw NotFoundException.For("Операция", id);

        var stock = await LoadStockAsync(movement.ChemicalId, movement.WarehouseId, ct, createBalance: true);
        var old = Snapshot(movement);

        // Откатываем старый эффект, применяем новые значения.
        ReverseEffect(movement, stock);

        var now = _clock.GetUtcNow();
        if (request.OccurredAt is { } occurred) movement.OccurredAt = occurred;
        if (request.Comment is not null)
            movement.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        // Культура — для списания (ТЗ §11.1).
        if (movement.MovementType == MovementType.Outcome && request.CropId is { } cropId)
        {
            await EnsureCropAsync(cropId, ct);
            movement.CropId = cropId;
        }

        // Поле — для списания (необязательно). Guid.Empty очищает.
        if (movement.MovementType == MovementType.Outcome && request.FieldId is { } fieldId)
        {
            if (fieldId == Guid.Empty)
            {
                movement.FieldId = null;
            }
            else
            {
                await EnsureFieldAsync(fieldId, ct);
                movement.FieldId = fieldId;
            }
        }

        ApplyEditedQuantity(movement, request);
        ApplyEffect(movement, stock, now);

        movement.UpdatedAt = now;
        Recompute(stock);
        GuardNonNegative(stock);

        _audit.Log(AuditAction.Update, MovementEntityType, movement.Id, old, Snapshot(movement));
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Мягкое удаление операции (ТЗ §20): остаётся для аудита, остатки откатываются.</summary>
    public async Task DeleteMovementAsync(Guid id, CancellationToken ct = default)
    {
        var movement = await _db.InventoryMovements
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct)
            ?? throw NotFoundException.For("Операция", id);

        var stock = await LoadStockAsync(movement.ChemicalId, movement.WarehouseId, ct, createBalance: true);
        var old = Snapshot(movement);

        ReverseEffect(movement, stock);
        movement.IsDeleted = true;
        movement.UpdatedAt = _clock.GetUtcNow();

        Recompute(stock);
        GuardNonNegative(stock);

        // В audit сохраняем все данные удалённой операции (ТЗ §20).
        _audit.Log(AuditAction.Delete, MovementEntityType, movement.Id, old, null);
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Эффект операции на остаток ----------

    private void ApplyEditedQuantity(InventoryMovement m, EditMovementRequest r)
    {
        switch (m.MovementType)
        {
            case MovementType.Income:
                if (m.UnitType == UnitType.Liter)
                {
                    if (r.QuantityLiters is { } q) { if (q <= 0) throw QtyError(); m.QuantityLiters = q; }
                }
                else
                {
                    if (r.Unit is { } unit) m.UnitType = unit;
                    if (r.PackageVolumeLiters is { } vol) m.PackageVolumeLiters = vol;
                    if (r.PackagesQuantity is { } pkg) m.PackagesQuantity = pkg;
                    if ((m.PackageVolumeLiters ?? 0) <= 0 || (m.PackagesQuantity ?? 0) <= 0) throw QtyError();
                    m.QuantityLiters = m.PackageVolumeLiters!.Value * m.PackagesQuantity!.Value;
                }
                break;

            case MovementType.Outcome:
                if (r.QuantityLiters is { } oq) { if (oq <= 0) throw QtyError(); m.QuantityLiters = oq; }
                break;

            case MovementType.Correction:
                if (r.QuantityLiters is { } delta) m.QuantityLiters = delta; // новая знаковая разница
                break;
        }
    }

    /// <summary>Применяет эффект операции к остатку (для повторного применения после правки).</summary>
    private void ApplyEffect(InventoryMovement m, LoadedStock stock, DateTimeOffset now)
    {
        switch (m.MovementType)
        {
            case MovementType.Income when m.UnitType == UnitType.Liter:
                stock.Balance!.LooseLiters += m.QuantityLiters;
                break;

            case MovementType.Income:
                var group = stock.Groups.FirstOrDefault(g =>
                    g.UnitType == m.UnitType && g.PackageVolumeLiters == m.PackageVolumeLiters);
                if (group is null)
                {
                    group = new PackageGroup
                    {
                        Id = Guid.NewGuid(), ChemicalId = m.ChemicalId, WarehouseId = m.WarehouseId,
                        UnitType = m.UnitType!.Value, PackageVolumeLiters = m.PackageVolumeLiters!.Value,
                        Quantity = 0, CreatedAt = now, UpdatedAt = now,
                    };
                    _db.PackageGroups.Add(group);
                    stock.Groups.Add(group);
                }
                group.Quantity += m.PackagesQuantity!.Value;
                group.UpdatedAt = now;
                break;

            case MovementType.Outcome:
                var plan = StockEngine.PlanOutcome(BuildState(stock), m.QuantityLiters);
                if (!plan.Sufficient)
                    throw new ConflictException(
                        $"Недостаточно остатка для новой суммы списания: доступно {plan.AvailableLiters:0.###} л.");
                _db.InventoryMovementDetails.RemoveRange(m.Details);
                m.Details.Clear();
                ApplyOutcome(plan, stock, m, now);
                m.UnitType = UnitType.Liter;
                m.PackageVolumeLiters = null;
                m.PackagesQuantity = null;
                break;

            case MovementType.Correction:
                stock.Balance!.LooseLiters += m.QuantityLiters;
                break;
        }
    }

    /// <summary>Откатывает эффект операции с остатка (для правки/удаления).</summary>
    private void ReverseEffect(InventoryMovement m, LoadedStock stock)
    {
        switch (m.MovementType)
        {
            case MovementType.Income when m.UnitType == UnitType.Liter:
                stock.Balance!.LooseLiters -= m.QuantityLiters;
                break;

            case MovementType.Income:
                var group = stock.Groups.FirstOrDefault(g =>
                    g.UnitType == m.UnitType && g.PackageVolumeLiters == m.PackageVolumeLiters);
                var packages = m.PackagesQuantity ?? 0;
                if (group is not null && group.Quantity >= packages)
                    group.Quantity -= packages;
                else
                    stock.Balance!.LooseLiters -= m.QuantityLiters; // упаковки уже израсходованы — по литрам
                break;

            case MovementType.Outcome:
                stock.Balance!.LooseLiters += m.QuantityLiters; // возвращаем в налив
                break;

            case MovementType.Correction:
                stock.Balance!.LooseLiters -= m.QuantityLiters;
                break;
        }
    }

    private void GuardNonNegative(LoadedStock stock)
    {
        if ((stock.Balance?.TotalLiters ?? 0) < 0 || (stock.Balance?.LooseLiters ?? 0) < 0)
            throw new ConflictException(
                "Операция сделает остаток отрицательным. Исправьте через детальную инвентаризацию.");
    }

    private static ValidationException QtyError() =>
        new(nameof(EditMovementRequest.QuantityLiters), "Количество должно быть больше нуля.");

    private static object Snapshot(InventoryMovement m) => new
    {
        m.MovementType, m.QuantityLiters, m.UnitType, m.PackageVolumeLiters, m.PackagesQuantity,
        m.CropId, m.FieldId, m.OccurredAt, m.Comment, m.WarehouseId, m.ChemicalId, m.IsDeleted,
    };
}
