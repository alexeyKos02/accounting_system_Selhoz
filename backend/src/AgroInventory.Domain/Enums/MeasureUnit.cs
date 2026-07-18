namespace AgroInventory.Domain.Enums;

/// <summary>
/// Единица измерения химии: жидкая — в литрах, сухая — в килограммах.
/// Задаётся при создании карточки и не меняется. Учёт ведётся одним числом в этой единице.
/// </summary>
public enum MeasureUnit
{
    Liter = 1,
    Kilogram = 2,
}
