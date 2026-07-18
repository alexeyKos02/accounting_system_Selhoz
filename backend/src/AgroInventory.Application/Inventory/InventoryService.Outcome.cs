using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    /// <summary>Проверка достаточности остатка без изменения данных — для формы списания.</summary>
    public async Task<OutcomePreviewResponse> PreviewOutcomeAsync(OutcomeRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException(nameof(request.Quantity), "Количество должно быть больше нуля.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        var balance = await LoadBalanceAsync(request.ChemicalId, request.WarehouseId, ct);
        var available = balance?.TotalQuantity ?? 0m;

        return new OutcomePreviewResponse(available >= request.Quantity, request.Quantity, available);
    }

    /// <summary>Фактическое списание (ТЗ §11).</summary>
    public async Task<OutcomeResultDto> OutcomeAsync(OutcomeRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException(nameof(request.Quantity), "Количество должно быть больше нуля.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await EnsureCropAsync(request.CropId, ct); // культура обязательна при списании (ТЗ §11.1)

        // Для списания нужен доступ и к складу, и к полю (ТЗ §6).
        await RequireWarehouseAccessAsync(request.WarehouseId, ct);
        if (request.FieldId is { } fieldId)
        {
            await EnsureFieldAsync(fieldId, ct); // поле — необязательно
            await RequireFieldAccessAsync(fieldId, ct);
        }

        var balance = await LoadBalanceAsync(request.ChemicalId, request.WarehouseId, ct);
        var available = balance?.TotalQuantity ?? 0m;

        if (available < request.Quantity)
            throw new ConflictException(
                $"Недостаточно остатка: доступно {available:0.###} из требуемых {request.Quantity:0.###}.");

        var now = _clock.GetUtcNow();
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ChemicalId = request.ChemicalId,
            WarehouseId = request.WarehouseId,
            MovementType = MovementType.Outcome,
            Quantity = request.Quantity,
            CropId = request.CropId,
            FieldId = request.FieldId,
            OccurredAt = request.OccurredAt ?? now,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedByUserId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        balance!.TotalQuantity -= request.Quantity;
        balance.UpdatedAt = now;

        _db.InventoryMovements.Add(movement);
        _audit.Log(AuditAction.Create, "InventoryMovement", movement.Id, null,
            new
            {
                movement.MovementType,
                movement.ChemicalId,
                movement.WarehouseId,
                movement.CropId,
                movement.FieldId,
                movement.Quantity,
                movement.OccurredAt
            });
        await _db.SaveChangesAsync(ct);

        return new OutcomeResultDto(request.Quantity, balance.TotalQuantity, movement.Id);
    }
}
