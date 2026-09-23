using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>intensity-reduction-in-interest-of-safety</c>, § 107.29(a)(2), (b), resolved through
/// <see cref="EntryPoints.IntensityReductionInInterestOfSafety"/> only. The map records the entry
/// as <c>kind: assertion</c>, asserted by the remote pilot in command, and suspended by
/// <c>waivable-regulations</c>. So these pin three things: what the caller asserts is what the
/// engine answers, unchanged and in either direction; asserting nothing is a demand on the caller
/// rather than an unresolved result; and the waiver gate is read first, so a suspended entry
/// declines without demanding a determination the waiver has made beside the point. The gate's
/// other side is pinned too: a statement that no waiver is in force is recorded on the resolved
/// answer as a typed field, so the determination and the statement it was answered under stay
/// together (decision 0001).
/// </summary>
/// <remarks>
/// Nothing here measures anything. § 107.29(a)(2) and (b) state no intensity, no candela, no
/// lumens and no proportion by which intensity may be reduced — they state a determination and
/// name who makes it — so there is no threshold for a test to sit either side of. The sentence's
/// computable half, that the lighting may be reduced but not extinguished, is
/// <c>anti-collision-lighting</c>'s and is not asserted here.
/// </remarks>
public class IntensityReductionInInterestOfSafetyEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    private const string Caller = nameof(IntensityReductionInInterestOfSafetyEntryPointTests);

    private static readonly MapEntry Entry = MapEntries.IntensityReductionInInterestOfSafety;

    /// <summary>The regulation § 107.205(b) lists, as that paragraph designates it.</summary>
    private const string Regulation = "§ 107.29(a)(2) and (b)";

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld(Regulation, Caller);

    private static IntensityReductionFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<IntensityReductionFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static Resolution<object> Resolve(object asserted) =>
        EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
            new IntensityReductionInInterestOfSafetyRequest(RuleRequest.Empty.Assert(Entry.Id, asserted))
            {
                Waiver = NoWaiver,
            });

    [Fact]
    public void An_assertion_that_reducing_the_intensity_is_in_the_interest_of_safety_resolves_to_what_was_asserted_citing_107_29_a_2_b()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Finding(Resolve(asserted));

        Assert.True(finding.Holds);
        Assert.Equal(RemotePilotInCommand, finding.AssertedBy);
        Assert.Equal(asserted, finding.Determination);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.29(a)(2), (b)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.IntensityReductionInInterestOfSafety.Registered.Locator, finding.Authority);
    }

    [Fact]
    public void An_assertion_that_it_is_not_in_the_interest_of_safety_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        // The corpus makes the reduction something the remote pilot in command "may" do on their
        // own determination, so a "no" is the permission left unexercised — the lighting stays at
        // full intensity — and not a finding against anybody. Either way the engine reports the
        // determination it was given and draws no further conclusion from it.
        var asserted = new Assertion(Entry, Holds: false, RemotePilotInCommand);

        var finding = Finding(Resolve(asserted));

        Assert.False(finding.Holds);
        Assert.Equal(RemotePilotInCommand, finding.AssertedBy);
        Assert.Equal(asserted, finding.Determination);
        Assert.Contains(RemotePilotInCommand, finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_resolved_answer_carries_the_waiver_statement_and_who_made_it()
    {
        // Decision 0001: "A statement that none is in force evaluates the rule normally and travels
        // with the result", and a resolved result "records it as a typed field". The statement is
        // the caller's second fact, and an answer that consumed it at the gate and then dropped it
        // would leave a caller unable to say which statement, or whose, the determination was
        // answered under — the same debt the decline pays by recording it in Attempted.
        var stated = WaiverStatement.NoneHeld(Regulation, "the remote pilot in command");
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Finding(EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
            new IntensityReductionInInterestOfSafetyRequest(RuleRequest.Empty.Assert(Entry.Id, asserted))
            {
                Waiver = stated,
            }));

        Assert.Same(stated, finding.Waiver);
        Assert.Equal("the remote pilot in command", finding.Waiver.StatedBy);
        Assert.Equal(Regulation, finding.Waiver.Regulation);
        Assert.False(finding.Waiver.InForce);
        Assert.Contains("the remote pilot in command", finding.ToString(), StringComparison.Ordinal);

        // It is the statement the caller made, not one the engine reconstructed: a second statement
        // about the same regulation, from somebody else, travels through as itself.
        var elsewhere = WaiverStatement.NoneHeld(Regulation, "the operator's compliance officer");

        var second = Finding(EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
            new IntensityReductionInInterestOfSafetyRequest(RuleRequest.Empty.Assert(Entry.Id, asserted))
            {
                Waiver = elsewhere,
            }));

        Assert.Same(elsewhere, second.Waiver);
        Assert.Equal("the operator's compliance officer", second.Waiver.StatedBy);
        Assert.NotEqual(finding.Waiver, second.Waiver);

        // The determination is unchanged by either, and is still the caller's own.
        Assert.Equal(asserted, second.Determination);
        Assert.True(second.Holds);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest { Waiver = NoWaiver }));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. With no waiver in force, nothing about this entry resolves to an
        // UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest(RuleRequest.Empty) { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_29_a_2_and_b_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_without_demanding_the_assertion()
    {
        var waiver = WaiverStatement.Held(Regulation, Caller, certificate: "107W-2026-00024");

        // Nothing is asserted. The gate is read first, so this declines rather than throwing
        // AssertionRequiredException for a determination § 107.205(b) has put out of reach.
        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest { Waiver = waiver })).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains(Entry.Id, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00024", unresolved.Attempted, StringComparison.Ordinal);

        // And an assertion supplied anyway does not lift the suspension or get answered under it.
        var anyway = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest(
                    RuleRequest.Empty.Assert(Entry.Id, new Assertion(Entry, Holds: true, RemotePilotInCommand)))
                {
                    Waiver = waiver,
                })).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, anyway.Reason);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, anyway.Locator);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest(
                    RuleRequest.Empty.Assert(Entry.Id, new Assertion(Entry, Holds: true, RemotePilotInCommand)))));

        Assert.Equal(nameof(IntensityReductionInInterestOfSafetyRequest.Waiver), error.ParamName);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest(
                    RuleRequest.Empty.Assert(Entry.Id, new Assertion(Entry, Holds: true, RemotePilotInCommand)))
                {
                    Waiver = WaiverStatement.Held("§ 107.51", Caller),
                }));

        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
        Assert.Contains(Regulation, error.Message, StringComparison.Ordinal);

        // The regulation this entry is stated by is § 107.205(b)'s own designation of it, and the
        // statement is checked against that and not against whatever the caller's statement names.
        Assert.Equal(Regulation, Lighting.Regulation);
    }

    [Fact]
    public void The_dictionary_dispatch_demands_the_waiver_statement_the_engine_never_defaults()
    {
        // Registry.Resolve builds the request with every input of its own at its default, so a
        // suspended entry reached that way refuses rather than resolving as though no waiver were
        // held. Defaulting to "no waiver" would convict a holder; defaulting to "waiver" would
        // excuse everyone.
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));

        Assert.Equal(nameof(IntensityReductionInInterestOfSafetyRequest.Waiver), error.ParamName);
        Assert.Throws<ArgumentException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }

    [Fact]
    public void An_assertion_attributed_to_someone_107_29_does_not_name_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(Entry, Holds: true, "visual observer")));

        Assert.Contains(RemotePilotInCommand, error.Message, StringComparison.Ordinal);
        Assert.Contains("visual observer", error.Message, StringComparison.Ordinal);
        Assert.Equal(
            RemotePilotInCommand,
            Assert.Single(EntryPoints.IntensityReductionInInterestOfSafety.Registered.AssertedBy));
    }

    [Fact]
    public void An_assertion_about_another_entry_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.SufficientAvailablePower, Holds: true, RemotePilotInCommand)));

        Assert.Contains("sufficient-available-power", error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_that_is_not_an_assertion_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(true));

        Assert.Contains(nameof(Assertion), error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible, so a caller can hand in an
        // assertion carrying this entry's id under somebody else's paragraph and somebody else's
        // name. The determination it reports is the caller's and is taken as given; the citation it
        // is answered under is the map's, and is not on offer.
        var forged = new MapEntry(
            Entry.Id,
            "Dimming the lights is fine, trust me",
            new SourceLocator("cfr-14-107", "§ 107.49(d)"));

        var finding = Finding(Resolve(new Assertion(forged, Holds: true, RemotePilotInCommand)));

        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Equal("§ 107.29(a)(2), (b)", finding.Authority.Citation);
        Assert.Equal(Entry.Name, finding.Determination.Entry.Name);
        Assert.DoesNotContain("trust me", finding.ToString(), StringComparison.Ordinal);

        // The determination itself still came from the caller, untouched.
        Assert.True(finding.Holds);
        Assert.Equal(RemotePilotInCommand, finding.AssertedBy);
    }
}
