using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>effective-communication</c>, § 107.33(a), resolved through
/// <see cref="EntryPoints.EffectiveCommunication"/> only. The map records the entry's question as
/// unresolved, <c>RequiresInterpretation</c>, so the engine declines and cites this entry's own
/// locator; § 107.205(d) lists § 107.33, so a stated waiver of it declines first, citing the gate.
/// These pin that it declines, which decline, why, and where — a decline citing the wrong place
/// says something false.
/// </summary>
public class EffectiveCommunicationEntryPointTests
{
    private const string Caller = nameof(EffectiveCommunicationEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.33", Caller);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Stated_that_no_waiver_is_in_force_it_declines_RequiresInterpretation_citing_107_33_a()
    {
        var unresolved = Decline(
            EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest { Waiver = NoWaiver }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.33(a)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.EffectiveCommunication.Registered.Locator, unresolved.Locator);
        Assert.Contains("effective-communication", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_naming_the_undefined_term_and_the_three_persons_107_33_a_names()
    {
        var unresolved = Decline(
            EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest { Waiver = NoWaiver }));

        Assert.Contains("does not define \"effective\"", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("vests the determination in nobody", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("the remote pilot in command", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            "the person manipulating the flight controls of the small unmanned aircraft system",
            unresolved.Attempted,
            StringComparison.Ordinal);
        Assert.Contains("the visual observer", unresolved.Attempted, StringComparison.Ordinal);

        // § 107.31(a) prints "flight control", singular, of a person the same paragraph also names;
        // § 107.33(a) prints "flight controls". This entry states § 107.33(a), and must not borrow
        // the other section's wording.
        Assert.DoesNotContain(
            "the person manipulating the flight control of",
            unresolved.Attempted,
            StringComparison.Ordinal);

        Assert.Equal(
            Communication.Effective(NoWaiver),
            EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_33_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.33", Caller, certificate: "107W-2026-00029");

        var unresolved = Decline(
            EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest { Waiver = waiver }));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("effective-communication", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.33(a)", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00029", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("does not define", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest()));

        Assert.Equal(nameof(EffectiveCommunicationRequest.Waiver), error.ParamName);
        Assert.Contains("effective-communication", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.EffectiveCommunication.Resolve(
                new EffectiveCommunicationRequest { Waiver = WaiverStatement.Held("§ 107.31", Caller) }));

        Assert.Contains("§ 107.31", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.33", error.Message, StringComparison.Ordinal);
    }
}
