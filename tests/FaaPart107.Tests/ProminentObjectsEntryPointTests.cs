using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>prominent-objects</c>, § 107.51(c), resolved through <see cref="EntryPoints.ProminentObjects"/>
/// only. The map records the entry's question as unresolved, <c>RequiresInterpretation</c>, so the
/// engine declines and cites this entry's own locator; these pin that it declines, why, and where —
/// a decline citing the wrong place says something false — and that the § 107.205 gate is read
/// first, with its own reason and its own citation.
/// </summary>
public class ProminentObjectsEntryPointTests
{
    private const string Caller = nameof(ProminentObjectsEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Resolving_it_declines_RequiresInterpretation_citing_107_51_c()
    {
        var unresolved = Decline(EntryPoints.ProminentObjects.Resolve(new ProminentObjectsRequest { Waiver = NoWaiver }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.51(c)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, unresolved.Locator);
        Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_and_names_the_term_the_corpus_leaves_undefined()
    {
        var unresolved = Decline(EntryPoints.ProminentObjects.Resolve(new ProminentObjectsRequest { Waiver = NoWaiver }));

        Assert.Contains("\"prominent\"", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("names nobody to decide it", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Equal(
            Prominence.Objects(NoWaiver),
            EntryPoints.ProminentObjects.Resolve(new ProminentObjectsRequest { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller, certificate: "107W-2026-00007");

        var unresolved = Decline(EntryPoints.ProminentObjects.Resolve(new ProminentObjectsRequest { Waiver = waiver }));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00007", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.ProminentObjects.Resolve(new ProminentObjectsRequest()));

        Assert.Equal(nameof(ProminentObjectsRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.ProminentObjects.Resolve(
            new ProminentObjectsRequest { Waiver = WaiverStatement.Held("§ 107.41", Caller) }));

        Assert.Contains("§ 107.41", error.Message, StringComparison.Ordinal);
    }
}
