using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    public async Task<IReadOnlyList<TransferItemDto>> GetTransfersAsync(CancellationToken ct = default)
    {
        var access = await _companyContext.RequireAsync(ct);
        var rows = await _db.InventoryMovements.AsNoTracking()
            .Where(m => !m.IsDeleted && m.MovementType == MovementType.Transfer && m.TargetWarehouseId != null)
            .OrderByDescending(m => m.OccurredAt)
            .Select(m => new TransferItemDto(
                m.Id,
                m.OccurredAt,
                m.ChemicalId,
                m.Chemical.Name,
                m.WarehouseId,
                m.Warehouse.Number,
                m.TargetWarehouseId!.Value,
                m.TargetWarehouse!.Number,
                m.QuantityLiters,
                m.Comment))
            .ToListAsync(ct);

        return rows
            .Where(t => access.CanAccessWarehouse(t.SourceWarehouseId) && access.CanAccessWarehouse(t.TargetWarehouseId))
            .ToList();
    }

    public async Task<TransferResultDto> TransferAsync(TransferRequest request, CancellationToken ct = default)
    {
        if (request.QuantityLiters <= 0)
            throw new ValidationException(nameof(request.QuantityLiters), "Количество должно быть больше нуля.");
        if (request.SourceWarehouseId == request.TargetWarehouseId)
            throw new ValidationException(nameof(request.TargetWarehouseId), "Выберите другой склад назначения.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.SourceWarehouseId, ct);
        await EnsureWarehouseAsync(request.TargetWarehouseId, ct);
        await RequireWarehouseAccessAsync(request.SourceWarehouseId, ct);
        await RequireWarehouseAccessAsync(request.TargetWarehouseId, ct);

        var sourceStock = await LoadStockAsync(request.ChemicalId, request.SourceWarehouseId, ct);
        var plan = StockEngine.PlanOutcome(BuildState(sourceStock), request.QuantityLiters, null);
        if (!plan.Sufficient)
            throw new ConflictException(
                $"Недостаточно остатка: доступно {plan.AvailableLiters:0.###} л из требуемых {plan.RequestedLiters:0.###} л.");

        var autoOpen = await GetAutoOpenAsync(ct);
        if (plan.WillOpenNewPackage && !autoOpen && !request.AllowOpenNewPackage)
            throw new ConflictException("Для перемещения нужно вскрыть новую упаковку. Подтвердите вскрытие.");

        var targetStock = await LoadStockAsync(request.ChemicalId, request.TargetWarehouseId, ct, createBalance: true);
        var now = _clock.GetUtcNow();
        var occurredAt = request.OccurredAt ?? now;
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ChemicalId = request.ChemicalId,
            WarehouseId = request.SourceWarehouseId,
            TargetWarehouseId = request.TargetWarehouseId,
            MovementType = MovementType.Transfer,
            QuantityLiters = plan.RequestedLiters,
            UnitType = UnitType.Liter,
            OccurredAt = occurredAt,
            Comment = comment,
            CreatedByUserId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        ApplyOutcome(plan, sourceStock, movement, now);
        targetStock.Balance!.LooseLiters += plan.RequestedLiters;

        _db.InventoryMovements.Add(movement);
        Recompute(sourceStock);
        Recompute(targetStock);
        _audit.Log(AuditAction.Create, "InventoryMovement", movement.Id, null,
            new
            {
                movement.MovementType,
                movement.ChemicalId,
                SourceWarehouseId = movement.WarehouseId,
                movement.TargetWarehouseId,
                movement.QuantityLiters,
                movement.OccurredAt
            });
        await _db.SaveChangesAsync(ct);

        return new TransferResultDto(
            movement.Id,
            sourceStock.Balance?.TotalLiters ?? 0,
            targetStock.Balance?.TotalLiters ?? 0);
    }
}
