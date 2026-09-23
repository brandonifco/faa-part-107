using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>observer-coordination</c>, § 107.33(c), resolved through
/// <see cref="EntryPoints.ObserverCoordination"/> only. The map records the entry as
/// <c>kind: assertion</c> asserted by the three persons the paragraph names, and
/// <c>suspendedBy</c> the § 107.205 gate, so these pin four things: what the caller asserts is
/// what the engine answers, unchanged and in either direction; asserting nothing is a demand on
/// the caller and never an unresolved result; the gate is read first, so a waiver of § 107.33
/// declines without the assertion ever being demanded; and the waiver statement travels with the
/// outcome in both directions — as text in <c>Attempted</c> on the decline, and as a typed field
/// on the resolved value (decision 0001). The three names are checked in the corpus's own words —
/// § 107.33(c)'s plural "flight controls", not § 107.31(a)'s singular "flight control" — because
/// the map keeps that difference and the engine must not normalize it away.
/// </summary>
public class ObserverCoordinationEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";
    private const string PersonManipulating = "person manipulating the flight controls";
    private const string VisualObserver = "visual observer";
    private const string Caller = nameof(ObserverCoordinationEntryPointTests);

    private static readonly MapEntry Entry = MapEntries.ObserverCoordination;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.33", Caller);

    private static ObserverCoordinationRequest Request(WaiverStatement waiver, object? asserted) =>
        new(asserted is null ? RuleRequest.Empty : RuleRequest.Empty.Assert(Entry.Id, asserted))
        {
            Waiver = waiver,
        };

    private static Resolution<object> Resolve(object asserted) => Resolve(NoWaiver, asserted);

    private static Resolution<object> Resolve(WaiverStatement waiver, object? asserted) =>
        EntryPoints.ObserverCoordination.Resolve(Request(waiver, asserted));

    private static ObserverCoordinationFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<ObserverCoordinationFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void An_assertion_that_the_three_named_persons_coordinate_resolves_to_what_was_asserted_citing_107_33_c()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Finding(Resolve(asserted));

        Assert.True(finding.Holds);
        Assert.Equal(RemotePilotInCommand, finding.AssertedBy);
        Assert.Equal(asserted, finding.Coordination);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.33(c)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.ObserverCoordination.Registered.Locator, finding.Authority);
        Assert.Equal(finding.Authority, finding.Coordination.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        // One proposition, not two: the paragraph's enumeration is closed and joined by "and", and
        // the entry the answer is carried on is the whole of (c). Both limbs are printed, verbatim
        // and in the paragraph's own order, because an assertion answered about the scan alone
        // would not be the paragraph's.
        Assert.Equal(
            "The three named persons coordinate to scan the airspace and maintain awareness",
            finding.Coordination.Entry.Name);
        Assert.Equal(
            new[]
            {
                "(1) Scan the airspace where the small unmanned aircraft is operating for any potential collision hazard; and",
                "(2) Maintain awareness of the position of the small unmanned aircraft through direct visual observation.",
            },
            finding.Purposes,
            StringComparer.Ordinal);
    }

    [Fact]
    public void An_assertion_that_they_do_not_coordinate_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        // "Silence" and "no" are different answers. The corpus gives the fact to three named
        // persons, so one of them saying the coordination did not happen is an answer the engine
        // records, not a gap it declines and not a fact it is free to re-decide.
        var asserted = new Assertion(Entry, Holds: false, VisualObserver);

        var finding = Finding(Resolve(asserted));

        Assert.False(finding.Holds);
        Assert.Equal(VisualObserver, finding.AssertedBy);
        Assert.Equal(asserted, finding.Coordination);
        Assert.Equal("§ 107.33(c)", finding.Authority.Citation);
        Assert.Contains(VisualObserver, finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(() => Resolve(NoWaiver, null));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. With no waiver in force, nothing about this entry is an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.ObserverCoordination.Resolve(new ObserverCoordinationRequest { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_33_is_stated_in_force_the_gate_declines_before_the_assertion_is_demanded()
    {
        var waiver = WaiverStatement.Held("§ 107.33", Caller, certificate: "107W-2026-00033");

        // Nothing is asserted in the first of these, and that is the point: the gate decides first,
        // so a suspended entry never demands a fact the waiver has made irrelevant. Were the order
        // the other way round this would throw AssertionRequiredException instead of declining.
        var withNothingAsserted = Declined(Resolve(waiver, null));
        var withAnAssertion = Declined(Resolve(waiver, new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        Assert.All(new[] { withNothingAsserted, withAnAssertion }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
            Assert.Contains(Entry.Id, unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.33(c)", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("107W-2026-00033", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_assertion_is_demanded_and_answered_and_the_statement_travels_with_it()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.33", "Jane Roe, RPIC");

        // The other direction of the gate: the entry is reachable, so the second caller fact is now
        // owed — demanded when it is missing, and answered when it is there.
        Assert.Throws<AssertionRequiredException>(() => Resolve(waiver, null));
        var finding = Finding(Resolve(waiver, new Assertion(Entry, Holds: true, VisualObserver)));

        // The engine demanded this statement, refused to infer it, and made an ArgumentException out
        // of its absence. It does not then drop it: a resolved result records it as a typed field
        // (decision 0001), so the answer says under whose statement the rule was evaluated normally.
        Assert.True(finding.Holds);
        Assert.Same(waiver, finding.Waiver);
        Assert.Equal("Jane Roe, RPIC", finding.Waiver.StatedBy);
        Assert.Equal("§ 107.33", finding.Waiver.Regulation);
        Assert.False(finding.Waiver.InForce);
        Assert.Contains("no certificate of waiver", finding.ToString(), StringComparison.Ordinal);
        Assert.Contains("Jane Roe, RPIC", finding.ToString(), StringComparison.Ordinal);

        // The same assertion under a statement by somebody else is a distinguishable answer.
        var byAnother = Finding(Resolve(
            WaiverStatement.NoneHeld("§ 107.33", "Richard Roe, VO"),
            new Assertion(Entry, Holds: true, VisualObserver)));

        Assert.NotEqual(finding, byAnother);
        Assert.Equal(finding.Coordination, byAnother.Coordination);
    }

    [Fact]
    public void Each_of_the_three_persons_107_33_c_names_may_assert_it_in_the_corpuss_own_words()
    {
        Assert.Equal(
            new[] { RemotePilotInCommand, PersonManipulating, VisualObserver },
            EntryPoints.ObserverCoordination.Registered.AssertedBy,
            StringComparer.Ordinal);

        foreach (var who in new[] { RemotePilotInCommand, PersonManipulating, VisualObserver })
        {
            Assert.Equal(who, Finding(Resolve(new Assertion(Entry, Holds: true, who))).AssertedBy);
        }
    }

    [Fact]
    public void An_assertion_attributed_to_someone_107_33_c_does_not_name_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(Entry, Holds: true, "person designated to maintain awareness")));

        Assert.Contains(PersonManipulating, error.Message, StringComparison.Ordinal);
        Assert.Contains("person designated to maintain awareness", error.Message, StringComparison.Ordinal);

        // § 107.31(a) says "the person manipulating the flight control"; § 107.33(c) says "flight
        // controls". The map keeps the difference, so the engine refuses the neighbouring
        // paragraph's wording rather than reading it as the same person.
        Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(Entry, Holds: true, "person manipulating the flight control")));

        // And the comparison is ordinal, so neither case nor a stray article passes.
        Assert.Throws<ArgumentException>(() => Resolve(new Assertion(Entry, Holds: true, "Visual Observer")));
        Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(Entry, Holds: true, "the remote pilot in command")));
    }

    [Fact]
    public void An_assertion_about_another_entry_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.EffectiveCommunication, Holds: true, VisualObserver)));

        Assert.Contains("effective-communication", error.Message, StringComparison.Ordinal);
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
        // assertion carrying this entry's id under the neighbouring paragraph and a name of its
        // own. The fact it reports is the caller's and is taken as given; the citation it is
        // answered under, and the proposition it names, are the map's.
        var forged = new MapEntry(
            Entry.Id,
            "Somebody was scanning, more or less",
            new SourceLocator("cfr-14-107", "§ 107.33(a)"));

        var finding = Finding(Resolve(new Assertion(forged, Holds: true, PersonManipulating)));

        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Equal(Entry.Locator, finding.Coordination.Authority);
        Assert.Equal("§ 107.33(c)", finding.Authority.Citation);
        Assert.Equal(Entry.Name, finding.Coordination.Entry.Name);
        Assert.DoesNotContain("more or less", finding.ToString(), StringComparison.Ordinal);

        // The fact itself still came from the caller, untouched.
        Assert.True(finding.Holds);
        Assert.Equal(PersonManipulating, finding.AssertedBy);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.ObserverCoordination.Resolve(
                ObserverCoordinationRequest.Asserting(new Assertion(Entry, Holds: true, VisualObserver))));

        Assert.Equal(nameof(ObserverCoordinationRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_refuses_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Finding(Registry.Resolve(Request(NoWaiver, asserted)));

        Assert.True(finding.Holds);
        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Equal(asserted, finding.Coordination);
        Assert.Same(NoWaiver, finding.Waiver);

        // Resolved by id, the request is built from the assertions alone, so it carries no waiver
        // statement and the entry refuses rather than inferring one — the caller's omission, not a
        // gap in § 107.33.
        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));

        Assert.Equal(nameof(ObserverCoordinationRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            WaiverStatement.Held("§ 107.31", Caller),
            new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        Assert.Contains("§ 107.31", error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }
}
