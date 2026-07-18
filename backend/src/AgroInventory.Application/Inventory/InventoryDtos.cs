using AgroInventory.Domain.Enums;

namespace AgroInventory.Application.Inventory;

// ---------- Приход (ТЗ §10) ----------

public sealed record IncomeRequest(
    Guid ChemicalId,
    Guid WarehouseId,
    decimal Quantity,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record IncomeResultDto(decimal TotalQuantity, Guid MovementId);

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
    DateTimeOffset? OccurredAt,
    string? Comment,
    IReadOnlyList<BulkIncomeLineRequest> Lines);

public sealed record BulkIncomeLineResultDto(
    Guid CompanyId,
    Guid ChemicalId,
    Guid WarehouseId,
    decimal TotalQuantity,
    Guid MovementId);

public sealed record BulkIncomeResultDto(IReadOnlyList<BulkIncomeLineResultDto> Lines);

// ---------- Списание (ТЗ §11) ----------

public sealed record OutcomeRequest(
    Guid ChemicalId,
    Guid WarehouseId,
    Guid CropId,
    decimal Quantity,
    DateTimeOffset? OccurredAt,
    string? Comment,
    Guid? FieldId = null);

public sealed record OutcomePreviewResponse(
    bool Sufficient,
    decimal Requested,
    decimal Available);

public sealed record OutcomeResultDto(decimal WrittenOffQuantity, decimal TotalQuantity, Guid MovementId);

// ---------- Перемещение между складами (этап I) ----------

public sealed record TransferRequest(
    Guid ChemicalId,
    Guid SourceWarehouseId,
    Guid TargetWarehouseId,
    decimal Quantity,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record TransferResultDto(
    Guid MovementId,
    decimal SourceTotalQuantity,
    decimal TargetTotalQuantity);

public sealed record TransferItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ChemicalId,
    string ChemicalName,
    Guid SourceWarehouseId,
    string SourceWarehouseNumber,
    Guid TargetWarehouseId,
    string TargetWarehouseNumber,
    decimal Quantity,
    string? Comment);

// ---------- Корректировка (ТЗ §13) ----------

public enum CorrectionMode { SetActual = 0, AdjustByDelta = 1 }

public sealed record CorrectionRequest(
    Guid ChemicalId,
    Guid WarehouseId,
    CorrectionMode Mode,
    decimal? ActualTotalQuantity,
    decimal? DeltaQuantity,
    bool Confirmed,
    DateTimeOffset? OccurredAt,
    string? Comment);

public sealed record CorrectionPreviewResponse(
    decimal CurrentTotal,
    decimal NewTotal,
    decimal Delta,
    bool IsBigChange,
    string? Message);

public sealed record CorrectionResultDto(decimal TotalQuantity, Guid MovementId);

// ---------- Инвентаризация склада (ТЗ §14) ----------

/// <summary>Строка ведомости инвентаризации: текущий остаток по химии на складе.</summary>
public sealed record InventoryCheckLineDto(
    Guid ChemicalId,
    string ChemicalName,
    MeasureUnit MeasureUnit,
    decimal CurrentTotalQuantity);

/// <summary>Ведомость инвентаризации по одному складу.</summary>
public sealed record InventoryCheckSheetDto(
    Guid WarehouseId,
    string WarehouseNumber,
    IReadOnlyList<InventoryCheckLineDto> Lines);

/// <summary>Введённый фактический остаток по одной химии.</summary>
public sealed record InventoryCheckEntry(Guid ChemicalId, decimal ActualTotalQuantity);

public sealed record InventoryCheckRequest(
    Guid WarehouseId,
    IReadOnlyList<InventoryCheckEntry> Entries,
    DateTimeOffset? OccurredAt,
    string? Comment);

/// <summary>Что произошло со строкой ведомости при применении.</summary>
public enum InventoryCheckOutcome { Unchanged = 0, Applied = 1 }

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
    IReadOnlyList<InventoryCheckLineResult> Lines);
