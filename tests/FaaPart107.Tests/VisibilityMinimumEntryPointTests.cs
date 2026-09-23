using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>visibility-minimum</c>, § 107.51(c), resolved through <see cref="EntryPoints.VisibilityMinimum"/>
/// only: the minimum as printed, and the waiver gate.
/// </summary>
public class VisibilityMinimumEntryPointTests
{
    private const string Caller = nameof(VisibilityMinimumEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    [Fact]
    public void The_minimum_resolves_as_printed_3_statute_miles_citing_107_51_c()
    {
        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.VisibilityMinimum.Resolve(new VisibilityMinimumRequest { Waiver = NoWaiver }));

        var minimum = Assert.IsType<VisibilityMinimum>(resolved.Value);
        Assert.Equal(3m, minimum.StatuteMiles);
        Assert.Equal("cfr-14-107", minimum.Authority.SourceId);
        Assert.Equal("§ 107.51(c)", minimum.Authority.Citation);
        Assert.Equal(EntryPoints.VisibilityMinimum.Registered.Locator, minimum.Authority);
        Assert.Same(NoWaiver, minimum.Waiver);
    }

    [Fact]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller, certificate: "107W-2026-00003");

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.VisibilityMinimum.Resolve(new VisibilityMinimumRequest { Waiver = waiver })).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("visibility-minimum", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00003", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.VisibilityMinimum.Resolve(new VisibilityMinimumRequest()));

        Assert.Equal(nameof(VisibilityMinimumRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.VisibilityMinimum.Resolve(
            new VisibilityMinimumRequest { Waiver = WaiverStatement.Held("§ 107.41", Caller) }));

        Assert.Contains("§ 107.41", error.Message, StringComparison.Ordinal);
    }
}
