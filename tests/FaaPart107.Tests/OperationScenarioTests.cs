using System.Collections.Immutable;
using FaaPart107.Evaluation;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// Seven operations a person who knows part 107 would recognise, each put whole to
/// <see cref="OperationEvaluator.Evaluate(OperationFacts)"/>, and each asserting what a caller
/// actually receives: the state, the map entry that produced it, the citation it rests on, and
/// what the outcome says about the assertion or the decline behind it.
/// </summary>
/// <remarks>
/// <para>
/// <b>These are not more unit tests for the entries.</b> Every entry has its own, with its own
/// mutation evidence. These go in at the top of the product API and prove that the states it
/// distinguishes are the states a real operation lands in, and that a caller can act on them.
/// </para>
/// <para>
/// <b>Nothing here recomputes a regulation.</b> There is no threshold in this file, no comparison
/// against a figure and no reading of part 107: each scenario states facts, asks the engine, and
/// asserts what the engine said. Where the honest expectation is that the engine declines, the
/// scenario asserts the decline and the citation it carries — it does not force the case into
/// allowed or denied, and it does not encode an answer the engine reaches by a defect.
/// </para>
/// <para>
/// <b>The facts are assembled in one place.</b> <see cref="ADaytimeSurveyFlight"/> is the whole
/// operation, written out; <see cref="TheAttestations"/> is every fact the corpus leaves to a
/// person. Each scenario changes exactly the one thing its situation is about, on the first line
/// of the test, so what separates it from the compliant flight is visible without reading anything
/// else.
/// </para>
/// </remarks>
public class OperationScenarioTests
{
    /// <summary>Who is answerable for the facts these scenarios state.</summary>
    private const string Operator = "the operator of N123AB";

    /// <summary>The person the map's <c>assertedBy</c> names on seven of the ten assertion entries.</summary>
    private const string RemotePilotInCommand = "remote pilot in command";

    /// <summary>The certificate the waiver scenario names, so that it can be seen travelling with the result.</summary>
    private const string Certificate = "107W-2024-01234";

    /// <summary>
    /// Every fact the corpus leaves to a person, as this flight's remote pilot in command would
    /// attest them, each filed under the map entry it is about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The polarities are the situation's and not a convention.</b> § 107.37(b)'s proposition —
    /// operating so close to another aircraft as to create a collision hazard — <em>holding</em> is
    /// the prohibited state, so this flight attests it false; the other nine hold.
    /// </para>
    /// <para>
    /// The attribution is the person who made it and is recorded rather than matched against the
    /// map's <c>assertedBy</c> (<c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>), and
    /// what a person attested is recorded rather than scored
    /// (<c>docs/decisions/0004-an-assertion-is-recorded-and-not-scored.md</c>) — so no verdict in
    /// any scenario below turns on which way round one of these is written.
    /// </para>
    /// </remarks>
    private static ImmutableArray<Assertion> TheAttestations { get; } =
    [
        new Assertion(MapEntries.CollisionHazardProximity, false, RemotePilotInCommand),
        new Assertion(MapEntries.ReasonableProtection, true, RemotePilotInCommand),
        new Assertion(MapEntries.FlashRateSufficient, true, RemotePilotInCommand),
        new Assertion(MapEntries.IntensityReductionInInterestOfSafety, true, RemotePilotInCommand),
        new Assertion(MapEntries.UnaidedVisualContact, true, RemotePilotInCommand),
        new Assertion(MapEntries.ObserverCoordination, true, RemotePilotInCommand),
        new Assertion(MapEntries.PreflightRiskAssessment, true, RemotePilotInCommand),
        new Assertion(MapEntries.ParticipantBriefing, true, RemotePilotInCommand),
        new Assertion(MapEntries.SufficientAvailablePower, true, RemotePilotInCommand),
        new Assertion(MapEntries.AttachedObjectNoAdverseEffect, true, RemotePilotInCommand),
    ];

    /// <summary>
    /// One ordinary operation, stated completely: a survey flight by a remote pilot in command, at
    /// 300 feet above ground level and 50 knots, in clear air with 5 statute miles of flight
    /// visibility, in Class G airspace outside any prohibited or restricted area, in broad daylight,
    /// flown from a fixed control station with the aircraft kept in sight, over one human being who
    /// is under a covered structure, with one aircraft, and with no certificate of waiver of
    /// anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every field the evaluator reads is set, and every one is the caller's own statement: this
    /// engine infers nothing and defaults nothing, so a fact left out comes back as a demand
    /// (<see cref="RequirementState.FactRequired"/>) rather than as an answer.
    /// </para>
    /// <para>
    /// The waiver statements are nine because § 107.205 lists <em>regulations</em> and a statement
    /// is filed under the one it names — several entries share a regulation, and one statement
    /// answers all of them. Each regulation is taken from the rule's own <c>Regulation</c> constant
    /// and never typed here, so this cannot state a waiver of a regulation no entry is under.
    /// </para>
    /// </remarks>
    private static OperationFacts ADaytimeSurveyFlight() => new OperationFacts
    {
        // § 107.51: which of the two people the introductory text binds is asking, and the
        // operation's own measured quantities.
        BoundPerson = BoundPerson.RemotePilotInCommand,
        Groundspeed = Groundspeed.InKnots(50m),
        AltitudeAboveGroundLevelFeet = 300m,
        Structure = StructureStatement.NoneWithinRadius(Operator),
        FlightVisibilityStatuteMiles = 5m,
        Cloud = CloudStatement.NoCloud(Operator),

        // § 107.41 and § 107.45: where the flight is, and what the operator holds for it.
        Airspace = AirspaceClass.ClassG,
        AtcAuthorization = AtcAuthorization.None(Operator),
        Area = AreaDesignation.NeitherProhibitedNorRestricted,
        AreaPermission = PermissionStatement.NotGranted(Operator),

        // § 107.25: the control station is not on a moving vehicle and not in an aircraft.
        FromMovingLandOrWaterBorneVehicle = false,
        TransportingAnotherPersonsPropertyForCompensationOrHire = false,
        FromAMovingAircraft = false,

        // § 107.29: daylight — outside Alaska, during neither period of civil twilight — with
        // anti-collision lighting fitted and lighted anyway, visible for 5 statute miles; and the
        // date § 107.29(d)'s second sentence is asked as of.
        OperationPlace = OperationPlace.OutsideAlaska,
        OperationPeriod = OperationPeriod.NeitherPeriod,
        Lighting = LightingStatement.LightedAndVisibleFor(5m, Operator),
        NightWaiverCertificate = new WaiverCertificate(new DateOnly(2020, 6, 1), true, Operator),
        AsOf = new DateOnly(2026, 1, 1),

        // § 107.31 and § 107.33: the pilot and the person on the controls keep the aircraft in
        // sight themselves, and no visual observer is used.
        Exercise = new ExerciseOfTheAbility(
            RemotePilotInCommand: true,
            PersonManipulatingTheFlightControls: true,
            VisualObserver: false),
        VisualObserverUse = VisualObserverUse.NotUsed,

        // § 107.35: one person, one aircraft.
        Person = "the remote pilot in command of N123AB",
        Engagements = [new AircraftEngagement("N123AB", AircraftRole.RemotePilotInCommand)],

        // § 107.37(a): nothing of the three kinds the paragraph names was passed, and in none of
        // the three relative positions it prohibits.
        Encountered = EncounteredObject.NoneOfThem,
        Position = RelativePosition.NoneOfThem,

        // § 107.39: the human being the aircraft is operated over is under a covered structure,
        // which is the place § 107.39(b)'s attestation above is about.
        HumanBeingLocation = HumanBeingLocation.UnderACoveredStructure,
        Shelter = Shelter.CoveredStructure,

        // § 107.49: a powered aircraft, on an operation not conducted over human beings under
        // subpart D.
        AircraftPower = AircraftPower.Powered,
        SubpartDOperation = SubpartDOperation.NotOverHumanBeings,
    }
        .Stating(WaiverStatement.NoneHeld(Speed.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(MultipleAircraft.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(Airspace.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(MovingVehicle.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(Yielding.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(Participation.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(Communication.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(Lighting.Regulation, Operator))
        .Stating(WaiverStatement.NoneHeld(UnaidedVision.Regulation, Operator));

    /// <summary>The flight above with every attestation made: the operation as a caller ready to fly states it.</summary>
    private static OperationFacts AReadyOperation() =>
        TheAttestations.Aggregate(ADaytimeSurveyFlight(), (facts, made) => facts.Asserting(made));

    /// <summary>The same operation with exactly one attestation not made.</summary>
    /// <param name="withheld">The entry nobody has attested.</param>
    private static OperationFacts AReadyOperationExceptFor(MapEntry withheld) =>
        TheAttestations
            .Where(made => !string.Equals(made.Entry.Id, withheld.Id, StringComparison.Ordinal))
            .Aggregate(ADaytimeSurveyFlight(), (facts, made) => facts.Asserting(made));

    [Fact]
    public void A_daytime_survey_flight_in_Class_G_airspace_has_nothing_the_engine_calls_a_violation()
    {
        var evaluation = OperationEvaluator.Evaluate(AReadyOperation());

        // Nothing is broken, and nothing is owed: every fact the evaluated entries read is stated
        // and every attestation the corpus leaves to a person is made.
        Assert.Empty(evaluation.InState(RequirementState.Violated));
        Assert.Empty(evaluation.Outstanding);

        // The requirements this engine can decide on these facts are decided, each in its own name
        // and at its own citation — not merely "it passed".
        foreach (var entry in new[]
        {
            MapEntries.SpeedWithinLimit,
            MapEntries.AltitudeWithinLimit,
            MapEntries.SingleAircraft,
            MapEntries.AirspaceAuthorized,
            MapEntries.RestrictedAreaPermitted,
            MapEntries.MovingVehicleOperation,
            MapEntries.MovingAircraftOperation,
            MapEntries.RightOfWay,
            MapEntries.OverHumanBeings,
            MapEntries.VisualLineOfSight,
            MapEntries.AntiCollisionLighting,
        })
        {
            var outcome = evaluation.Requirement(entry.Id);
            Assert.Equal(RequirementState.Satisfied, outcome.State);
            Assert.Equal(entry.Id, outcome.EntryId);
            Assert.Equal(entry.Locator, outcome.Locator);
            Assert.Null(outcome.Reason);
            Assert.Null(outcome.DeclineCites);
        }

        // Every attestation came back recorded against its own entry, at the paragraph where the
        // corpus states it, with who the corpus lets make it — and none of them scored either way.
        foreach (var entry in Registry.Entries.Where(entry =>
            entry.Row == CorrespondenceRow.Assertion && entry.Status == EntryStatus.Implemented))
        {
            var outcome = evaluation.Requirement(entry.Id);
            Assert.Equal(RequirementState.HumanAssertionRecorded, outcome.State);
            Assert.Equal(entry.Locator, outcome.Locator);
            Assert.Equal(entry.AssertedBy, outcome.AssertedBy);
            Assert.NotNull(outcome.Finding);
            Assert.Contains($"as asserted by {RemotePilotInCommand}", outcome.Explanation, StringComparison.Ordinal);
        }

        // And the honest half of the answer, which is not a defect and must not be dressed as one.
        // § 107.51(c) defines flight visibility by the distance at which "prominent objects" may be
        // seen and identified; part 107 does not say which objects are prominent, and the map
        // records that as unresolved. So the weather limitation cannot be decided for any operation
        // at this head — clear air and measured cloud alike — and the engine declines, naming whose
        // question it is, rather than calling this flight compliant with § 107.51 as a whole.
        var weather = evaluation.Requirement("weather-minimums-met");
        Assert.Equal(RequirementState.RequiresInterpretation, weather.State);
        Assert.Equal(MapEntries.WeatherMinimumsMet.Locator, weather.Locator);
        Assert.Equal(UnresolvedReason.RequiresInterpretation, weather.Reason);
        Assert.Equal(MapEntries.ProminentObjects.Locator, weather.DeclineCites);
        Assert.Contains("prominent-objects", weather.Explanation, StringComparison.Ordinal);

        var section = evaluation.Requirement("operating-limitations");
        Assert.Equal(RequirementState.RequiresInterpretation, section.State);
        Assert.Equal(MapEntries.OperatingLimitations.Locator, section.Locator);
        Assert.Equal(MapEntries.WeatherMinimumsMet.Locator, section.DeclineCites);

        // Which is why the answer is more than a boolean: a product can say "nothing here is
        // broken" and "§ 107.51 as a whole is not settled" at the same time, with neither standing
        // in for the other.
        Assert.Contains(section, evaluation.Unanswered);
        Assert.DoesNotContain(section, evaluation.InState(RequirementState.Satisfied));
    }

    [Fact]
    public void A_flight_at_500_feet_with_no_structure_nearby_breaks_the_altitude_limit()
    {
        var tooHigh = AReadyOperation() with { AltitudeAboveGroundLevelFeet = 500m };

        var evaluation = OperationEvaluator.Evaluate(tooHigh);

        // The paragraph that states the ceiling says so, in its own name and at its own citation,
        // and the rule's own verdict property is what the state was read from.
        var altitude = evaluation.Requirement("altitude-within-limit");
        Assert.Equal(RequirementState.Violated, altitude.State);
        Assert.Equal("altitude-within-limit", altitude.EntryId);
        Assert.Equal(MapEntries.AltitudeWithinLimit.Locator, altitude.Locator);
        Assert.Null(altitude.Reason);
        Assert.False(Assert.IsType<AltitudeFinding>(altitude.Finding).WithinLimit);

        // The section that conjoins it says so too, at its own citation, naming this limitation
        // among the ones it read. § 107.51's introductory text is a different locator from
        // § 107.51(b), and a caller gets both.
        var section = evaluation.Requirement("operating-limitations");
        Assert.Equal(RequirementState.Violated, section.State);
        Assert.Equal(MapEntries.OperatingLimitations.Locator, section.Locator);
        Assert.NotEqual(altitude.Locator, section.Locator);
        Assert.False(Assert.IsType<OperatingLimitationsFinding>(section.Finding).CompliedWith);
        Assert.Contains("altitude-within-limit", section.Explanation, StringComparison.Ordinal);

        // Nothing else moved: the violation is the altitude's, it does not spread to the limitation
        // beside it in the same section, and those two are the whole of what a product would show
        // as blocking.
        Assert.Equal(RequirementState.Satisfied, evaluation.Requirement("speed-within-limit").State);
        Assert.Equal(
            ["altitude-within-limit", "operating-limitations"],
            evaluation.InState(RequirementState.Violated).Select(outcome => outcome.EntryId).ToArray());
    }

    [Fact]
    public void A_flight_into_Class_B_airspace_with_no_ATC_authorization_is_told_the_section_prohibits_it()
    {
        var intoClassB = AReadyOperation() with { Airspace = AirspaceClass.ClassB };

        var evaluation = OperationEvaluator.Evaluate(intoClassB);
        var stated = evaluation.Requirement("airspace-authorized");

        // The operator has stated they hold no prior authorization. That is an answer to § 107.41
        // and not a question for the caller, so the state is the rule's own verdict:
        // docs/decisions/0005-the-rules-verdict-is-the-verdict.md, which deleted the branch that
        // reported this as something still to be obtained and filed it under what the caller owes.
        Assert.Equal(RequirementState.Violated, stated.State);
        Assert.Equal("airspace-authorized", stated.EntryId);
        Assert.Equal(MapEntries.AirspaceAuthorized.Locator, stated.Locator);
        Assert.NotEqual(RequirementState.ActionRequired, stated.State);
        Assert.DoesNotContain(stated, evaluation.Outstanding);

        // What a product would render as "you may still be able to obtain one" is on the rule's own
        // finding, which travels with the outcome. The engine does not summarise it into the state.
        var finding = Assert.IsType<AirspaceFinding>(stated.Finding);
        Assert.False(finding.MayOperate);
        Assert.True(finding.AuthorizationRequired);
        Assert.False(finding.Authorization.Held);

        // The separating case, which is what makes the state worth having: saying nothing about the
        // authorization is a different situation from saying there is none, and the engine keeps
        // them apart. It asks, naming the input it wants, rather than convicting the caller.
        var silent = OperationEvaluator.Evaluate(intoClassB with { AtcAuthorization = null })
            .Requirement("airspace-authorized");
        Assert.Equal(RequirementState.FactRequired, silent.State);
        Assert.Equal(MapEntries.AirspaceAuthorized.Locator, silent.Locator);
        Assert.Equal("Authorization", silent.MissingInput);
        Assert.Null(silent.Finding);
        Assert.NotEqual(stated.State, silent.State);
    }

    [Fact]
    public void A_flight_nobody_has_attested_they_can_see_waits_on_a_person_and_not_on_a_fact()
    {
        // Every fact is stated and every other attestation is made; the one nobody has signed is
        // § 107.31(a)'s — that the aircraft can be seen unaided throughout the entire flight.
        var unattested = AReadyOperationExceptFor(MapEntries.UnaidedVisualContact);

        var evaluation = OperationEvaluator.Evaluate(unattested);

        // The entry that is the attestation says so about itself.
        var owed = evaluation.Requirement("unaided-visual-contact");
        Assert.Equal(RequirementState.HumanAssertionRequired, owed.State);
        Assert.Equal(MapEntries.UnaidedVisualContact.Locator, owed.Locator);
        Assert.Equal("unaided-visual-contact", owed.AssertionOwed);
        Assert.Equal(MapEntries.UnaidedVisualContact.Locator, owed.AssertionCites);
        Assert.Contains(RemotePilotInCommand, owed.AssertedBy);

        // And the section that wanted it names the attestation it is waiting for, at the paragraph
        // where the corpus states it, with the people the corpus lets make it. § 107.31 as a whole
        // is the entry the caller asked about; § 107.31(a) is where the attestation lives; and who
        // may make it is the demanded entry's, not the asked entry's.
        var section = evaluation.Requirement("visual-line-of-sight");
        Assert.Equal(RequirementState.HumanAssertionRequired, section.State);
        Assert.Equal("visual-line-of-sight", section.EntryId);
        Assert.Equal(MapEntries.VisualLineOfSight.Locator, section.Locator);
        Assert.Equal("unaided-visual-contact", section.AssertionOwed);
        Assert.Equal(MapEntries.UnaidedVisualContact.Locator, section.AssertionCites);
        Assert.NotEqual(section.Locator, section.AssertionCites);
        Assert.Equal(Registry.Entry("unaided-visual-contact").AssertedBy, section.AssertedBy);
        Assert.Empty(Registry.Entry("visual-line-of-sight").AssertedBy);

        // It is a demand on a person and not a gap in the corpus, so it is outstanding rather than
        // unanswerable, and it is never reported as a decline.
        Assert.Contains(section, evaluation.Outstanding);
        Assert.DoesNotContain(section, evaluation.Unanswered);
        Assert.Null(section.Reason);

        // Making it closes both, which is what "outstanding" is supposed to mean.
        var attested = OperationEvaluator.Evaluate(AReadyOperation());
        Assert.Equal(RequirementState.HumanAssertionRecorded, attested.Requirement("unaided-visual-contact").State);
        Assert.Equal(RequirementState.Satisfied, attested.Requirement("visual-line-of-sight").State);
    }

    [Fact]
    public void A_pass_over_an_aircraft_turns_on_well_clear_which_part_107_does_not_define()
    {
        var passedOverAnAircraft = AReadyOperation() with
        {
            Encountered = EncounteredObject.Aircraft,
            Position = RelativePosition.Over,
        };

        var evaluation = OperationEvaluator.Evaluate(passedOverAnAircraft);

        // Both of § 107.37(a)'s enumerations reach this pass — an aircraft, passed over — so
        // whether the paragraph prohibits it turns on its exception, "unless well clear". The
        // corpus states no measure for that term, the map records it unresolved, and the engine
        // declines rather than choosing a separation distance of its own.
        var pass = evaluation.Requirement("right-of-way");
        Assert.Equal(RequirementState.RequiresInterpretation, pass.State);
        Assert.Equal("right-of-way", pass.EntryId);
        Assert.Equal(MapEntries.RightOfWay.Locator, pass.Locator);
        Assert.Equal(UnresolvedReason.RequiresInterpretation, pass.Reason);
        Assert.Equal(MapEntries.WellClear.Locator, pass.DeclineCites);
        Assert.Contains("well-clear", pass.Explanation, StringComparison.Ordinal);
        Assert.Null(pass.Finding);

        // The open term answers in its own name too, so a caller sees the question and not only its
        // effect.
        var term = evaluation.Requirement("well-clear");
        Assert.Equal(RequirementState.RequiresInterpretation, term.State);
        Assert.Equal(MapEntries.WellClear.Locator, term.Locator);

        // It is never forced into allowed or denied, and it is not a demand on the caller either:
        // there is no fact and no attestation that would close it.
        Assert.Contains(pass, evaluation.Unanswered);
        Assert.DoesNotContain(pass, evaluation.Outstanding);
        Assert.NotEqual(RequirementState.Satisfied, pass.State);
        Assert.NotEqual(RequirementState.Violated, pass.State);

        // And the same paragraph decides the case without the open term whenever one of its two
        // enumerations does not reach the pass — which is why the daytime flight above got an
        // answer at § 107.37(a) and this one does not.
        Assert.Equal(
            RequirementState.Satisfied,
            OperationEvaluator.Evaluate(AReadyOperation()).Requirement("right-of-way").State);
    }

    [Fact]
    public void A_flight_over_people_relying_on_a_subpart_D_category_is_outside_what_this_engine_covers()
    {
        var overPeople = AReadyOperation() with
        {
            SubpartDOperation = SubpartDOperation.OverHumanBeings,
            HumanBeingLocation = HumanBeingLocation.NeitherOfThem,
        };

        var evaluation = OperationEvaluator.Evaluate(overPeople);

        // The category itself is a scope: out entry of the map. It answers in its own name, at
        // subpart D's own locator, and says the engine does not cover it — which is not a finding
        // about the operation in either direction.
        var category = evaluation.Requirement("subpart-d-categories");
        Assert.Equal(RequirementState.OutsideCurrentScope, category.State);
        Assert.Equal("subpart-d-categories", category.EntryId);
        Assert.Equal(MapEntries.SubpartDCategories.Locator, category.Locator);
        Assert.Equal(CorrespondenceRow.ScopeOut, category.Row);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, category.Reason);

        // Its citation is its own section, and that is the source relationship to check: an entry
        // the MAP puts out of scope cites where the rule lives, where an entry a certificate of
        // waiver suspends cites § 107.205 instead. The two reach the same state by different routes
        // and the citation is what tells them apart.
        Assert.Equal(MapEntries.SubpartDCategories.Locator, category.DeclineCites);
        Assert.NotEqual(MapEntries.WaivableRegulations.Locator, category.DeclineCites);

        // And an entry that has to consult it does not answer in its place. § 107.39 is left
        // undetermined for a human being under neither of paragraph (b)'s two places, and its
        // explanation names the entry that could not answer.
        var overflight = evaluation.Requirement("over-human-beings");
        Assert.Equal(RequirementState.RequiresInterpretation, overflight.State);
        Assert.Contains("subpart-d-categories", overflight.Explanation, StringComparison.Ordinal);
        Assert.Contains(overflight, evaluation.Unanswered);
        Assert.DoesNotContain(overflight, evaluation.InState(RequirementState.Satisfied));
        Assert.DoesNotContain(overflight, evaluation.InState(RequirementState.Violated));

        // § 107.49(f) asks the same question of the same operation, and is likewise left
        // undetermined rather than answering where § 107.39 could not.
        var preflight = evaluation.Requirement("preflight-actions");
        Assert.Contains(preflight, evaluation.Unanswered);
        Assert.Contains("subpart-d-categories", preflight.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void A_certificate_of_waiver_of_107_51_suspends_that_section_and_leaves_the_rest_standing()
    {
        var waived = AReadyOperation().Stating(WaiverStatement.Held(Speed.Regulation, Operator, Certificate));

        var evaluation = OperationEvaluator.Evaluate(waived);

        // Every entry § 107.51 states is suspended, and each says so citing § 107.205 — the section
        // that lists the regulations a certificate of waiver may authorize deviation from — rather
        // than its own paragraph. A decline's citation is where the rule that could not answer
        // lives, which here is the waiver gate and not § 107.51.
        foreach (var entryId in new[]
        {
            "speed-limit",
            "altitude-limit",
            "visibility-minimum",
            "cloud-clearance",
            "speed-within-limit",
            "altitude-within-limit",
            "weather-minimums-met",
            "prominent-objects",
            "operating-limitations",
        })
        {
            var outcome = evaluation.Requirement(entryId);
            Assert.Equal(RequirementState.OutsideCurrentScope, outcome.State);
            Assert.Equal(entryId, outcome.EntryId);
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, outcome.Reason);
            Assert.Equal(MapEntries.WaivableRegulations.Locator, outcome.DeclineCites);
            Assert.NotEqual(outcome.Locator, outcome.DeclineCites);

            // The statement travels with the result, certificate and all, so a caller reading one
            // outcome can see what suspended it and who said so (docs/decisions/0001).
            Assert.Contains(Certificate, outcome.Explanation, StringComparison.Ordinal);
            Assert.Contains(Operator, outcome.Explanation, StringComparison.Ordinal);
        }

        // A waiver is per regulation and not per operation: § 107.41, § 107.35 and § 107.39 are
        // answered exactly as they were before, because this certificate says nothing about them.
        Assert.Equal(RequirementState.Satisfied, evaluation.Requirement("airspace-authorized").State);
        Assert.Equal(RequirementState.Satisfied, evaluation.Requirement("single-aircraft").State);
        Assert.Equal(RequirementState.Satisfied, evaluation.Requirement("over-human-beings").State);

        // And the entries the MAP puts out of scope are still out of scope for their own reason,
        // citing their own sections and not the waiver gate.
        var byTheMap = evaluation.Requirement("knowledge-recency");
        Assert.Equal(RequirementState.OutsideCurrentScope, byTheMap.State);
        Assert.Equal(MapEntries.KnowledgeRecency.Locator, byTheMap.DeclineCites);
        Assert.NotEqual(MapEntries.WaivableRegulations.Locator, byTheMap.DeclineCites);
    }
}
