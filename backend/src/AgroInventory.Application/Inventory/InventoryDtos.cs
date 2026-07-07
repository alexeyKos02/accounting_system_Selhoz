using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Inventory;

// ---------- Приход (ТЗ §10) ----------

public sealed record IncomeRequest(
    Guid ChemicalId,
    Guid WarehouseId,
    UnitType Unit,
    decimal Quantity,
    decimal? PackageVolumeLiters,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record IncomeResultDto(decimal TotalLiters, Guid MovementId);

// ---------- Списание (ТЗ §11) ----------

public sealed record OutcomeSourceDto(MovementSourceType Type, Guid? Id);

public sealed record OutcomeRequest(
    Guid ChemicalId,
    Guid WarehouseId,
    Guid CropId,
    decimal QuantityLiters,
    OutcomeSourceDto? Source,
    bool AllowOpenNewPackage,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record OutcomeStepDto(
    MovementSourceType SourceType,
    Guid? SourceId,
    UnitType? UnitType,
    decimal? PackageVolumeLiters,
    decimal Liters,
    int WholePackages,
    decimal OpenedRemainder,
    bool OpensNewPackage);

public sealed record OutcomePreviewResponse(
    bool Sufficient,
    decimal Requested,
    decimal Fulfilled,
    decimal Available,
    bool WillOpenNewPackage,
    bool TopUpUsed,
    bool RequiresOpenConfirmation,
    IReadOnlyList<OutcomeStepDto> Steps);

public sealed record OutcomeResultDto(decimal WrittenOffLiters, decimal TotalLiters, Guid MovementId);

// ---------- Корректировка (ТЗ §13) ----------

public enum CorrectionMode { SetActual = 0, AdjustByDelta = 1, DetailedInventory = 2 }

public sealed record DetailedGroupDto(UnitType UnitType, decimal PackageVolumeLiters, int Quantity);

public sealed record DetailedOpenedDto(UnitType UnitType, decimal InitialLiters, decimal RemainingLiters);

public sealed record DetailedInventoryDto(
    decimal LooseLiters,
    IReadOnlyList<DetailedGroupDto> Groups,
    IReadOnlyList<DetailedOpenedDto> Opened);

public sealed record CorrectionRequest(
    Guid ChemicalId,
    Guid WarehouseId,
    CorrectionMode Mode,
    decimal? ActualTotalLiters,
    decimal? DeltaLiters,
    DetailedInventoryDto? Detailed,
    bool Confirmed,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record CorrectionPreviewResponse(
    decimal CurrentTotal,
    decimal NewTotal,
    decimal Delta,
    bool IsBigChange,
    bool RequiresDetailed,
    string? Message);

public sealed record CorrectionResultDto(decimal TotalLiters, Guid MovementId);
