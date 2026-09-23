using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>flash-rate-sufficient</c>, § 107.29(a)(2), (b), resolved through
/// <see cref="EntryPoints.FlashRateSufficient"/> only. The map records the entry as
/// <c>kind: assertion</c> with <c>suspendedBy: ["waivable-regulations"]</c> over a clause that
/// states a standard and no rate, so these pin four things: correspondence row 8 in both directions
/// — what the caller asserts is what the engine answers, and asserting nothing is a demand on the
/// caller rather than an unresolved result; the order of the two caller facts, the waiver gate
/// deciding before the assertion is demanded at all; that no figure is modelled, because the corpus
/// states none; and that this entry's <c>assertedBy</c> is the marker <c>caller</c>, so the
/// attribution is recorded and not matched against a list of people.
/// </summary>
/// <remarks>
/// The marker's reading is <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>, on
/// rules-factory decision 0025's authority, and it is the shared <see cref="Assertions"/>
/// mechanism's — already changed, by the first of the three entries that carry the marker. What is
/// checked here is that this entry inherited it:
/// <see cref="Any_attribution_the_caller_gives_is_accepted_and_recorded_because_107_29_a_and_b_name_nobody"/>
/// and <see cref="The_literal_word_caller_is_an_attribution_like_any_other_and_is_not_refused"/> are
/// that, and
/// <see cref="The_determination_the_remote_pilot_in_command_makes_on_this_clause_belongs_to_the_other_entry"/>
/// is the distinction the entry's note draws against the sibling that shares this locator.
/// </remarks>
public class FlashRateSufficientEntryPointTests
{
    private const string Caller = nameof(FlashRateSufficientEntryPointTests);

    private const string Regulation = "§ 107.29(a)(2) and (b)";

    private static readonly MapEntry Entry = MapEntries.FlashRateSufficient;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld(Regulation, Caller);

    private static FlashRateSufficientRequest Request(WaiverStatement waiver, object? asserted) =>
        new(asserted is null ? RuleRequest.Empty : RuleRequest.Empty.Assert(Entry.Id, asserted))
        {
            Waiver = waiver,
        };

    private static Resolution<object> Resolve(object asserted) => Resolve(NoWaiver, asserted);

    private static Resolution<object> Resolve(WaiverStatement waiver, object? asserted) =>
        EntryPoints.FlashRateSufficient.Resolve(Request(waiver, asserted));

    private static FlashRateSufficientFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<FlashRateSufficientFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void An_assertion_that_the_flash_rate_is_sufficient_resolves_to_what_was_asserted_citing_107_29_a_2_and_b()
    {
        var asserted = new Assertion(Entry, Holds: true, "the operator");

        var finding = Finding(Resolve(asserted));

        Assert.True(finding.Holds);
        Assert.Equal("the operator", finding.AssertedBy);
        Assert.Equal(asserted, finding.Sufficiency);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.29(a)(2), (b)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.FlashRateSufficient.Registered.Locator, finding.Authority);
        Assert.Equal(finding.Authority, finding.Sufficiency.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Fact]
    public void An_assertion_that_the_flash_rate_is_not_sufficient_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        // "Silence" and "no" are different answers. The corpus gives the fact to whoever the caller
        // says determined it, so a report that the flash rate is not sufficient to avoid a collision
        // is an answer the engine records, not a gap it declines and not a fact it may re-decide.
        var asserted = new Assertion(Entry, Holds: false, "the aircraft's manufacturer");

        var finding = Finding(Resolve(asserted));

        Assert.False(finding.Holds);
        Assert.Equal("the aircraft's manufacturer", finding.AssertedBy);
        Assert.Equal(asserted, finding.Sufficiency);
        Assert.Equal("§ 107.29(a)(2), (b)", finding.Authority.Citation);
        Assert.Contains("the aircraft's manufacturer", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(() => Resolve(NoWaiver, null));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline, and the entry's note says the fact is "never defaulted to true
        // when absent": the corpus gave the engine the means to proceed and the caller owes the
        // value. With no waiver in force, nothing about this entry is an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.FlashRateSufficient.Resolve(new FlashRateSufficientRequest { Waiver = NoWaiver }));
    }

    [Fact]
    public void While_a_waiver_of_107_29_a_2_and_b_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_without_demanding_the_assertion()
    {
        var waiver = WaiverStatement.Held(Regulation, Caller, "107W-0000-00000");

        // Nothing is asserted in the first case, and that is the point: the gate decides first, so a
        // suspended entry never demands a fact the waiver has made irrelevant. Were the order the
        // other way round this would throw AssertionRequiredException instead of declining.
        var withNothingAsserted = Declined(Resolve(waiver, null));
        var withAnAssertion = Declined(Resolve(waiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.All(new[] { withNothingAsserted, withAnAssertion }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);

            // Three entries share this locator, so the citation alone cannot say which of them
            // declined (decision 0001's consequence). What was attempted names the entry.
            Assert.Contains("flash-rate-sufficient", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.29(a)(2), (b)", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_assertion_is_demanded_and_answered_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld(Regulation, "remote pilot in command");

        // The other direction of the gate: the entry is reachable, so the second caller fact is now
        // owed — demanded when it is missing, and answered when it is there.
        Assert.Throws<AssertionRequiredException>(() => Resolve(waiver, null));
        var finding = Finding(Resolve(waiver, new Assertion(Entry, Holds: true, "the operator")));

        Assert.True(finding.Holds);
        Assert.Same(waiver, finding.Waiver);
        Assert.Equal("remote pilot in command", finding.Waiver.StatedBy);
        Assert.False(finding.Waiver.InForce);
        Assert.Contains("no certificate of waiver", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Any_attribution_the_caller_gives_is_accepted_and_recorded_because_107_29_a_and_b_name_nobody()
    {
        // The map's assertedBy for this entry is the marker, not a person: § 107.29(a) and (b) name
        // the subject of the prohibition, "no person may operate", and nobody whose determination
        // settles the flash rate. Rules-factory 0025: caller "means that the corpus does not narrow
        // who may assert, so the engine attributes the assertion to whoever the caller says made
        // it". The mechanism is the shared one, already changed by collision-hazard-proximity under
        // docs/decisions/0003; this is the check that this entry inherited it.
        Assert.Equal("caller", Assert.Single(EntryPoints.FlashRateSufficient.Registered.AssertedBy));

        string[] whoever =
        [
            "remote pilot in command",
            "visual observer",
            "the person manipulating the flight controls",
            "the aircraft's manufacturer, in its published specification",
            "the operator's maintenance technician",
            "the FAA inspector who witnessed the flight",
        ];

        foreach (var who in whoever)
        {
            var finding = Finding(Resolve(new Assertion(Entry, Holds: true, who)));

            // Accepted, and *recorded*: an attribution the answer did not carry would record
            // nobody, which is the opposite of what an assertion's attribution is for.
            Assert.Equal(who, finding.AssertedBy);
            Assert.Equal(who, finding.Sufficiency.AssertedBy);
            Assert.Contains(who, finding.ToString(), StringComparison.Ordinal);
            Assert.Equal(Entry.Locator, finding.Authority);
        }
    }

    [Fact]
    public void The_literal_word_caller_is_an_attribution_like_any_other_and_is_not_refused()
    {
        // The marker is the map's word for "the corpus names nobody". It is neither required of a
        // caller — that would record nobody — nor refused: 0025 does not say to refuse it, and a
        // constraint the decision does not state would be this engine overruling the map.
        var finding = Finding(Resolve(new Assertion(Entry, Holds: false, "caller")));

        Assert.Equal("caller", finding.AssertedBy);
        Assert.False(finding.Holds);
    }

    [Fact]
    public void The_determination_the_remote_pilot_in_command_makes_on_this_clause_belongs_to_the_other_entry()
    {
        // The entry's note draws this line: § 107.29(a) and (b) name nobody whose determination
        // settles the flash rate, and "the remote pilot in command they name determines only whether
        // to reduce the lighting's intensity, which is intensity-reduction-in-interest-of-safety".
        // The two entries share this locator and are different questions, which is why one's
        // assertedBy is the marker and the other's is that person.
        Assert.Equal(Entry.Locator, MapEntries.IntensityReductionInInterestOfSafety.Locator);
        Assert.NotEqual(Entry.Id, MapEntries.IntensityReductionInInterestOfSafety.Id);
        Assert.Equal("caller", Assert.Single(Entry.AssertedBy));
        Assert.Equal(
            "remote pilot in command",
            Assert.Single(MapEntries.IntensityReductionInInterestOfSafety.AssertedBy));

        // So an answer to this entry is an answer about the flash rate, under this entry's own id
        // and name, whoever the caller attributes it to — the shared locator cannot tell the two
        // apart, and the entry the engine answered is what does.
        var finding = Finding(Resolve(new Assertion(Entry, Holds: true, "remote pilot in command")));

        Assert.Equal(Entry.Id, finding.Sufficiency.Entry.Id);
        Assert.Equal(Entry.Name, finding.Sufficiency.Entry.Name);
        Assert.Contains("flash rate", finding.Sufficiency.Entry.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("intensity", finding.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_assertion_about_another_entry_is_refused()
    {
        // intensity-reduction-in-interest-of-safety is the other assertion stated by this clause and
        // carries this entry's locator; collision-hazard-proximity carries the same caller marker.
        // Neither is this entry.
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(
                MapEntries.IntensityReductionInInterestOfSafety,
                Holds: true,
                "remote pilot in command")));

        Assert.Contains("intensity-reduction-in-interest-of-safety", error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);

        var marked = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.CollisionHazardProximity, Holds: true, "the operator")));

        Assert.Contains("collision-hazard-proximity", marked.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_that_is_not_an_assertion_is_refused()
    {
        // A bare true is a fact with nobody behind it. The entry's note requires the fact to be
        // "asserted and withheld, attributed and recorded with the outcome", so an unattributed
        // value is refused rather than attributed to somebody by the engine.
        var error = Assert.Throws<ArgumentException>(() => Resolve(true));

        Assert.Contains(nameof(Assertion), error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible. Because this entry accepts any
        // attribution, the caller owns more of the answer here than on an entry naming people —
        // which makes it the more important that the caller owns none of the citation.
        var forged = new MapEntry(
            Entry.Id,
            "Reducing the intensity was in the interest of safety, obviously",
            new SourceLocator("cfr-14-107", "§ 107.29(d)"))
        { AssertedBy = ["the operator's own say-so"] };

        var finding = Finding(Resolve(new Assertion(forged, Holds: true, "the operator")));

        Assert.Equal(MapEntries.FlashRateSufficient.Locator, finding.Authority);
        Assert.Equal(MapEntries.FlashRateSufficient.Locator, finding.Sufficiency.Authority);
        Assert.Equal("§ 107.29(a)(2), (b)", finding.Sufficiency.Authority.Citation);
        Assert.Equal(MapEntries.FlashRateSufficient.Name, finding.Sufficiency.Entry.Name);
        Assert.Equal("caller", Assert.Single(finding.Sufficiency.Entry.AssertedBy));
        Assert.DoesNotContain("obviously", finding.ToString(), StringComparison.Ordinal);

        // The fact and who is answerable for it still came from the caller, untouched.
        Assert.True(finding.Holds);
        Assert.Equal("the operator", finding.AssertedBy);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        // § 107.205(b) designates this one "Section 107.29(a)(2) and (b)", and that is what a
        // statement must be about. A statement about § 107.29 whole is about a regulation the list
        // does not carry, and so is one about the paragraph the entry's locator cites.
        var wholeSection = Assert.Throws<ArgumentException>(() => Resolve(
            WaiverStatement.Held("§ 107.29", Caller),
            new Assertion(Entry, Holds: true, "the operator")));

        Assert.Contains("flash-rate-sufficient", wholeSection.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.29", wholeSection.Message, StringComparison.Ordinal);

        var anotherSection = Assert.Throws<ArgumentException>(() => Resolve(
            WaiverStatement.Held("§ 107.31", Caller),
            new Assertion(Entry, Holds: true, "the operator")));

        Assert.Contains("§ 107.31", anotherSection.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.FlashRateSufficient.Resolve(
                FlashRateSufficientRequest.Asserting(new Assertion(Entry, Holds: true, "the operator"))));

        Assert.Equal(nameof(FlashRateSufficientRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_refuses_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, "the operator");

        var finding = Finding(Registry.Resolve(Request(NoWaiver, asserted)));

        Assert.True(finding.Holds);
        Assert.Equal("the operator", finding.AssertedBy);
        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        // Resolved by id, the request is built from the assertions alone, so it carries no waiver
        // statement — and a suspended entry refuses rather than infer one, in either direction.
        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));
        Assert.Equal(nameof(FlashRateSufficientRequest.Waiver), error.ParamName);
        Assert.Throws<ArgumentException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }

    [Fact]
    public void No_flash_rate_is_modelled_because_the_corpus_states_none()
    {
        // The entry's note: "the corpus states no rate, so no figure is evidenced". A flash rate, a
        // period or a duty cycle on this request would be surface for a comparison this engine has
        // no figure to make — its own rule standing in for § 107.29(a)(2) and (b)'s standard. The
        // only input is the waiver statement, which is not about the lighting at all.
        var declared = typeof(FlashRateSufficientRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Assertions", "EntryId", "Waiver"], declared);

        // And the one figure this clause states plainly — the 3 statute miles the lighting must be
        // visible for — is anti-collision-lighting's, the entry that depends on this one, and is not
        // answered here in any form.
        var finding = Finding(Resolve(new Assertion(Entry, Holds: true, "the operator")));

        Assert.DoesNotContain("3 statute miles", finding.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("statute", finding.Sufficiency.Entry.Name, StringComparison.OrdinalIgnoreCase);
    }
}
