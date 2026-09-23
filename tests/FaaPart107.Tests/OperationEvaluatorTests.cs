using System.Reflection;
using FaaPart107.Evaluation;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <see cref="OperationEvaluator.Evaluate(OperationFacts)"/>: the product-facing orchestration API,
/// proved at the level it is a contract at.
/// </summary>
/// <remarks>
/// What these pin, in order: that the map is enumerated rather than listed; that every outcome
/// names its entry and its citation; that "we did not say", "we said no", "the engine cannot
/// determine this", "the engine has not built this" and "this is outside the engine's scope" are
/// five different answers and stay five; that every verdict is read from the rule's own property in
/// the direction that rule's own documentation states, polarity included; and that nothing here
/// collapses to a boolean.
/// </remarks>
public class OperationEvaluatorTests
{
    private const string Caller = nameof(OperationEvaluatorTests);

    /// <summary>The person § 107.49 names, used where the map's <c>assertedBy</c> names people.</summary>
    private const string RemotePilotInCommand = "remote pilot in command";

    /// <summary>
    /// Facts complete enough that every entry this engine has built answers: every input supplied,
    /// a statement that no waiver is in force for each regulation the evaluator asks about, and an
    /// assertion for each implemented <c>kind: assertion</c> entry.
    /// </summary>
    /// <remarks>
    /// The regulations are taken from the rules' own <c>Regulation</c> constants, never typed here,
    /// so a test cannot state a waiver about a regulation no entry is under. <c>Coordination</c>
    /// and <c>Communication</c> are both § 107.33, so one statement answers both entries — which is
    /// the point of filing a statement under the regulation it names rather than under an entry.
    /// </remarks>
    private static OperationFacts Complete() =>
        Registry.Entries
            .Where(entry => entry.Row == CorrespondenceRow.Assertion && entry.Status == EntryStatus.Implemented)
            .Aggregate(Stated(), (facts, entry) => facts.Asserting(new Assertion(MapEntry(entry.Id), true, entry.AssertedBy[0])))

            // The one entry the fold gets backwards, and the only semantic thing hand-written here.
            // § 107.37(b)'s proposition is "operating so close to another aircraft as to create a
            // collision hazard", so it HOLDING is the prohibited state: an operation these facts
            // describe as compliant asserts it false, where every other row-8 entry's proposition
            // holding is the required state. The engine records no polarity to derive this from —
            // that is docs/decisions/0004 and rules-factory#453 — so it is stated here, once.
            //
            // What keeps the fold honest if the map ever adds a second prohibition-shaped entry is
            // the count pinned in The_cost_recorded_in_decision_0004_is_the_cost_the_engine
            // _actually_has: an eleventh row-8 entry turns it red, and whoever adds it has to come
            // and decide which way round this one goes.
            .Asserting(new Assertion(MapEntries.CollisionHazardProximity, false, RemotePilotInCommand));

    /// <summary>
    /// Every input and every waiver statement, and not one assertion. This is the realistic case a
    /// caller reaches first — the facts are known, the attestations are not yet made — and it is the
    /// case that reaches every composite's demand for a constituent's assertion.
    /// </summary>
    private static OperationFacts Stated() => new OperationFacts
    {
        Groundspeed = Groundspeed.InKnots(50m),
        AltitudeAboveGroundLevelFeet = 300m,
        Structure = StructureStatement.NoneWithinRadius(Caller),
        FlightVisibilityStatuteMiles = 5m,
        Cloud = CloudStatement.Measured(100m, 100m, Caller),
        BoundPerson = BoundPerson.RemotePilotInCommand,
        Lighting = LightingStatement.LightedAndVisibleFor(5m, Caller),
        VisualObserverUse = VisualObserverUse.NotUsed,
        SubpartDOperation = SubpartDOperation.NotOverHumanBeings,
        OperationPlace = OperationPlace.OutsideAlaska,
        OperationPeriod = OperationPeriod.BeforeOfficialSunrise,
        Shelter = Shelter.CoveredStructure,
        HumanBeingLocation = HumanBeingLocation.UnderACoveredStructure,
        Airspace = AirspaceClass.ClassG,
        AtcAuthorization = AtcAuthorization.None(Caller),
        Area = AreaDesignation.NeitherProhibitedNorRestricted,
        AreaPermission = PermissionStatement.NotGranted(Caller),
        FromMovingLandOrWaterBorneVehicle = false,
        TransportingAnotherPersonsPropertyForCompensationOrHire = false,
        FromAMovingAircraft = false,
        Person = "the remote pilot in command of N123AB",
        Engagements = [new AircraftEngagement("N123AB", AircraftRole.RemotePilotInCommand)],
        Encountered = EncounteredObject.NoneOfThem,
        Exercise = new ExerciseOfTheAbility(
            RemotePilotInCommand: true,
            PersonManipulatingTheFlightControls: true,
            VisualObserver: false),
        Position = RelativePosition.NoneOfThem,
        NightWaiverCertificate = new WaiverCertificate(new DateOnly(2020, 6, 1), true, Caller),
        AsOf = new DateOnly(2026, 1, 1),
    }
        .Stating(WaiverStatement.NoneHeld(Speed.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(MultipleAircraft.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(Airspace.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(MovingVehicle.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(Yielding.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(Participation.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(Communication.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(Lighting.Regulation, Caller))
        .Stating(WaiverStatement.NoneHeld(UnaidedVision.Regulation, Caller));

    private static EvaluatedRequirement Outcome(string entryId, OperationFacts facts) =>
        OperationEvaluator.Evaluate(facts).Requirement(entryId);

    private static RequirementState State(string entryId, OperationFacts facts) =>
        Outcome(entryId, facts).State;

    [Fact]
    public void Every_map_entry_is_evaluated_once_in_the_map_s_order()
    {
        var evaluation = OperationEvaluator.Evaluate(OperationFacts.Nothing);

        Assert.Equal(Registry.Entries.Length, evaluation.Requirements.Length);
        Assert.Equal(
            Registry.Entries.Select(entry => entry.Id).ToArray(),
            evaluation.Requirements.Select(outcome => outcome.EntryId).ToArray());
    }

    /// <summary>
    /// The state an entry this engine has not built reports, read from the map's own correspondence
    /// row: row 1 is out of scope, row 2 is not built, and rows 3, 4 and 5 are missing rules data.
    /// Null where the row does not fix the answer on its own.
    /// </summary>
    private static RequirementState? FromItsRow(CorrespondenceRow row) => row switch
    {
        CorrespondenceRow.ScopeOut => RequirementState.OutsideCurrentScope,
        CorrespondenceRow.NotBuilt => RequirementState.NotBuilt,
        CorrespondenceRow.DefinedElsewhere or CorrespondenceRow.BeyondAdapter
            or CorrespondenceRow.ValueDependencyUnimplemented => RequirementState.MissingRulesData,
        _ => null,
    };

    [Fact]
    public void An_entry_this_evaluator_builds_no_request_for_is_still_evaluated_and_answers_from_its_row()
    {
        var evaluation = OperationEvaluator.Evaluate(Complete());

        // Driven from the registry, not from a list of entry ids: an entry built later moves off
        // this list by itself rather than turning this test red.
        var unbuilt = Registry.Entries
            .Where(entry => entry.Status != EntryStatus.Implemented && FromItsRow(entry.Row) is not null)
            .ToArray();

        Assert.NotEmpty(unbuilt);

        foreach (var entry in unbuilt)
        {
            var outcome = evaluation.Requirement(entry.Id);
            Assert.Equal(FromItsRow(entry.Row), outcome.State);
            Assert.Equal(RequirementStates.For(outcome.Reason!.Value), outcome.State);
            Assert.Null(outcome.Finding);
        }

        // And the three answers it gives are three, not one.
        Assert.Equal(3, unbuilt.Select(entry => FromItsRow(entry.Row)).Distinct().Count());
    }

    /// <summary>
    /// The <c>init</c> properties an entry's request declares beyond the generated shape — the
    /// inputs a caller can state. The generated members (<c>EntryId</c>, <c>Assertions</c>) are
    /// get-only, so an init-only setter is exactly a hand-declared input.
    /// </summary>
    private static string[] DeclaredInputsOf(string entryId) =>
        [.. RequestTypeOf(entryId)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { } setter
                && setter.ReturnParameter.GetRequiredCustomModifiers()
                    .Any(modifier => modifier == typeof(System.Runtime.CompilerServices.IsExternalInit)))
            .Select(property => property.Name)];

    [Fact]
    public void Every_implemented_entry_whose_request_declares_inputs_has_a_builder()
    {
        // The property this API most needs to keep true as the engine grows, and the one that had
        // been checked only by whoever happened to review it. An implemented entry with inputs and
        // no builder is not a wrong answer — it reports FactRequired — but it is an entry a caller
        // cannot reach at all, which is worse for being quiet.
        var owed = Registry.Entries
            .Where(entry => entry.Status == EntryStatus.Implemented && DeclaredInputsOf(entry.Id).Length > 0)
            .Select(entry => entry.Id)
            .ToArray();

        var missing = owed.Except(OperationEvaluator.EntriesBuiltFromFacts, StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0,
            $"these implemented entries declare inputs and have no arm in OperationEvaluator.Builder, so a caller "
            + $"cannot state their facts: {string.Join(", ", missing)}. Add the arm, and the OperationFacts field "
            + "each input needs.");

        // And the other direction: nothing is built for an entry that is not implemented, or for one
        // that declares no inputs — either would be surface this evaluator cannot justify.
        foreach (var entryId in OperationEvaluator.EntriesBuiltFromFacts)
        {
            Assert.Equal(EntryStatus.Implemented, Registry.Entry(entryId).Status);
            Assert.NotEmpty(DeclaredInputsOf(entryId));
        }

        Assert.Equal(owed.Order(StringComparer.Ordinal), OperationEvaluator.EntriesBuiltFromFacts.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_complete_fact_set_reaches_every_entry_this_engine_has_built()
    {
        // The same property from the other end, and the one that catches a missing OperationFacts
        // field as well as a missing arm: with everything stated, no built entry may come back
        // asking the caller for something.
        var evaluation = OperationEvaluator.Evaluate(Complete());

        var owing = evaluation.Requirements
            .Where(outcome => outcome.State == RequirementState.FactRequired)
            .Select(outcome => $"{outcome.EntryId} wants {outcome.MissingInput}")
            .ToArray();

        Assert.True(
            owing.Length == 0,
            "a complete fact set left entries still asking for facts, so either an arm or an OperationFacts "
            + $"field is missing: {string.Join("; ", owing)}");
    }

    [Fact]
    public void Every_outcome_names_the_entry_that_produced_it_and_the_locators_that_entry_cites()
    {
        var evaluation = OperationEvaluator.Evaluate(Complete());

        foreach (var outcome in evaluation.Requirements)
        {
            var entry = Registry.Entry(outcome.EntryId);
            Assert.Equal(Registry.Citations(outcome.EntryId), outcome.Citations);
            Assert.Equal(entry.Locator, outcome.Locator);
            Assert.Equal(MapEntries.SourceId, outcome.Locator.SourceId);
            Assert.Equal(entry.Status, outcome.Status);
            Assert.Equal(entry.Row, outcome.Row);
            Assert.NotEmpty(outcome.Explanation);
        }
    }

    [Fact]
    public void A_fact_not_stated_is_not_read_as_false_and_not_read_as_true()
    {
        var facts = Complete();

        // § 107.25(a). Three different statements, three different answers, and the first of them
        // is the one this API exists for: the caller did not say.
        var silent = Outcome("moving-aircraft-operation", facts with { FromAMovingAircraft = null });
        var no = Outcome("moving-aircraft-operation", facts with { FromAMovingAircraft = false });
        var yes = Outcome("moving-aircraft-operation", facts with { FromAMovingAircraft = true });

        Assert.Equal(RequirementState.FactRequired, silent.State);
        Assert.Equal(nameof(Requests.MovingAircraftOperationRequest.FromAMovingAircraft), silent.MissingInput);
        Assert.Null(silent.Reason);
        Assert.Null(silent.Finding);
        Assert.Equal(RequirementState.Satisfied, no.State);
        Assert.Equal(RequirementState.Violated, yes.State);
    }

    [Fact]
    public void A_waiver_statement_not_made_is_a_demand_on_the_caller_and_never_an_assumed_answer()
    {
        var facts = Complete();
        var unstated = new OperationFacts
        {
            Groundspeed = facts.Groundspeed,
        };

        var outcome = Outcome("speed-within-limit", unstated);

        Assert.Equal(RequirementState.FactRequired, outcome.State);
        Assert.Equal(nameof(Requests.SpeedWithinLimitRequest.Waiver), outcome.MissingInput);
    }

    [Fact]
    public void A_waiver_is_per_regulation_and_suspends_only_the_entries_that_regulation_states()
    {
        var facts = Complete().Stating(WaiverStatement.Held(Speed.Regulation, Caller, "waiver 2026-0007"));

        // § 107.51 suspended: the two entries under it report the gate's own answer, and that is
        // "outside current scope", never "satisfied" and never "violated".
        var suspended = Outcome("speed-within-limit", facts);
        Assert.Equal(RequirementState.OutsideCurrentScope, suspended.State);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, suspended.Reason);
        Assert.Equal(RequirementState.OutsideCurrentScope, State("altitude-within-limit", facts));

        // § 107.41 is a different regulation and is untouched by it.
        Assert.Equal(RequirementState.Satisfied, State("airspace-authorized", facts));
    }

    [Fact]
    public void An_assertion_not_supplied_is_a_demand_on_the_caller_and_never_a_decline()
    {
        var outcome = Outcome("sufficient-available-power", OperationFacts.Nothing);

        Assert.Equal(RequirementState.HumanAssertionRequired, outcome.State);
        Assert.Null(outcome.Reason);
        Assert.Contains(RemotePilotInCommand, outcome.AssertedBy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_assertion_supplied_is_recorded_with_who_made_it_and_is_not_scored(bool holds)
    {
        var facts = Complete().Asserting(new Assertion(MapEntries.SufficientAvailablePower, holds, RemotePilotInCommand));

        var outcome = Outcome("sufficient-available-power", facts);

        // § 107.49(d)'s proposition holding is the required state and § 107.37(b)'s holding is the
        // prohibited one, and neither the map nor any rule here records which. So the fact is
        // reported with its asserter, and it is not turned into a verdict in either direction.
        Assert.Equal(RequirementState.HumanAssertionRecorded, outcome.State);
        Assert.Equal(new Assertion(MapEntries.SufficientAvailablePower, holds, RemotePilotInCommand), outcome.Finding);
        Assert.Equal(MapEntries.SufficientAvailablePower.Locator, outcome.Locator);
    }

    /// <summary>
    /// The assertion entries whose rule wraps the <see cref="Assertion"/> in a finding of its own,
    /// because the § 107.205 gate has to be recorded beside it. Three shapes, one answer: the state
    /// is read from the map's correspondence row, not from a list of finding types.
    /// </summary>
    public static TheoryData<string, bool> GatedAssertions =>
        new()
        {
            { "unaided-visual-contact", true },
            { "unaided-visual-contact", false },
            { "observer-coordination", true },
            { "observer-coordination", false },
            { "intensity-reduction-in-interest-of-safety", true },
            { "intensity-reduction-in-interest-of-safety", false },
        };

    [Theory]
    [MemberData(nameof(GatedAssertions))]
    public void An_assertion_behind_the_waiver_gate_is_recorded_the_same_way(string entryId, bool holds)
    {
        var entry = Registry.Entry(entryId);
        var facts = Complete().Asserting(new Assertion(MapEntry(entryId), holds, entry.AssertedBy[0]));

        var outcome = Outcome(entryId, facts);

        // The gate decides whether the entry is reachable; it does not turn the asserted fact into
        // a verdict, and the finding the rule wrapped it in does not either.
        Assert.Equal(RequirementState.HumanAssertionRecorded, outcome.State);
        Assert.Equal(MapEntry(entryId).Locator, outcome.Locator);
        Assert.Null(outcome.Reason);
    }

    /// <summary>
    /// The assertion entries whose polarity a built consumer supplies, which is the cost
    /// <c>docs/decisions/0004</c> records. Measured, not read off the source: an entry has such a
    /// consumer exactly when flipping the asserted fact moves some <em>other</em> entry's answer.
    /// </summary>
    /// <remarks>
    /// The facts are chosen so that every consumer that exists can fire — the intensity stated
    /// reduced, because <c>anti-collision-lighting</c> reads
    /// <c>intensity-reduction-in-interest-of-safety</c> only then, and a visual observer used,
    /// because § 107.33's chapeau gates <c>observer-coordination</c>. So this measures whether a
    /// consumer can <em>ever</em> supply the polarity, which is the claim the record makes.
    /// </remarks>
    private static string[] AssertionsABuiltConsumerReads()
    {
        var facts = Complete() with
        {
            Lighting = LightingStatement.IntensityReducedAndVisibleFor(5m, Caller),
            VisualObserverUse = VisualObserverUse.Used,
        };
        var consumed = new List<string>();
        foreach (var entry in Registry.Entries
            .Where(entry => entry.Row == CorrespondenceRow.Assertion && entry.Status == EntryStatus.Implemented))
        {
            // Both directions, against each other — never against a baseline. Comparing a flip
            // with Complete() is vacuous for an entry Complete() already asserts that way, which
            // it is for collision-hazard-proximity: § 107.37(b)'s proposition holding is the
            // prohibited state, so Complete() asserts it false, and "flipping" it to false probed
            // nothing. Asserting each value explicitly makes Holds the only difference for every
            // entry alike.
            var holds = OperationEvaluator.Evaluate(
                facts.Asserting(new Assertion(MapEntry(entry.Id), true, entry.AssertedBy[0])));
            var doesNot = OperationEvaluator.Evaluate(
                facts.Asserting(new Assertion(MapEntry(entry.Id), false, entry.AssertedBy[0])));
            var movedElsewhere = holds.Requirements
                .Zip(doesNot.Requirements, (whenItHolds, whenItDoesNot) => (whenItHolds, whenItDoesNot))
                .Any(pair => pair.whenItHolds != pair.whenItDoesNot
                    && !string.Equals(pair.whenItHolds.EntryId, entry.Id, StringComparison.Ordinal));
            if (movedElsewhere)
            {
                consumed.Add(entry.Id);
            }
        }

        return [.. consumed];
    }

    [Fact]
    public void The_cost_recorded_in_decision_0004_is_the_cost_the_engine_actually_has()
    {
        // docs/decisions/0004 names these four, and names the six that have no built consumer. The
        // number has been wrong twice by being written at one head and left at another, so it is
        // pinned here rather than proof-read: build a consumer for one of the six and this goes
        // red, naming the record to update.
        string[] recorded =
        [
            "reasonable-protection",
            "flash-rate-sufficient",
            "intensity-reduction-in-interest-of-safety",
            "unaided-visual-contact",
            "observer-coordination",
            "preflight-risk-assessment",
            "participant-briefing",
            "sufficient-available-power",
            "attached-object-no-adverse-effect",
        ];

        var measured = AssertionsABuiltConsumerReads();

        Assert.Equal(
            recorded.Order(StringComparer.Ordinal),
            measured.Order(StringComparer.Ordinal));

        // And the rest of the census the record states: ten assertion entries, six of them with no
        // built consumer to supply the polarity.
        var rowEight = Registry.Entries
            .Where(entry => entry.Row == CorrespondenceRow.Assertion && entry.Status == EntryStatus.Implemented)
            .ToArray();

        Assert.Equal(10, rowEight.Length);
        Assert.Equal(1, rowEight.Length - measured.Length);

        // The probe is only worth anything where the flip actually takes effect, so check that it
        // does for every entry — including collision-hazard-proximity, which Complete() asserts
        // false and for which a flip against that baseline would have been a no-op.
        var facts = Complete() with
        {
            Lighting = LightingStatement.IntensityReducedAndVisibleFor(5m, Caller),
            VisualObserverUse = VisualObserverUse.Used,
        };
        foreach (var entry in rowEight)
        {
            var holds = OperationEvaluator.Evaluate(
                facts.Asserting(new Assertion(MapEntry(entry.Id), true, entry.AssertedBy[0])));
            var doesNot = OperationEvaluator.Evaluate(
                facts.Asserting(new Assertion(MapEntry(entry.Id), false, entry.AssertedBy[0])));

            Assert.NotEqual(holds.Requirement(entry.Id), doesNot.Requirement(entry.Id));
        }
    }

    [Fact]
    public void No_assertion_entry_is_ever_reported_as_satisfied_or_violated()
    {
        // Registry-driven: every entry the map records as kind: assertion (correspondence row 8)
        // and this engine has built, asked with the fact supplied in both directions.
        var rowEight = Registry.Entries
            .Where(entry => entry.Row == CorrespondenceRow.Assertion && entry.Status == EntryStatus.Implemented)
            .ToArray();

        Assert.NotEmpty(rowEight);

        foreach (var holds in new[] { true, false })
        {
            var facts = rowEight.Aggregate(
                Complete(),
                (built, entry) => built.Asserting(new Assertion(MapEntry(entry.Id), holds, entry.AssertedBy[0])));
            var evaluation = OperationEvaluator.Evaluate(facts);

            foreach (var entry in rowEight)
            {
                var outcome = evaluation.Requirement(entry.Id);
                Assert.NotEqual(RequirementState.Satisfied, outcome.State);
                Assert.NotEqual(RequirementState.Violated, outcome.State);
                Assert.NotEqual(RequirementState.ActionRequired, outcome.State);
            }
        }
    }

    /// <summary>The map's own <see cref="MapEntry"/> for <paramref name="entryId"/>, from generated <see cref="MapEntries"/>.</summary>
    private static MapEntry MapEntry(string entryId) =>
        typeof(MapEntries).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => property.GetValue(null))
            .OfType<MapEntry>()
            .Single(entry => string.Equals(entry.Id, entryId, StringComparison.Ordinal));

    [Fact]
    public void An_entry_this_engine_has_not_built_is_not_a_finding_about_the_operation()
    {
        var facts = Complete();
        var entry = Registry.Entries
            .First(candidate => candidate.Status != EntryStatus.Implemented && candidate.Row == CorrespondenceRow.NotBuilt);

        var outcome = Outcome(entry.Id, facts);

        Assert.Equal(RequirementState.NotBuilt, outcome.State);
        Assert.Equal(UnresolvedReason.UnsupportedRule, outcome.Reason);
        Assert.Equal(EntryStatus.Mapped, outcome.Status);
        Assert.Null(outcome.Finding);

        // Nothing whatever follows about the operation from a hole in the engine.
        Assert.NotEqual(RequirementState.Satisfied, outcome.State);
        Assert.NotEqual(RequirementState.Violated, outcome.State);
    }

    [Fact]
    public void A_composite_is_resolved_through_its_own_entry_and_never_rebuilt_from_its_leaves()
    {
        var facts = Complete();

        // Its three constituents answer, and they do not agree with each other.
        Assert.Equal(RequirementState.Satisfied, State("speed-within-limit", facts));
        Assert.Equal(RequirementState.Satisfied, State("altitude-within-limit", facts));
        Assert.Equal(RequirementState.Violated, State("weather-minimums-met", facts));

        var composite = Outcome("operating-limitations", facts);
        var finding = Assert.IsType<OperatingLimitationsFinding>(composite.Finding);

        // The answer is § 107.51's introductory text's own, made by its own rule from what its
        // constituents said, and cited to its own locator — not a conjunction this orchestrator
        // computed from the three outcomes above.
        Assert.Equal(RequirementState.Violated, composite.State);
        Assert.False(finding.CompliedWith);
        Assert.Equal(MapEntries.OperatingLimitations.Locator, composite.Locator);
        Assert.Equal(3, finding.Limitations.Count);
        Assert.NotEqual(MapEntries.SpeedWithinLimit.Locator, composite.Locator);
    }

    [Fact]
    public void A_stated_absence_of_an_authorization_is_an_answer_and_not_a_question()
    {
        var facts = Complete();

        // § 107.41, the headline case: Class B, and the caller has stated they hold no prior ATC
        // authorization. The rule says the section prohibits the operation, and so does this.
        var none = Outcome(
            "airspace-authorized",
            facts with { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.None(Caller) });
        var noneFinding = Assert.IsType<AirspaceFinding>(none.Finding);

        Assert.Equal(RequirementState.Violated, none.State);
        Assert.NotEqual(RequirementState.ActionRequired, none.State);
        Assert.False(noneFinding.MayOperate);

        // What the caller could still obtain is not lost: it is on the rule's own finding, where
        // the rule put it, and so is the fact that the caller answered.
        Assert.True(noneFinding.AuthorizationRequired);
        Assert.False(noneFinding.Authorization.Held);

        // An authorization held but not obtained before the operation is a different fact, and the
        // finding still says which — the distinction AtcAuthorization keeps is not flattened here.
        var notPrior = Outcome(
            "airspace-authorized",
            facts with { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.NotPrior(Caller, "ATC 2026-0002") });
        var notPriorFinding = Assert.IsType<AirspaceFinding>(notPrior.Finding);

        Assert.Equal(RequirementState.Violated, notPrior.State);
        Assert.True(notPriorFinding.Authorization.Held);
        Assert.False(notPriorFinding.Authorization.ObtainedBeforeTheOperation);

        // § 107.45 has the identical shape and is answered the identical way.
        var noPermission = Outcome(
            "restricted-area-permitted",
            facts with { Area = AreaDesignation.Prohibited, AreaPermission = PermissionStatement.NotGranted(Caller) });
        var permissionFinding = Assert.IsType<AreaPermissionFinding>(noPermission.Finding);

        Assert.Equal(RequirementState.Violated, noPermission.State);
        Assert.True(permissionFinding.PermissionRequired);

        // And saying nothing at all is still a third answer, distinct from both.
        Assert.Equal(
            RequirementState.FactRequired,
            State("airspace-authorized", facts with { Airspace = AirspaceClass.ClassB, AtcAuthorization = null }));
    }

    [Fact]
    public void A_section_that_does_not_reach_the_operation_is_not_reported_as_complied_with()
    {
        var notUsed = Outcome("visual-observer-conditions", Complete() with { VisualObserverUse = VisualObserverUse.NotUsed });
        var used = Outcome("visual-observer-conditions", Complete() with { VisualObserverUse = VisualObserverUse.Used });

        // § 107.33's chapeau makes the section conditional, and where no visual observer is used it
        // states no requirement to meet. The rule says so with a null verdict and a SectionApplies
        // of its own; reading that null as compliance would be the engine inventing an answer.
        var finding = Assert.IsType<VisualObserverConditionsFinding>(notUsed.Finding);
        Assert.False(finding.SectionApplies);
        Assert.Null(finding.AllRequirementsMet);
        Assert.Equal(RequirementState.Informational, notUsed.State);
        Assert.NotEqual(RequirementState.Satisfied, notUsed.State);
        Assert.NotEqual(RequirementState.Violated, notUsed.State);

        // Where the section does reach the operation, the answer is a different one.
        Assert.NotEqual(notUsed.State, used.State);
    }

    [Fact]
    public void The_three_ways_107_39_can_want_something_are_three_different_answers()
    {
        var facts = Complete();

        // Where the human being is, not stated at all: the engine locates nobody.
        var unlocated = Outcome("over-human-beings", facts with { HumanBeingLocation = null });
        Assert.Equal(RequirementState.FactRequired, unlocated.State);
        Assert.Equal(nameof(Requests.OverHumanBeingsRequest.Location), unlocated.MissingInput);

        // Located under one of § 107.39(b)'s two places, and the standard asserted for neither:
        // a demand on a person, and never a decline.
        var unasserted = OperationEvaluator
            .Evaluate(new OperationFacts { HumanBeingLocation = HumanBeingLocation.UnderACoveredStructure, Shelter = Shelter.CoveredStructure }
                .Stating(WaiverStatement.NoneHeld(Overflight.Regulation, Caller)))
            .Requirement("over-human-beings");
        Assert.Equal(RequirementState.HumanAssertionRequired, unasserted.State);
        Assert.Null(unasserted.Reason);

        // What is owed is reasonable-protection's assertion at § 107.39(b), not this entry's, and
        // a product can name it without reading the message.
        Assert.Equal("over-human-beings", unasserted.EntryId);
        Assert.Equal("reasonable-protection", unasserted.AssertionOwed);
        Assert.Equal(MapEntries.ReasonableProtection.Locator, unasserted.AssertionCites);
        Assert.NotEqual(unasserted.Locator, unasserted.AssertionCites);
        Assert.NotEmpty(unasserted.AssertedBy);
        Assert.Equal(Registry.Entry("reasonable-protection").AssertedBy, unasserted.AssertedBy);

        // Both facts stated, and about different places. The caller has spoken twice and the rule
        // will not read the one as the other, so it is still owed the value for the place in hand —
        // reported with the rule's own words, which name both places.
        var mismatched = Outcome(
            "over-human-beings",
            facts with { HumanBeingLocation = HumanBeingLocation.InsideAStationaryVehicle, Shelter = Shelter.CoveredStructure });
        Assert.Equal(RequirementState.FactRequired, mismatched.State);
        Assert.Equal(nameof(Requests.OverHumanBeingsRequest.Shelter), mismatched.MissingInput);
        Assert.Contains("stationary vehicle", mismatched.Explanation, StringComparison.Ordinal);
        Assert.Contains("covered structure", mismatched.Explanation, StringComparison.Ordinal);

        // The three are told apart by what they name, not by sharing a state: the two that ask for
        // a fact name different inputs, and the one that asks a person is a different state.
        Assert.NotEqual(unlocated.MissingInput, mismatched.MissingInput);
        Assert.NotEqual(unlocated.State, unasserted.State);

        // And where the facts agree, § 107.39 answers.
        var answered = Outcome("over-human-beings", facts);
        Assert.Equal(RequirementState.Satisfied, answered.State);
        Assert.IsType<OverHumanBeingsFinding>(answered.Finding);
    }

    [Fact]
    public void Every_outcome_carries_the_identity_its_state_promises()
    {
        // The audit, mechanised. Everything the evaluator reads has some identity of its own —
        // UnresolvedResult's reason and locator, AssertionRequiredException's entry id,
        // ArgumentException's parameter name, the rule's finding — and each was dropped once in
        // favour of the evaluated entry's. This asserts the invariants across fact sets, so the
        // next one that goes missing is caught here rather than by a reviewer reading the diff.
        foreach (var facts in new[]
        {
            OperationFacts.Nothing,
            Stated(),
            Complete(),
            Complete() with { OperationPlace = OperationPlace.InAlaska },
            Complete().Stating(WaiverStatement.Held(Speed.Regulation, Caller)),
        })
        {
            foreach (var outcome in OperationEvaluator.Evaluate(facts).Requirements)
            {
                var what = $"{outcome.EntryId} in {outcome.State}";

                // Every clause is a biconditional — "and nothing else does" is half the point, and
                // the half that was missing on MissingInput — and every clause names the entry, so
                // a failure says which one rather than "Expected: True, Actual: False".

                var declined = outcome.State is RequirementState.RequiresInterpretation
                    or RequirementState.OutsideCurrentScope or RequirementState.NotBuilt
                    or RequirementState.MissingRulesData or RequirementState.UnresolvedInteraction;
                var answered = outcome.State is RequirementState.Satisfied or RequirementState.Violated
                    or RequirementState.ActionRequired or RequirementState.Informational
                    or RequirementState.HumanAssertionRecorded;

                // A decline names why and where the real rule lives; nothing else does.
                Assert.True((outcome.Reason is not null) == (outcome.DeclineCites is not null), what);
                Assert.True((outcome.Reason is not null) == declined, what);

                // An assertion owed names which, where, and who may make it; nothing else does.
                Assert.True(
                    (outcome.State == RequirementState.HumanAssertionRequired) == (outcome.AssertionOwed is not null),
                    what);
                Assert.True((outcome.AssertionOwed is not null) == (outcome.AssertionCites is not null), what);

                // AssertedBy is "who may assert the fact this outcome is about", which is in play
                // in two cases and exactly two: the entry evaluated is itself kind: assertion — and
                // then it is non-empty whatever the state, because who may assert § 107.39(b) does
                // not depend on the caller having stated a Shelter — or an assertion is owed, and
                // then it is the owed entry's. Empty otherwise.
                Assert.True(
                    !outcome.AssertedBy.IsEmpty
                        == (outcome.Row == CorrespondenceRow.Assertion || outcome.AssertionOwed is not null),
                    what);
                if (outcome.State == RequirementState.HumanAssertionRequired)
                {
                    Assert.False(outcome.AssertedBy.IsEmpty, what);
                }

                // A fact owed names which input; nothing else carries one.
                Assert.True((outcome.State == RequirementState.FactRequired) == (outcome.MissingInput is not null), what);
                if (outcome.State == RequirementState.FactRequired)
                {
                    Assert.False(string.IsNullOrWhiteSpace(outcome.MissingInput), what);
                }

                // An answer carries the rule's own finding, and the engine's own words always.
                Assert.True(answered == (outcome.Finding is not null), what);
                Assert.NotEmpty(outcome.Explanation);

                // The five states partition: every outcome is a decline, an answer, or something
                // the caller owes, and never two of those.
                Assert.False(declined && answered, what);
            }
        }
    }

    [Fact]
    public void Every_assertion_the_engine_demands_is_named_with_where_it_is_stated_and_who_may_make_it()
    {
        // Waivers stated, attestations not: the case a caller reaches first, and the one that
        // reaches every composite's demand for a constituent's assertion. Registry-driven, so a
        // composite built later is covered.
        var owed = OperationEvaluator.Evaluate(Stated()).InState(RequirementState.HumanAssertionRequired);

        Assert.NotEmpty(owed);

        foreach (var outcome in owed)
        {
            Assert.False(string.IsNullOrEmpty(outcome.AssertionOwed), $"{outcome.EntryId} owes an assertion it does not name");

            var demanded = Registry.Entry(outcome.AssertionOwed!);

            Assert.Equal(CorrespondenceRow.Assertion, demanded.Row);
            Assert.Equal(demanded.Locator, outcome.AssertionCites);
            Assert.Equal(demanded.AssertedBy, outcome.AssertedBy);
            Assert.NotEmpty(outcome.AssertedBy);
            Assert.Null(outcome.Reason);
        }

        // And at least one is a composite owing another entry's assertion, which is the case that
        // was empty: the promise on AssertedBy is kept where the evaluated entry is not itself
        // kind: assertion.
        var composites = owed.Where(outcome => !string.Equals(outcome.AssertionOwed, outcome.EntryId, StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(composites);
        Assert.All(composites, outcome => Assert.NotEqual(CorrespondenceRow.Assertion, outcome.Row));
    }

    [Fact]
    public void A_decline_carries_the_citation_of_the_rule_that_could_not_answer()
    {
        // Three declines whose own citation is not the entry's, which is why it is carried
        // separately: the entry says what was asked, the decline says where the real rule lives.
        var suspended = Outcome(
            "speed-within-limit",
            Complete().Stating(WaiverStatement.Held(Speed.Regulation, Caller)));
        Assert.Equal(MapEntries.SpeedWithinLimit.Locator, suspended.Locator);
        Assert.Equal(MapEntries.WaivableRegulations.Locator, suspended.DeclineCites);
        Assert.NotEqual(suspended.Locator, suspended.DeclineCites);

        // § 107.51(a)'s two printed figures: the question is speed-limit's, and so is the citation
        // (docs/decisions/0001).
        var betweenFigures = Outcome("speed-within-limit", Complete() with { Groundspeed = Groundspeed.InKnots(87m) });
        Assert.Equal(RequirementState.RequiresInterpretation, betweenFigures.State);
        Assert.Equal(MapEntries.SpeedLimit.Locator, betweenFigures.DeclineCites);

        // An entry that answers carries no decline citation at all.
        Assert.Null(Outcome("speed-within-limit", Complete()).DeclineCites);

        // And every decline carries one.
        foreach (var outcome in OperationEvaluator.Evaluate(Complete()).Requirements)
        {
            Assert.Equal(outcome.Reason is not null, outcome.DeclineCites is not null);
        }
    }

    [Fact]
    public void A_section_that_can_never_resolve_complete_still_answers_when_an_obligation_is_not_done()
    {
        // § 107.49 conjoins obligations two of which state no measure — (c) "working properly" and
        // the "is secure" half of (e) — so with every assertion answered true the section still
        // cannot be resolved complied with, and the entry declines rather than inventing a verdict.
        var undetermined = Outcome("preflight-actions", Complete());

        Assert.Equal(RequirementState.RequiresInterpretation, undetermined.State);
        Assert.NotNull(undetermined.DeclineCites);
        Assert.Null(undetermined.Finding);

        // And one obligation answered not done settles the conjunction against the operation,
        // whatever the undetermined ones would have said. That is the rule's own reading, and this
        // arm reads AllDone forward like every other.
        var notDone = Outcome(
            "preflight-actions",
            Complete().Asserting(new Assertion(MapEntries.SufficientAvailablePower, false, RemotePilotInCommand)));
        var finding = Assert.IsType<PreflightActionsFinding>(notDone.Finding);

        Assert.Equal(RequirementState.Violated, notDone.State);
        Assert.False(finding.AllDone);
        Assert.Null(notDone.Reason);

        // The assertion itself is still recorded and not scored, on its own entry.
        Assert.Equal(
            RequirementState.HumanAssertionRecorded,
            State("sufficient-available-power", Complete().Asserting(new Assertion(MapEntries.SufficientAvailablePower, false, RemotePilotInCommand))));
    }

    [Fact]
    public void A_stated_place_can_move_an_entry_into_what_the_engine_cannot_determine()
    {
        var facts = Complete();

        // Outside Alaska § 107.29(c)(1)-(2) define civil twilight and the entry answers.
        var outside = Outcome("civil-twilight-operation", facts with { OperationPlace = OperationPlace.OutsideAlaska });
        Assert.Equal(RequirementState.Satisfied, outside.State);

        // In Alaska the definition is civil-twilight-alaska's, which this engine has not got, so a
        // fact the caller stated moves the entry into missing rules data — the engine naming a hole
        // in itself. That is not "violated", and it is not "we did not say".
        var inAlaska = Outcome("civil-twilight-operation", facts with { OperationPlace = OperationPlace.InAlaska });
        Assert.Equal(RequirementState.MissingRulesData, inAlaska.State);
        Assert.Equal(UnresolvedReason.MissingRulesData, inAlaska.Reason);
        Assert.Equal(MapEntries.CivilTwilightOperation.Locator, inAlaska.Locator);
        Assert.Equal(MapEntries.CivilTwilightAlaska.Locator, inAlaska.DeclineCites);
        Assert.Null(inAlaska.Finding);
        Assert.NotEqual(outside.State, inAlaska.State);

        // And the paragraph's third answer, which is neither verdict: during neither period it says
        // nothing about the operation, and DuringCivilTwilight on the finding says so.
        var neither = Outcome("civil-twilight-operation", facts with { OperationPeriod = OperationPeriod.NeitherPeriod });
        var finding = Assert.IsType<CivilTwilightOperationFinding>(neither.Finding);
        Assert.Equal(RequirementState.Informational, neither.State);
        Assert.Null(finding.Permitted);
        Assert.False(finding.DuringCivilTwilight);
        Assert.NotEqual(RequirementState.Satisfied, neither.State);
        Assert.NotEqual(RequirementState.Violated, neither.State);
    }

    [Fact]
    public void No_rule_this_engine_has_built_reports_that_an_action_is_required()
    {
        // docs/decisions/0005: every rule here that names an obtainable thing also demands the
        // caller's statement about it, so the caller has either not spoken (FactRequired) or has
        // been answered (Violated). The state stays in the enumeration for a rule that draws the
        // distinction itself; nothing produces it today, and this says so out loud.
        foreach (var facts in new[]
        {
            OperationFacts.Nothing,
            Complete(),
            Complete() with { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.None(Caller) },
            Complete() with { Area = AreaDesignation.Restricted, AreaPermission = PermissionStatement.NotGranted(Caller) },
        })
        {
            var evaluation = OperationEvaluator.Evaluate(facts);

            Assert.Empty(evaluation.InState(RequirementState.ActionRequired));
            Assert.DoesNotContain(evaluation.Outstanding, outcome => outcome.State == RequirementState.ActionRequired);
        }
    }

    [Fact]
    public void A_value_a_rule_refuses_is_a_fault_and_is_not_dressed_as_a_fact_the_caller_owes()
    {
        // § 107.51(c)'s rule refuses a negative flight visibility with ArgumentOutOfRangeException.
        // That is a malformed value, not an input the caller failed to supply, and reporting it as
        // FactRequired would tell a product to go and ask somebody for something it already has.
        //
        // The cloud distances cannot reach the evaluator malformed at all: they are carried by a
        // CloudStatement, which refuses a negative distance when the statement is made. Same
        // policy, one step earlier.
        var facts = Complete() with { FlightVisibilityStatuteMiles = -1m };

        Assert.Throws<ArgumentOutOfRangeException>(() => OperationEvaluator.Evaluate(facts));

        // The same fact supplied properly is answered, so this is about the value and not the field.
        Assert.Equal(RequirementState.Violated, State("weather-minimums-met", Complete()));
    }

    [Fact]
    public void The_five_things_the_engine_cannot_answer_are_five_different_answers()
    {
        var facts = Complete();

        // "the engine cannot determine this", from three different causes the map distinguishes;
        // "the engine has not built this"; and "this is outside the engine's scope".
        var notBuilt = Registry.Entries
            .First(entry => entry.Status != EntryStatus.Implemented && entry.Row == CorrespondenceRow.NotBuilt);

        Assert.Equal(RequirementState.RequiresInterpretation, State("control-links-working", facts));
        Assert.Equal(RequirementState.MissingRulesData, State("night-operation", facts));
        Assert.Equal(RequirementState.NotBuilt, State(notBuilt.Id, facts));
        Assert.Equal(RequirementState.OutsideCurrentScope, State("knowledge-recency", facts));

        // And the two that are demands on the caller, which are neither of those.
        Assert.Equal(RequirementState.HumanAssertionRequired, State("sufficient-available-power", OperationFacts.Nothing));
        Assert.Equal(RequirementState.FactRequired, State("speed-within-limit", OperationFacts.Nothing));

        var states = new[]
        {
            State("control-links-working", facts),
            State("night-operation", facts),
            State(notBuilt.Id, facts),
            State("knowledge-recency", facts),
            State("sufficient-available-power", OperationFacts.Nothing),
            State("speed-within-limit", OperationFacts.Nothing),
        };

        Assert.Equal(states.Length, states.Distinct().Count());
    }

    [Fact]
    public void Every_kernel_decline_reason_is_reported_as_a_state_of_its_own()
    {
        var reasons = Enum.GetValues<UnresolvedReason>();

        var states = reasons.Select(RequirementStates.For).ToArray();

        Assert.Equal(reasons.Length, states.Distinct().Count());
        Assert.Equal(RequirementState.NotBuilt, RequirementStates.For(UnresolvedReason.UnsupportedRule));
        Assert.Equal(RequirementState.RequiresInterpretation, RequirementStates.For(UnresolvedReason.RequiresInterpretation));
        Assert.Equal(RequirementState.OutsideCurrentScope, RequirementStates.For(UnresolvedReason.OutsideCurrentScope));
        Assert.Equal(RequirementState.MissingRulesData, RequirementStates.For(UnresolvedReason.MissingRulesData));
        Assert.Equal(RequirementState.UnresolvedInteraction, RequirementStates.For(UnresolvedReason.UnsupportedInteraction));
    }

    /// <summary>
    /// Every entry whose rule states a compliance verdict, in both directions where the rule can
    /// reach both. The polarity is the point: <c>Prohibited</c> reads the other way round from
    /// <c>WithinLimit</c>, <c>Permitted</c>, <c>MayPass</c> and <c>MayOperate</c>, and one arm read
    /// backwards would look perfectly natural.
    /// </summary>
    public static TheoryData<string, RequirementState, Func<OperationFacts, OperationFacts>> Verdicts =>
        new()
        {
            // § 107.51(a), within the limit and beyond both printed figures.
            { "speed-within-limit", RequirementState.Satisfied, f => f with { Groundspeed = Groundspeed.InKnots(50m) } },
            { "speed-within-limit", RequirementState.Violated, f => f with { Groundspeed = Groundspeed.InKnots(120m) } },

            // § 107.51(b), under the ceiling and above it with no structure stated.
            { "altitude-within-limit", RequirementState.Satisfied, f => f with { AltitudeAboveGroundLevelFeet = 300m } },
            { "altitude-within-limit", RequirementState.Violated, f => f with { AltitudeAboveGroundLevelFeet = 500m } },

            // § 107.51(c)-(d): the one finding the engine can resolve is that they are not met.
            { "weather-minimums-met", RequirementState.Violated, f => f },

            // § 107.29(a)(2) and (b): lighting lighted and visible far enough, and none fitted.
            {
                "anti-collision-lighting", RequirementState.Satisfied,
                f => f with { Lighting = LightingStatement.LightedAndVisibleFor(5m, Caller) }
            },
            {
                "anti-collision-lighting", RequirementState.Violated,
                f => f with { Lighting = LightingStatement.NoneFitted(Caller) }
            },
            {
                // Fitted, and extinguished: the clause is not met, and an arm reading "fitted"
                // alone would say it was.
                "anti-collision-lighting", RequirementState.Violated,
                f => f with { Lighting = LightingStatement.FittedButExtinguished(Caller) }
            },
            {
                // Lighted, and not visible for the printed distance: likewise.
                "anti-collision-lighting", RequirementState.Violated,
                f => f with { Lighting = LightingStatement.LightedAndVisibleFor(1m, Caller) }
            },

            // § 107.29(b)-(c): during a period of civil twilight with the lighting the paragraph's
            // unless-clause requires, and without it. The third answer is below, not here.
            {
                "civil-twilight-operation", RequirementState.Satisfied,
                f => f with { OperationPeriod = OperationPeriod.BeforeOfficialSunrise }
            },
            {
                "civil-twilight-operation", RequirementState.Violated,
                f => f with
                {
                    OperationPeriod = OperationPeriod.AfterOfficialSunset,
                    Lighting = LightingStatement.FittedButExtinguished(Caller),
                }
            },
            {
                // Neither period: § 107.29(b) states no prohibition about this operation, which is
                // not a permission and not a breach.
                "civil-twilight-operation", RequirementState.Informational,
                f => f with { OperationPeriod = OperationPeriod.NeitherPeriod }
            },

            // § 107.33 as a whole, with no visual observer used: the section states no requirement
            // to meet, which is neither satisfied nor violated.
            {
                "visual-observer-conditions", RequirementState.Informational,
                f => f with { VisualObserverUse = VisualObserverUse.NotUsed }
            },

            // § 107.31 as a whole: paragraph (b)'s first combination, and nobody at all.
            {
                "visual-line-of-sight", RequirementState.Satisfied,
                f => f with { Exercise = new ExerciseOfTheAbility(true, true, false) }
            },
            {
                "visual-line-of-sight", RequirementState.Violated,
                f => f with { Exercise = ExerciseOfTheAbility.Nobody }
            },

            // § 107.35, one aircraft and two.
            {
                "single-aircraft", RequirementState.Satisfied,
                f => f with { Engagements = [new AircraftEngagement("N123AB", AircraftRole.RemotePilotInCommand)] }
            },
            {
                "single-aircraft", RequirementState.Violated,
                f => f with
                {
                    Engagements =
                    [
                        new AircraftEngagement("N123AB", AircraftRole.RemotePilotInCommand),
                        new AircraftEngagement("N456CD", AircraftRole.VisualObserver),
                    ],
                }
            },

            // § 107.37(a): the section does not reach a pass neither enumeration names.
            {
                "right-of-way", RequirementState.Satisfied,
                f => f with { Encountered = EncounteredObject.Aircraft, Position = RelativePosition.NoneOfThem }
            },

            // § 107.25(b): the polarity inverts here — Prohibited true is a violation.
            {
                "moving-vehicle-operation", RequirementState.Satisfied,
                f => f with { FromMovingLandOrWaterBorneVehicle = false }
            },
            {
                "moving-vehicle-operation", RequirementState.Violated,
                f => f with
                {
                    FromMovingLandOrWaterBorneVehicle = true,
                    TransportingAnotherPersonsPropertyForCompensationOrHire = true,
                }
            },

            // § 107.25(a): likewise inverted.
            { "moving-aircraft-operation", RequirementState.Satisfied, f => f with { FromAMovingAircraft = false } },
            { "moving-aircraft-operation", RequirementState.Violated, f => f with { FromAMovingAircraft = true } },

            // § 107.51 as a whole, through its own entry: the conjunction the rule makes from what
            // its three constituents answered. It cannot resolve met, because weather-minimums-met
            // cannot; that is that entry's limit and not this one's.
            { "operating-limitations", RequirementState.Violated, f => f },

            // § 107.41: the airspace the section does not name; the one it names with a prior ATC
            // authorization; and the two ways the section prohibits it — a stated absence, and an
            // authorization held but not obtained before the operation. Both of the last two are
            // the rule saying no (docs/decisions/0005), not a question put back to the caller.
            { "airspace-authorized", RequirementState.Satisfied, f => f with { Airspace = AirspaceClass.ClassG } },
            {
                "airspace-authorized", RequirementState.Satisfied,
                f => f with { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.Prior(Caller, "ATC 2026-0001") }
            },
            {
                "airspace-authorized", RequirementState.Violated,
                f => f with { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.None(Caller) }
            },
            {
                "airspace-authorized", RequirementState.Violated,
                f => f with { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.NotPrior(Caller, "ATC 2026-0002") }
            },

            // § 107.45: the same shape, with the permission the section names.
            {
                "restricted-area-permitted", RequirementState.Satisfied,
                f => f with { Area = AreaDesignation.NeitherProhibitedNorRestricted }
            },
            {
                "restricted-area-permitted", RequirementState.Violated,
                f => f with { Area = AreaDesignation.Prohibited, AreaPermission = PermissionStatement.NotGranted(Caller) }
            },
            {
                "restricted-area-permitted", RequirementState.Satisfied,
                f => f with { Area = AreaDesignation.Restricted, AreaPermission = PermissionStatement.Granted(Caller, "the using agency") }
            },
        };

    [Theory]
    [MemberData(nameof(Verdicts))]
    public void Each_verdict_is_read_from_its_own_rule_in_its_own_direction(
        string entryId,
        RequirementState expected,
        Func<OperationFacts, OperationFacts> state)
    {
        var outcome = Outcome(entryId, state(Complete()));

        Assert.Equal(expected, outcome.State);
        Assert.NotNull(outcome.Finding);
        Assert.Null(outcome.Reason);
    }

    /// <summary>The entries that state what the rule says rather than whether an operation complies.</summary>
    public static TheoryData<string> ValueEntries =>
        ["speed-limit", "altitude-limit", "visibility-minimum", "cloud-clearance", "civil-twilight-window", "night-waiver-termination"];

    [Theory]
    [MemberData(nameof(ValueEntries))]
    public void An_entry_that_states_a_figure_is_informational_and_never_satisfied(string entryId)
    {
        var outcome = Outcome(entryId, Complete());

        Assert.Equal(RequirementState.Informational, outcome.State);
        Assert.NotNull(outcome.Finding);
    }

    [Fact]
    public void The_evaluation_does_not_collapse_to_a_boolean_and_offers_no_way_to_claim_one()
    {
        var members = typeof(OperationEvaluation).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Concat(typeof(EvaluatedRequirement).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(member => member is PropertyInfo or MethodInfo or FieldInfo)
            .ToArray();

        foreach (var member in members)
        {
            var type = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                MethodInfo method => method.ReturnType,
                _ => typeof(void),
            };

            // Equals and GetHashCode are object's contract, not this engine's claim about an
            // operation; everything else that could answer yes or no is refused.
            if (member.Name is nameof(Equals) or "op_Equality" or "op_Inequality")
            {
                continue;
            }

            Assert.False(
                type == typeof(bool) || type == typeof(bool?),
                $"{member.DeclaringType?.Name}.{member.Name} answers yes or no; this engine's answer is a state, not a verdict");
        }

        foreach (var word in new[] { "Compliant", "Legal", "Lawful", "Passed", "Valid", "Approved", "Clear" })
        {
            Assert.DoesNotContain(members, member => member.Name.Contains(word, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void The_result_can_be_rendered_incrementally_without_losing_a_state()
    {
        // Complete facts answer everything the engine can answer, so a caller with something
        // still to do is the case worth rendering: an airspace § 107.41 names with no prior
        // authorization stated, and a certificate § 107.29(d) asks about that was never described.
        var evaluation = OperationEvaluator.Evaluate(Complete() with
        {
            Airspace = AirspaceClass.ClassB,
            AtcAuthorization = AtcAuthorization.None(Caller),
            NightWaiverCertificate = null,
        });

        var outstanding = evaluation.Outstanding;
        var unanswered = evaluation.Unanswered;

        Assert.NotEmpty(outstanding);
        Assert.NotEmpty(unanswered);
        Assert.All(
            outstanding,
            outcome => Assert.Contains(
                outcome.State,
                new[] { RequirementState.ActionRequired, RequirementState.HumanAssertionRequired, RequirementState.FactRequired }));
        Assert.All(
            unanswered,
            outcome => Assert.Contains(
                outcome.State,
                new[]
                {
                    RequirementState.RequiresInterpretation,
                    RequirementState.OutsideCurrentScope,
                    RequirementState.NotBuilt,
                    RequirementState.MissingRulesData,
                    RequirementState.UnresolvedInteraction,
                }));
        Assert.Empty(outstanding.Intersect(unanswered));

        var counted = 0;
        foreach (var state in Enum.GetValues<RequirementState>())
        {
            counted += evaluation.Count(state);
            Assert.Equal(evaluation.Count(state), evaluation.InState(state).Length);
        }

        Assert.Equal(evaluation.Requirements.Length, counted);
    }

    [Fact]
    public void The_same_facts_evaluate_the_same_way_every_time()
    {
        var facts = Complete();

        var first = OperationEvaluator.Evaluate(facts);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(first, OperationEvaluator.Evaluate(facts));
        }
    }

    [Fact]
    public void The_evaluation_carries_the_engine_s_own_identity_and_its_own_provenance()
    {
        var evaluation = OperationEvaluator.Evaluate(OperationFacts.Nothing);

        Assert.Equal(Ruleset.Identity, evaluation.EvaluatedBy);
        Assert.Equal("faa-part-107", evaluation.EvaluatedBy.Ruleset.Id);
        Assert.Equal(MapEntries.Baseline, Assert.Single(evaluation.EvaluatedBy.SourceBaselines));
        Assert.Equal(EngineProvenance.ReadBytes(), evaluation.ProvenanceJson());
    }

    [Fact]
    public void A_fact_supplied_reaches_the_rule_that_reads_it_and_no_other()
    {
        var facts = Complete();

        // Changing one rule's input changes that requirement and nothing else.
        var before = OperationEvaluator.Evaluate(facts);
        var after = OperationEvaluator.Evaluate(facts with { Groundspeed = Groundspeed.InKnots(120m) });

        var changed = before.Requirements
            .Zip(after.Requirements, (first, second) => (first, second))
            .Where(pair => pair.first != pair.second)
            .Select(pair => pair.first.EntryId)
            .ToArray();

        // The entries that may move are exactly the ones whose own request declares a groundspeed:
        // § 107.51(a) and § 107.51's composite, which reads the same fact through its own entry.
        // Read from the generated request types, so an entry that declares it later is allowed for
        // and an entry that does not is still refused.
        var readers = Registry.Entries
            .Where(entry => RequestTypeOf(entry.Id).GetProperty(nameof(OperationFacts.Groundspeed)) is not null)
            .Select(entry => entry.Id)
            .ToArray();

        Assert.Contains("speed-within-limit", changed);
        Assert.All(changed, entryId => Assert.Contains(entryId, readers));
        Assert.DoesNotContain("altitude-within-limit", changed);
        Assert.DoesNotContain("airspace-authorized", changed);
    }

    /// <summary>The generated request type of <paramref name="entryId"/>, from its own entry point.</summary>
    private static Type RequestTypeOf(string entryId) =>
        typeof(EntryPoints).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => property.GetValue(null))
            .OfType<object>()
            .Single(entry => string.Equals(
                (string)entry.GetType().GetProperty("Id")!.GetValue(entry)!,
                entryId,
                StringComparison.Ordinal))
            .GetType()
            .GetGenericArguments()[0];

    [Fact]
    public void Nothing_stated_is_a_meaningful_question_and_never_an_answer_the_engine_invented()
    {
        var evaluation = OperationEvaluator.Evaluate(OperationFacts.Nothing);

        Assert.Equal(Registry.Entries.Length, evaluation.Requirements.Length);
        Assert.Empty(evaluation.InState(RequirementState.Satisfied));
        Assert.Empty(evaluation.InState(RequirementState.Violated));
        Assert.Empty(evaluation.InState(RequirementState.ActionRequired));
        Assert.Empty(evaluation.InState(RequirementState.HumanAssertionRecorded));

        var assertionsOwed = evaluation.InState(RequirementState.HumanAssertionRequired);
        Assert.NotEmpty(assertionsOwed);
        Assert.All(assertionsOwed, outcome => Assert.NotEmpty(outcome.AssertedBy));

        var factsOwed = evaluation.InState(RequirementState.FactRequired);
        Assert.NotEmpty(factsOwed);
        Assert.All(factsOwed, outcome => Assert.False(string.IsNullOrWhiteSpace(outcome.MissingInput)));
    }

    [Fact]
    public void The_evaluation_says_what_it_is_not()
    {
        var text = OperationEvaluator.Evaluate(Complete()).ToString();

        Assert.Contains("it is not a determination that an operation is lawful", text, StringComparison.Ordinal);
        Assert.Contains("faa-part-107", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waiver_statement_is_filed_under_the_regulation_it_names_and_not_under_one_the_caller_chooses()
    {
        var facts = OperationFacts.Nothing.Stating(WaiverStatement.Held(Airspace.Regulation, Caller));

        Assert.Equal(Airspace.Regulation, Assert.Single(facts.Waivers.Keys));
        Assert.Equal(Airspace.Regulation, facts.WaiverOf(Airspace.Regulation)?.Regulation);
        Assert.Null(facts.WaiverOf(Speed.Regulation));

        // A second statement about another regulation is filed beside the first, not over it.
        var both = facts.Stating(WaiverStatement.NoneHeld(Speed.Regulation, Caller));
        Assert.True(both.WaiverOf(Airspace.Regulation)?.InForce);
        Assert.False(both.WaiverOf(Speed.Regulation)?.InForce);

        // § 107.37(a) is a third regulation, and the caller said nothing about it: the entry it
        // suspends refuses rather than read the silence either way.
        Assert.Equal(RequirementState.FactRequired, State("well-clear", both));
    }
}
