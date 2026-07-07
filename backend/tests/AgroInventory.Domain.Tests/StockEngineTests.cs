using AgroInventory.Domain.Enums;
using AgroInventory.Domain.Stock;
using Xunit;

namespace AgroInventory.Domain.Tests;

public class StockEngineTests
{
    private static StockState State(
        decimal loose,
        (decimal vol, int qty)[]? groups = null,
        (decimal init, decimal rem)[]? opened = null)
        => new(
            loose,
            (groups ?? Array.Empty<(decimal, int)>())
                .Select(g => new GroupState(Guid.NewGuid(), UnitType.Can, g.vol, g.qty)).ToList(),
            (opened ?? Array.Empty<(decimal, decimal)>())
                .Select(o => new OpenedState(Guid.NewGuid(), UnitType.Can, o.init, o.rem)).ToList());

    [Fact]
    public void Total_liters_sums_all_three_levels() // ТЗ §8.1: 15 + 1000 + 6 = 1021
    {
        var state = State(15m, new[] { (10m, 100) }, new[] { (10m, 6m) });
        Assert.Equal(1021m, state.TotalLiters);
    }

    [Fact]
    public void Auto_drains_opened_packages_first_by_smallest_remaining() // ТЗ §11.4
    {
        var state = State(20m, opened: new[] { (10m, 6m), (10m, 4m) });

        var plan = StockEngine.PlanOutcome(state, 5m);

        Assert.True(plan.Sufficient);
        // Первый шаг — упаковка с меньшим остатком (4 л).
        Assert.Equal(MovementSourceType.OpenedPackage, plan.Steps[0].SourceType);
        Assert.Equal(4m, plan.Steps[0].Liters);
        Assert.Equal(1m, plan.Steps[1].Liters);
        Assert.False(plan.WillOpenNewPackage);
    }

    [Fact]
    public void Partial_open_creates_opened_remainder() // ТЗ §11.6: списали 3 из банки 10 → остаток 7
    {
        var state = State(0m, new[] { (10m, 10) });

        var plan = StockEngine.PlanOutcome(state, 3m);

        var step = Assert.Single(plan.Steps);
        Assert.Equal(MovementSourceType.PackageGroup, step.SourceType);
        Assert.Equal(3m, step.Liters);
        Assert.Equal(7m, step.OpenedRemainder);
        Assert.True(step.OpensNewPackage);
        Assert.True(plan.WillOpenNewPackage);
    }

    [Fact]
    public void Whole_packages_do_not_open() // ТЗ §11.7
    {
        var state = State(0m, new[] { (10m, 10) });

        var plan = StockEngine.PlanOutcome(state, 20m);

        var step = Assert.Single(plan.Steps);
        Assert.Equal(2, step.WholePackages);
        Assert.Equal(20m, step.Liters);
        Assert.Equal(0m, step.OpenedRemainder);
        Assert.False(plan.WillOpenNewPackage);
    }

    [Fact]
    public void Full_drain_is_sufficient_and_opens_nothing()
    {
        var state = State(15m, new[] { (10m, 100) }, new[] { (10m, 6m) });

        var plan = StockEngine.PlanOutcome(state, 1021m);

        Assert.True(plan.Sufficient);
        Assert.Equal(1021m, plan.FulfilledLiters);
        Assert.False(plan.WillOpenNewPackage);
    }

    [Fact]
    public void Insufficient_stock_is_reported()
    {
        var state = State(10m);

        var plan = StockEngine.PlanOutcome(state, 15m);

        Assert.False(plan.Sufficient);
        Assert.Equal(10m, plan.FulfilledLiters);
        Assert.Equal(10m, plan.AvailableLiters);
    }

    [Fact]
    public void Manual_source_tops_up_automatically_when_insufficient() // ТЗ §11.8
    {
        var opened = new OpenedState(Guid.NewGuid(), UnitType.Can, 10m, 2m);
        var state = new StockState(10m, Array.Empty<GroupState>(), new[] { opened });

        var plan = StockEngine.PlanOutcome(state, 5m, new OutcomeSource(MovementSourceType.OpenedPackage, opened.Id));

        Assert.True(plan.Sufficient);
        Assert.True(plan.TopUpUsed);
        Assert.Equal(2m, plan.Steps[0].Liters); // из выбранной упаковки
        Assert.Equal(3m, plan.Steps[1].Liters); // добор из налива
        Assert.Equal(MovementSourceType.LooseLiters, plan.Steps[1].SourceType);
    }

    [Fact]
    public void Manual_package_group_opens_new_package()
    {
        var group = new GroupState(Guid.NewGuid(), UnitType.Can, 10m, 5);
        var state = new StockState(0m, new[] { group }, Array.Empty<OpenedState>());

        var plan = StockEngine.PlanOutcome(state, 4m, new OutcomeSource(MovementSourceType.PackageGroup, group.Id));

        Assert.True(plan.WillOpenNewPackage);
        Assert.Equal(6m, plan.Steps[0].OpenedRemainder);
    }
}
