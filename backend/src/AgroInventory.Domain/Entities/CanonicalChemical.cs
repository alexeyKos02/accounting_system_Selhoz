namespace AgroInventory.Domain.Entities;

/// <summary>
/// Канонический препарат — общий верхнеуровневый справочник (ТЗ §12). Таблица `canonical_chemicals`.
/// Глобальная сущность (без company_id): одна запись на реальный препарат, общая для всех хозяйств.
/// Ведёт её только SystemAdmin. Карточки химии хозяйств (inventory_items) опционально ссылаются на
/// каноническую запись через canonical_chemical_id — по ней и объединяется одинаковая химия в общем
/// режиме (ТЗ §17). Автообъединение по названию запрещено — привязку ставит человек.
/// «Каталог хозяйства» — это его карточки химии; отдельной таблицы каталога нет.
/// </summary>
public class CanonicalChemical
{
    public Guid Id { get; set; }

    /// <summary>Каноническое (эталонное) название препарата.</summary>
    public string CanonicalName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }
    public string? ActiveIngredient { get; set; }
    public string? Concentration { get; set; }
    public string? Formulation { get; set; }
    public string? RegistrationNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
