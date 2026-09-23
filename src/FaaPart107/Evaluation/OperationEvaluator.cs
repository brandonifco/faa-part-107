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
/// map's order, so an entry this engine builds next is covered here the day it lands, and an entry
/// this evaluator can build no request for is still evaluated and reports whatever its
/// correspondence row gives — which is the right answer for an entry this engine has not built.
/// </para>
/// <para>
/// <b>Reading a verdict is one property of the rule's own finding.</b> Each arm of
/// <c>Verdict</c> reads a single property the rule already computed, whose own documentation says
/// which direction is compliance — <c>WithinLimit</c>, <c>MinimumsMet</c>, <c>MayPass</c>,
/// <c>MayOperate</c>, <c>Permitted</c>, <c>Prohibited</c> — and nothing is combined, computed or
/// defaulted. Where a rule models an obtainable authorization in a field of its own
/// (<see cref="AirspaceFinding.AuthorizationRequired"/>,
/// <see cref="AreaPermissionFinding.PermissionRequired"/>) an unmet requirement is
/// <see cref="RequirementState.ActionRequired"/>; that distinction is the rule's, and it is not
/// invented for a rule that does not draw it.
/// </para>
/// </remarks>
public static class OperationEvaluator
{
    /// <summary>Evaluates <paramref name="facts"/> against every entry of the map.</summary>
    /// <param name="facts">What the caller states about one operation.</param>
    /// <returns>One outcome per map entry, in the map's order, with the engine's identity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="facts"/> is null.</exception>
    public static OperationEvaluation Evaluate(OperationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var outcomes = ImmutableArray.CreateBuilder<RequirementOutcome>(Registry.Entries.Length);
        foreach (var entry in Registry.Entries)
        {
            outcomes.Add(Answer(entry, facts));
        }

        return new OperationEvaluation(outcomes.ToImmutable());
    }

    private static RequirementOutcome Answer(RegisteredEntry entry, OperationFacts facts)
    {
        try
        {
            return Resolve(entry, facts).Match(
                value => new RequirementOutcome(entry, StateOf(entry, value), value.ToString() ?? string.Empty) { Finding = value },
                decline => new RequirementOutcome(entry, RequirementStates.For(decline.Reason), decline.Attempted)
                {
                    Reason = decline.Reason,
                });
        }
        catch (AssertionRequiredException required)
        {
            // Row 8, and never an unresolved result: the corpus gave the engine the means to
            // proceed and the caller owes the value. Folding this into a decline would tell a
            // caller the corpus is silent where in fact the caller is.
            return new RequirementOutcome(entry, RequirementState.HumanAssertionRequired, required.Message);
        }
        catch (ArgumentException refused)
        {
            // A fact the entry demands and did not get. It is the caller's error, not a gap in the
            // corpus, so it is reported as a demand on the caller and never as a decline — and
            // never quietly answered from a default.
            return new RequirementOutcome(entry, RequirementState.FactRequired, refused.Message)
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
    /// <see cref="RequirementOutcome.Finding"/>.
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

        // § 107.41: prior ATC authorization is what lifts the prohibition, and the rule says in a
        // field of its own when the section asks for it.
        AirspaceFinding finding => finding.MayOperate
            ? RequirementState.Satisfied
            : Obtainable(finding.AuthorizationRequired),

        // § 107.45: permission from the using or controlling agency, the same shape.
        AreaPermissionFinding finding => finding.Permitted
            ? RequirementState.Satisfied
            : Obtainable(finding.PermissionRequired),

        // What the rule says, not whether an operation complies: the printed figures, and a
        // finding whose own documentation disclaims a verdict.
        _ => RequirementState.Informational,
    };

    private static RequirementState Met(bool met) =>
        met ? RequirementState.Satisfied : RequirementState.Violated;

    private static RequirementState Obtainable(bool required) =>
        required ? RequirementState.ActionRequired : RequirementState.Violated;
}
