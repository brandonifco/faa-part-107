using System.Reflection;
using FaaPart107.Evaluation;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// One ordering, across every entry § 107.205 suspends:
/// <c>docs/decisions/0007-the-waiver-gate-precedes-every-other-demand.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// The property under test is the one thing a caller can observe about the ordering: what a
/// <b>waived request that describes nothing else</b> is told. Before #93 the engine had two answers
/// for it — thirteen entries refused <see cref="ArgumentException"/> naming whichever typed input
/// their handler happened to demand first, and two declined
/// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 — and nothing in this
/// repository said which was right. This pins the one that is: the decline, for all of them.
/// </para>
/// <para>
/// <b>The table is checked against the code rather than trusted.</b>
/// <see cref="The_table_is_every_entry_whose_request_carries_a_waiver_statement_of_its_own"/>
/// compares it with the generated request types by reflection, so a gated entry built later cannot
/// be left out of this file silently: the day its request declares a <c>Waiver</c> property, this
/// test fails naming it.
/// </para>
/// </remarks>
public sealed class WaiverGateOrderingTests
{
    private const string Caller = "the remote pilot in command";

    /// <summary>
    /// Every entry whose own request carries a § 107.205 waiver statement, with the regulation
    /// § 107.205 lists it under — read from the rule that states the entry, never spelt out here.
    /// </summary>
    /// <remarks>
    /// <c>Owes</c> is the request property the entry demands first once a statement that no waiver
    /// is in force has put it in reach, or null where the entry owes no typed input at all —
    /// <c>speed-limit</c>'s <c>AsOneFigureIn</c> and <c>cloud-clearance</c>'s <c>AsOneDistance</c>
    /// are ways of asking for a printed figure rather than facts the entry demands. The fifteen
    /// rows with an <c>Owes</c> are exactly the entries on which the two orderings differ at all.
    /// </remarks>
    private static readonly (string Entry, Type Request, string Regulation, string? Owes)[] Gated =
    [
        ("speed-limit", typeof(SpeedLimitRequest), Speed.Regulation, null),
        ("altitude-limit", typeof(AltitudeLimitRequest), Altitude.Regulation, null),
        ("visibility-minimum", typeof(VisibilityMinimumRequest), Visibility.Regulation, null),
        ("cloud-clearance", typeof(CloudClearanceRequest), Clouds.Regulation, null),
        ("speed-within-limit", typeof(SpeedWithinLimitRequest), Speed.Regulation, "Groundspeed"),
        ("altitude-within-limit", typeof(AltitudeWithinLimitRequest), Altitude.Regulation, "AltitudeAboveGroundLevelFeet"),
        ("weather-minimums-met", typeof(WeatherMinimumsMetRequest), Weather.Regulation, "FlightVisibilityStatuteMiles"),
        ("operating-limitations", typeof(OperatingLimitationsRequest), Compliance.Regulation, "Person"),
        ("prominent-objects", typeof(ProminentObjectsRequest), Prominence.Regulation, null),
        ("single-aircraft", typeof(SingleAircraftRequest), MultipleAircraft.Regulation, "Person"),
        ("airspace-authorized", typeof(AirspaceAuthorizedRequest), Airspace.Regulation, "Airspace"),
        ("moving-vehicle-operation", typeof(MovingVehicleOperationRequest), MovingVehicle.Regulation, "FromMovingLandOrWaterBorneVehicle"),
        ("moving-aircraft-operation", typeof(MovingAircraftOperationRequest), MovingAircraft.Regulation, "FromAMovingAircraft"),
        ("civil-twilight-operation", typeof(CivilTwilightOperationRequest), Twilight.Regulation, "Place"),
        ("right-of-way", typeof(RightOfWayRequest), Yielding.Regulation, "Encountered"),
        ("well-clear", typeof(WellClearRequest), Yielding.Regulation, null),
        ("reasonable-protection", typeof(ReasonableProtectionRequest), Protection.Regulation, "Shelter"),
        ("over-human-beings", typeof(OverHumanBeingsRequest), Overflight.Regulation, "Location"),
        ("direct-participation", typeof(DirectParticipationRequest), Participation.Regulation, null),
        ("flash-rate-sufficient", typeof(FlashRateSufficientRequest), FlashRate.Regulation, null),
        ("intensity-reduction-in-interest-of-safety", typeof(IntensityReductionInInterestOfSafetyRequest), Lighting.Regulation, null),
        ("anti-collision-lighting", typeof(AntiCollisionLightingRequest), Lights.Regulation, "Lighting"),
        ("visual-line-of-sight", typeof(VisualLineOfSightRequest), LineOfSight.Regulation, "Exercise"),
        ("unaided-visual-contact", typeof(UnaidedVisualContactRequest), UnaidedVision.Regulation, null),
        ("visual-observer-conditions", typeof(VisualObserverConditionsRequest), Observers.Regulation, "Use"),
        ("effective-communication", typeof(EffectiveCommunicationRequest), Communication.Regulation, null),
        ("observer-coordination", typeof(ObserverCoordinationRequest), Coordination.Regulation, null),
    ];

    /// <summary>Every gated entry, one theory case each.</summary>
    public static TheoryData<string> GatedEntries => [.. Gated.Select(row => row.Entry)];

    /// <summary>
    /// The gated entries that demand a typed input of their own: the ones on which the two
    /// orderings differ at all.
    /// </summary>
    public static TheoryData<string> GatedEntriesWithTypedInputs =>
        [.. Gated.Where(row => row.Owes is not null).Select(row => row.Entry)];

    [Theory]
    [MemberData(nameof(GatedEntries))]
    public void A_waived_request_that_describes_nothing_else_is_declined_OutsideCurrentScope_citing_107_205(string entryId)
    {
        var row = Row(entryId);
        var unresolved = Declined(Registry.Resolve(Waived(row.Request, row.Regulation)), entryId);

        // One answer, and the same one, whatever typed inputs the entry would otherwise demand: the
        // waiver has put the entry out of reach, so there is nothing left for the caller to supply.
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal(MapEntries.WaivableRegulations.Locator, unresolved.Locator);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);

        // And the decline is this entry's own, not a constituent's: the gate ran against the entry
        // the caller asked about.
        Assert.Contains($"'{entryId}'", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(GatedEntriesWithTypedInputs))]
    public void A_waived_request_is_never_asked_for_a_typed_input_the_waiver_has_made_irrelevant(string entryId)
    {
        var row = Row(entryId);
        var outcome = Evaluated(entryId, OperationFacts.Nothing.Stating(WaiverStatement.Held(row.Regulation, Caller)));

        // The state a product renders. FactRequired here would put the entry in Outstanding —
        // "everything the caller still owes this engine before it could say more" — and name a
        // specific input on MissingInput, for an entry the caller has been told does not apply.
        Assert.Equal(RequirementState.OutsideCurrentScope, outcome.State);
        Assert.Null(outcome.MissingInput);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, outcome.Reason);
        Assert.Equal(MapEntries.WaivableRegulations.Locator, outcome.DeclineCites);

        // And a caller who has said nothing at all — not even about the waiver — is asked for the
        // waiver statement first, on every one of them, because the gate is what the engine reaches
        // first and it cannot run without it.
        Assert.Equal(
            "Waiver",
            Assert.Throws<ArgumentException>(() => Registry.Resolve(New(row.Request))).ParamName);
    }

    [Theory]
    [MemberData(nameof(GatedEntriesWithTypedInputs))]
    public void Stated_that_no_waiver_is_in_force_the_same_request_is_refused_for_the_input_the_entry_owes(string entryId)
    {
        var row = Row(entryId);

        // What moved is the gate, and not what a reachable entry demands. The same request that
        // declines above is refused here, by name, for the first fact the entry owes.
        var refused = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Waived(row.Request, row.Regulation, inForce: false)));

        Assert.Equal(typeof(ArgumentException), refused.GetType());
        Assert.Equal(row.Owes, refused.ParamName);
        Assert.Contains($"'{entryId}'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_table_is_every_entry_whose_request_carries_a_waiver_statement_of_its_own()
    {
        // Measured against the code rather than restated: every generated request type that
        // declares a Waiver property is a gated entry and belongs in the table above, and nothing
        // else does. A gated entry built later fails here until it is added.
        var declaring = typeof(SpeedWithinLimitRequest).Assembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(SpeedWithinLimitRequest).Namespace
                && type.GetProperty("Waiver", BindingFlags.Public | BindingFlags.Instance) is not null)
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal);

        Assert.Equal(
            declaring,
            Gated.Select(row => row.Request.Name).Order(StringComparer.Ordinal));

        // And the table's entry ids are the map's own, not spellings kept here: each request
        // reports the entry it resolves.
        Assert.All(Gated, row => Assert.Equal(row.Entry, New(row.Request).EntryId));
    }

    [Fact]
    public void Nothing_a_stated_waiver_suspends_is_reported_as_something_the_caller_still_owes()
    {
        // Every regulation § 107.205 reaches in this engine, all waived at once, and no other fact
        // stated at all: the whole gated surface in one evaluation, which is the case the divergence
        // this decision settles was found in.
        var facts = Gated
            .Select(row => row.Regulation)
            .Distinct(StringComparer.Ordinal)
            .Aggregate(
                OperationFacts.Nothing,
                (carried, regulation) => carried.Stating(WaiverStatement.Held(regulation, Caller)));

        var evaluation = OperationEvaluator.Evaluate(facts);
        var owed = evaluation.Outstanding.Select(outcome => outcome.EntryId).ToArray();

        Assert.All(
            Gated.Select(row => row.Entry),
            entryId => Assert.DoesNotContain(entryId, owed, StringComparer.Ordinal));

        // The other half of it: each is in Unanswered instead, which is where an entry this engine
        // cannot speak to belongs.
        var unanswered = evaluation.Unanswered.Select(outcome => outcome.EntryId).ToArray();

        Assert.All(
            Gated.Select(row => row.Entry),
            entryId => Assert.Contains(entryId, unanswered, StringComparer.Ordinal));
    }

    private static (string Entry, Type Request, string Regulation, string? Owes) Row(string entryId) =>
        Array.Find(Gated, row => string.Equals(row.Entry, entryId, StringComparison.Ordinal));

    /// <summary>A request stating only what § 107.205 asks, and nothing whatever about the operation.</summary>
    private static IEntryRequest Waived(Type request, string regulation, bool inForce = true)
    {
        var built = New(request);
        request.GetProperty("Waiver")!.SetValue(
            built,
            inForce ? WaiverStatement.Held(regulation, Caller) : WaiverStatement.NoneHeld(regulation, Caller));
        return built;
    }

    private static IEntryRequest New(Type request) => (IEntryRequest)Activator.CreateInstance(request)!;

    private static EvaluatedRequirement Evaluated(string entryId, OperationFacts facts) =>
        OperationEvaluator.Evaluate(facts).Requirements
            .Single(outcome => string.Equals(outcome.EntryId, entryId, StringComparison.Ordinal));

    private static UnresolvedResult Declined(Resolution<object> resolution, string entryId)
    {
        Assert.True(resolution is Resolution<object>.Unresolved, $"{entryId} resolved a value");
        return Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;
    }
}
