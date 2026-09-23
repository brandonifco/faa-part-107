using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>unaided-visual-contact</c>, § 107.31(a), resolved through
/// <see cref="EntryPoints.UnaidedVisualContact"/> only. The map records the entry as
/// <c>kind: assertion</c> with <c>suspendedBy: ["waivable-regulations"]</c>, so these pin three
/// things: correspondence row 8 in both directions — what the caller asserts is what the engine
/// answers, and asserting nothing is a demand on the caller rather than an unresolved result; the
/// order of the two caller facts, the waiver gate deciding before the assertion is demanded at all;
/// and the three people the paragraph names, in the map's own words, which are § 107.31(a)'s
/// singular "flight control" and not § 107.33's plural.
/// </summary>
public class UnaidedVisualContactEntryPointTests
{
    private const string Caller = nameof(UnaidedVisualContactEntryPointTests);

    private const string RemotePilotInCommand = "remote pilot in command";

    private const string VisualObserver = "visual observer";

    private const string PersonManipulatingTheFlightControl = "person manipulating the flight control";

    private static readonly MapEntry Entry = MapEntries.UnaidedVisualContact;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.31", Caller);

    private static UnaidedVisualContactRequest Request(WaiverStatement waiver, object? asserted) =>
        new(asserted is null ? RuleRequest.Empty : RuleRequest.Empty.Assert(Entry.Id, asserted))
        {
            Waiver = waiver,
        };

    private static Resolution<object> Resolve(object asserted) => Resolve(NoWaiver, asserted);

    private static Resolution<object> Resolve(WaiverStatement waiver, object? asserted) =>
        EntryPoints.UnaidedVisualContact.Resolve(Request(waiver, asserted));

    private static UnaidedVisualContactFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<UnaidedVisualContactFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void An_assertion_that_the_aircraft_can_be_seen_unaided_resolves_to_what_was_asserted_citing_107_31_a()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Finding(Resolve(asserted));

        Assert.True(finding.Holds);
        Assert.Equal(RemotePilotInCommand, finding.AssertedBy);
        Assert.Equal(asserted, finding.Contact);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.31(a)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.UnaidedVisualContact.Registered.Locator, finding.Authority);
        Assert.Equal(finding.Authority, finding.Contact.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Fact]
    public void An_assertion_that_the_aircraft_cannot_be_seen_unaided_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        // "Silence" and "no" are different answers. The corpus gives the fact to a person, so a
        // person saying the aircraft cannot be seen unaided is an answer the engine records, not a
        // gap it declines and not a fact it is free to re-decide.
        var asserted = new Assertion(Entry, Holds: false, VisualObserver);

        var finding = Finding(Resolve(asserted));

        Assert.False(finding.Holds);
        Assert.Equal(VisualObserver, finding.AssertedBy);
        Assert.Equal(asserted, finding.Contact);
        Assert.Equal("§ 107.31(a)", finding.Authority.Citation);
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
            () => EntryPoints.UnaidedVisualContact.Resolve(new UnaidedVisualContactRequest { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_31_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_without_demanding_the_assertion()
    {
        var waiver = WaiverStatement.Held("§ 107.31", Caller, "107W-0000-00000");

        // Nothing is asserted here, and that is the point: the gate decides first, so a suspended
        // entry never demands a fact the waiver has made irrelevant. Were the order the other way
        // round this would throw AssertionRequiredException instead of declining.
        var withNothingAsserted = Declined(Resolve(waiver, null));
        var withAnAssertion = Declined(Resolve(waiver, new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        Assert.All(new[] { withNothingAsserted, withAnAssertion }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
            Assert.Contains("unaided-visual-contact", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.31(a)", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_assertion_is_demanded_and_answered_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.31", RemotePilotInCommand);

        // The other direction of the gate: the entry is reachable, so the second caller fact is now
        // owed — demanded when it is missing, and answered when it is there.
        Assert.Throws<AssertionRequiredException>(() => Resolve(waiver, null));
        var finding = Finding(Resolve(waiver, new Assertion(Entry, Holds: true, PersonManipulatingTheFlightControl)));

        Assert.True(finding.Holds);
        Assert.Same(waiver, finding.Waiver);
        Assert.Equal(RemotePilotInCommand, finding.Waiver.StatedBy);
        Assert.False(finding.Waiver.InForce);
        Assert.Contains("no certificate of waiver", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Each_of_the_three_people_107_31_a_names_may_assert_it_and_nobody_else_may()
    {
        // The map's own three strings, ordinally. § 107.31(a) says "the person manipulating the
        // flight control" where § 107.33 says "flight controls"; the corpus differs between the two
        // sections and the map is faithful to it, so the plural is somebody this paragraph does not
        // name and is refused here.
        Assert.Equal(
            new[] { RemotePilotInCommand, VisualObserver, PersonManipulatingTheFlightControl },
            EntryPoints.UnaidedVisualContact.Registered.AssertedBy);

        foreach (var who in new[] { RemotePilotInCommand, VisualObserver, PersonManipulatingTheFlightControl })
        {
            var finding = Finding(Resolve(new Assertion(Entry, Holds: true, who)));
            Assert.Equal(who, finding.AssertedBy);
        }

        foreach (var stranger in new[]
        {
            "person manipulating the flight controls",
            "the person manipulating the flight control",
            "the remote pilot in command",
            "person on the ground",
        })
        {
            var error = Assert.Throws<ArgumentException>(
                () => Resolve(new Assertion(Entry, Holds: true, stranger)));

            Assert.Contains(stranger, error.Message, StringComparison.Ordinal);
            Assert.Contains(PersonManipulatingTheFlightControl, error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_four_purposes_the_ability_is_stated_against_are_the_paragraphs_own_four_in_order()
    {
        // One ability, four purposes, one assertion. The paragraph states the ability once — "must
        // be able to see the unmanned aircraft throughout the entire flight" — and "in order to:"
        // introduces all four in the same constituent, so the map records one entry and the engine
        // demands one value. The four are carried verbatim, under the paragraph's own numbers, and
        // none of them is a second open term for the engine to decide.
        var finding = Finding(Resolve(new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        Assert.Equal(
            new[]
            {
                "(1) Know the unmanned aircraft's location;",
                "(2) Determine the unmanned aircraft's attitude, altitude, and direction of flight;",
                "(3) Observe the airspace for other air traffic or hazards; and",
                "(4) Determine that the unmanned aircraft does not endanger the life or property of another.",
            },
            finding.Purposes);
    }

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible, so a caller can hand in an
        // assertion carrying this entry's id under somebody else's paragraph and somebody else's
        // name. The fact it reports is the caller's and is taken as given; the citation it is
        // answered under is the map's, and is not on offer.
        var forged = new MapEntry(
            Entry.Id,
            "I could see it the whole time, trust me",
            new SourceLocator("cfr-14-107", "§ 107.33(c)"));

        var finding = Finding(Resolve(new Assertion(forged, Holds: true, VisualObserver)));

        Assert.Equal(MapEntries.UnaidedVisualContact.Locator, finding.Authority);
        Assert.Equal(MapEntries.UnaidedVisualContact.Locator, finding.Contact.Authority);
        Assert.Equal("§ 107.31(a)", finding.Contact.Authority.Citation);
        Assert.Equal(MapEntries.UnaidedVisualContact.Name, finding.Contact.Entry.Name);
        Assert.DoesNotContain("trust me", finding.ToString(), StringComparison.Ordinal);

        // The fact itself still came from the caller, untouched.
        Assert.True(finding.Holds);
        Assert.Equal(VisualObserver, finding.AssertedBy);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            WaiverStatement.Held("§ 107.33", Caller),
            new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        Assert.Contains("unaided-visual-contact", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.33", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.UnaidedVisualContact.Resolve(
                UnaidedVisualContactRequest.Asserting(new Assertion(Entry, Holds: true, RemotePilotInCommand))));

        Assert.Equal(nameof(UnaidedVisualContactRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_refuses_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, VisualObserver);

        var finding = Finding(Registry.Resolve(Request(NoWaiver, asserted)));

        Assert.True(finding.Holds);
        Assert.Equal(VisualObserver, finding.AssertedBy);
        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        // Resolved by id, the request is built from the assertions alone, so it carries no waiver
        // statement — and a suspended entry refuses rather than infer one, in either direction.
        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));
        Assert.Equal(nameof(UnaidedVisualContactRequest.Waiver), error.ParamName);
        Assert.Throws<ArgumentException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }
}
