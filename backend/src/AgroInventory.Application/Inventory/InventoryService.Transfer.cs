using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
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
                m.Quantity,
                m.Comment))
            .ToListAsync(ct);

        return rows
            .Where(t => access.CanAccessWarehouse(t.SourceWarehouseId) && access.CanAccessWarehouse(t.TargetWarehouseId))
            .ToList();
    }

    public async Task<TransferResultDto> TransferAsync(TransferRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException(nameof(request.Quantity), "Количество должно быть больше нуля.");
        if (request.SourceWarehouseId == request.TargetWarehouseId)
            throw new ValidationException(nameof(request.TargetWarehouseId), "Выберите другой склад назначения.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.SourceWarehouseId, ct);
        await EnsureWarehouseAsync(request.TargetWarehouseId, ct);
        await RequireWarehouseAccessAsync(request.SourceWarehouseId, ct);
        await RequireWarehouseAccessAsync(request.TargetWarehouseId, ct);

        var sourceBalance = await LoadBalanceAsync(request.ChemicalId, request.SourceWarehouseId, ct);
        var available = sourceBalance?.TotalQuantity ?? 0m;
        if (available < request.Quantity)
            throw new ConflictException(
                $"Недостаточно остатка: доступно {available:0.###} из требуемых {request.Quantity:0.###}.");

        var targetBalance = await LoadBalanceAsync(request.ChemicalId, request.TargetWarehouseId, ct, createBalance: true);
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
            Quantity = request.Quantity,
            OccurredAt = occurredAt,
            Comment = comment,
            CreatedByUserId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        sourceBalance!.TotalQuantity -= request.Quantity;
        sourceBalance.UpdatedAt = now;
        targetBalance!.TotalQuantity += request.Quantity;
        targetBalance.UpdatedAt = now;

        _db.InventoryMovements.Add(movement);
        _audit.Log(AuditAction.Create, "InventoryMovement", movement.Id, null,
            new
            {
                movement.MovementType,
                movement.ChemicalId,
                SourceWarehouseId = movement.WarehouseId,
                movement.TargetWarehouseId,
                movement.Quantity,
                movement.OccurredAt
            });
        await _db.SaveChangesAsync(ct);

        return new TransferResultDto(
            movement.Id,
            sourceBalance.TotalQuantity,
            targetBalance.TotalQuantity);
    }
}
