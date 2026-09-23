using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>direct-participation</c>, § 107.39(a), resolved through <see cref="EntryPoints.DirectParticipation"/>
/// only. The map records the entry's question as unresolved, <c>RequiresInterpretation</c>, so the engine
/// declines and cites this entry's own locator; these pin that it declines, why, where, that nothing the
/// caller supplies turns the decline into an answer, and the § 107.205 gate the entry's <c>suspendedBy</c>
/// names.
/// </summary>
public class DirectParticipationEntryPointTests
{
    private const string Caller = nameof(DirectParticipationEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.39", Caller);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Resolving_it_declines_RequiresInterpretation_citing_107_39_a()
    {
        var unresolved = Decline(EntryPoints.DirectParticipation.Resolve(
            new DirectParticipationRequest { Waiver = NoWaiver }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.39(a)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.DirectParticipation.Registered.Locator, unresolved.Locator);
        Assert.Contains("direct-participation", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_and_names_the_term_the_corpus_leaves_undefined()
    {
        var request = new DirectParticipationRequest { Waiver = NoWaiver };

        var unresolved = Decline(EntryPoints.DirectParticipation.Resolve(request));

        Assert.Contains("directly participating", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("not how direct it must be", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("vested in nobody", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Equal(Participation.Direct(NoWaiver), EntryPoints.DirectParticipation.Resolve(request));
    }

    [Fact]
    public void The_decline_is_the_same_whatever_the_caller_asserts_about_the_human_being()
    {
        var asserted = RuleRequest.Empty
            .Assert("reasonable-protection", true)
            .Assert("participant-briefing", true);

        var unresolved = Decline(EntryPoints.DirectParticipation.Resolve(
            new DirectParticipationRequest(asserted) { Waiver = NoWaiver }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal(
            Participation.Direct(NoWaiver),
            EntryPoints.DirectParticipation.Resolve(new DirectParticipationRequest(asserted) { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_39_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.39", Caller, certificate: "107W-2026-00021");

        var unresolved = Decline(EntryPoints.DirectParticipation.Resolve(
            new DirectParticipationRequest { Waiver = waiver }));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("direct-participation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00021", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.DirectParticipation.Resolve(new DirectParticipationRequest()));

        Assert.Equal(nameof(DirectParticipationRequest.Waiver), error.ParamName);
        Assert.Throws<ArgumentException>(() => Registry.Resolve("direct-participation", RuleRequest.Empty));
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.DirectParticipation.Resolve(
            new DirectParticipationRequest { Waiver = WaiverStatement.Held("§ 107.49", Caller) }));

        Assert.Contains("§ 107.49", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.39", error.Message, StringComparison.Ordinal);
    }
}
