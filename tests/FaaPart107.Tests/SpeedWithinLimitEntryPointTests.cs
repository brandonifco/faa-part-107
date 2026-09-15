using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>speed-within-limit</c>, § 107.51(a), resolved through <see cref="EntryPoints.SpeedWithinLimit"/>
/// only: at, just below and just above 87 knots, a groundspeed between 100 mph and 87 knots, and the
/// waiver gate in both directions (the entry's note, and rules-factory decision 0021).
/// </summary>
public class SpeedWithinLimitEntryPointTests
{
    private const string Caller = nameof(SpeedWithinLimitEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    private static Resolution<object> Resolve(Groundspeed groundspeed, WaiverStatement waiver) =>
        EntryPoints.SpeedWithinLimit.Resolve(new SpeedWithinLimitRequest { Groundspeed = groundspeed, Waiver = waiver });

    private static GroundspeedFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<GroundspeedFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    public static TheoryData<decimal, SpeedUnit> WithinBothFigures => new()
    {
        { 0m, SpeedUnit.Knots },
        { 50m, SpeedUnit.Knots },
        { 99.99m, SpeedUnit.MilesPerHour },
        { 100m, SpeedUnit.MilesPerHour },

        // Just below 87 knots and not beyond 100 mph: 86.89 knots is 160.92028 km/h, 100 mph 160.9344.
        { 86.89m, SpeedUnit.Knots },
    };

    [Theory]
    [MemberData(nameof(WithinBothFigures))]
    public void A_groundspeed_exceeding_neither_figure_resolves_within_the_limit_citing_107_51_a(decimal value, SpeedUnit unit)
    {
        var finding = Finding(Resolve(new Groundspeed(value, unit), NoWaiver));

        Assert.True(finding.WithinLimit);
        Assert.Equal(new Groundspeed(value, unit), finding.Groundspeed);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.51(a)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.SpeedWithinLimit.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    public static TheoryData<decimal, SpeedUnit> BeyondBothFigures => new()
    {
        { 87.01m, SpeedUnit.Knots },
        { 100.12m, SpeedUnit.MilesPerHour },
        { 400m, SpeedUnit.Knots },
    };

    [Theory]
    [MemberData(nameof(BeyondBothFigures))]
    public void A_groundspeed_exceeding_both_figures_resolves_beyond_the_limit_citing_107_51_a(decimal value, SpeedUnit unit)
    {
        var finding = Finding(Resolve(new Groundspeed(value, unit), NoWaiver));

        Assert.False(finding.WithinLimit);
        Assert.Equal("§ 107.51(a)", finding.Authority.Citation);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    public static TheoryData<decimal, SpeedUnit> BetweenTheFigures => new()
    {
        // At 87 knots: not beyond 87 knots, and beyond 100 mph (87 knots is about 100.12 mph).
        { 87m, SpeedUnit.Knots },

        // Just below 87 knots, and still beyond 100 mph.
        { 86.9m, SpeedUnit.Knots },

        // Between 100 mph and 87 knots, stated in miles per hour.
        { 100.01m, SpeedUnit.MilesPerHour },
        { 100.11m, SpeedUnit.MilesPerHour },
    };

    [Theory]
    [MemberData(nameof(BetweenTheFigures))]
    public void A_groundspeed_beyond_100_mph_and_not_beyond_87_knots_declines_RequiresInterpretation_citing_speed_limit_107_51_a(decimal value, SpeedUnit unit)
    {
        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(Resolve(new Groundspeed(value, unit), NoWaiver)).Result;

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.51(a)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.SpeedLimit.Registered.Locator, unresolved.Locator);
        Assert.Contains("speed-limit", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(87)]
    [InlineData(200)]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_whatever_the_groundspeed(int knots)
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller);

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(Resolve(Groundspeed.InKnots(knots), waiver)).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("speed-within-limit", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.51", "the remote pilot in command");

        var within = Finding(Resolve(Groundspeed.InKnots(50m), waiver));
        var beyond = Finding(Resolve(Groundspeed.InKnots(200m), waiver));

        Assert.True(within.WithinLimit);
        Assert.False(beyond.WithinLimit);
        Assert.Same(waiver, within.Waiver);
        Assert.Same(waiver, beyond.Waiver);
        Assert.Equal("the remote pilot in command", beyond.Waiver.StatedBy);
        Assert.False(beyond.Waiver.InForce);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SpeedWithinLimit.Resolve(
            new SpeedWithinLimitRequest { Groundspeed = Groundspeed.InKnots(50m) }));

        Assert.Equal(nameof(SpeedWithinLimitRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_a_groundspeed_it_refuses()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SpeedWithinLimit.Resolve(
            new SpeedWithinLimitRequest { Waiver = NoWaiver }));

        Assert.Equal(nameof(SpeedWithinLimitRequest.Groundspeed), error.ParamName);
    }
}
