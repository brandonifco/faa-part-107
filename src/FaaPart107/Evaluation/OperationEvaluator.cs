using System.Collections.Immutable;
using RulesKernel.Resolution;

namespace FaaPart107.Evaluation;

/// <summary>
/// One question in, one structured answer out: <see cref="Evaluate(OperationFacts)"/> puts
/// <see cref="OperationFacts"/> to every entry of the map and reports what each said.
/// </summary>
/// <remarks>
/// <para>
/// <b>The orchestrator orchestrates; every regulatory answer in the output is a handler's.</b>
/// There is no threshold here, no comparison against a figure, no unit conversion and no decision
/// about what a rule requires. Each entry's request is built from the facts, resolved through the
/// generated entry point in <see cref="EntryPoints"/>, and the <see cref="Resolution{T}"/> that
/// comes back is classified. A composite entry is evaluated <em>through its own entry</em>, which
/// consumes its dependencies as the map declares — <c>operating-limitations</c> is resolved, never
/// re-derived from <c>speed-within-limit</c>, <c>altitude-within-limit</c> and
/// <c>weather-minimums-met</c> — so changing one rule's implementation cannot change an unrelated
/// requirement's result.
/// </para>
/// <para>
/// <b>The map is enumerated, never listed.</b> <see cref="Registry.Entries"/> is walked in the
/// map's order, so every entry of the map is in the answer and no hard-coded list decides which.
/// What an entry with no arm of its own in <c>ThroughItsEntryPoint</c> reports depends on the
/// entry, and it is worth being exact about it: one this engine has not built, or one it has built
/// whose request declares no inputs, answers correctly — from its correspondence row, or from its
/// handler. One this engine <em>has</em> built whose request <em>does</em> declare inputs reports
/// <see cref="RequirementState.FactRequired"/> naming the first input it demanded, because nothing
/// supplied it. That is a demand on the caller rather than a wrong answer, so the failure mode of a
/// missing arm is safe and legible — but it is a missing arm, and the entry needs one here before
/// this API can put an operation's facts to it.
/// </para>
/// <para>
/// <b>Reading a verdict is one property of the rule's own finding, and only one.</b> Each arm of
/// <c>Verdict</c> reads a single property the rule already computed, whose own documentation says
/// which direction is compliance — <c>WithinLimit</c>, <c>MinimumsMet</c>, <c>MayPass</c>,
/// <c>MayOperate</c>, <c>Permitted</c>, <c>Prohibited</c>, <c>CompliedWith</c>, <c>Maintained</c>
/// — and nothing is combined, computed, softened or defaulted. Where the rule's verdict says the
/// section is not met, this reports <see cref="RequirementState.Violated"/>, including where the
/// rule also records that what would have met it is an authorization or a permission: a caller who
/// has stated they hold none has been answered, not asked
/// (<c>docs/decisions/0005-the-rules-verdict-is-the-verdict.md</c>). What the caller could obtain
/// is on the finding, which travels on <see cref="EvaluatedRequirement.Finding"/>.
/// </para>
/// </remarks>
public static class OperationEvaluator
{
    /// <summary>Evaluates <paramref name="facts"/> against every entry of the map.</summary>
    /// <param name="facts">What the caller states about one operation.</param>
    /// <returns>One outcome per map entry, in the map's order, with the engine's identity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="facts"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// A rule refused a value it was given as malformed rather than merely absent — a negative
    /// distance, say — and the whole evaluation is abandoned rather than one entry's answer being
    /// dressed as a fact the caller owes. A fact <em>not supplied</em> never throws: that entry
    /// reports <see cref="RequirementState.FactRequired"/> and every other entry is still answered.
    /// In practice this surfaces as the derived type the rule threw, usually
    /// <see cref="ArgumentOutOfRangeException"/>. One malformed field therefore aborts all of it,
    /// which is deliberate: it is a defect in the caller's own facts, not an answer this engine
    /// could give about part of them.
    /// </exception>
    public static OperationEvaluation Evaluate(OperationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var outcomes = ImmutableArray.CreateBuilder<EvaluatedRequirement>(Registry.Entries.Length);
        foreach (var entry in Registry.Entries)
        {
            outcomes.Add(Answer(entry, facts));
        }

        return new OperationEvaluation(outcomes.ToImmutable());
    }

    private static EvaluatedRequirement Answer(RegisteredEntry entry, OperationFacts facts)
    {
        try
        {
            return Resolve(entry, facts).Match(
                value => new EvaluatedRequirement(entry, StateOf(entry, value), value.ToString() ?? string.Empty) { Finding = value },
                decline => new EvaluatedRequirement(entry, RequirementStates.For(decline.Reason), decline.Attempted)
                {
                    Reason = decline.Reason,
                });
        }
        catch (AssertionRequiredException required)
        {
            // Row 8, and never an unresolved result: the corpus gave the engine the means to
            // proceed and the caller owes the value. Folding this into a decline would tell a
            // caller the corpus is silent where in fact the caller is.
            return new EvaluatedRequirement(entry, RequirementState.HumanAssertionRequired, required.Message);
        }
        catch (ArgumentException refused) when (refused.GetType() == typeof(ArgumentException))
        {
            // A fact the entry demands and did not get. It is the caller's error, not a gap in the
            // corpus, so it is reported as a demand on the caller and never as a decline — and
            // never quietly answered from a default.
            //
            // Exactly ArgumentException, and not a derived one. `Handlers.Missing` throws exactly
            // this type; a rule that fails on its own terms throws something more specific
            // (ArgumentNullException, ArgumentOutOfRangeException), and that is a fault inside the
            // engine or a malformed value, not a fact the caller owes. Reporting one of those as
            // FactRequired would tell a product to go and ask somebody for something.
            return new EvaluatedRequirement(entry, RequirementState.FactRequired, refused.Message)
            {
                MissingInput = refused.ParamName,
            };
        }
    }

    private static Resolution<object> Resolve(RegisteredEntry entry, OperationFacts facts) =>
        ThroughItsEntryPoint(entry.Id, facts) ?? Registry.Resolve(entry.Id, facts.Assertions);

    /// <summary>
    /// The entries whose requests declare inputs, each built from the facts and resolved through
    /// its own generated entry point. Null for every other entry, which is then resolved through
    /// the registry with the caller's assertions and nothing else: an entry with no inputs needs
    /// none, and an entry this engine has not built answers from its correspondence row whatever
    /// it is handed.
    /// </summary>
    private static Resolution<object>? ThroughItsEntryPoint(string entryId, OperationFacts facts) => entryId switch
    {
        "speed-limit" => EntryPoints.SpeedLimit.Resolve(
            new Requests.SpeedLimitRequest(facts.Assertions) { Waiver = facts.WaiverOf(Speed.Regulation) }),
        "altitude-limit" => EntryPoints.AltitudeLimit.Resolve(
            new Requests.AltitudeLimitRequest(facts.Assertions) { Waiver = facts.WaiverOf(Altitude.Regulation) }),
        "visibility-minimum" => EntryPoints.VisibilityMinimum.Resolve(
            new Requests.VisibilityMinimumRequest(facts.Assertions) { Waiver = facts.WaiverOf(Visibility.Regulation) }),
        "cloud-clearance" => EntryPoints.CloudClearance.Resolve(
            new Requests.CloudClearanceRequest(facts.Assertions) { Waiver = facts.WaiverOf(Clouds.Regulation) }),
        "speed-within-limit" => EntryPoints.SpeedWithinLimit.Resolve(
            new Requests.SpeedWithinLimitRequest(facts.Assertions)
            {
                Groundspeed = facts.Groundspeed,
                Waiver = facts.WaiverOf(Speed.Regulation),
            }),
        "altitude-within-limit" => EntryPoints.AltitudeWithinLimit.Resolve(
            new Requests.AltitudeWithinLimitRequest(facts.Assertions)
            {
                AltitudeAboveGroundLevelFeet = facts.AltitudeAboveGroundLevelFeet,
                Structure = facts.Structure,
                Waiver = facts.WaiverOf(Altitude.Regulation),
            }),
        "weather-minimums-met" => EntryPoints.WeatherMinimumsMet.Resolve(
            new Requests.WeatherMinimumsMetRequest(facts.Assertions)
            {
                FlightVisibilityStatuteMiles = facts.FlightVisibilityStatuteMiles,
                FeetBelowCloud = facts.FeetBelowCloud,
                FeetHorizontallyFromCloud = facts.FeetHorizontallyFromCloud,
                Waiver = facts.WaiverOf(Weather.Regulation),
            }),
        "operating-limitations" => EntryPoints.OperatingLimitations.Resolve(
            new Requests.OperatingLimitationsRequest(facts.Assertions)
            {
                Person = facts.BoundPerson,
                Groundspeed = facts.Groundspeed,
                AltitudeAboveGroundLevelFeet = facts.AltitudeAboveGroundLevelFeet,
                Structure = facts.Structure,
                FlightVisibilityStatuteMiles = facts.FlightVisibilityStatuteMiles,
                FeetBelowCloud = facts.FeetBelowCloud,
                FeetHorizontallyFromCloud = facts.FeetHorizontallyFromCloud,
                Waiver = facts.WaiverOf(Compliance.Regulation),
            }),
        "reasonable-protection" => EntryPoints.ReasonableProtection.Resolve(
            new Requests.ReasonableProtectionRequest(facts.Assertions)
            {
                Shelter = facts.Shelter,
                Waiver = facts.WaiverOf(Protection.Regulation),
            }),
        "prominent-objects" => EntryPoints.ProminentObjects.Resolve(
            new Requests.ProminentObjectsRequest(facts.Assertions) { Waiver = facts.WaiverOf(Prominence.Regulation) }),
        "single-aircraft" => EntryPoints.SingleAircraft.Resolve(
            new Requests.SingleAircraftRequest(facts.Assertions)
            {
                Person = facts.Person,
                Engagements = facts.Engagements,
                Waiver = facts.WaiverOf(MultipleAircraft.Regulation),
            }),
        "airspace-authorized" => EntryPoints.AirspaceAuthorized.Resolve(
            new Requests.AirspaceAuthorizedRequest(facts.Assertions)
            {
                Airspace = facts.Airspace,
                Authorization = facts.AtcAuthorization,
                Waiver = facts.WaiverOf(Airspace.Regulation),
            }),
        "restricted-area-permitted" => EntryPoints.RestrictedAreaPermitted.Resolve(
            new Requests.RestrictedAreaPermittedRequest(facts.Assertions)
            {
                Area = facts.Area,
                Permission = facts.AreaPermission,
            }),
        "moving-vehicle-operation" => EntryPoints.MovingVehicleOperation.Resolve(
            new Requests.MovingVehicleOperationRequest(facts.Assertions)
            {
                FromMovingLandOrWaterBorneVehicle = facts.FromMovingLandOrWaterBorneVehicle,
                TransportingAnotherPersonsPropertyForCompensationOrHire =
                    facts.TransportingAnotherPersonsPropertyForCompensationOrHire,
                Waiver = facts.WaiverOf(MovingVehicle.Regulation),
            }),
        "moving-aircraft-operation" => EntryPoints.MovingAircraftOperation.Resolve(
            new Requests.MovingAircraftOperationRequest(facts.Assertions)
            {
                FromAMovingAircraft = facts.FromAMovingAircraft,
                Waiver = facts.WaiverOf(MovingAircraft.Regulation),
            }),
        "night-waiver-termination" => EntryPoints.NightWaiverTermination.Resolve(
            new Requests.NightWaiverTerminationRequest(facts.Assertions)
            {
                Certificate = facts.NightWaiverCertificate,
                AsOf = facts.AsOf,
            }),
        "right-of-way" => EntryPoints.RightOfWay.Resolve(
            new Requests.RightOfWayRequest(facts.Assertions)
            {
                Encountered = facts.Encountered,
                Position = facts.Position,
                Waiver = facts.WaiverOf(Yielding.Regulation),
            }),
        "well-clear" => EntryPoints.WellClear.Resolve(
            new Requests.WellClearRequest(facts.Assertions) { Waiver = facts.WaiverOf(Yielding.Regulation) }),
        "direct-participation" => EntryPoints.DirectParticipation.Resolve(
            new Requests.DirectParticipationRequest(facts.Assertions) { Waiver = facts.WaiverOf(Participation.Regulation) }),
        "effective-communication" => EntryPoints.EffectiveCommunication.Resolve(
            new Requests.EffectiveCommunicationRequest(facts.Assertions) { Waiver = facts.WaiverOf(Communication.Regulation) }),
        "unaided-visual-contact" => EntryPoints.UnaidedVisualContact.Resolve(
            new Requests.UnaidedVisualContactRequest(facts.Assertions) { Waiver = facts.WaiverOf(UnaidedVision.Regulation) }),
        "observer-coordination" => EntryPoints.ObserverCoordination.Resolve(
            new Requests.ObserverCoordinationRequest(facts.Assertions) { Waiver = facts.WaiverOf(Coordination.Regulation) }),
        "intensity-reduction-in-interest-of-safety" => EntryPoints.IntensityReductionInInterestOfSafety.Resolve(
            new Requests.IntensityReductionInInterestOfSafetyRequest(facts.Assertions) { Waiver = facts.WaiverOf(Lighting.Regulation) }),
        "visual-observer-conditions" => EntryPoints.VisualObserverConditions.Resolve(
            new Requests.VisualObserverConditionsRequest(facts.Assertions)
            {
                Use = facts.VisualObserverUse,
                Exercise = facts.Exercise,
                Waiver = facts.WaiverOf(Observers.Regulation),
                VisualLineOfSightWaiver = facts.WaiverOf(LineOfSight.Regulation),
            }),
        "anti-collision-lighting" => EntryPoints.AntiCollisionLighting.Resolve(
            new Requests.AntiCollisionLightingRequest(facts.Assertions)
            {
                Lighting = facts.Lighting,
                Waiver = facts.WaiverOf(Lights.Regulation),
            }),
        "flash-rate-sufficient" => EntryPoints.FlashRateSufficient.Resolve(
            new Requests.FlashRateSufficientRequest(facts.Assertions) { Waiver = facts.WaiverOf(FlashRate.Regulation) }),
        "visual-line-of-sight" => EntryPoints.VisualLineOfSight.Resolve(
            new Requests.VisualLineOfSightRequest(facts.Assertions)
            {
                Exercise = facts.Exercise,
                Waiver = facts.WaiverOf(LineOfSight.Regulation),
            }),
        _ => null,
    };

    /// <summary>
    /// The state a resolved value is reported as.
    /// </summary>
    /// <remarks>
    /// An entry whose first correspondence row is row 8 is <c>kind: assertion</c>: what it resolved
    /// to is the caller's own fact, answered unchanged, and it is recorded rather than scored
    /// (<c>docs/decisions/0004-an-assertion-is-recorded-and-not-scored.md</c>). That test is the
    /// map's — <see cref="RegisteredEntry.Row"/> — and not a list of finding types this orchestrator
    /// keeps, so an assertion entry built later is covered the day it lands, whatever type its rule
    /// wraps the <see cref="Assertion"/> in.
    /// </remarks>
    private static RequirementState StateOf(RegisteredEntry entry, object value) =>
        entry.Row == CorrespondenceRow.Assertion ? RequirementState.HumanAssertionRecorded : Verdict(value);

    /// <summary>
    /// The state a resolved value is reported as where the entry states a rule of its own: one
    /// property of the rule's own finding, read in the direction that finding's own documentation
    /// states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Watch the polarity, arm by arm. <c>Prohibited</c> is true when the paragraph prohibits the
    /// operation, so it inverts relative to <c>Permitted</c>, <c>MayPass</c>, <c>MayOperate</c>,
    /// <c>WithinLimit</c> and <c>MinimumsMet</c>, which are all true when the paragraph is met.
    /// </para>
    /// <para>
    /// A value this table does not name is <see cref="RequirementState.Informational"/>: the engine
    /// resolved something and this orchestrator has no compliance reading for it, which is the
    /// honest answer and never a verdict in either direction. The value is on
    /// <see cref="EvaluatedRequirement.Finding"/>.
    /// </para>
    /// </remarks>
    private static RequirementState Verdict(object value) => value switch
    {
        // § 107.51(a): true when the groundspeed is within the limit.
        GroundspeedFinding finding => Met(finding.WithinLimit),

        // § 107.51(b): true when the altitude is within the limit.
        AltitudeFinding finding => Met(finding.WithinLimit),

        // § 107.51(c)-(d): true when both paragraphs are met.
        WeatherMinimumsFinding finding => Met(finding.MinimumsMet),

        // § 107.51 as a whole: the conjunction of its constituent limitations, as the rule made
        // it from what each constituent entry answered. This orchestrator does not re-derive it.
        OperatingLimitationsFinding finding => Met(finding.CompliedWith),

        // § 107.29(a)(2) and (b): true when the lighting the clause requires is there. The rule
        // makes that conjunction from the caller's statement and two assertions; this reads it.
        AntiCollisionLightingFinding finding => Met(finding.Met),

        // § 107.33 as a whole. Three answers, not two: the rule's own property is bool?, and null
        // is the section not reaching this operation at all — which that property's own
        // documentation is explicit is neither "met" nor "not met". SectionApplies, on the finding,
        // says which case it is, and reading null as either verdict would invent one.
        VisualObserverConditionsFinding finding => finding.AllRequirementsMet switch
        {
            true => RequirementState.Satisfied,
            false => RequirementState.Violated,
            null => RequirementState.Informational,
        },

        // § 107.31 as a whole: true when paragraph (a)'s ability is there and paragraph (b)'s
        // requirement that it be exercised is satisfied. The rule makes that conjunction, not
        // this orchestrator, and this arm reads the one property it named.
        VisualLineOfSightFinding finding => Met(finding.Maintained),

        // § 107.35: true when that is not more than one unmanned aircraft, which the section permits.
        MultipleAircraftFinding finding => Met(finding.Permitted),

        // § 107.37(a): true when the section does not prohibit this pass.
        RightOfWayFinding finding => Met(finding.MayPass),

        // § 107.25(b): true when the paragraph PROHIBITS the operation, so this arm reads the
        // other way round from every arm above it.
        MovingVehicleFinding finding => finding.Prohibited
            ? RequirementState.Violated
            : RequirementState.Satisfied,

        // § 107.25(a): true when the constituent PROHIBITS the operation — likewise inverted.
        MovingAircraftFinding finding => finding.Prohibited
            ? RequirementState.Violated
            : RequirementState.Satisfied,

        // § 107.41: true when the section does not prohibit the operation. A stated absence of
        // prior ATC authorization is the section prohibiting it, and is reported as such
        // (docs/decisions/0005-the-rules-verdict-is-the-verdict.md).
        AirspaceFinding finding => Met(finding.MayOperate),

        // § 107.45: true when the section does not bar the operation.
        AreaPermissionFinding finding => Met(finding.Permitted),

        // What the rule says, not whether an operation complies: the printed figures, and a
        // finding whose own documentation disclaims a verdict.
        _ => RequirementState.Informational,
    };

    private static RequirementState Met(bool met) =>
        met ? RequirementState.Satisfied : RequirementState.Violated;
}
