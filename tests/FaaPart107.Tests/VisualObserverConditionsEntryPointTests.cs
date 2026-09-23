using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>visual-observer-conditions</c>, § 107.33, resolved through
/// <see cref="EntryPoints.VisualObserverConditions"/> only.
/// </summary>
/// <remarks>
/// <para>
/// The entry states the section's chapeau, paragraph (b), and the conjunction of the three
/// paragraphs; (a) and (c) are their own entries. So these pin, first, the two things no other
/// entry carries. The chapeau is a <b>condition</b>: an operation in which no visual observer is
/// used is one § 107.33 states no requirement about, which is neither "the requirements are met"
/// nor "they are not", and the constituents are not asked at all. And § 107.33(b) is this entry's
/// own requirement — no map entry carries its locator — answered from § 107.31(a)'s ability as
/// <c>unaided-visual-contact</c> asserted it and <c>visual-line-of-sight</c> carries it.
/// </para>
/// <para>
/// Then the composite's shape (#78): every requirement is asked and the answer follows what each
/// one returned, and where an unanswered requirement decides the outcome the decline emitted is
/// this entry's own, naming both entries and citing the blocking entry's locator. That citation is
/// <c>effective-communication</c>'s § 107.33(a), and the reason is its <c>RequiresInterpretation</c>
/// — so this entry's decline and that entry's agree on both, and only what was attempted tells them
/// apart. Both halves are asserted below.
/// </para>
/// <para>
/// One case cannot be reached and it is not reachable by writing a different test. "All the
/// requirements met" needs § 107.33(a) to resolve a requirement met, and
/// <c>effective-communication</c> resolves nothing at all: part 107 does not define "effective".
/// The nearest situation is every requirement this engine resolves met with (a) undetermined, and
/// the answer there is a decline, pinned below. What this entry must not do is invent the missing
/// half, and what it must not do either is assert that the half is missing:
/// <see cref="VisualObserverConditionsFinding.AllRequirementsMet"/> is computed from what the
/// entries answered at runtime, so if (a) is ever settled this entry follows it.
/// </para>
/// </remarks>
public class VisualObserverConditionsEntryPointTests
{
    private const string Caller = nameof(VisualObserverConditionsEntryPointTests);

    private const string RemotePilotInCommand = "remote pilot in command";

    /// <summary>§ 107.205(d): the statement this entry, § 107.33(a) and § 107.33(c) are all asked under.</summary>
    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.33", Caller);

    /// <summary>§ 107.205(c): the separate statement <c>visual-line-of-sight</c> is asked under, for § 107.33(b).</summary>
    private static readonly WaiverStatement NoSightWaiver = WaiverStatement.NoneHeld("§ 107.31", Caller);

    /// <summary>§ 107.31(b)(2): a visual observer exercised the ability throughout the entire flight.</summary>
    private static readonly ExerciseOfTheAbility RowTwo =
        new(RemotePilotInCommand: false, PersonManipulatingTheFlightControls: false, VisualObserver: true);

    /// <summary>
    /// What the caller asserts for the two assertion entries this section reaches: § 107.31(a)'s
    /// ability, under <c>unaided-visual-contact</c>'s own id, and § 107.33(c)'s coordination, under
    /// <c>observer-coordination</c>'s. A null leaves that entry unasserted, so an entry that is
    /// asked for it throws instead of answering.
    /// </summary>
    private static RuleRequest Asserted(bool? seen, bool? coordinate)
    {
        var assertions = RuleRequest.Empty;
        if (seen is { } ability)
        {
            assertions = assertions.Assert(
                MapEntries.UnaidedVisualContact.Id,
                new Assertion(MapEntries.UnaidedVisualContact, ability, RemotePilotInCommand));
        }

        if (coordinate is { } coordination)
        {
            assertions = assertions.Assert(
                MapEntries.ObserverCoordination.Id,
                new Assertion(MapEntries.ObserverCoordination, coordination, RemotePilotInCommand));
        }

        return assertions;
    }

    /// <summary>
    /// An operation with a visual observer used, both assertions made in the affirmative and no
    /// waiver of either regulation in force — the situation every requirement this engine can
    /// resolve is met on.
    /// </summary>
    private static Resolution<object> Resolve(
        VisualObserverUse? use = null,
        ExerciseOfTheAbility? exercise = null,
        WaiverStatement? waiver = null,
        WaiverStatement? sightWaiver = null,
        bool? seen = true,
        bool? coordinate = true) =>
        EntryPoints.VisualObserverConditions.Resolve(new VisualObserverConditionsRequest(Asserted(seen, coordinate))
        {
            Use = use ?? VisualObserverUse.Used,
            Exercise = exercise ?? RowTwo,
            Waiver = waiver ?? NoWaiver,
            VisualLineOfSightWaiver = sightWaiver ?? NoSightWaiver,
        });

    /// <summary>A request with every input set but the ones passed false.</summary>
    private static VisualObserverConditionsRequest Incomplete(
        bool use = true,
        bool exercise = true,
        bool waiver = true,
        bool sightWaiver = true) =>
        new(Asserted(true, true))
        {
            Use = use ? VisualObserverUse.Used : null,
            Exercise = exercise ? RowTwo : null,
            Waiver = waiver ? NoWaiver : null,
            VisualLineOfSightWaiver = sightWaiver ? NoSightWaiver : null,
        };

    private static VisualObserverConditionsFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<VisualObserverConditionsFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    private static RequirementOutcome Requirement(VisualObserverConditionsFinding finding, string paragraph) =>
        finding.Requirements.Single(requirement => requirement.Paragraph == paragraph);

    [Fact]
    public void No_visual_observer_used_means_107_33_states_no_requirement_and_no_constituent_is_asked()
    {
        // Nothing is asserted at all. If any constituent were asked, unaided-visual-contact and
        // observer-coordination would demand their values and this would throw rather than answer —
        // so resolving is itself the evidence that the chapeau stopped the section before them.
        var finding = Finding(Resolve(VisualObserverUse.NotUsed, seen: null, coordinate: null));

        Assert.False(finding.SectionApplies);
        Assert.Empty(finding.Requirements);
        Assert.Same(VisualObserverUse.NotUsed, finding.Use);

        // The third answer, and it is neither of the other two: not met, and not unmet.
        Assert.Null(finding.AllRequirementsMet);
        Assert.Contains(
            "no visual observer is used during the aircraft operation, so § 107.33 states no requirement about this operation",
            finding.ToString(),
            StringComparison.Ordinal);

        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.33", finding.Authority.Citation);
        Assert.Equal(EntryPoints.VisualObserverConditions.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        // And the same request with a visual observer used does reach them, so the difference above
        // is the chapeau's condition and not an engine that never asks.
        Assert.Throws<AssertionRequiredException>(() =>
            Resolve(VisualObserverUse.Used, seen: null, coordinate: null));
    }

    [Fact]
    public void The_chapeaus_two_values_are_the_only_two_and_neither_is_a_default()
    {
        // "If a visual observer is used during the aircraft operation": a condition with two sides
        // and no middle, in the chapeau's own terms.
        Assert.Equal(
            new[]
            {
                "a visual observer is used during the aircraft operation",
                "no visual observer is used during the aircraft operation",
            },
            VisualObserverUse.All.Select(use => use.Designation),
            StringComparer.Ordinal);

        // And neither side is what an unstated operation means. Whether a whole section of the
        // corpus applies is not something this engine decides from an absence.
        Assert.Equal(
            nameof(VisualObserverConditionsRequest.Use),
            Assert.Throws<ArgumentException>(() =>
                EntryPoints.VisualObserverConditions.Resolve(Incomplete(use: false))).ParamName);
    }

    /// <summary>
    /// One situation per requirement this engine can resolve unmet: § 107.33(b), where the ability
    /// § 107.31(a) describes is asserted not to be there; and § 107.33(c), where the coordination is
    /// asserted not to happen. § 107.33(a) has no such situation — it resolves nothing.
    /// </summary>
    public static TheoryData<string, bool, bool> EachRequirementUnmet => new()
    {
        { "(b)", false, true },
        { "(c)", true, false },
    };

    [Theory]
    [MemberData(nameof(EachRequirementUnmet))]
    public void A_requirement_the_engine_resolves_unmet_makes_not_all_of_them_met_whatever_is_undetermined(
        string unmetParagraph,
        bool seen,
        bool coordinate)
    {
        var finding = Finding(Resolve(seen: seen, coordinate: coordinate));

        Assert.True(finding.SectionApplies);
        Assert.Equal(false, finding.AllRequirementsMet);
        Assert.False(Requirement(finding, unmetParagraph).Met);
        Assert.Equal("not met", Requirement(finding, unmetParagraph).Verdict);

        // Exactly that one is unmet: the others are met or, where the map holds their question
        // open, undetermined — and an undetermined one does not stop "all of the following
        // requirements" being settled by one that is not met. § 107.33(a) is undetermined in both
        // rows, and the answer is still reached.
        Assert.All(
            finding.Requirements.Where(requirement => requirement.Paragraph != unmetParagraph),
            requirement => Assert.NotEqual(false, requirement.Met));
        Assert.Null(Requirement(finding, "(a)").Met);

        Assert.Equal("§ 107.33", finding.Authority.Citation);
        Assert.Equal(EntryPoints.VisualObserverConditions.Registered.Locator, finding.Authority);
    }

    [Fact]
    public void Paragraph_b_is_this_entrys_own_and_is_answered_from_107_31s_ability_through_visual_line_of_sight()
    {
        // No map entry states § 107.33(b): the entry's note says "what survives the split is (b)
        // and the condition of application", and this engine is what answers it.
        Assert.DoesNotContain(
            Registry.Entries,
            entry => entry.Locators.Any(locator => locator.Citation == "§ 107.33(b)"));
        Assert.Equal(
            "(b) The remote pilot in command must ensure that the visual observer is able to see the "
            + "unmanned aircraft in the manner specified in § 107.31.",
            Observers.ParagraphB);

        // The ability asserted not to be there makes (b) unmet, and the entry the map's
        // crossReferences row points at is the one that answered it: its citation is § 107.31, not
        // § 107.33.
        var unmet = Finding(Resolve(seen: false));

        Assert.False(Requirement(unmet, "(b)").Met);
        Assert.Equal("§ 107.33(b)", Requirement(unmet, "(b)").Citation);
        Assert.Equal(MapEntries.VisualLineOfSight.Id, Requirement(unmet, "(b)").Answering.Id);
        Assert.Equal(
            EntryPoints.VisualLineOfSight.Registered.Locator,
            Requirement(unmet, "(b)").Answering.Locator);
        Assert.Equal("§ 107.31", Requirement(unmet, "(b)").Answering.Locator.Citation);

        // (b) asks whether the visual observer "is able to see", which is § 107.31(a)'s ability —
        // the paragraph that names "the visual observer (if one is used)" among the persons who
        // must be able to see. It is not § 107.31(b)'s requirement that the ability be exercised by
        // one of two combinations: with nobody exercising it, § 107.31 is not maintained and
        // § 107.33(b) is still met, because the observer is still able to see.
        var met = Finding(Resolve(exercise: ExerciseOfTheAbility.Nobody, coordinate: false));
        var sight = Assert.IsType<VisualLineOfSightFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.VisualLineOfSight.Resolve(new VisualLineOfSightRequest(Asserted(true, null))
            {
                Exercise = ExerciseOfTheAbility.Nobody,
                Waiver = NoSightWaiver,
            })).Value);

        Assert.False(sight.Maintained);
        Assert.True(sight.Ability.Holds);
        Assert.True(Requirement(met, "(b)").Met);
        Assert.Equal(sight.Ability.Holds, Requirement(met, "(b)").Met);
    }

    [Fact]
    public void The_verdicts_and_the_accounts_are_the_answering_entries_own_and_are_not_restated_here()
    {
        var finding = Finding(Resolve(coordinate: false));

        var sight = Assert.IsType<VisualLineOfSightFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.VisualLineOfSight.Resolve(new VisualLineOfSightRequest(Asserted(true, null))
            {
                Exercise = RowTwo,
                Waiver = NoSightWaiver,
            })).Value);
        var coordination = Assert.IsType<ObserverCoordinationFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.ObserverCoordination.Resolve(new ObserverCoordinationRequest(Asserted(null, false))
            {
                Waiver = NoWaiver,
            })).Value);
        var communication = Declined(
            EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest { Waiver = NoWaiver }));

        Assert.Equal(sight.ToString(), Requirement(finding, "(b)").Account);
        Assert.Equal(coordination.Holds, Requirement(finding, "(c)").Met);
        Assert.Equal(coordination.ToString(), Requirement(finding, "(c)").Account);

        // § 107.33(a) resolves nothing, so what is recorded for it is what that entry said it could
        // not do, with that entry's own reason.
        Assert.Null(Requirement(finding, "(a)").Met);
        Assert.Equal("undetermined", Requirement(finding, "(a)").Verdict);
        Assert.Equal(communication.Attempted, Requirement(finding, "(a)").Account);
        Assert.Equal(communication.Reason, Requirement(finding, "(a)").Reason);
        Assert.Equal(UnresolvedReason.RequiresInterpretation, Requirement(finding, "(a)").Reason);
    }

    [Fact]
    public void With_nothing_unmet_the_undefined_effective_of_107_33_a_blocks_and_this_entry_declines_naming_both()
    {
        var unresolved = Declined(Resolve());

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal(EntryPoints.EffectiveCommunication.Registered.Locator, unresolved.Locator);
        Assert.Equal("§ 107.33(a)", unresolved.Locator.Citation);

        // Both entries are named, and so is the operation the question was asked about.
        Assert.Contains("visual-observer-conditions", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("effective-communication", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.33(a)", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            "a visual observer is used during the aircraft operation",
            unresolved.Attempted,
            StringComparison.Ordinal);

        // The requirements this engine did answer are not named: they did not block anything.
        Assert.DoesNotContain("observer-coordination", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("visual-line-of-sight", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_this_entrys_own_and_not_effective_communications_handed_back()
    {
        var mine = Declined(Resolve());
        var constituent = Declined(
            EntryPoints.EffectiveCommunication.Resolve(new EffectiveCommunicationRequest { Waiver = NoWaiver }));

        // Not the same result, and not the same subject: the caller asked about § 107.33 and is
        // told so, by name.
        Assert.NotEqual(constituent, mine);
        Assert.NotEqual(constituent.Attempted, mine.Attempted);
        Assert.Contains("visual-observer-conditions", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("visual-observer-conditions", constituent.Attempted, StringComparison.Ordinal);

        // And the blind spot that makes Attempted the thing that carries it: the reason and the
        // citation are deliberately the constituent's, because the question that blocks the answer
        // is the constituent's question, so neither can tell the two declines apart
        // (docs/decisions/0001).
        Assert.Equal(constituent.Reason, mine.Reason);
        Assert.Equal(constituent.Locator, mine.Locator);
    }

    [Fact]
    public void While_a_waiver_of_107_33_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_naming_this_entry()
    {
        var waiver = WaiverStatement.Held("§ 107.33", Caller);

        // Nothing is asserted: the gate is read before anything is demanded.
        var unresolved = Declined(Resolve(waiver: waiver, seen: null, coordinate: null));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);

        // This entry's own gate. § 107.205(d) lists § 107.33 whole, so the same statement would
        // suspend (a) and (c) too, and only the entry named tells the three declines apart.
        Assert.Contains("visual-observer-conditions", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.33]", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("effective-communication", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);

        // And the gate is read before the chapeau's condition, so a waived section is not answered
        // as one that states no requirement.
        var notUsed = Declined(Resolve(VisualObserverUse.NotUsed, waiver: waiver, seen: null, coordinate: null));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, notUsed.Reason);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, notUsed.Locator);
    }

    [Fact]
    public void A_waiver_of_107_31_leaves_paragraph_b_undetermined_and_is_carried_as_the_constituents_own_account()
    {
        // § 107.205 lists § 107.31 and § 107.33 in different paragraphs, so a waiver of § 107.31
        // waives nothing in § 107.33: it makes the entry § 107.33(b) is answered from decline, and
        // this entry reports that as the requirement going unanswered. The ability is not asserted,
        // because that entry's own gate stops it being demanded.
        var finding = Finding(Resolve(sightWaiver: WaiverStatement.Held("§ 107.31", Caller), seen: null, coordinate: false));

        Assert.Null(Requirement(finding, "(b)").Met);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, Requirement(finding, "(b)").Reason);
        Assert.Contains("visual-line-of-sight", Requirement(finding, "(b)").Account, StringComparison.Ordinal);
        Assert.Contains("in force", Requirement(finding, "(b)").Account, StringComparison.Ordinal);

        // § 107.33(c) is still answered, and answered unmet, which settles the conjunction.
        Assert.Equal(false, finding.AllRequirementsMet);
        Assert.False(Requirement(finding, "(c)").Met);

        // The § 107.33 statement is untouched by it.
        Assert.Same(NoWaiver, finding.Waiver);
        Assert.False(finding.Waiver.InForce);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statements_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.33", "the person manipulating the flight controls");
        var sightWaiver = WaiverStatement.NoneHeld("§ 107.31", "the visual observer");

        var finding = Finding(Resolve(waiver: waiver, sightWaiver: sightWaiver, coordinate: false));

        Assert.Equal(false, finding.AllRequirementsMet);
        Assert.Same(waiver, finding.Waiver);
        Assert.False(finding.Waiver.InForce);
        Assert.Equal("the person manipulating the flight controls", finding.Waiver.StatedBy);

        // The second statement travels on the account of the requirement it was made for.
        Assert.Contains("the visual observer", Requirement(finding, "(b)").Account, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        // This entry's own statement must be about § 107.33 …
        Assert.Contains(
            "visual-observer-conditions",
            Assert.Throws<ArgumentException>(() => Resolve(waiver: WaiverStatement.NoneHeld("§ 107.37(a)", Caller))).Message,
            StringComparison.Ordinal);

        // … and the one § 107.33(b)'s constituent is asked under must be about § 107.31. The two are
        // separate paragraphs of § 107.205 and neither statement stands in for the other.
        Assert.Contains(
            "visual-line-of-sight",
            Assert.Throws<ArgumentException>(() => Resolve(sightWaiver: WaiverStatement.NoneHeld("§ 107.33", Caller))).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_three_requirements_are_the_sections_own_paragraphs_in_its_own_order()
    {
        var finding = Finding(Resolve(coordinate: false));

        Assert.Equal(new[] { "(a)", "(b)", "(c)" }, Observers.Paragraphs, StringComparer.Ordinal);
        Assert.Equal(
            new[] { "(a)", "(b)", "(c)" },
            finding.Requirements.Select(requirement => requirement.Paragraph),
            StringComparer.Ordinal);
        Assert.Equal(
            new[] { "§ 107.33(a)", "§ 107.33(b)", "§ 107.33(c)" },
            finding.Requirements.Select(requirement => requirement.Citation),
            StringComparer.Ordinal);

        // The section's order, which is not this entry's dependsOn order — dependsOn orders the
        // work and lists visual-line-of-sight first, while "all of the following requirements"
        // enumerates the paragraphs as the corpus prints them.
        Assert.Equal(
            new[] { "effective-communication", "visual-line-of-sight", "observer-coordination" },
            finding.Requirements.Select(requirement => requirement.Answering.Id),
            StringComparer.Ordinal);
    }

    [Fact]
    public void Without_the_facts_the_section_is_tested_against_it_refuses_rather_than_assume_them() =>
        Assert.All(
            new (string Name, VisualObserverConditionsRequest Request)[]
            {
                (nameof(VisualObserverConditionsRequest.Use), Incomplete(use: false)),
                (nameof(VisualObserverConditionsRequest.Exercise), Incomplete(exercise: false)),
                (nameof(VisualObserverConditionsRequest.Waiver), Incomplete(waiver: false)),
                (nameof(VisualObserverConditionsRequest.VisualLineOfSightWaiver), Incomplete(sightWaiver: false)),
            },
            missing => Assert.Equal(
                missing.Name,
                Assert.Throws<ArgumentException>(
                    () => EntryPoints.VisualObserverConditions.Resolve(missing.Request)).ParamName));

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_answering_from_defaults()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Registry.Resolve("visual-observer-conditions", RuleRequest.Empty));

        Assert.Equal(nameof(VisualObserverConditionsRequest.Use), error.ParamName);
    }
}
