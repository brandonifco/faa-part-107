using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>speed-limit</c>, § 107.51(a), resolved through <see cref="EntryPoints.SpeedLimit"/> only: the
/// limit as printed, the decline where the answer turns on the entry's question, and the waiver gate.
/// </summary>
public class SpeedLimitEntryPointTests
{
    private const string Caller = nameof(SpeedLimitEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    [Fact]
    public void The_limit_resolves_as_printed_87_knots_and_100_miles_per_hour_citing_107_51_a()
    {
        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.SpeedLimit.Resolve(new SpeedLimitRequest { Waiver = NoWaiver }));

        var limit = Assert.IsType<GroundspeedLimit>(resolved.Value);
        Assert.Equal(87m, limit.Knots.Value);
        Assert.Equal(SpeedUnit.Knots, limit.Knots.Unit);
        Assert.Equal(100m, limit.MilesPerHour.Value);
        Assert.Equal(SpeedUnit.MilesPerHour, limit.MilesPerHour.Unit);
        Assert.Equal("cfr-14-107", limit.Authority.SourceId);
        Assert.Equal("§ 107.51(a)", limit.Authority.Citation);
        Assert.Same(NoWaiver, limit.Waiver);
    }

    [Theory]
    [InlineData(SpeedUnit.Knots)]
    [InlineData(SpeedUnit.MilesPerHour)]
    public void Asked_for_the_limit_as_one_figure_it_declines_RequiresInterpretation_citing_107_51_a(SpeedUnit unit)
    {
        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.SpeedLimit.Resolve(new SpeedLimitRequest { Waiver = NoWaiver, AsOneFigureIn = unit })).Result;

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.51(a)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.SpeedLimit.Registered.Locator, unresolved.Locator);
    }

    [Fact]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller, certificate: "107W-2026-00001");

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.SpeedLimit.Resolve(new SpeedLimitRequest { Waiver = waiver })).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00001", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SpeedLimit.Resolve(new SpeedLimitRequest()));

        Assert.Equal(nameof(SpeedLimitRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SpeedLimit.Resolve(
            new SpeedLimitRequest { Waiver = WaiverStatement.Held("§ 107.41", Caller) }));

        Assert.Contains("§ 107.41", error.Message, StringComparison.Ordinal);
    }
}
