using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>well-clear</c>, § 107.37(a), resolved through <see cref="EntryPoints.WellClear"/> only. The map
/// records the entry's question as unresolved, <c>RequiresInterpretation</c>, so the engine declines
/// whatever it is asked; these pin that it declines, why, and where, that no route through the engine
/// produces a well-clear verdict instead, and that the waiver gate answers first and says something
/// else. A decline citing the wrong place, or one dressed as an answer, says something false.
/// </summary>
public class WellClearEntryPointTests
{
    private const string Caller = nameof(WellClearEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.37(a)", Caller);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Resolving_it_declines_RequiresInterpretation_citing_107_37_a()
    {
        var unresolved = Decline(EntryPoints.WellClear.Resolve(new WellClearRequest { Waiver = NoWaiver }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.37(a)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WellClear.Registered.Locator, unresolved.Locator);
        Assert.Contains("well-clear", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_and_names_the_term_the_corpus_leaves_undefined()
    {
        var resolution = EntryPoints.WellClear.Resolve(new WellClearRequest { Waiver = NoWaiver });
        var unresolved = Decline(resolution);

        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("\"well clear\"", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("passing over, under or ahead", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("states no measure for it", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Equal(Yielding.WellClear(NoWaiver), resolution);
    }

    [Fact]
    public void It_declines_the_same_way_whatever_the_caller_asserts_for_it()
    {
        // well-clear is kind: operation, not assertion. § 107.37(a) states no measure for the term,
        // so the engine neither demands the answer nor takes it from a caller who offers one.
        var asserted = new WellClearRequest(RuleRequest.Empty.Assert("well-clear", true)) { Waiver = NoWaiver };

        var unresolved = Decline(EntryPoints.WellClear.Resolve(asserted));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal(Yielding.WellClear(NoWaiver), EntryPoints.WellClear.Resolve(asserted));
    }

    [Fact]
    public void While_a_waiver_of_107_37_a_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.37(a)", Caller, certificate: "107W-2026-00017");

        var unresolved = Decline(EntryPoints.WellClear.Resolve(new WellClearRequest { Waiver = waiver }));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("well-clear", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00017", unresolved.Attempted, StringComparison.Ordinal);

        // right-of-way cites § 107.37(a) too, so the locator cannot tell the two entries apart and
        // only what was attempted names which one was suspended.
        Assert.DoesNotContain("right-of-way", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.WellClear.Resolve(new WellClearRequest()));

        Assert.Equal(nameof(WellClearRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.WellClear.Resolve(
            new WellClearRequest { Waiver = WaiverStatement.Held("§ 107.51", Caller) }));

        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
    }
}
