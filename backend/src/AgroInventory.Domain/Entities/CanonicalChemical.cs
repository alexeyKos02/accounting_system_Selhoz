using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Entities;

/// <summary>
/// Канонический препарат — общий верхнеуровневый справочник (ТЗ §12). Таблица `canonical_chemicals`.
/// Глобальная сущность (без company_id): одна запись на реальный препарат, общая для всех хозяйств.
/// Ведёт её только SystemAdmin. Карточки химии хозяйств (inventory_items) опционально ссылаются на
/// каноническую запись через canonical_chemical_id — по ней и объединяется одинаковая химия в общем
/// режиме (ТЗ §17). Автообъединение по названию запрещено — привязку ставит человек.
/// «Каталог хозяйства» — это его карточки химии; отдельной таблицы каталога нет.
/// Набор полей повторяет карточку химии (ТЗ §7.1–7.3): тип, единица измерения, производитель,
/// культуры, комментарий — чтобы привязка/предзаполнение карточки из каталога были прямыми.
/// </summary>
public class CanonicalChemical
{
    public Guid Id { get; set; }

    /// <summary>Каноническое (эталонное) название препарата.</summary>
    public string CanonicalName { get; set; } = string.Empty;

    /// <summary>Тип средства (гербицид, фунгицид и т.д.). Необязателен.</summary>
    public ChemicalType? Type { get; set; }

    /// <summary>Единица измерения препарата (литры/кг). У каталога — свойство самого препарата.</summary>
    public MeasureUnit MeasureUnit { get; set; } = MeasureUnit.Liter;

    public string? Manufacturer { get; set; }
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Навигация
    public ICollection<CanonicalChemicalCrop> CanonicalChemicalCrops { get; set; } = new List<CanonicalChemicalCrop>();
}
