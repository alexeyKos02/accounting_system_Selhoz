using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    private const string StockEntityType = "ChemicalStockBalance";

    /// <summary>Предпросмотр корректировки: было/станет/разница + флаг большого изменения (ТЗ §13.6).</summary>
    public async Task<CorrectionPreviewResponse> PreviewCorrectionAsync(
        CorrectionRequest request, CancellationToken ct = default)
    {
        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        var balance = await LoadBalanceAsync(request.ChemicalId, request.WarehouseId, ct);
        var current = balance?.TotalQuantity ?? 0m;
        var newTotal = Resolve(request, current);
        var delta = newTotal - current;

        return new CorrectionPreviewResponse(
            current, newTotal, delta, IsBigChange(current, newTotal), null);
    }

    /// <summary>Применение корректировки (ТЗ §13). Пишется в движения и в audit_log (ТЗ §13.5).</summary>
    public async Task<CorrectionResultDto> CorrectionAsync(CorrectionRequest request, CancellationToken ct = default)
    {
        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        var balance = await LoadBalanceAsync(request.ChemicalId, request.WarehouseId, ct, createBalance: true);
        var oldTotal = balance!.TotalQuantity;
        var newTotal = Resolve(request, oldTotal);

        if (newTotal < 0)
            throw new ValidationException(nameof(request.DeltaQuantity), "Остаток не может стать отрицательным.");

        // Большое изменение требует подтверждения (ТЗ §13.6).
        if (IsBigChange(oldTotal, newTotal) && !request.Confirmed)
            throw new ConflictException("Изменение больше 20%. Подтвердите корректировку.");

        var now = _clock.GetUtcNow();
        var delta = newTotal - oldTotal;

        balance.TotalQuantity = newTotal;
        balance.UpdatedAt = now;

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ChemicalId = request.ChemicalId,
            WarehouseId = request.WarehouseId,
            MovementType = MovementType.Correction,
            Quantity = delta,
            OccurredAt = request.OccurredAt ?? now,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedByUserId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.InventoryMovements.Add(movement);

        _audit.Log(AuditAction.Update, StockEntityType, balance.Id,
            new { TotalQuantity = oldTotal },
            new { TotalQuantity = balance.TotalQuantity, request.Mode, MovementId = movement.Id });

        await _db.SaveChangesAsync(ct);

        return new CorrectionResultDto(balance.TotalQuantity, movement.Id);
    }

    /// <summary>Считает целевой остаток по режиму корректировки.</summary>
    private static decimal Resolve(CorrectionRequest request, decimal current)
    {
        switch (request.Mode)
        {
            case CorrectionMode.SetActual:
            {
                if (request.ActualTotalQuantity is not { } actual || actual < 0)
                    throw new ValidationException(nameof(request.ActualTotalQuantity), "Укажите фактический остаток.");
                return actual;
            }

            case CorrectionMode.AdjustByDelta:
            {
                if (request.DeltaQuantity is not { } delta || delta == 0)
                    throw new ValidationException(nameof(request.DeltaQuantity), "Укажите ненулевую разницу.");
                return current + delta;
            }

            default:
                throw new ValidationException(nameof(request.Mode), "Неизвестный режим корректировки.");
        }
    }

    /// <summary>Изменение больше 20% (или любое положительное при нулевом остатке) — большое (ТЗ §13.6).</summary>
    private static bool IsBigChange(decimal currentTotal, decimal newTotal)
    {
        if (currentTotal == 0) return newTotal > 0;
        return Math.Abs(newTotal - currentTotal) > 0.2m * currentTotal;
    }
}
