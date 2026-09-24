using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>collision-hazard-proximity</c>, § 107.37(b), resolved through
/// <see cref="EntryPoints.CollisionHazardProximity"/> only. The map records the entry as
/// <c>kind: assertion</c> over a sentence that states a standard rather than a distance, so these
/// pin correspondence row 8 on it — what the caller asserts is what the engine answers, in either
/// direction, and asserting nothing is a demand on the caller rather than an unresolved result —
/// and the one thing this entry decides that the seven remote-pilot entries do not: its
/// <c>assertedBy</c> is the marker <c>caller</c>, so the attribution is recorded and not matched
/// against a list of people.
/// </summary>
/// <remarks>
/// The reading is <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>, on rules-factory
/// decision 0025's authority. Both halves of it are pinned here, because the change that admits
/// any attribution for this entry lives in the shared <see cref="Assertions"/> and must not reach
/// the entries whose lists are people:
/// <see cref="An_entry_whose_assertedBy_names_people_still_refuses_an_attribution_it_does_not_name"/>
/// is that side of it.
/// </remarks>
public class CollisionHazardProximityEntryPointTests
{
    private static readonly MapEntry Entry = MapEntries.CollisionHazardProximity;

    private static Resolution<object> Resolve(object asserted) =>
        EntryPoints.CollisionHazardProximity.Resolve(CollisionHazardProximityRequest.Asserting(asserted));

    [Fact]
    public void An_assertion_that_the_aircraft_was_operated_that_close_resolves_to_what_was_asserted_citing_107_37_b()
    {
        var asserted = new Assertion(Entry, Holds: true, "the remote pilot in command, after the fact");

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal("the remote pilot in command, after the fact", assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Equal("cfr-14-107", assertion.Authority.SourceId);
        Assert.Equal("§ 107.37(b)", assertion.Authority.Citation);
        Assert.Equal(EntryPoints.CollisionHazardProximity.Registered.Locator, assertion.Authority);
    }

    [Fact]
    public void An_assertion_that_it_was_not_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        var asserted = new Assertion(Entry, Holds: false, "the operator");

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.False(assertion.Holds);
        Assert.Equal("the operator", assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Contains("the operator", assertion.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.CollisionHazardProximity.Resolve(CollisionHazardProximityRequest.Empty));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline, and the entry's note says the fact is "never defaulted to true
        // when absent". Silence and "no" are different answers, and neither is an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.CollisionHazardProximity.Resolve(new CollisionHazardProximityRequest()));
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_demands_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, "the pilot of the other aircraft");

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal("the pilot of the other aircraft", assertion.AssertedBy);
        Assert.Equal(Entry.Locator, assertion.Authority);
        Assert.Equal(asserted, assertion);
        Assert.Throws<AssertionRequiredException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }

    [Fact]
    public void Any_attribution_the_caller_gives_is_accepted_and_recorded_because_107_37_b_names_nobody()
    {
        // The map's assertedBy for this entry is the marker, not a person: § 107.37(b) names the
        // subject of a prohibition, "No person", and nobody whose determination settles the hazard.
        // Rules-factory 0025: caller "means that the corpus does not narrow who may assert, so the
        // engine attributes the assertion to whoever the caller says made it".
        Assert.Equal("caller", Assert.Single(EntryPoints.CollisionHazardProximity.Registered.AssertedBy));

        string[] whoever =
        [
            "remote pilot in command",
            "visual observer",
            "the person manipulating the flight controls",
            "the operator's safety officer",
            "N123AB's pilot",
            "the FAA inspector who witnessed it",
        ];

        foreach (var who in whoever)
        {
            var resolved = Assert.IsType<Resolution<object>.Resolved>(
                Resolve(new Assertion(Entry, Holds: true, who)));

            var assertion = Assert.IsType<Assertion>(resolved.Value);

            // Accepted, and *recorded*: an attribution the answer did not carry would record
            // nobody, which is the opposite of what an assertion's attribution is for.
            Assert.Equal(who, assertion.AssertedBy);
            Assert.Contains(who, assertion.ToString(), StringComparison.Ordinal);
            Assert.Equal(Entry.Locator, assertion.Authority);
        }
    }

    [Fact]
    public void The_literal_word_caller_is_an_attribution_like_any_other_and_is_not_refused()
    {
        // The marker is the map's word for "the corpus names nobody". It is neither required of a
        // caller — that would record nobody — nor refused: 0025 does not say to refuse it, and a
        // constraint the decision does not state would be this engine overruling the map.
        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Resolve(new Assertion(Entry, Holds: false, "caller")));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Equal("caller", assertion.AssertedBy);
        Assert.False(assertion.Holds);
    }

    /// <summary>
    /// A <c>sufficient-available-power</c> request that states § 107.49(d)'s own condition, so the
    /// attribution check is what the entry refuses on rather than the condition being unstated.
    /// </summary>
    private static SufficientAvailablePowerRequest PoweredAndAsserting(Assertion assertion) =>
        new(RuleRequest.Empty.Assert(MapEntries.SufficientAvailablePower.Id, assertion))
        {
            Power = AircraftPower.Powered,
        };

    [Fact]
    public void An_entry_whose_assertedBy_names_people_still_refuses_an_attribution_it_does_not_name()
    {
        // The relaxation is the marker's, and reaches only entries carrying it. Seven assertion
        // entries name people, and their strings are exact and not normalized. This is the half of
        // the shared change that must not move, checked on the entry that established the rule.
        Assert.Equal(
            "remote pilot in command",
            Assert.Single(EntryPoints.SufficientAvailablePower.Registered.AssertedBy));

        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.SufficientAvailablePower.Resolve(
                PoweredAndAsserting(new Assertion(MapEntries.SufficientAvailablePower, Holds: true, "visual observer"))));

        Assert.Contains("remote pilot in command", error.Message, StringComparison.Ordinal);
        Assert.Contains("visual observer", error.Message, StringComparison.Ordinal);

        // And the marker is not a skeleton key: it is refused there like any other name the list
        // does not carry.
        var marker = Assert.Throws<ArgumentException>(
            () => EntryPoints.SufficientAvailablePower.Resolve(
                PoweredAndAsserting(new Assertion(MapEntries.SufficientAvailablePower, Holds: true, "caller"))));

        Assert.Contains("remote pilot in command", marker.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_assertion_about_another_entry_is_refused()
    {
        // well-clear is the other constituent of § 107.37 and is not an assertion at all;
        // reasonable-protection carries the same marker as this entry. Neither is this entry.
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.ReasonableProtection, Holds: true, "the operator")));

        Assert.Contains("reasonable-protection", error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_that_is_not_an_assertion_is_refused()
    {
        // A bare true is a fact with nobody behind it. The entry's note requires the fact to be
        // "attributed and recorded with the outcome", so an unattributed value is refused rather
        // than attributed to somebody by the engine.
        var error = Assert.Throws<ArgumentException>(() => Resolve(true));

        Assert.Contains(nameof(Assertion), error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible. Because this entry accepts any
        // attribution, the caller owns more of the answer here than anywhere else — which makes it
        // the entry where it matters most that the caller owns none of the citation.
        var forged = new MapEntry(
            Entry.Id,
            "Nowhere near it, honestly",
            new SourceLocator("cfr-14-107", "§ 107.37(a)"))
        { AssertedBy = ["the pilot's own say-so"] };

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Resolve(new Assertion(forged, Holds: true, "the operator")));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Equal(MapEntries.CollisionHazardProximity.Locator, assertion.Authority);
        Assert.Equal("§ 107.37(b)", assertion.Authority.Citation);
        Assert.Equal(MapEntries.CollisionHazardProximity.Name, assertion.Entry.Name);
        Assert.Equal("caller", Assert.Single(assertion.Entry.AssertedBy));
        Assert.DoesNotContain("honestly", assertion.ToString(), StringComparison.Ordinal);

        // The fact and who is answerable for it still came from the caller, untouched.
        Assert.True(assertion.Holds);
        Assert.Equal("the operator", assertion.AssertedBy);
    }

    [Fact]
    public void The_standard_is_not_resolved_to_a_distance_and_does_not_borrow_well_clears_decline()
    {
        // § 107.37(a) and (b) are different constituents, mapped differently: well-clear is
        // kind: operation and declines RequiresInterpretation over an undefined term, and this
        // entry is kind: assertion over a fact somebody reports. This entry does not inherit that
        // decline, and nothing about it is ever unresolved.
        Assert.Equal("§ 107.37(a)", EntryPoints.WellClear.Registered.Locator.Citation);
        Assert.NotEqual(EntryPoints.WellClear.Registered.Locator, Entry.Locator);

        var resolved = Resolve(new Assertion(Entry, Holds: true, "the operator"));

        Assert.IsType<Resolution<object>.Resolved>(resolved);
        Assert.Null(resolved.Match<UnresolvedResult?>(_ => null, unresolved => unresolved));

        // And the entry demands no measurement, because the corpus states none. The request type
        // declares no inputs of its own: a separation, a closing rate or a hazard threshold would
        // be this engine's rule standing in for § 107.37(b)'s.
        var declared = typeof(CollisionHazardProximityRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Assertions", "EntryId"], declared);
    }
}
