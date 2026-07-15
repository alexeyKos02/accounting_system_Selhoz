using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Domain.Stock;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    private const string StockEntityType = "ChemicalStockBalance";

    /// <summary>Предпросмотр корректировки: было/станет/разница + флаги (ТЗ §13.6).</summary>
    public async Task<CorrectionPreviewResponse> PreviewCorrectionAsync(
        CorrectionRequest request, CancellationToken ct = default)
    {
        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        var stock = await LoadStockAsync(request.ChemicalId, request.WarehouseId, ct);
        var state = BuildState(stock);
        var (newTotal, requiresDetailed, message) = Resolve(request, state);
        var delta = newTotal - state.TotalLiters;

        return new CorrectionPreviewResponse(
            state.TotalLiters, newTotal, delta, IsBigChange(state.TotalLiters, newTotal), requiresDetailed, message);
    }

    /// <summary>Применение корректировки (ТЗ §13). Пишется в движения и в audit_log (ТЗ §13.5).</summary>
    public async Task<CorrectionResultDto> CorrectionAsync(CorrectionRequest request, CancellationToken ct = default)
    {
        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await RequireWarehouseAccessAsync(request.WarehouseId, ct); // область доступа (ТЗ §6)

        var stock = await LoadStockAsync(request.ChemicalId, request.WarehouseId, ct, createBalance: true);
        var state = BuildState(stock);
        var (newTotal, requiresDetailed, message) = Resolve(request, state);

        if (requiresDetailed)
            throw new ConflictException(message ?? "Требуется детальная инвентаризация.");

        // Большое изменение требует подтверждения (ТЗ §13.6).
        if (IsBigChange(state.TotalLiters, newTotal) && !request.Confirmed)
            throw new ConflictException("Изменение больше 20%. Подтвердите корректировку.");

        var now = _clock.GetUtcNow();
        var oldTotal = state.TotalLiters;
        var delta = newTotal - oldTotal;

        switch (request.Mode)
        {
            case CorrectionMode.SetActual:
            case CorrectionMode.AdjustByDelta:
                // Изменение применяем к наливному остатку (ТЗ §13.2, §13.4).
                stock.Balance!.LooseLiters += delta;
                break;

            case CorrectionMode.DetailedInventory:
                ApplyDetailed(request.Detailed!, stock, now);
                break;
        }

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ChemicalId = request.ChemicalId,
            WarehouseId = request.WarehouseId,
            MovementType = MovementType.Correction,
            QuantityLiters = delta,
            UnitType = UnitType.Liter,
            OccurredAt = request.OccurredAt ?? now,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedByUserId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.InventoryMovements.Add(movement);

        Recompute(stock);

        _audit.Log(AuditAction.Update, StockEntityType, stock.Balance!.Id,
            new { TotalLiters = oldTotal },
            new { TotalLiters = stock.Balance.TotalLiters, request.Mode, MovementId = movement.Id });

        await _db.SaveChangesAsync(ct);

        return new CorrectionResultDto(stock.Balance.TotalLiters, movement.Id);
    }

    /// <summary>Считает целевой total и признак необходимости детальной инвентаризации.</summary>
    private (decimal NewTotal, bool RequiresDetailed, string? Message) Resolve(CorrectionRequest request, StockState state)
    {
        switch (request.Mode)
        {
            case CorrectionMode.SetActual:
            {
                if (request.ActualTotalLiters is not { } actual || actual < 0)
                    throw new ValidationException(nameof(request.ActualTotalLiters), "Укажите фактический остаток.");
                var delta = actual - state.TotalLiters;
                // Отрицательную разницу нельзя безопасно применить к наливу — нужна детальная (ТЗ §13.2).
                if (delta < 0 && -delta > state.LooseLiters)
                    return (actual, true,
                        "Разница не помещается в наливной остаток — перейдите в детальную инвентаризацию.");
                return (actual, false, null);
            }

            case CorrectionMode.AdjustByDelta:
            {
                if (request.DeltaLiters is not { } delta || delta == 0)
                    throw new ValidationException(nameof(request.DeltaLiters), "Укажите ненулевую разницу.");
                if (delta < 0 && -delta > state.LooseLiters)
                    return (state.TotalLiters + delta, true,
                        "Списание больше наливного остатка — перейдите в детальную инвентаризацию.");
                return (state.TotalLiters + delta, false, null);
            }

            case CorrectionMode.DetailedInventory:
            {
                if (request.Detailed is not { } d)
                    throw new ValidationException(nameof(request.Detailed), "Не заданы данные инвентаризации.");
                var total = ValidateDetailed(d);
                return (total, false, null);
            }

            default:
                throw new ValidationException(nameof(request.Mode), "Неизвестный режим корректировки.");
        }
    }

    private static decimal ValidateDetailed(DetailedInventoryDto d)
    {
        if (d.LooseLiters < 0)
            throw new ValidationException(nameof(d.LooseLiters), "Наливной остаток не может быть отрицательным.");

        var total = d.LooseLiters;
        foreach (var g in d.Groups)
        {
            if (g.PackageVolumeLiters <= 0 || g.Quantity < 0)
                throw new ValidationException(nameof(d.Groups), "Некорректные данные по полным упаковкам.");
            total += g.PackageVolumeLiters * g.Quantity;
        }
        foreach (var o in d.Opened)
        {
            if (o.InitialLiters <= 0 || o.RemainingLiters <= 0 || o.RemainingLiters > o.InitialLiters)
                throw new ValidationException(nameof(d.Opened), "Некорректные данные по вскрытым упаковкам.");
            total += o.RemainingLiters;
        }
        return total;
    }

    private void ApplyDetailed(DetailedInventoryDto d, LoadedStock stock, DateTimeOffset now)
    {
        // Полная пересборка остатков склада (ТЗ §13.3).
        _db.PackageGroups.RemoveRange(stock.Groups);
        _db.OpenedPackages.RemoveRange(stock.Opened);
        stock.Groups.Clear();
        stock.Opened.Clear();

        stock.Balance!.LooseLiters = d.LooseLiters;

        foreach (var g in d.Groups.Where(g => g.Quantity > 0))
        {
            var group = new PackageGroup
            {
                Id = Guid.NewGuid(),
                CompanyId = stock.Balance.CompanyId,
                ChemicalId = stock.Balance.ChemicalId,
                WarehouseId = stock.Balance.WarehouseId,
                UnitType = g.UnitType,
                PackageVolumeLiters = g.PackageVolumeLiters,
                Quantity = g.Quantity,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.PackageGroups.Add(group);
            stock.Groups.Add(group);
        }

        foreach (var o in d.Opened)
        {
            var opened = new OpenedPackage
            {
                Id = Guid.NewGuid(),
                CompanyId = stock.Balance.CompanyId,
                ChemicalId = stock.Balance.ChemicalId,
                WarehouseId = stock.Balance.WarehouseId,
                UnitType = o.UnitType,
                InitialLiters = o.InitialLiters,
                RemainingLiters = o.RemainingLiters,
                OpenedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.OpenedPackages.Add(opened);
            stock.Opened.Add(opened);
        }
    }

    /// <summary>Изменение больше 20% (или любое положительное при нулевом остатке) — большое (ТЗ §13.6).</summary>
    private static bool IsBigChange(decimal currentTotal, decimal newTotal)
    {
        if (currentTotal == 0) return newTotal > 0;
        return Math.Abs(newTotal - currentTotal) > 0.2m * currentTotal;
    }
}
