using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>anti-collision-lighting</c>, § 107.29(a)(2), (b), resolved through
/// <see cref="EntryPoints.AntiCollisionLighting"/> only: the requirement the clause states, on the
/// situations the entry's note asks for — "Fitted and unfitted, for night and for civil twilight;
/// the 3 statute mile visibility figure the corpus states plainly; and the bound the same clause
/// states — the intensity may be reduced but the lighting may not be extinguished".
/// </summary>
/// <remarks>
/// <para>
/// The entry is a composite over the two assertion entries on its own locator, so what each of them
/// answers is asked for here through the caller's assertions and never restated: the flash rate is
/// <c>flash-rate-sufficient</c>'s and the determination about reducing the intensity is
/// <c>intensity-reduction-in-interest-of-safety</c>'s. What belongs to this entry is the printed 3
/// statute miles, and the tests below pin it at the figure and either side of it.
/// </para>
/// <para>
/// All three entries carry the locator § 107.29(a)(2), (b), so a citation cannot say which of them
/// answered or which of them declined —
/// <see cref="The_three_entries_on_this_locator_are_told_apart_by_what_was_attempted_and_not_by_the_citation"/>
/// pins that in both directions, as <c>flash-rate-sufficient</c>'s own tests do against its sibling.
/// One case is not reachable and cannot be made so by writing a different test: this entry's waiver
/// gate and both constituents' are the same gate on the same statement, read here first, so once it
/// has passed neither constituent can decline. The decline this entry would emit for a constituent
/// it could not resolve is written and documented in <see cref="Lights"/>, and what is pinned below
/// instead is the reason it cannot be reached — the gate answers under this entry's own name, and
/// the verdict follows what the constituents actually returned.
/// </para>
/// </remarks>
public class AntiCollisionLightingEntryPointTests
{
    private const string Caller = nameof(AntiCollisionLightingEntryPointTests);

    private const string Regulation = "§ 107.29(a)(2) and (b)";

    private static readonly MapEntry Entry = MapEntries.AntiCollisionLighting;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld(Regulation, Caller);

    private static Assertion Rate(bool holds) => new(MapEntries.FlashRateSufficient, holds, "the operator");

    private static Assertion Safety(bool holds) =>
        new(MapEntries.IntensityReductionInInterestOfSafety, holds, "remote pilot in command");

    private static RuleRequest Asserting(Assertion? rate, Assertion? safety)
    {
        var assertions = RuleRequest.Empty;
        if (rate is not null)
        {
            assertions = assertions.Assert(MapEntries.FlashRateSufficient.Id, rate);
        }

        return safety is null
            ? assertions
            : assertions.Assert(MapEntries.IntensityReductionInInterestOfSafety.Id, safety);
    }

    private static Resolution<object> Resolve(
        LightingStatement lighting,
        Assertion? rate = null,
        Assertion? safety = null,
        WaiverStatement? waiver = null) =>
        EntryPoints.AntiCollisionLighting.Resolve(new AntiCollisionLightingRequest(Asserting(rate, safety))
        {
            Lighting = lighting,
            Waiver = waiver ?? NoWaiver,
        });

    private static AntiCollisionLightingFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<AntiCollisionLightingFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Lighted_anti_collision_lighting_visible_for_at_least_3_statute_miles_with_a_sufficient_flash_rate_meets_the_clause()
    {
        var finding = Finding(Resolve(LightingStatement.LightedAndVisibleFor(5m, Caller), rate: Rate(true)));

        Assert.True(finding.Met);
        Assert.True(finding.Fitted);
        Assert.True(finding.Lighted);
        Assert.True(finding.VisibleFarEnough);
        Assert.True(finding.FlashRateSufficient);
        Assert.True(finding.WithinTheBound);

        // Nothing was reduced, so the second sentence's determination was not reached. Null there
        // records that the question was not asked, and is not an answer of "no".
        Assert.Null(finding.Reduction);
        Assert.Null(finding.ReductionInInterestOfSafety);

        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.29(a)(2), (b)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.AntiCollisionLighting.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
        Assert.Contains($"as stated by {Caller}", finding.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2.9, false)]
    [InlineData(2.99, false)]
    [InlineData(3, true)]
    [InlineData(3.01, true)]
    [InlineData(10, true)]
    public void At_3_statute_miles_it_is_met_and_below_it_is_not_because_the_corpus_prints_at_least_3(
        double statuteMiles,
        bool farEnough)
    {
        // "visible for at least 3 statute miles": met at the figure, broken below it. Every other
        // conjunct is as the clause requires here, so the distance alone decides the case.
        var finding = Finding(Resolve(
            LightingStatement.LightedAndVisibleFor((decimal)statuteMiles, Caller),
            rate: Rate(true)));

        Assert.Equal(farEnough, finding.VisibleFarEnough);
        Assert.Equal(farEnough, finding.Met);
        Assert.True(finding.Lighted);
        Assert.True(finding.WithinTheBound);
    }

    [Fact]
    public void The_3_statute_mile_figure_is_this_entrys_own_as_printed()
    {
        var finding = Finding(Resolve(LightingStatement.LightedAndVisibleFor(3m, Caller), rate: Rate(true)));

        Assert.Equal(3m, finding.VisibleForAtLeastStatuteMiles);
        Assert.Contains("at least 3 statute miles", finding.ToString(), StringComparison.Ordinal);

        // The figure is printed in this entry's own evidence, and the answer cites this entry.
        Assert.Equal(Entry.Locator, finding.Authority);

        // § 107.51(c)'s minimum flight visibility is also 3 statute miles. It is another entry's
        // figure, stated by another section about another quantity, and nothing here reads it.
        Assert.Equal("§ 107.51(c)", MapEntries.VisibilityMinimum.Locator.Citation);
        Assert.NotEqual(MapEntries.VisibilityMinimum.Locator, finding.Authority);
    }

    [Fact]
    public void An_aircraft_with_no_anti_collision_lighting_does_not_meet_it_and_no_fact_about_lighting_it_lacks_is_demanded()
    {
        // Nothing is asserted, and that is the case: "has lighted anti-collision lighting" is false,
        // so the conjunction is settled whatever the flash rate would have been, and a fact about
        // the flash rate of lighting the aircraft does not have is not demanded of the caller.
        var finding = Finding(Resolve(LightingStatement.NoneFitted(Caller)));

        Assert.False(finding.Met);
        Assert.False(finding.Fitted);
        Assert.False(finding.Lighted);
        Assert.Null(finding.VisibleFarEnough);
        Assert.Null(finding.FlashRate);
        Assert.Null(finding.FlashRateSufficient);
        Assert.Null(finding.Reduction);
        Assert.Contains("has no anti-collision lighting", finding.ToString(), StringComparison.Ordinal);
        Assert.Equal(Entry.Locator, finding.Authority);
    }

    [Fact]
    public void Extinguished_lighting_does_not_meet_it_whatever_the_remote_pilot_in_command_determined()
    {
        // "may reduce the intensity of, but may not extinguish": extinguished lighting is outside
        // the bound however the determination went, so the determination is not asked — and where
        // the caller supplies it anyway it changes nothing.
        var determined = Finding(Resolve(LightingStatement.FittedButExtinguished(Caller), safety: Safety(true)));
        var silent = Finding(Resolve(LightingStatement.FittedButExtinguished(Caller)));

        Assert.All(
            new[] { determined, silent },
            finding =>
            {
                Assert.True(finding.Fitted);
                Assert.False(finding.Lighted);
                Assert.False(finding.WithinTheBound);
                Assert.False(finding.Met);
                Assert.Null(finding.VisibleFarEnough);
                Assert.Null(finding.FlashRate);
                Assert.Null(finding.Reduction);
                Assert.Contains("has been extinguished", finding.ToString(), StringComparison.Ordinal);
            });

        Assert.Equal(determined, silent);
    }

    [Fact]
    public void A_reduced_intensity_the_remote_pilot_in_command_determined_to_be_in_the_interest_of_safety_is_within_the_bound()
    {
        var finding = Finding(Resolve(
            LightingStatement.IntensityReducedAndVisibleFor(4m, Caller),
            rate: Rate(true),
            safety: Safety(true)));

        Assert.True(finding.WithinTheBound);
        Assert.True(finding.Met);
        Assert.True(finding.ReductionInInterestOfSafety);
        Assert.NotNull(finding.Reduction);
        Assert.Equal("remote pilot in command", finding.Reduction.AssertedBy);
        Assert.Contains("its intensity reduced", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_reduction_without_that_determination_is_outside_the_bound_and_does_not_meet_the_clause()
    {
        // The clause permits the reduction "if he or she determines that … it would be in the
        // interest of safety to do so", and the determination is the map entry this one depends on.
        // Reported as not made, the reduction is not the one the sentence permits.
        var finding = Finding(Resolve(
            LightingStatement.IntensityReducedAndVisibleFor(4m, Caller),
            rate: Rate(true),
            safety: Safety(false)));

        Assert.False(finding.WithinTheBound);
        Assert.False(finding.Met);
        Assert.False(finding.ReductionInInterestOfSafety);

        // Everything else about the lighting is as the clause requires, so the bound alone decides
        // it: this is not a lighting failure reported twice.
        Assert.True(finding.Lighted);
        Assert.True(finding.VisibleFarEnough);
        Assert.True(finding.FlashRateSufficient);
    }

    [Fact]
    public void The_determination_is_asked_only_where_the_intensity_was_reduced()
    {
        // Nothing reduced: the second sentence's condition is not in play, so the caller owes
        // nothing for it and the entry resolves without it.
        var unreduced = Finding(Resolve(LightingStatement.LightedAndVisibleFor(5m, Caller), rate: Rate(true)));

        Assert.True(unreduced.Met);
        Assert.Null(unreduced.Reduction);

        // Reduced: it is owed, and asserting nothing is a demand on the caller rather than a
        // decline — correspondence row 8, through the entry that states it.
        var error = Assert.Throws<AssertionRequiredException>(
            () => Resolve(LightingStatement.IntensityReducedAndVisibleFor(5m, Caller), rate: Rate(true)));

        Assert.Equal(MapEntries.IntensityReductionInInterestOfSafety.Id, error.EntryId);
    }

    [Fact]
    public void The_constituents_answers_are_their_own_entries_and_the_verdict_follows_what_they_returned()
    {
        var finding = Finding(Resolve(
            LightingStatement.IntensityReducedAndVisibleFor(5m, Caller),
            rate: Rate(true),
            safety: Safety(true)));

        var rate = Assert.IsType<FlashRateSufficientFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.FlashRateSufficient.Resolve(
                new FlashRateSufficientRequest(Asserting(Rate(true), null)) { Waiver = NoWaiver })).Value);
        var reduction = Assert.IsType<IntensityReductionFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
                new IntensityReductionInInterestOfSafetyRequest(Asserting(null, Safety(true)))
                {
                    Waiver = NoWaiver,
                })).Value);

        // What each constituent answered here is what that entry answers through its own entry
        // point, built on its own map entry and citing its own locator.
        Assert.Equal(rate, finding.FlashRate);
        Assert.Equal(reduction, finding.Reduction);
        Assert.Equal(MapEntries.FlashRateSufficient.Name, finding.FlashRate!.Sufficiency.Entry.Name);
        Assert.Equal(
            MapEntries.IntensityReductionInInterestOfSafety.Name,
            finding.Reduction!.Determination.Entry.Name);

        // And the verdict is read off what they returned, not from anything recorded here about
        // what they can return: change one answer and this entry's changes with it.
        Assert.True(finding.Met);
        Assert.False(Finding(Resolve(
            LightingStatement.IntensityReducedAndVisibleFor(5m, Caller),
            rate: Rate(false),
            safety: Safety(true))).Met);
    }

    [Fact]
    public void While_a_waiver_of_107_29_a_2_and_b_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_naming_this_entry()
    {
        var waiver = WaiverStatement.Held(Regulation, Caller, "107W-0000-00000");

        // Nothing is asserted in the first case, and that is the point: this entry's own gate is
        // read before any constituent is asked, so a suspended entry demands no fact a waiver has
        // made beside the point. Were a constituent asked first, this would throw instead.
        var withNothingAsserted = Declined(Resolve(
            LightingStatement.IntensityReducedAndVisibleFor(5m, Caller),
            waiver: waiver));
        var withBoth = Declined(Resolve(
            LightingStatement.IntensityReducedAndVisibleFor(5m, Caller),
            rate: Rate(true),
            safety: Safety(true),
            waiver: waiver));

        Assert.All(new[] { withNothingAsserted, withBoth }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
            Assert.Contains("anti-collision-lighting", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.29(a)(2), (b)", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        });

        Assert.Equal(withNothingAsserted, withBoth);
    }

    [Fact]
    public void The_three_entries_on_this_locator_are_told_apart_by_what_was_attempted_and_not_by_the_citation()
    {
        var waiver = WaiverStatement.Held(Regulation, Caller, "107W-0000-00000");

        var mine = Declined(Resolve(LightingStatement.LightedAndVisibleFor(5m, Caller), waiver: waiver));
        var rate = Declined(EntryPoints.FlashRateSufficient.Resolve(
            new FlashRateSufficientRequest(RuleRequest.Empty) { Waiver = waiver }));
        var reduction = Declined(EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
            new IntensityReductionInInterestOfSafetyRequest(RuleRequest.Empty) { Waiver = waiver }));

        // One locator for all three entries, and one gate, so neither the entries' citation nor the
        // declines' says which of the three was asked.
        Assert.Equal(Entry.Locator, MapEntries.FlashRateSufficient.Locator);
        Assert.Equal(Entry.Locator, MapEntries.IntensityReductionInInterestOfSafety.Locator);
        Assert.Equal(mine.Locator, rate.Locator);
        Assert.Equal(mine.Locator, reduction.Locator);
        Assert.Equal(mine.Reason, rate.Reason);
        Assert.Equal(mine.Reason, reduction.Reason);

        // What was attempted is what does, in both directions: this entry's decline names this
        // entry and neither constituent, and each constituent's names itself and not this entry.
        Assert.Contains("anti-collision-lighting", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("flash-rate-sufficient", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "intensity-reduction-in-interest-of-safety",
            mine.Attempted,
            StringComparison.Ordinal);

        Assert.Contains("flash-rate-sufficient", rate.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("anti-collision-lighting", rate.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            "intensity-reduction-in-interest-of-safety",
            reduction.Attempted,
            StringComparison.Ordinal);
        Assert.DoesNotContain("anti-collision-lighting", reduction.Attempted, StringComparison.Ordinal);

        Assert.NotEqual(rate.Attempted, mine.Attempted);
        Assert.NotEqual(reduction.Attempted, mine.Attempted);

        // The resolved side shares the blind spot, and is told apart the same way: the answers cite
        // one locator between them, and what distinguishes them is the entry each is built on.
        var finding = Finding(Resolve(LightingStatement.LightedAndVisibleFor(5m, Caller), rate: Rate(true)));

        Assert.Equal(finding.Authority, finding.FlashRate!.Authority);
        Assert.Equal(Entry.Name, MapEntries.AntiCollisionLighting.Name);
        Assert.NotEqual(Entry.Name, MapEntries.FlashRateSufficient.Name);
        Assert.Contains(MapEntries.FlashRateSufficient.Name, finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_clause_is_stated_twice_for_night_and_for_civil_twilight_and_this_entry_answers_one_requirement()
    {
        // § 107.29(a)(2) states the requirement for operation at night and § 107.29(b) states it,
        // word for word, for periods of civil twilight. The entry's locator cites both, and which
        // period an operation is in is not this entry's question: it is night-operation's
        // (§ 107.29(a)) and civil-twilight-operation's (§ 107.29(b)-(c)), and both of those depend
        // on this entry for the lighting.
        Assert.Equal("§ 107.29(a)(2), (b)", Entry.Locator.Citation);
        Assert.Equal("§ 107.29(a)", MapEntries.NightOperation.Locator.Citation);
        Assert.Equal("§ 107.29(b)-(c)", MapEntries.CivilTwilightOperation.Locator.Citation);

        // So there is no period among this entry's inputs — nor any flash rate or intensity, which
        // the corpus states no figure for.
        var declared = typeof(AntiCollisionLightingRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Assertions", "EntryId", "Lighting", "Waiver"], declared);

        // And one answer serves both paragraphs: the same facts are answered the same way, because
        // there is nothing to say about which of them is being operated under.
        var finding = Finding(Resolve(LightingStatement.LightedAndVisibleFor(5m, Caller), rate: Rate(true)));

        Assert.True(finding.Met);
        Assert.Equal(Entry.Locator, finding.Authority);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        // § 107.205(b) designates this one "Section 107.29(a)(2) and (b)", and that is what a
        // statement must be about. A statement about § 107.29 whole is about a regulation the list
        // does not carry, and so is one about another section.
        var wholeSection = Assert.Throws<ArgumentException>(() => Resolve(
            LightingStatement.LightedAndVisibleFor(5m, Caller),
            rate: Rate(true),
            waiver: WaiverStatement.Held("§ 107.29", Caller)));

        Assert.Contains("anti-collision-lighting", wholeSection.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.29", wholeSection.Message, StringComparison.Ordinal);

        var anotherSection = Assert.Throws<ArgumentException>(() => Resolve(
            LightingStatement.LightedAndVisibleFor(5m, Caller),
            rate: Rate(true),
            waiver: WaiverStatement.Held("§ 107.51", Caller)));

        Assert.Contains("§ 107.51", anotherSection.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_the_facts_the_clause_turns_on_it_refuses_rather_than_assume_them_including_through_the_dictionary_dispatch()
    {
        var noLighting = Assert.Throws<ArgumentException>(
            () => EntryPoints.AntiCollisionLighting.Resolve(
                new AntiCollisionLightingRequest(Asserting(Rate(true), null)) { Waiver = NoWaiver }));

        Assert.Equal(nameof(AntiCollisionLightingRequest.Lighting), noLighting.ParamName);

        var noWaiver = Assert.Throws<ArgumentException>(
            () => EntryPoints.AntiCollisionLighting.Resolve(
                new AntiCollisionLightingRequest(Asserting(Rate(true), null))
                {
                    Lighting = LightingStatement.LightedAndVisibleFor(5m, Caller),
                }));

        Assert.Equal(nameof(AntiCollisionLightingRequest.Waiver), noWaiver.ParamName);

        // Resolved by id, the request is built from the assertions alone and carries neither input,
        // so the dictionary dispatch refuses rather than answering from defaults — an aircraft
        // nobody described is not an aircraft with no lighting.
        Assert.Throws<ArgumentException>(() => Registry.Resolve(
            Entry.Id,
            RuleRequest.Empty.Assert(MapEntries.FlashRateSufficient.Id, Rate(true))));
        Assert.Throws<ArgumentException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }
}
