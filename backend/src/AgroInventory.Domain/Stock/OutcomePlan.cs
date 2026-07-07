using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Stock;

/// <summary>
/// Один шаг плана списания: сколько литров и из какого источника (ТЗ §9.2, §11.10).
/// </summary>
public sealed record OutcomeStep
{
    public required MovementSourceType SourceType { get; init; }

    /// <summary>Id вскрытой упаковки или группы полных упаковок (для loose — null).</summary>
    public Guid? SourceId { get; init; }

    public UnitType? UnitType { get; init; }
    public decimal? PackageVolumeLiters { get; init; }

    /// <summary>Списано литров на этом шаге.</summary>
    public required decimal Liters { get; init; }

    /// <summary>Сколько целых упаковок использовано целиком (ТЗ §11.7).</summary>
    public int WholePackages { get; init; }

    /// <summary>Остаток, который станет вскрытой упаковкой при частичном вскрытии (ТЗ §11.6).</summary>
    public decimal OpenedRemainder { get; init; }

    /// <summary>Шаг вскрывает новую упаковку (создаёт opened_package) — требует подтверждения (ТЗ §11.9).</summary>
    public bool OpensNewPackage => SourceType == MovementSourceType.PackageGroup && OpenedRemainder > 0;
}

/// <summary>
/// План списания (ТЗ §11.10). Строится без изменения остатков — годится для preview.
/// </summary>
public sealed record OutcomePlan
{
    public required decimal RequestedLiters { get; init; }
    public required decimal FulfilledLiters { get; init; }
    public required decimal AvailableLiters { get; init; }
    public required IReadOnlyList<OutcomeStep> Steps { get; init; }

    /// <summary>Хватает ли остатка на всё количество.</summary>
    public bool Sufficient => FulfilledLiters >= RequestedLiters;

    /// <summary>План вскрывает хотя бы одну новую упаковку (ТЗ §11.9).</summary>
    public bool WillOpenNewPackage => Steps.Any(s => s.OpensNewPackage);

    /// <summary>Пришлось добирать автоматически поверх выбранного вручную источника (ТЗ §11.8).</summary>
    public bool TopUpUsed { get; init; }
}
