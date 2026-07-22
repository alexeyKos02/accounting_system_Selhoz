using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Constants;

/// <summary>
/// Статическая карта «роль → права» (ТЗ §4, §5). Роли фиксированы для v1, редактор ролей не делаем.
/// Ограничения CompanyAdmin (нельзя удалить хозяйство, сменить владельца, назначить SystemAdmin,
/// удалить последнего Owner) — это бизнес-правила, а не отдельные права; проверяются в коде операций.
/// </summary>
public static class RolePermissionMap
{
    private static readonly IReadOnlyList<string> All = Permissions.All;

    /// <summary>Права роли. SystemAdmin/Owner/CompanyAdmin — полный набор; остальные — урезанный.</summary>
    public static readonly IReadOnlyDictionary<AppRole, IReadOnlySet<string>> Map =
        new Dictionary<AppRole, IReadOnlySet<string>>
        {
            [AppRole.SystemAdmin] = new HashSet<string>(All),
            [AppRole.Owner] = new HashSet<string>(All),
            [AppRole.CompanyAdmin] = new HashSet<string>(All),

            [AppRole.Manager] = new HashSet<string>
            {
                Permissions.CompanyView,
                Permissions.WarehousesView,
                Permissions.FieldsView, Permissions.FieldsManage,
                Permissions.InventoryView, Permissions.InventoryManage,
                Permissions.ReceiptsView, Permissions.ReceiptsCreate,
                Permissions.WriteoffsView, Permissions.WriteoffsCreate,
                Permissions.TransfersView, Permissions.TransfersCreate,
                Permissions.TreatmentsView, Permissions.TreatmentsManage,
                Permissions.HarvestsView, Permissions.HarvestsManage,
                Permissions.ReportsView,
            },

            // Агроном — «полевая» половина Manager: поля/обработки/урожай. Обработка сама
            // списывает химию со склада (TreatmentsManage достаточно), но приход/перемещения не ведёт.
            [AppRole.Agronomist] = new HashSet<string>
            {
                Permissions.CompanyView,
                Permissions.WarehousesView,
                Permissions.FieldsView, Permissions.FieldsManage,
                Permissions.InventoryView,
                Permissions.WriteoffsView,
                Permissions.TreatmentsView, Permissions.TreatmentsManage,
                Permissions.HarvestsView, Permissions.HarvestsManage,
                Permissions.ReportsView,
            },

            [AppRole.Storekeeper] = new HashSet<string>
            {
                Permissions.CompanyView,
                Permissions.WarehousesView,
                Permissions.InventoryView,
                Permissions.ReceiptsView, Permissions.ReceiptsCreate,
                Permissions.WriteoffsView, Permissions.WriteoffsCreate,
                Permissions.TransfersView, Permissions.TransfersCreate,
                Permissions.AdjustmentsCreate,
            },

            // Учётчик/ревизор — только чтение по всему + отчёты и аудит. Ничего не меняет.
            [AppRole.Auditor] = new HashSet<string>
            {
                Permissions.CompanyView,
                Permissions.WarehousesView,
                Permissions.FieldsView,
                Permissions.InventoryView,
                Permissions.ReceiptsView,
                Permissions.WriteoffsView,
                Permissions.TransfersView,
                Permissions.TreatmentsView,
                Permissions.HarvestsView,
                Permissions.ReportsView,
                Permissions.AuditView,
            },

            [AppRole.Viewer] = new HashSet<string>
            {
                Permissions.CompanyView,
                Permissions.WarehousesView,
                Permissions.FieldsView,
                Permissions.InventoryView,
                Permissions.ReceiptsView,
                Permissions.WriteoffsView,
                Permissions.TransfersView,
                Permissions.TreatmentsView,
                Permissions.HarvestsView,
                Permissions.ReportsView,
            },
        };

    /// <summary>Есть ли у роли право.</summary>
    public static bool Has(AppRole role, string permission) =>
        Map.TryGetValue(role, out var perms) && perms.Contains(permission);
}
