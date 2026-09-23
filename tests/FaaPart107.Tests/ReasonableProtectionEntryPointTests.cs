using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>reasonable-protection</c>, § 107.39(b), resolved through
/// <see cref="EntryPoints.ReasonableProtection"/> only. The map records the entry as
/// <c>kind: assertion</c> with <c>suspendedBy: ["waivable-regulations"]</c> and
/// <c>assertedBy: ["caller"]</c>, so these pin four things: correspondence row 8 in both directions —
/// what the caller asserts is what the engine answers, and asserting nothing is a demand on the
/// caller rather than an unresolved result; the order of the three caller facts, the waiver gate
/// deciding before either of the other two is demanded, with the statement recorded on the resolved
/// answer; the entry's note distributing the assertion "for each of a covered structure and a
/// stationary vehicle", so that an assertion about one is not an answer about the other; and that
/// this entry inherits the marker behaviour
/// <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c> settled, rather than restating it.
/// </summary>
public class ReasonableProtectionEntryPointTests
{
    private const string Caller = nameof(ReasonableProtectionEntryPointTests);

    private static readonly MapEntry Entry = MapEntries.ReasonableProtection;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.39", Caller);

    private static ReasonableProtectionRequest Request(Shelter? shelter, WaiverStatement waiver, object? asserted) =>
        new(asserted is null ? RuleRequest.Empty : RuleRequest.Empty.Assert(Entry.Id, asserted))
        {
            Shelter = shelter,
            Waiver = waiver,
        };

    private static Resolution<object> Resolve(object asserted) =>
        Resolve(Shelter.CoveredStructure, NoWaiver, asserted);

    private static Resolution<object> Resolve(Shelter? shelter, WaiverStatement waiver, object? asserted) =>
        EntryPoints.ReasonableProtection.Resolve(Request(shelter, waiver, asserted));

    private static ReasonableProtectionFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<ReasonableProtectionFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void An_assertion_that_the_shelter_provides_reasonable_protection_resolves_to_what_was_asserted_citing_107_39_b()
    {
        var asserted = new Assertion(Entry, Holds: true, "the remote pilot in command");

        var finding = Finding(Resolve(Shelter.CoveredStructure, NoWaiver, asserted));

        Assert.True(finding.Holds);
        Assert.Equal("the remote pilot in command", finding.AssertedBy);
        Assert.Same(Shelter.CoveredStructure, finding.Shelter);
        Assert.Equal(asserted, finding.Protection);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.39(b)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.ReasonableProtection.Registered.Locator, finding.Authority);
        Assert.Equal(finding.Authority, finding.Protection.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Fact]
    public void An_assertion_that_it_does_not_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        // "Silence" and "no" are different answers. The corpus gives the fact to whoever the caller
        // says determined it, so a report that the structure cannot provide reasonable protection is
        // an answer the engine records, not a gap it declines and not a fact it is free to re-decide.
        var asserted = new Assertion(Entry, Holds: false, "the operator's safety officer");

        var finding = Finding(Resolve(Shelter.StationaryVehicle, NoWaiver, asserted));

        Assert.False(finding.Holds);
        Assert.Equal("the operator's safety officer", finding.AssertedBy);
        Assert.Same(Shelter.StationaryVehicle, finding.Shelter);
        Assert.Equal(asserted, finding.Protection);
        Assert.Equal("§ 107.39(b)", finding.Authority.Citation);
        Assert.Contains("the operator's safety officer", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(
            () => Resolve(Shelter.CoveredStructure, NoWaiver, null));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. The entry's note says the fact is "never defaulted to true when absent",
        // and with no waiver in force nothing about this entry is an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.ReasonableProtection.Resolve(
                new ReasonableProtectionRequest { Shelter = Shelter.StationaryVehicle, Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_39_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_without_demanding_the_shelter_or_the_assertion()
    {
        var waiver = WaiverStatement.Held("§ 107.39", Caller, "107W-0000-00000");

        // Nothing is asserted and no place is named in the first case, and that is the only input
        // that tells the orders apart: the gate decides first, so a suspended entry never demands a
        // fact the waiver has made irrelevant. Were the order the other way round this would throw
        // ArgumentException for the missing Shelter, or AssertionRequiredException, instead of
        // declining.
        var withNothingAtAll = Declined(Resolve(null, waiver, null));
        var withEverything = Declined(
            Resolve(Shelter.CoveredStructure, waiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.All(new[] { withNothingAtAll, withEverything }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
            Assert.Contains("reasonable-protection", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.39(b)", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_assertion_is_demanded_and_answered_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.39", "the remote pilot in command");

        // The other direction of the gate: the entry is reachable, so the other two caller facts are
        // now owed — demanded when missing, and answered when there. And the statement the entry
        // resolved under is carried on the answer as a typed field, not dropped: an answer that lost
        // it would say § 107.39(b) was reached without saying on whose word (decision 0001).
        Assert.Throws<AssertionRequiredException>(() => Resolve(Shelter.CoveredStructure, waiver, null));
        var finding = Finding(
            Resolve(Shelter.CoveredStructure, waiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.True(finding.Holds);
        Assert.Same(waiver, finding.Waiver);
        Assert.Equal("§ 107.39", finding.Waiver.Regulation);
        Assert.Equal("the remote pilot in command", finding.Waiver.StatedBy);
        Assert.False(finding.Waiver.InForce);
        Assert.Contains("no certificate of waiver", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_assertion_is_asserted_and_withheld_for_each_of_a_covered_structure_and_a_stationary_vehicle()
    {
        // The entry's note distributes the assertion over the paragraph's two: it "is asserted and
        // withheld for each of a covered structure and a stationary vehicle". The map uses that
        // phrase only where a proposition distributes — observer-coordination, which also names
        // three actors, says "attributed to the three named persons" instead, because coordinating
        // is one joint fact. So each of the two is asserted in either direction, and withheld, on
        // its own account, and the answer records which one it is about.
        foreach (var shelter in Shelter.All)
        {
            foreach (var holds in new[] { true, false })
            {
                var finding = Finding(Resolve(shelter, NoWaiver, new Assertion(Entry, holds, "the operator")));

                Assert.Equal(holds, finding.Holds);
                Assert.Same(shelter, finding.Shelter);
                Assert.StartsWith(shelter.Designation, finding.ToString(), StringComparison.Ordinal);
                Assert.Equal(Entry.Locator, finding.Authority);
            }

            // Withheld for this one, on its own account: nothing is asserted, and the engine demands
            // rather than reaching for anything else.
            Assert.Equal(
                Entry.Id,
                Assert.Throws<AssertionRequiredException>(() => Resolve(shelter, NoWaiver, null)).EntryId);
        }
    }

    [Fact]
    public void An_assertion_about_the_stationary_vehicle_is_not_an_answer_about_the_covered_structure()
    {
        // The input that separates the readings. A caller with a stationary vehicle on site asserts
        // the standard for it, and says nothing about the carport the human being is actually under.
        // The answer is about the vehicle and says so; it is not an answer about the covered
        // structure, which stays unasserted — "never defaulted to true when absent", in the same
        // sentence of the note as "for each of".
        var aboutTheVehicle = Finding(
            Resolve(Shelter.StationaryVehicle, NoWaiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.True(aboutTheVehicle.Holds);
        Assert.Same(Shelter.StationaryVehicle, aboutTheVehicle.Shelter);
        Assert.NotSame(Shelter.CoveredStructure, aboutTheVehicle.Shelter);

        // The answer leads with the place it is about. (The map's own entry name goes on to say "A
        // covered structure or stationary vehicle …", which is the entry's name and not this
        // answer's subject, so the check is on the place and not on the whole rendering.)
        Assert.StartsWith(
            Shelter.StationaryVehicle.Designation, aboutTheVehicle.ToString(), StringComparison.Ordinal);

        // Asked about the covered structure with nothing asserted for it, the engine demands the
        // value. It does not answer true from the vehicle's assertion, and it does not decline.
        Assert.Throws<AssertionRequiredException>(() => Resolve(Shelter.CoveredStructure, NoWaiver, null));

        // And the same the other way round, so neither place is privileged.
        var aboutTheStructure = Finding(
            Resolve(Shelter.CoveredStructure, NoWaiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.Same(Shelter.CoveredStructure, aboutTheStructure.Shelter);
        Assert.StartsWith(
            Shelter.CoveredStructure.Designation, aboutTheStructure.ToString(), StringComparison.Ordinal);
        Assert.Throws<AssertionRequiredException>(() => Resolve(Shelter.StationaryVehicle, NoWaiver, null));
    }

    [Fact]
    public void The_two_places_are_the_paragraphs_own_and_closed_and_the_caller_states_which_rather_than_the_engine_picking()
    {
        // § 107.39(b)'s own words, in its own order, and exactly two. There is no third and no
        // "neither of them": a human being under neither is a case § 107.39(b) does not reach, and
        // over-human-beings' note keeps it — "a plain overflight with none of them" is in that
        // entry's evidence, not this one's.
        Assert.Equal(
            new[] { "under a covered structure", "inside a stationary vehicle" },
            Shelter.All.Select(shelter => shelter.Designation));

        Assert.Empty(
            typeof(Shelter).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        // And the engine does not pick one. Stating none refuses, naming the request property, and
        // is an ArgumentException rather than an unresolved result: a missing input is the caller's
        // error, not a gap in the corpus.
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(null, NoWaiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.Equal(nameof(ReasonableProtectionRequest.Shelter), error.ParamName);
        Assert.Contains("reasonable-protection", error.Message, StringComparison.Ordinal);
        Assert.Contains("for each of a covered structure and a stationary vehicle", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Any_attribution_the_caller_gives_is_accepted_and_recorded_because_107_39_names_nobody_who_settles_the_protection()
    {
        // The map's assertedBy for this entry is the marker, not a person: § 107.39 names the subject
        // of the prohibition and the human being protected, and nobody whose determination settles
        // the protection. This entry inherits that behaviour from the shared Assertions, which
        // collision-hazard-proximity changed under docs/decisions/0003-caller-is-not-a-name-to-match.md;
        // these checks are that it was inherited, not a second reading of it.
        Assert.Equal("caller", Assert.Single(EntryPoints.ReasonableProtection.Registered.AssertedBy));

        string[] whoever =
        [
            "remote pilot in command",
            "visual observer",
            "the person manipulating the flight controls",
            "the building's structural engineer",
            "the owner of the stationary vehicle",
            "the human being under the covered structure",
        ];

        foreach (var who in whoever)
        {
            var finding = Finding(Resolve(new Assertion(Entry, Holds: true, who)));

            // Accepted, and *recorded*: an attribution the answer did not carry would record nobody,
            // which is the opposite of what an assertion's attribution is for.
            Assert.Equal(who, finding.AssertedBy);
            Assert.Equal(who, finding.Protection.AssertedBy);
            Assert.Contains(who, finding.ToString(), StringComparison.Ordinal);
            Assert.Equal(Entry.Locator, finding.Authority);
        }
    }

    [Fact]
    public void The_inherited_marker_neither_requires_nor_refuses_the_literal_word_caller_and_does_not_reach_entries_naming_people()
    {
        // The marker is the map's word for "the corpus names nobody". It is neither required of a
        // caller — that would record nobody — nor refused: 0025 does not say to refuse it, and a
        // constraint the decision does not state would be this engine overruling the map.
        var finding = Finding(Resolve(new Assertion(Entry, Holds: false, "caller")));

        Assert.Equal("caller", finding.AssertedBy);
        Assert.False(finding.Holds);

        // And the relaxation is the marker's, reaching only entries carrying it. § 107.31(a) names
        // three people, in the map's own words, and still refuses anybody else — including the marker.
        // That half of the shared mechanism must not have moved for this entry to be implemented.
        Assert.Equal(
            new[] { "remote pilot in command", "visual observer", "person manipulating the flight control" },
            EntryPoints.UnaidedVisualContact.Registered.AssertedBy);

        foreach (var stranger in new[] { "the building's structural engineer", "caller" })
        {
            var error = Assert.Throws<ArgumentException>(
                () => EntryPoints.UnaidedVisualContact.Resolve(
                    new UnaidedVisualContactRequest(
                        RuleRequest.Empty.Assert(
                            MapEntries.UnaidedVisualContact.Id,
                            new Assertion(MapEntries.UnaidedVisualContact, Holds: true, stranger)))
                    {
                        Waiver = WaiverStatement.NoneHeld("§ 107.31", Caller),
                    }));

            Assert.Contains(stranger, error.Message, StringComparison.Ordinal);
            Assert.Contains("remote pilot in command", error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_standard_is_not_resolved_to_a_measure_of_the_structure_or_the_vehicle_and_does_not_borrow_107_39_a_s_decline()
    {
        // § 107.39(a) and (b) are different constituents of one sentence, mapped opposite ways:
        // direct-participation is kind: operation and declines RequiresInterpretation over an
        // undefined term, and this entry is kind: assertion over a fact somebody reports. This entry
        // does not inherit that decline, and with no waiver in force nothing about it is unresolved.
        Assert.Equal("§ 107.39(a)", EntryPoints.DirectParticipation.Registered.Locator.Citation);
        Assert.NotEqual(EntryPoints.DirectParticipation.Registered.Locator, Entry.Locator);
        Assert.Equal(
            UnresolvedReason.RequiresInterpretation,
            Declined(EntryPoints.DirectParticipation.Resolve(
                new DirectParticipationRequest { Waiver = WaiverStatement.NoneHeld("§ 107.39", Caller) })).Reason);

        var resolved = Resolve(new Assertion(Entry, Holds: true, "the operator"));

        Assert.IsType<Resolution<object>.Resolved>(resolved);
        Assert.Null(resolved.Match<UnresolvedResult?>(_ => null, unresolved => unresolved));

        // And the entry demands no measurement of the structure or the vehicle, because the corpus
        // states none. Its inputs are exactly the place the assertion is about — the paragraph's own
        // closed pair, not a measure of either — and the waiver statement its suspendedBy owes.
        var declared = typeof(ReasonableProtectionRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Assertions", "EntryId", "Shelter", "Waiver"], declared);
    }

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible. Because this entry accepts any
        // attribution, the caller owns more of the answer here than on an entry naming people — which
        // makes it the entry where it matters most that the caller owns none of the citation.
        var forged = new MapEntry(
            Entry.Id,
            "It was a sturdy enough carport, honestly",
            new SourceLocator("cfr-14-107", "§ 107.39(a)"))
        { AssertedBy = ["the operator's own say-so"] };

        var finding = Finding(Resolve(new Assertion(forged, Holds: true, "the operator")));

        Assert.Equal(MapEntries.ReasonableProtection.Locator, finding.Authority);
        Assert.Equal(MapEntries.ReasonableProtection.Locator, finding.Protection.Authority);
        Assert.Equal("§ 107.39(b)", finding.Protection.Authority.Citation);
        Assert.Equal(MapEntries.ReasonableProtection.Name, finding.Protection.Entry.Name);
        Assert.Equal("caller", Assert.Single(finding.Protection.Entry.AssertedBy));
        Assert.DoesNotContain("honestly", finding.ToString(), StringComparison.Ordinal);

        // The fact and who is answerable for it still came from the caller, untouched.
        Assert.True(finding.Holds);
        Assert.Equal("the operator", finding.AssertedBy);
    }

    [Fact]
    public void A_value_that_is_not_an_assertion_or_is_about_another_entry_is_refused()
    {
        // A bare true is a fact with nobody behind it. The entry's note requires the fact to be
        // "attributed and recorded with the outcome", so an unattributed value is refused rather than
        // attributed to somebody by the engine.
        var untyped = Assert.Throws<ArgumentException>(() => Resolve(true));

        Assert.Contains(nameof(Assertion), untyped.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, untyped.Message, StringComparison.Ordinal);

        // collision-hazard-proximity carries the same marker as this entry and is not this entry.
        var elsewhere = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.CollisionHazardProximity, Holds: true, "the operator")));

        Assert.Contains("collision-hazard-proximity", elsewhere.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, elsewhere.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        // § 107.205 lists § 107.39 as a whole, at (g). A certificate about § 107.31 does not reach it.
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            Shelter.CoveredStructure,
            WaiverStatement.Held("§ 107.31", Caller),
            new Assertion(Entry, Holds: true, "the operator")));

        Assert.Contains("reasonable-protection", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.31", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.39", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.ReasonableProtection.Resolve(
                new ReasonableProtectionRequest(
                    RuleRequest.Empty.Assert(Entry.Id, new Assertion(Entry, Holds: true, "the operator")))
                {
                    Shelter = Shelter.CoveredStructure,
                }));

        Assert.Equal(nameof(ReasonableProtectionRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_refuses_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, "the operator");

        var finding = Finding(Registry.Resolve(Request(Shelter.StationaryVehicle, NoWaiver, asserted)));

        Assert.True(finding.Holds);
        Assert.Equal("the operator", finding.AssertedBy);
        Assert.Same(Shelter.StationaryVehicle, finding.Shelter);
        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        // Resolved by id, the request is built from the assertions alone, so it carries neither the
        // waiver statement nor the place — and the entry refuses rather than infer either.
        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));
        Assert.Equal(nameof(ReasonableProtectionRequest.Waiver), error.ParamName);
        Assert.Throws<ArgumentException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }
}
