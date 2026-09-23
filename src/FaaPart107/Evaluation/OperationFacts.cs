using System.Collections.Immutable;

namespace FaaPart107.Evaluation;

/// <summary>
/// What a caller states about one operation: the facts the entries this engine has built actually
/// read, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every field traces to an entry.</b> Each one below is an <c>init</c> property some entry's
/// request declares in <c>Handlers/</c>, carried here under the entry's own name and handed to the
/// entry unchanged. A field no implemented entry reads is a field that does not belong, so there
/// is no mission, customer, organization, user, account, document or record identifier here, no
/// timestamp of record, and no persistence concern. <see cref="AsOf"/> is the one date, and it is
/// here because § 107.29(d)'s second sentence is a comparison of dates that
/// <c>night-waiver-termination</c>'s request demands — the engine has no clock.
/// </para>
/// <para>
/// <b>Absence is representable, and it is not falsity.</b> Every fact is nullable and defaults to
/// null, which means <em>the caller did not say</em>. Nothing here is defaulted to <c>false</c>,
/// <c>0</c> or <c>default</c> on the way to a rule: an entry that needs a fact it was not given
/// refuses to resolve (<see cref="RequirementState.FactRequired"/>), and the evaluation reports
/// that rather than an answer the engine invented. That is the property the whole result model
/// exists to preserve.
/// </para>
/// <para>
/// <b>Two facts are not ordinary properties, because getting them wrong loses the attribution.</b>
/// A waiver statement is per regulation (§ 107.205 lists the regulations a certificate of waiver
/// may authorize deviation from, and each suspended entry demands a statement about its own
/// regulation and refuses one about another), and an assertion is per map entry. Both are added
/// through a method — <see cref="Stating"/> and <see cref="Asserting"/> — which files each under
/// the key the value itself carries, so a statement about § 107.41 can never reach § 107.51's
/// entry and an assertion about one entry can never be read as another's.
/// </para>
/// <para>
/// <b>What is deliberately not here.</b> <c>SpeedLimitRequest.AsOneFigureIn</c> and
/// <c>CloudClearanceRequest.AsOneDistance</c> are ways of asking for a printed figure, not facts
/// about an operation, so the evaluator asks for each figure as the corpus prints it and this type
/// carries neither.
/// </para>
/// </remarks>
public sealed record OperationFacts
{
    private static readonly ImmutableDictionary<string, WaiverStatement> NoWaiversStated =
        ImmutableDictionary<string, WaiverStatement>.Empty.WithComparers(StringComparer.Ordinal);

    /// <summary>Facts about no operation at all: nothing stated, nothing asserted, no waiver named.</summary>
    /// <remarks>
    /// Evaluating these is meaningful and is the honest starting point: every entry that demands an
    /// input reports <see cref="RequirementState.FactRequired"/>, every assertion entry reports
    /// <see cref="RequirementState.HumanAssertionRequired"/>, and every entry this engine has not
    /// built reports what its correspondence row gives.
    /// </remarks>
    public static OperationFacts Nothing { get; } = new();

    /// <summary>
    /// The small unmanned aircraft's groundspeed, in the unit the caller measured it in
    /// (<c>speed-within-limit</c>, § 107.51(a)).
    /// </summary>
    public Groundspeed? Groundspeed { get; init; }

    /// <summary>
    /// The small unmanned aircraft's altitude, in feet above ground level
    /// (<c>altitude-within-limit</c>, § 107.51(b)).
    /// </summary>
    public decimal? AltitudeAboveGroundLevelFeet { get; init; }

    /// <summary>
    /// What the caller states about the structure § 107.51(b)'s exception is claimed under
    /// (<c>altitude-within-limit</c>). Never inferred: the engine does not assume there is no
    /// structure, and does not invent one.
    /// </summary>
    public StructureStatement? Structure { get; init; }

    /// <summary>
    /// The flight visibility observed from the location of the control station, in statute miles
    /// (<c>weather-minimums-met</c>, § 107.51(c)-(d)).
    /// </summary>
    public decimal? FlightVisibilityStatuteMiles { get; init; }

    /// <summary>
    /// How far below the cloud the small unmanned aircraft is, in feet
    /// (<c>weather-minimums-met</c>, § 107.51(c)-(d)).
    /// </summary>
    public decimal? FeetBelowCloud { get; init; }

    /// <summary>
    /// How far horizontally from the cloud the small unmanned aircraft is, in feet
    /// (<c>weather-minimums-met</c>, § 107.51(c)-(d)).
    /// </summary>
    public decimal? FeetHorizontallyFromCloud { get; init; }

    /// <summary>
    /// Which of the two people § 107.51's introductory text binds the caller is asking about
    /// (<c>operating-limitations</c>). Required by that entry, and never inferred.
    /// </summary>
    public BoundPerson? BoundPerson { get; init; }

    /// <summary>
    /// Which of § 107.39(b)'s two the caller's assertion is about — a covered structure, or a
    /// stationary vehicle (<c>reasonable-protection</c>).
    /// </summary>
    public Shelter? Shelter { get; init; }

    /// <summary>The airspace the operation is in (<c>airspace-authorized</c>, § 107.41).</summary>
    public AirspaceClass? Airspace { get; init; }

    /// <summary>
    /// What the caller states about prior authorization from Air Traffic Control
    /// (<c>airspace-authorized</c>, § 107.41). Never inferred in either direction.
    /// </summary>
    public AtcAuthorization? AtcAuthorization { get; init; }

    /// <summary>
    /// The designation of the area the operation is in (<c>restricted-area-permitted</c>,
    /// § 107.45). The engine reads no chart and classifies no area.
    /// </summary>
    public AreaDesignation? Area { get; init; }

    /// <summary>
    /// What the caller states about the permission § 107.45 names
    /// (<c>restricted-area-permitted</c>).
    /// </summary>
    public PermissionStatement? AreaPermission { get; init; }

    /// <summary>
    /// Whether the small unmanned aircraft system is operated from a moving land or water-borne
    /// vehicle (<c>moving-vehicle-operation</c>, § 107.25(b)).
    /// </summary>
    public bool? FromMovingLandOrWaterBorneVehicle { get; init; }

    /// <summary>
    /// Whether the small unmanned aircraft is transporting another person's property for
    /// compensation or hire (<c>moving-vehicle-operation</c>, § 107.25(b)).
    /// </summary>
    public bool? TransportingAnotherPersonsPropertyForCompensationOrHire { get; init; }

    /// <summary>
    /// Whether the small unmanned aircraft system is operated from a moving aircraft
    /// (<c>moving-aircraft-operation</c>, § 107.25(a)).
    /// </summary>
    public bool? FromAMovingAircraft { get; init; }

    /// <summary>
    /// The person § 107.35 binds, as the caller names them (<c>single-aircraft</c>). Free text; the
    /// engine does not parse it.
    /// </summary>
    public string? Person { get; init; }

    /// <summary>
    /// Every unmanned aircraft <see cref="Person"/> is, at the same time, in one of § 107.35's
    /// three roles for (<c>single-aircraft</c>). An empty list states that there are none, which is
    /// a different statement from not supplying the list at all.
    /// </summary>
    public IReadOnlyList<AircraftEngagement>? Engagements { get; init; }

    /// <summary>
    /// What the small unmanned aircraft passed (<c>right-of-way</c>, § 107.37(a)). Never inferred:
    /// the engine classifies nothing.
    /// </summary>
    public EncounteredObject? Encountered { get; init; }

    /// <summary>
    /// Where the small unmanned aircraft passed it, relative to it (<c>right-of-way</c>,
    /// § 107.37(a)).
    /// </summary>
    public RelativePosition? Position { get; init; }

    /// <summary>
    /// Who exercised the ability § 107.31(a) describes, throughout the entire flight, as the
    /// caller states it (<c>visual-line-of-sight</c>, § 107.31(b)).
    /// </summary>
    public ExerciseOfTheAbility? Exercise { get; init; }

    /// <summary>
    /// The certificate of waiver § 107.29(d)'s second sentence is asked about, as the caller
    /// describes it (<c>night-waiver-termination</c>).
    /// </summary>
    /// <remarks>
    /// This is not a <see cref="WaiverStatement"/> and does not belong with
    /// <see cref="Waivers"/>: whether a certificate has terminated is what the entry works out, so
    /// it cannot also be an input to it.
    /// </remarks>
    public WaiverCertificate? NightWaiverCertificate { get; init; }

    /// <summary>
    /// The date the question is asked as of (<c>night-waiver-termination</c>, § 107.29(d)), which
    /// the sentence's "terminate on May 17, 2021" is compared with.
    /// </summary>
    /// <remarks>
    /// The engine has no clock, and a date it invented would be an answer the caller never asked
    /// for (<c>AGENTS.md</c> §8). This is the operation's own date, not a timestamp of record.
    /// </remarks>
    public DateOnly? AsOf { get; init; }

    /// <summary>
    /// What the caller states about certificates of waiver, one statement per regulation, keyed
    /// ordinally by the statement's own <see cref="WaiverStatement.Regulation"/>. Add one with
    /// <see cref="Stating"/>.
    /// </summary>
    /// <remarks>
    /// A regulation with no statement is a regulation the caller did not speak to, and every entry
    /// § 107.205 suspends then reports <see cref="RequirementState.FactRequired"/> rather than
    /// being evaluated under an assumption. Defaulting to "no waiver" convicts a holder;
    /// defaulting to "waiver" excuses everyone (rules-factory decision 0021).
    /// </remarks>
    public ImmutableDictionary<string, WaiverStatement> Waivers { get; private init; } = NoWaiversStated;

    /// <summary>
    /// What the caller asserts for the map's <c>kind: assertion</c> entries, as the registry's own
    /// <see cref="RuleRequest"/> holds it. Add one with <see cref="Asserting"/>.
    /// </summary>
    public RuleRequest Assertions { get; private init; } = RuleRequest.Empty;

    /// <summary>
    /// These facts, also stating <paramref name="waiver"/> about the regulation the statement
    /// itself names. A later statement about the same regulation replaces an earlier one.
    /// </summary>
    /// <param name="waiver">What the caller states about a certificate of waiver.</param>
    /// <returns>The facts, with the statement filed under its own regulation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="waiver"/> is null.</exception>
    public OperationFacts Stating(WaiverStatement waiver)
    {
        ArgumentNullException.ThrowIfNull(waiver);
        return this with { Waivers = Waivers.SetItem(waiver.Regulation, waiver) };
    }

    /// <summary>
    /// These facts, also asserting <paramref name="assertion"/> for the map entry the assertion
    /// itself names. A later assertion about the same entry replaces an earlier one.
    /// </summary>
    /// <param name="assertion">What the caller asserts, and who is answerable for it.</param>
    /// <returns>The facts, with the assertion filed under its own entry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assertion"/> is null.</exception>
    public OperationFacts Asserting(Assertion assertion)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        return this with { Assertions = Assertions.Assert(assertion.Entry.Id, assertion) };
    }

    /// <summary>What the caller stated about a certificate of waiver of <paramref name="regulation"/>, or null where the caller said nothing.</summary>
    /// <param name="regulation">The regulation, as § 107.205 designates it.</param>
    /// <returns>The statement, or null.</returns>
    /// <exception cref="ArgumentException"><paramref name="regulation"/> is null or blank.</exception>
    public WaiverStatement? WaiverOf(string regulation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regulation);
        return Waivers.TryGetValue(regulation, out var waiver) ? waiver : null;
    }
}
