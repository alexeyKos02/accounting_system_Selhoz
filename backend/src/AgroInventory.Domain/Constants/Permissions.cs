namespace AgroInventory.Domain.Constants;

/// <summary>
/// Коды прав доступа (ТЗ §5). Роли работают через права; итоговая проверка — всегда на backend.
/// Права разрешаются из роли статической картой <see cref="RolePermissionMap"/>.
/// </summary>
public static class Permissions
{
    public const string CompanyView = "company.view";
    public const string CompanyManage = "company.manage";

    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";

    public const string WarehousesView = "warehouses.view";
    public const string WarehousesManage = "warehouses.manage";

    public const string FieldsView = "fields.view";
    public const string FieldsManage = "fields.manage";

    public const string InventoryView = "inventory.view";
    public const string InventoryManage = "inventory.manage";

    public const string ReceiptsView = "receipts.view";
    public const string ReceiptsCreate = "receipts.create";

    public const string WriteoffsView = "writeoffs.view";
    public const string WriteoffsCreate = "writeoffs.create";

    public const string TransfersView = "transfers.view";
    public const string TransfersCreate = "transfers.create";

    public const string AdjustmentsCreate = "adjustments.create";

    public const string TreatmentsView = "treatments.view";
    public const string TreatmentsManage = "treatments.manage";

    public const string HarvestsView = "harvests.view";
    public const string HarvestsManage = "harvests.manage";

    public const string ReportsView = "reports.view";
    public const string AuditView = "audit.view";
    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyList<string> All = new[]
    {
        CompanyView, CompanyManage,
        UsersView, UsersManage,
        WarehousesView, WarehousesManage,
        FieldsView, FieldsManage,
        InventoryView, InventoryManage,
        ReceiptsView, ReceiptsCreate,
        WriteoffsView, WriteoffsCreate,
        TransfersView, TransfersCreate,
        AdjustmentsCreate,
        TreatmentsView, TreatmentsManage,
        HarvestsView, HarvestsManage,
        ReportsView, AuditView, SettingsManage,
    };
}
