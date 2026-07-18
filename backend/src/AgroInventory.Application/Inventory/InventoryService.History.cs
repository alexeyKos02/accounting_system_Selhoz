using AgroInventory.Application.Common;
using AgroInventory.Application.History;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    private const string MovementEntityType = "InventoryMovement";

    /// <summary>Редактирование операции с пересчётом остатков (ТЗ §20).</summary>
    public async Task EditMovementAsync(Guid id, EditMovementRequest request, CancellationToken ct = default)
    {
        var movement = await _db.InventoryMovements
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct)
            ?? throw NotFoundException.For("Операция", id);

        var balance = await LoadBalanceAsync(movement.ChemicalId, movement.WarehouseId, ct, createBalance: true);
        var old = Snapshot(movement);

        if (movement.MovementType == MovementType.Transfer)
            throw new ConflictException("Перемещения редактируются отдельной операцией. Создайте обратное перемещение при ошибке.");

        // Откатываем старый эффект, применяем новые значения.
        ReverseEffect(movement, balance!);

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
        ApplyEffect(movement, balance!, now);

        movement.UpdatedAt = now;
        GuardNonNegative(balance);

        _audit.Log(AuditAction.Update, MovementEntityType, movement.Id, old, Snapshot(movement));
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Мягкое удаление операции (ТЗ §20): остаётся для аудита, остатки откатываются.</summary>
    public async Task DeleteMovementAsync(Guid id, CancellationToken ct = default)
    {
        var movement = await _db.InventoryMovements
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct)
            ?? throw NotFoundException.For("Операция", id);

        var balance = await LoadBalanceAsync(movement.ChemicalId, movement.WarehouseId, ct, createBalance: true);
        var old = Snapshot(movement);

        if (movement.MovementType == MovementType.Transfer)
            throw new ConflictException("Перемещения нельзя удалить из истории. Создайте обратное перемещение при ошибке.");

        ReverseEffect(movement, balance!);
        movement.IsDeleted = true;
        movement.UpdatedAt = _clock.GetUtcNow();

        GuardNonNegative(balance);

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
            case MovementType.Outcome:
                if (r.Quantity is { } q) { if (q <= 0) throw QtyError(); m.Quantity = q; }
                break;

            case MovementType.Correction:
                if (r.Quantity is { } delta) m.Quantity = delta; // новая знаковая разница
                break;
        }
    }

    /// <summary>Применяет эффект операции к остатку (для повторного применения после правки).</summary>
    private static void ApplyEffect(InventoryMovement m, ChemicalStockBalance balance, DateTimeOffset now)
    {
        balance.TotalQuantity += m.MovementType == MovementType.Outcome ? -m.Quantity : m.Quantity;
        balance.UpdatedAt = now;
    }

    /// <summary>Откатывает эффект операции с остатка (для правки/удаления).</summary>
    private static void ReverseEffect(InventoryMovement m, ChemicalStockBalance balance)
    {
        balance.TotalQuantity += m.MovementType == MovementType.Outcome ? m.Quantity : -m.Quantity;
    }

    private static void GuardNonNegative(ChemicalStockBalance? balance)
    {
        if ((balance?.TotalQuantity ?? 0) < 0)
            throw new ConflictException(
                "Операция сделает остаток отрицательным. Исправьте количество или остаток через корректировку.");
    }

    private static ValidationException QtyError() =>
        new(nameof(EditMovementRequest.Quantity), "Количество должно быть больше нуля.");

    private static object Snapshot(InventoryMovement m) => new
    {
        m.MovementType, m.Quantity, m.CropId, m.FieldId, m.OccurredAt, m.Comment, m.WarehouseId, m.ChemicalId, m.IsDeleted,
    };
}
