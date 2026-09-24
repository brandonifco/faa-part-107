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
    /// <para>
    /// Evaluating these is meaningful and is the honest starting point: every entry that demands an
    /// input reports <see cref="RequirementState.FactRequired"/>, an assertion entry that demands no
    /// input of its own reports <see cref="RequirementState.HumanAssertionRequired"/>, and every
    /// entry this engine has not built reports what its correspondence row gives.
    /// </para>
    /// <para>
    /// <b>Six of the ten assertion entries do demand an input first</b>, and so report
    /// <see cref="RequirementState.FactRequired"/> here rather than
    /// <see cref="RequirementState.HumanAssertionRequired"/>: the five § 107.205-gated ones want the
    /// waiver statement, and <c>sufficient-available-power</c> wants § 107.49(d)'s own condition
    /// (<c>#95</c>). That is the order those entries owe their caller facts in and not an accident of
    /// this value. Which entries fall which way is measured rather than stated here —
    /// <c>OperationEvaluatorTests
    /// .An_assertion_is_wrapped_exactly_where_a_caller_fact_is_demanded_ahead_of_it</c>, and
    /// <c>docs/decisions/0004-an-assertion-is-recorded-and-not-scored.md</c>.
    /// </para>
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
    /// What the caller states about the cloud § 107.51(d)'s two minimums are distances from — how
    /// far below it and how far horizontally from it the small unmanned aircraft is, or that the
    /// aircraft is not operated near a cloud at all (<c>weather-minimums-met</c>, § 107.51(c)-(d)).
    /// Never inferred: the engine does not invent a cloud, and does not assume there is none.
    /// </summary>
    /// <remarks>
    /// The two distances are not two properties here, and cannot be: two bare figures leave a
    /// caller in clear air only zero to state, and zero feet below a cloud is the closest an
    /// aircraft can be to one. <see cref="CloudStatement.NoCloud"/> is the other case, stated.
    /// </remarks>
    public CloudStatement? Cloud { get; init; }

    /// <summary>
    /// Which of the two people § 107.51's introductory text binds the caller is asking about
    /// (<c>operating-limitations</c>). Required by that entry, and never inferred.
    /// </summary>
    public BoundPerson? BoundPerson { get; init; }

    /// <summary>
    /// Which of § 107.39(b)'s two the caller's assertion is about — a covered structure, or a
    /// stationary vehicle (<c>reasonable-protection</c>, and <c>over-human-beings</c>, which reads
    /// it to check the assertion is about the place the human being is).
    /// </summary>
    public Shelter? Shelter { get; init; }

    /// <summary>
    /// Where the human being the aircraft is operated over is located, as the caller states it:
    /// under a covered structure, inside a stationary vehicle, or under neither
    /// (<c>over-human-beings</c>, § 107.39). Never inferred, and never derived from
    /// <see cref="Shelter"/> — § 107.39(b) binds the two, and deciding which place an assertion was
    /// about is the caller's to state.
    /// </summary>
    public HumanBeingLocation? HumanBeingLocation { get; init; }

    /// <summary>
    /// What the caller states about the small unmanned aircraft's anti-collision lighting —
    /// fitted or not, lighted or extinguished, its intensity reduced or not, and how far it is
    /// visible (<c>anti-collision-lighting</c>, § 107.29(a)(2) and (b)). Never assumed in either
    /// direction.
    /// </summary>
    public LightingStatement? Lighting { get; init; }

    /// <summary>
    /// Whether a visual observer is used during the aircraft operation — § 107.33's chapeau's
    /// condition (<c>visual-observer-conditions</c>). Never inferred in either direction.
    /// </summary>
    public VisualObserverUse? VisualObserverUse { get; init; }

    /// <summary>
    /// Where the operation is, as the caller states it — what § 107.29(c) selects the definition of
    /// civil twilight by (<c>civil-twilight-operation</c>). Stating Alaska makes the entry decline
    /// <see cref="RequirementState.MissingRulesData"/> citing <c>civil-twilight-alaska</c>, which is
    /// the engine naming a hole in itself rather than a finding about the operation.
    /// </summary>
    public OperationPlace? OperationPlace { get; init; }

    /// <summary>
    /// Which of the periods § 107.29(c)(1)-(2) state the operation is during, as the caller states
    /// it, or neither of them (<c>civil-twilight-operation</c>). Never inferred: when official
    /// sunrise and sunset occur at a place on a date is not in this corpus, and the engine has no
    /// clock.
    /// </summary>
    public OperationPeriod? OperationPeriod { get; init; }

    /// <summary>
    /// Whether the operation will be conducted over human beings under subpart D — § 107.49(f)'s
    /// condition (<c>preflight-actions</c>). Never inferred in either direction, and not derived
    /// from <see cref="HumanBeingLocation"/>: § 107.49(f) asks which operation this is, and
    /// § 107.39 asks where a human being is, and they are different questions.
    /// </summary>
    public SubpartDOperation? SubpartDOperation { get; init; }

    /// <summary>
    /// Whether the small unmanned aircraft is powered — § 107.49(d)'s condition
    /// (<c>sufficient-available-power</c>, and <c>preflight-actions</c>, which conjoins that
    /// paragraph). Never inferred in either direction, and never read off the assertion made under
    /// that entry: whether the aircraft is powered is the condition the paragraph states its
    /// obligation under, and whether there is enough available power for the small unmanned aircraft
    /// system to operate for the intended operational time is the fact the remote pilot in command
    /// reports under it.
    /// </summary>
    /// <remarks>
    /// One field feeds both entries, which is how the two cannot answer one operation differently:
    /// <c>sufficient-available-power</c> tests it, and <c>preflight-actions</c> hands it to that
    /// entry's rule rather than testing it again (<c>#99</c>).
    /// </remarks>
    public AircraftPower? AircraftPower { get; init; }

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
