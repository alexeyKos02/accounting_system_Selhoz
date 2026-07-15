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

// ---------- Приход сразу в несколько хозяйств (этап F) ----------

public sealed record BulkIncomeCompanyOptionDto(
    Guid CompanyId,
    string CompanyName,
    Guid? ChemicalId,
    string? ChemicalName,
    IReadOnlyList<BulkIncomeWarehouseOptionDto> Warehouses,
    string? BlockedReason);

public sealed record BulkIncomeWarehouseOptionDto(Guid WarehouseId, string WarehouseNumber);

public sealed record BulkIncomeOptionsDto(IReadOnlyList<BulkIncomeCompanyOptionDto> Companies);

public sealed record BulkIncomeLineRequest(Guid CompanyId, Guid WarehouseId, decimal Quantity);

public sealed record BulkIncomeRequest(
    Guid CanonicalChemicalId,
    UnitType Unit,
    decimal? PackageVolumeLiters,
    DateTimeOffset? OccurredAt,
    string? Comment,
    IReadOnlyList<BulkIncomeLineRequest> Lines);

public sealed record BulkIncomeLineResultDto(
    Guid CompanyId,
    Guid ChemicalId,
    Guid WarehouseId,
    decimal TotalLiters,
    Guid MovementId);

public sealed record BulkIncomeResultDto(IReadOnlyList<BulkIncomeLineResultDto> Lines);

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
    string? Comment,
    Guid? FieldId = null);

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

// ---------- Перемещение между складами (этап I) ----------

public sealed record TransferRequest(
    Guid ChemicalId,
    Guid SourceWarehouseId,
    Guid TargetWarehouseId,
    decimal QuantityLiters,
    bool AllowOpenNewPackage,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record TransferResultDto(
    Guid MovementId,
    decimal SourceTotalLiters,
    decimal TargetTotalLiters);

public sealed record TransferItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ChemicalId,
    string ChemicalName,
    Guid SourceWarehouseId,
    string SourceWarehouseNumber,
    Guid TargetWarehouseId,
    string TargetWarehouseNumber,
    decimal QuantityLiters,
    string? Comment);

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

// ---------- Инвентаризация склада (ТЗ §14) ----------

/// <summary>Строка ведомости инвентаризации: текущий остаток по химии на складе.</summary>
public sealed record InventoryCheckLineDto(
    Guid ChemicalId,
    string ChemicalName,
    decimal CurrentTotalLiters,
    decimal LooseLiters,
    int FullPackages,
    int OpenedPackages);

/// <summary>Ведомость инвентаризации по одному складу.</summary>
public sealed record InventoryCheckSheetDto(
    Guid WarehouseId,
    string WarehouseNumber,
    IReadOnlyList<InventoryCheckLineDto> Lines);

/// <summary>Введённый фактический остаток по одной химии.</summary>
public sealed record InventoryCheckEntry(Guid ChemicalId, decimal ActualTotalLiters);

public sealed record InventoryCheckRequest(
    Guid WarehouseId,
    IReadOnlyList<InventoryCheckEntry> Entries,
    DateTimeOffset? OccurredAt,
    string? Comment);

/// <summary>Что произошло со строкой ведомости при применении.</summary>
public enum InventoryCheckOutcome { Unchanged = 0, Applied = 1, NeedsDetailed = 2 }

public sealed record InventoryCheckLineResult(
    Guid ChemicalId,
    string ChemicalName,
    decimal PreviousTotal,
    decimal NewTotal,
    decimal Delta,
    InventoryCheckOutcome Outcome);

public sealed record InventoryCheckResultDto(
    int AppliedCount,
    int UnchangedCount,
    int NeedsDetailedCount,
    IReadOnlyList<InventoryCheckLineResult> Lines);
