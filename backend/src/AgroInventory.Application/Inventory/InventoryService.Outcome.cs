using AgroInventory.Application.Common;
using AgroInventory.Domain.Entities;
using AgroInventory.Domain.Enums;
using AgroInventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace AgroInventory.Application.Inventory;

public sealed partial class InventoryService
{
    /// <summary>Расчёт плана списания без изменения остатков (ТЗ §11.10) — для формы списания.</summary>
    public async Task<OutcomePreviewResponse> PreviewOutcomeAsync(OutcomeRequest request, CancellationToken ct = default)
    {
        if (request.QuantityLiters <= 0)
            throw new ValidationException(nameof(request.QuantityLiters), "Количество должно быть больше нуля.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);

        var stock = await LoadStockAsync(request.ChemicalId, request.WarehouseId, ct);
        var plan = StockEngine.PlanOutcome(BuildState(stock), request.QuantityLiters, ToSource(request.Source));
        var autoOpen = await GetAutoOpenAsync(ct);

        return BuildPreview(plan, autoOpen);
    }

    /// <summary>Фактическое списание (ТЗ §11).</summary>
    public async Task<OutcomeResultDto> OutcomeAsync(OutcomeRequest request, CancellationToken ct = default)
    {
        if (request.QuantityLiters <= 0)
            throw new ValidationException(nameof(request.QuantityLiters), "Количество должно быть больше нуля.");

        await EnsureActiveChemicalAsync(request.ChemicalId, ct);
        await EnsureWarehouseAsync(request.WarehouseId, ct);
        await EnsureCropAsync(request.CropId, ct); // культура обязательна при списании (ТЗ §11.1)
        if (request.FieldId is { } fieldId) await EnsureFieldAsync(fieldId, ct); // поле — необязательно

        var stock = await LoadStockAsync(request.ChemicalId, request.WarehouseId, ct);
        var plan = StockEngine.PlanOutcome(BuildState(stock), request.QuantityLiters, ToSource(request.Source));

        if (!plan.Sufficient)
            throw new ConflictException(
                $"Недостаточно остатка: доступно {plan.AvailableLiters:0.###} л из требуемых {plan.RequestedLiters:0.###} л.");

        var autoOpen = await GetAutoOpenAsync(ct);
        if (plan.WillOpenNewPackage && !autoOpen && !request.AllowOpenNewPackage)
            throw new ConflictException(
                "Для списания нужно вскрыть новую упаковку. Подтвердите вскрытие.");

        var now = _clock.GetUtcNow();
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            ChemicalId = request.ChemicalId,
            WarehouseId = request.WarehouseId,
            MovementType = MovementType.Outcome,
            QuantityLiters = plan.RequestedLiters,
            UnitType = UnitType.Liter,
            CropId = request.CropId,
            FieldId = request.FieldId,
            OccurredAt = request.OccurredAt ?? now,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedByUserId = SystemUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        ApplyOutcome(plan, stock, movement, now);

        _db.InventoryMovements.Add(movement);
        Recompute(stock);
        await _db.SaveChangesAsync(ct);

        return new OutcomeResultDto(plan.FulfilledLiters, stock.Balance?.TotalLiters ?? 0, movement.Id);
    }

    private void ApplyOutcome(OutcomePlan plan, LoadedStock stock, InventoryMovement movement, DateTimeOffset now)
    {
        foreach (var step in plan.Steps)
        {
            switch (step.SourceType)
            {
                case MovementSourceType.LooseLiters:
                    stock.Balance!.LooseLiters -= step.Liters;
                    break;

                case MovementSourceType.OpenedPackage:
                    var opened = stock.Opened.First(o => o.Id == step.SourceId);
                    opened.RemainingLiters -= step.Liters;
                    opened.UpdatedAt = now;
                    break;

                case MovementSourceType.PackageGroup:
                    var group = stock.Groups.First(g => g.Id == step.SourceId);
                    if (step.WholePackages > 0)
                    {
                        group.Quantity -= step.WholePackages; // целые упаковки (ТЗ §11.7)
                    }
                    if (step.OpenedRemainder > 0)
                    {
                        group.Quantity -= 1; // вскрываем одну (ТЗ §11.6)
                        var newOpened = new OpenedPackage
                        {
                            Id = Guid.NewGuid(),
                            ChemicalId = movement.ChemicalId,
                            WarehouseId = movement.WarehouseId,
                            UnitType = step.UnitType ?? group.UnitType,
                            InitialLiters = step.PackageVolumeLiters ?? group.PackageVolumeLiters,
                            RemainingLiters = step.OpenedRemainder,
                            OpenedAt = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                        };
                        _db.OpenedPackages.Add(newOpened);
                        stock.Opened.Add(newOpened);
                    }
                    group.UpdatedAt = now;
                    break;
            }

            movement.Details.Add(new InventoryMovementDetail
            {
                Id = Guid.NewGuid(),
                MovementId = movement.Id,
                SourceType = step.SourceType,
                SourceId = step.SourceId,
                UnitType = step.UnitType,
                PackageVolumeLiters = step.PackageVolumeLiters,
                QuantityLiters = step.Liters,
                PackagesQuantity = step.WholePackages > 0 ? step.WholePackages
                    : step.OpenedRemainder > 0 ? 1 : null,
            });
        }
    }

    private static OutcomeSource? ToSource(OutcomeSourceDto? dto) =>
        dto is null ? null : new OutcomeSource(dto.Type, dto.Id);

    private async Task<bool> GetAutoOpenAsync(CancellationToken ct) =>
        await _db.AppSettings.Select(s => s.AutoOpenPackages).FirstOrDefaultAsync(ct);

    private static OutcomePreviewResponse BuildPreview(OutcomePlan plan, bool autoOpen) =>
        new(
            plan.Sufficient,
            plan.RequestedLiters,
            plan.FulfilledLiters,
            plan.AvailableLiters,
            plan.WillOpenNewPackage,
            plan.TopUpUsed,
            RequiresOpenConfirmation: plan.WillOpenNewPackage && !autoOpen,
            plan.Steps.Select(s => new OutcomeStepDto(
                s.SourceType, s.SourceId, s.UnitType, s.PackageVolumeLiters,
                s.Liters, s.WholePackages, s.OpenedRemainder, s.OpensNewPackage)).ToList());
}
