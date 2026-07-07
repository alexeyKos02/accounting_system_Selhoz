using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Stock;

/// <summary>
/// Неизменяемый снимок остатка химии на складе для расчётов движком (ТЗ §8).
/// Движок работает с этим снимком, не завися от EF и БД.
/// </summary>
public sealed record StockState(
    decimal LooseLiters,
    IReadOnlyList<GroupState> Groups,
    IReadOnlyList<OpenedState> Opened)
{
    /// <summary>Полный остаток в литрах: наливом + полные упаковки + вскрытые (ТЗ §8.1).</summary>
    public decimal TotalLiters =>
        LooseLiters
        + Groups.Sum(g => g.Volume * g.Quantity)
        + Opened.Sum(o => o.Remaining);
}

/// <summary>Группа полных упаковок.</summary>
public sealed record GroupState(Guid Id, UnitType UnitType, decimal Volume, int Quantity);

/// <summary>Вскрытая упаковка.</summary>
public sealed record OpenedState(Guid Id, UnitType UnitType, decimal Initial, decimal Remaining);

/// <summary>Ручной выбор источника списания (ТЗ §11.3).</summary>
public sealed record OutcomeSource(MovementSourceType Type, Guid? Id);
