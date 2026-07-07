using AgroInventory.Domain.Enums;

namespace AgroInventory.Domain.Stock;

/// <summary>
/// Чистая доменная логика остатков (ТЗ §11). Строит план списания без изменения данных —
/// один и тот же расчёт используется и для preview, и для фактического списания.
/// </summary>
public static class StockEngine
{
    /// <summary>
    /// Планирует списание litersNeeded из остатка. Если задан ручной источник — сначала
    /// берём из него, затем при нехватке добираем автоматически (ТЗ §11.4, §11.8).
    /// </summary>
    public static OutcomePlan PlanOutcome(StockState state, decimal litersNeeded, OutcomeSource? manual = null)
    {
        if (litersNeeded <= 0)
            throw new ArgumentException("Количество для списания должно быть больше нуля.", nameof(litersNeeded));

        var loose = state.LooseLiters;
        var groups = state.Groups.Select(g => new MutableGroup(g)).ToList();
        var opened = state.Opened.Select(o => new MutableOpened(o)).ToList();
        var steps = new List<OutcomeStep>();
        var need = litersNeeded;

        // 1. Ручной источник (ТЗ §11.3).
        if (manual is not null)
            need = DrawManual(manual, ref loose, groups, opened, steps, need);

        var topUpUsed = manual is not null && need > 0;

        // 2. Автоматический добор (ТЗ §11.4): сначала почти пустые вскрытые, затем налив, затем полные.
        foreach (var o in opened.Where(o => o.Remaining > 0).OrderBy(o => o.Remaining).ToList())
        {
            if (need <= 0) break;
            need = DrawOpened(o, steps, need);
        }

        need = DrawLoose(ref loose, steps, need);

        foreach (var g in groups.Where(g => g.Quantity > 0).OrderBy(g => g.Volume).ToList())
        {
            if (need <= 0) break;
            need = DrawGroup(g, steps, need);
        }

        return new OutcomePlan
        {
            RequestedLiters = litersNeeded,
            FulfilledLiters = litersNeeded - Math.Max(need, 0),
            AvailableLiters = state.TotalLiters,
            Steps = steps,
            TopUpUsed = topUpUsed,
        };
    }

    private static decimal DrawManual(
        OutcomeSource manual, ref decimal loose, List<MutableGroup> groups, List<MutableOpened> opened,
        List<OutcomeStep> steps, decimal need)
    {
        switch (manual.Type)
        {
            case MovementSourceType.LooseLiters:
                return DrawLoose(ref loose, steps, need);

            case MovementSourceType.OpenedPackage:
                var o = opened.FirstOrDefault(x => x.Id == manual.Id)
                        ?? throw new ArgumentException("Выбранная вскрытая упаковка не найдена.");
                return DrawOpened(o, steps, need);

            case MovementSourceType.PackageGroup:
                var g = groups.FirstOrDefault(x => x.Id == manual.Id)
                        ?? throw new ArgumentException("Выбранная группа упаковок не найдена.");
                return DrawGroup(g, steps, need);

            default:
                return need;
        }
    }

    private static decimal DrawLoose(ref decimal loose, List<OutcomeStep> steps, decimal need)
    {
        if (need <= 0 || loose <= 0) return need;
        var take = Math.Min(loose, need);
        loose -= take;
        steps.Add(new OutcomeStep { SourceType = MovementSourceType.LooseLiters, UnitType = UnitType.Liter, Liters = take });
        return need - take;
    }

    private static decimal DrawOpened(MutableOpened o, List<OutcomeStep> steps, decimal need)
    {
        if (need <= 0 || o.Remaining <= 0) return need;
        var take = Math.Min(o.Remaining, need);
        o.Remaining -= take;
        steps.Add(new OutcomeStep
        {
            SourceType = MovementSourceType.OpenedPackage,
            SourceId = o.Id,
            UnitType = o.UnitType,
            PackageVolumeLiters = o.Initial,
            Liters = take,
        });
        return need - take;
    }

    private static decimal DrawGroup(MutableGroup g, List<OutcomeStep> steps, decimal need)
    {
        if (need <= 0 || g.Quantity <= 0) return need;

        // Целые упаковки, пока хватает потребности на полную (ТЗ §11.7).
        var whole = 0;
        while (need >= g.Volume && g.Quantity > 0)
        {
            g.Quantity--;
            need -= g.Volume;
            whole++;
        }

        if (whole > 0)
            steps.Add(new OutcomeStep
            {
                SourceType = MovementSourceType.PackageGroup,
                SourceId = g.Id,
                UnitType = g.UnitType,
                PackageVolumeLiters = g.Volume,
                Liters = whole * g.Volume,
                WholePackages = whole,
            });

        // Частичное вскрытие одной упаковки: остаток становится вскрытой упаковкой (ТЗ §11.6).
        if (need > 0 && g.Quantity > 0)
        {
            g.Quantity--;
            var take = need;
            var remainder = g.Volume - need;
            need = 0;
            steps.Add(new OutcomeStep
            {
                SourceType = MovementSourceType.PackageGroup,
                SourceId = g.Id,
                UnitType = g.UnitType,
                PackageVolumeLiters = g.Volume,
                Liters = take,
                OpenedRemainder = remainder,
            });
        }

        return need;
    }

    private sealed class MutableGroup(GroupState g)
    {
        public Guid Id { get; } = g.Id;
        public UnitType UnitType { get; } = g.UnitType;
        public decimal Volume { get; } = g.Volume;
        public int Quantity { get; set; } = g.Quantity;
    }

    private sealed class MutableOpened(OpenedState o)
    {
        public Guid Id { get; } = o.Id;
        public UnitType UnitType { get; } = o.UnitType;
        public decimal Initial { get; } = o.Initial;
        public decimal Remaining { get; set; } = o.Remaining;
    }
}
