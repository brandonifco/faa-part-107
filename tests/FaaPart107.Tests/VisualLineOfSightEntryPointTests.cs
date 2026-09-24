using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>visual-line-of-sight</c>, § 107.31, resolved through
/// <see cref="EntryPoints.VisualLineOfSight"/> only.
/// </summary>
/// <remarks>
/// <para>
/// The entry is § 107.31 as a whole and it depends on <c>unaided-visual-contact</c>, which is
/// § 107.31(a). So these pin the division: paragraph (a)'s ability is never restated here, it is
/// demanded through the dependency under the dependency's own entry id; paragraph (b) is this
/// entry's, and both of its rows are checked — each on its own, both at once, and neither —
/// exhaustively over every way the three persons the paragraph names can exercise the ability.
/// </para>
/// <para>
/// Two things the two entries share are pinned as well: the § 107.205(c) row that suspends both,
/// where the decline's own locator is § 107.205 for either entry and only what was attempted tells
/// them apart; and § 107.31(b)(1)'s plural "flight controls" against § 107.31(a)'s singular "flight
/// control", which the corpus really does differ on.
/// </para>
/// </remarks>
public class VisualLineOfSightEntryPointTests
{
    private const string Caller = nameof(VisualLineOfSightEntryPointTests);

    private const string RemotePilotInCommand = "remote pilot in command";

    private const string VisualObserver = "visual observer";

    private static readonly MapEntry Ability = MapEntries.UnaidedVisualContact;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.31", Caller);

    /// <summary>§ 107.31(b)(1): the remote pilot in command and the person manipulating the flight controls.</summary>
    private static readonly ExerciseOfTheAbility RowOne =
        new(RemotePilotInCommand: true, PersonManipulatingTheFlightControls: true, VisualObserver: false);

    /// <summary>§ 107.31(b)(2): a visual observer.</summary>
    private static readonly ExerciseOfTheAbility RowTwo =
        new(RemotePilotInCommand: false, PersonManipulatingTheFlightControls: false, VisualObserver: true);

    private static VisualLineOfSightRequest Request(
        ExerciseOfTheAbility? exercise,
        WaiverStatement? waiver,
        object? asserted) =>
        new(asserted is null ? RuleRequest.Empty : RuleRequest.Empty.Assert(Ability.Id, asserted))
        {
            Exercise = exercise,
            Waiver = waiver,
        };

    private static Resolution<object> Resolve(ExerciseOfTheAbility exercise, bool seen = true) =>
        Resolve(exercise, NoWaiver, new Assertion(Ability, seen, RemotePilotInCommand));

    private static Resolution<object> Resolve(
        ExerciseOfTheAbility? exercise,
        WaiverStatement? waiver,
        object? asserted) =>
        EntryPoints.VisualLineOfSight.Resolve(Request(exercise, waiver, asserted));

    private static VisualLineOfSightFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<VisualLineOfSightFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void The_two_permitted_combinations_are_107_31_b_s_own_two_rows_verbatim_and_keep_its_plural_flight_controls()
    {
        // Both rows of the enumeration, in the paragraph's own order and under its own numbers. The
        // first row is where § 107.31(b) says "flight controls" and § 107.31(a) says "flight
        // control": the corpus differs between the two paragraphs, so the engine carries the plural
        // here and the map carries the singular in the dependency's assertedBy, and neither is
        // normalised into the other.
        var finding = Finding(Resolve(RowOne));

        Assert.Equal(
            new[]
            {
                "(1) The remote pilot in command and the person manipulating the flight controls of the small unmanned aircraft system; or",
                "(2) A visual observer.",
            },
            finding.Combinations);

        Assert.Contains("person manipulating the flight controls", finding.Combinations[0], StringComparison.Ordinal);
        Assert.Contains("person manipulating the flight control", EntryPoints.UnaidedVisualContact.Registered.AssertedBy);
        Assert.DoesNotContain("person manipulating the flight controls", EntryPoints.UnaidedVisualContact.Registered.AssertedBy);
    }

    [Fact]
    public void Each_of_107_31_b_s_two_rows_exercised_maintains_visual_line_of_sight_citing_107_31()
    {
        var byRowOne = Finding(Resolve(RowOne));
        var byRowTwo = Finding(Resolve(RowTwo));

        // (b)(1): both of the persons it names, and no visual observer at all.
        Assert.True(byRowOne.ExercisedByTheRemotePilotInCommandAndThePersonManipulatingTheFlightControls);
        Assert.False(byRowOne.ExercisedByAVisualObserver);
        Assert.True(byRowOne.Maintained);

        // (b)(2): the one person it names, and neither of (b)(1)'s.
        Assert.False(byRowTwo.ExercisedByTheRemotePilotInCommandAndThePersonManipulatingTheFlightControls);
        Assert.True(byRowTwo.ExercisedByAVisualObserver);
        Assert.True(byRowTwo.Maintained);

        foreach (var finding in new[] { byRowOne, byRowTwo })
        {
            Assert.True(finding.Exercised);
            Assert.True(finding.Ability.Holds);
            Assert.Equal("cfr-14-107", finding.Authority.SourceId);
            Assert.Equal("§ 107.31", finding.Authority.Citation);
            Assert.Equal(EntryPoints.VisualLineOfSight.Registered.Locator, finding.Authority);
            Assert.Same(NoWaiver, finding.Waiver);
        }

        Assert.Contains("maintained", byRowOne.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Every one of the eight ways the three persons § 107.31(b) names can have exercised the
    /// ability, with what each row of the enumeration says about it. Exhaustive, not sampled: the
    /// enumeration is finite and both of its rows are checked satisfied and unsatisfied, including
    /// the two partial cases where only one half of (b)(1) exercised the ability, and the case where
    /// both rows are satisfied at once.
    /// </summary>
    public static TheoryData<bool, bool, bool, bool, bool> EveryExercise => new()
    {
        //  rpic,  manipulator, observer,  (b)(1), (b)(2)
        { false, false, false, false, false },
        { true, false, false, false, false },
        { false, true, false, false, false },
        { false, false, true, false, true },
        { true, true, false, true, false },
        { true, false, true, false, true },
        { false, true, true, false, true },
        { true, true, true, true, true },
    };

    [Theory]
    [MemberData(nameof(EveryExercise))]
    public void Every_way_the_three_persons_can_exercise_the_ability_is_membership_tested_against_both_rows_and_neither(
        bool remotePilotInCommand,
        bool personManipulatingTheFlightControls,
        bool visualObserver,
        bool rowOne,
        bool rowTwo)
    {
        var finding = Finding(Resolve(new ExerciseOfTheAbility(
            remotePilotInCommand,
            personManipulatingTheFlightControls,
            visualObserver)));

        Assert.Equal(rowOne, finding.ExercisedByTheRemotePilotInCommandAndThePersonManipulatingTheFlightControls);
        Assert.Equal(rowTwo, finding.ExercisedByAVisualObserver);

        // "must be exercised by either: (1) ... or (2) ..." is a requirement met by a disjunction,
        // not a prohibition on anybody else also exercising the ability. Neither row satisfied is
        // the only way it is not exercised; both rows satisfied at once is met.
        Assert.Equal(rowOne || rowTwo, finding.Exercised);
        Assert.Equal(rowOne || rowTwo, finding.Maintained);
        Assert.Equal(finding.Exercise.RemotePilotInCommand, remotePilotInCommand);
        Assert.Equal(finding.Exercise.PersonManipulatingTheFlightControls, personManipulatingTheFlightControls);
        Assert.Equal(finding.Exercise.VisualObserver, visualObserver);
    }

    [Fact]
    public void A_negative_assertion_of_the_ability_leaves_it_not_maintained_whichever_row_is_stated()
    {
        // § 107.31(b) requires "the ability described in paragraph (a)" to be exercised. An asserter
        // who says that ability is not there has answered the dependency, not left a gap in the
        // corpus, so this is not a decline — and (b)'s membership test is still reported, because
        // what the caller stated about who was watching is not discarded.
        foreach (var exercise in new[] { RowOne, RowTwo })
        {
            var finding = Finding(Resolve(exercise, seen: false));

            Assert.False(finding.Ability.Holds);
            Assert.True(finding.Exercised);
            Assert.False(finding.Maintained);
            Assert.Contains("not maintained", finding.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_ability_is_demanded_under_its_own_entry_id_and_asserting_it_under_this_one_does_not_answer_it()
    {
        // The value belongs to unaided-visual-contact, and this entry has no assertedBy of its own.
        // So nothing asserted is a demand naming the dependency, and a value asserted under
        // visual-line-of-sight's id is not the dependency's answer.
        var nothing = Assert.Throws<AssertionRequiredException>(() => Resolve(RowOne, NoWaiver, null));
        Assert.Equal(Ability.Id, nothing.EntryId);
        Assert.Equal("unaided-visual-contact", nothing.EntryId);

        var underThisEntry = Assert.Throws<AssertionRequiredException>(() =>
            EntryPoints.VisualLineOfSight.Resolve(new VisualLineOfSightRequest(
                RuleRequest.Empty.Assert("visual-line-of-sight", new Assertion(Ability, Holds: true, RemotePilotInCommand)))
            {
                Exercise = RowOne,
                Waiver = NoWaiver,
            }));
        Assert.Equal(Ability.Id, underThisEntry.EntryId);

        // And the dependency's own checks still apply, unchanged, through this entry: the assertion
        // must be attributed to somebody § 107.31(a) names — its singular "flight control", not
        // § 107.31(b)(1)'s plural.
        var stranger = Assert.Throws<ArgumentException>(() =>
            Resolve(RowOne, NoWaiver, new Assertion(Ability, Holds: true, "person manipulating the flight controls")));
        Assert.Contains("person manipulating the flight control", stranger.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void While_a_waiver_of_107_31_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_naming_this_entry_without_demanding_the_assertion()
    {
        var waiver = WaiverStatement.Held("§ 107.31", Caller, "107W-0000-00000");

        // Nothing is asserted here, and that is the point: this entry's own gate decides before the
        // dependency is reached, so a suspended entry never demands a fact the waiver has made
        // irrelevant. Were the order the other way round this would throw AssertionRequiredException.
        var withNothingAsserted = Declined(Resolve(RowOne, waiver, null));
        var withAnAssertion = Declined(Resolve(RowTwo, waiver, new Assertion(Ability, Holds: true, VisualObserver)));
        var withNobodyExercising = Declined(Resolve(ExerciseOfTheAbility.Nobody, waiver, null));

        Assert.All(new[] { withNothingAsserted, withAnAssertion, withNobodyExercising }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);

            // This entry, not its dependency: § 107.205(c) lists the whole of § 107.31 and suspends
            // both, so the decline names visual-line-of-sight and § 107.31.
            Assert.Contains("visual-line-of-sight", unresolved.Attempted, StringComparison.Ordinal);
            Assert.DoesNotContain("unaided-visual-contact", unresolved.Attempted, StringComparison.Ordinal);
            Assert.DoesNotContain("§ 107.31(a)", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_callers_statement_is_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.31", "the remote pilot in command");

        var maintained = Finding(Resolve(RowTwo, waiver, new Assertion(Ability, Holds: true, VisualObserver)));
        var notMaintained = Finding(Resolve(
            ExerciseOfTheAbility.Nobody,
            waiver,
            new Assertion(Ability, Holds: true, VisualObserver)));

        Assert.True(maintained.Maintained);
        Assert.False(notMaintained.Maintained);

        // The statement is the caller's own object, carried through the dependency untouched, and
        // never one the engine made up.
        Assert.Same(waiver, maintained.Waiver);
        Assert.Same(waiver, notMaintained.Waiver);
        Assert.Same(waiver, maintained.Ability.Waiver);
        Assert.Equal("the remote pilot in command", maintained.Waiver.StatedBy);
        Assert.False(maintained.Waiver.InForce);
    }

    [Fact]
    public void Without_a_waiver_statement_or_without_the_exercise_it_refuses_rather_than_infer_either()
    {
        var noWaiverStatement = Assert.Throws<ArgumentException>(() => Resolve(
            RowOne,
            waiver: null,
            new Assertion(Ability, Holds: true, RemotePilotInCommand)));
        Assert.Equal(nameof(VisualLineOfSightRequest.Waiver), noWaiverStatement.ParamName);

        var noExercise = Assert.Throws<ArgumentException>(() => Resolve(
            exercise: null,
            NoWaiver,
            new Assertion(Ability, Holds: true, RemotePilotInCommand)));
        Assert.Equal(nameof(VisualLineOfSightRequest.Exercise), noExercise.ParamName);
        Assert.Contains("visual-line-of-sight", noExercise.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused_naming_this_entry()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            RowOne,
            WaiverStatement.Held("§ 107.33", Caller),
            new Assertion(Ability, Holds: true, RemotePilotInCommand)));

        Assert.Contains("visual-line-of-sight", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.33", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_finding_cites_107_31_where_its_dependency_cites_107_31_a_and_the_two_declines_are_told_apart_by_what_was_attempted()
    {
        var asserted = new Assertion(Ability, Holds: true, RemotePilotInCommand);

        var finding = Finding(Resolve(RowOne, NoWaiver, asserted));

        // Resolved, the two are told apart by their citations: the section and the paragraph.
        Assert.Equal("§ 107.31", finding.Authority.Citation);
        Assert.Equal("§ 107.31(a)", finding.Ability.Authority.Citation);
        Assert.NotEqual(finding.Authority, finding.Ability.Authority);

        // Declined under the gate, they are not: § 107.205(c) lists the whole of § 107.31, so both
        // declines cite § 107.205 and carry the same reason. What was attempted is what separates
        // them, and it names the entry id and that entry's own citation.
        var waiver = WaiverStatement.Held("§ 107.31", Caller);
        var mine = Declined(Resolve(RowOne, waiver, asserted));
        var dependency = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.UnaidedVisualContact.Resolve(
                new UnaidedVisualContactRequest(RuleRequest.Empty.Assert(Ability.Id, asserted)) { Waiver = waiver })).Result;

        Assert.Equal(mine.Locator, dependency.Locator);
        Assert.Equal(mine.Reason, dependency.Reason);
        Assert.NotEqual(mine.Attempted, dependency.Attempted);
        Assert.Contains("'visual-line-of-sight' [§ 107.31]", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains("'unaided-visual-contact' [§ 107.31(a)]", dependency.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_refuses_the_same_way()
    {
        var asserted = new Assertion(Ability, Holds: true, VisualObserver);

        var finding = Finding(Registry.Resolve(Request(RowTwo, NoWaiver, asserted)));

        Assert.True(finding.Maintained);
        Assert.Equal(MapEntries.VisualLineOfSight.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        // Resolved by id, the request is built from the assertions alone, so it carries neither the
        // exercise nor a waiver statement — and the entry refuses rather than infer either. The
        // waiver statement is the one it names, because it is the one the gate reads and the gate
        // comes first (docs/decisions/0008); the exercise is owed next, once a statement that no
        // waiver is in force has put the entry back in reach.
        var error = Assert.Throws<ArgumentException>(() => Registry.Resolve(
            "visual-line-of-sight",
            RuleRequest.Empty.Assert(Ability.Id, asserted)));
        Assert.Equal(nameof(VisualLineOfSightRequest.Waiver), error.ParamName);
        Assert.Equal(
            nameof(VisualLineOfSightRequest.Exercise),
            Assert.Throws<ArgumentException>(() => EntryPoints.VisualLineOfSight.Resolve(
                new VisualLineOfSightRequest(RuleRequest.Empty.Assert(Ability.Id, asserted)) { Waiver = NoWaiver })).ParamName);
        Assert.Throws<ArgumentException>(() => Registry.Resolve("visual-line-of-sight", RuleRequest.Empty));
    }
}
