namespace FaaPart107;

/// <summary>
/// Whether the small unmanned aircraft is powered, as the caller states it: the condition
/// § 107.49(d) states its obligation under. It is a fact the rule tests, not a rule, so the map has
/// no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// § 107.49(d) reads "<b>If the small unmanned aircraft is powered</b>, ensure that there is enough
/// available power for the small unmanned aircraft system to operate for the intended operational
/// time". That condition is in <see cref="MapEntries.SufficientAvailablePower"/>'s own evidence, so
/// it is that entry's to carry — the asymmetry <c>#95</c> records against
/// <see cref="SubpartDOperation"/>, which carries § 107.49(f)'s condition because that one is in
/// the composite's evidence and in no constituent's.
/// </para>
/// <para>
/// <b>This is the antecedent and never the assertion.</b> Whether the aircraft <em>is powered</em>
/// is the condition the paragraph reaches an operation under; whether there is <em>enough available
/// power for the small unmanned aircraft system to operate for the intended operational time</em>
/// is the fact § 107.49(d) gives the remote pilot in command to report, which arrives as an
/// <see cref="Assertion"/> under this entry's own id and which this engine records unchanged
/// (correspondence row 8). The two are different questions about different things — the aircraft,
/// and the system's endurance against a planned time — and this type answers only the first.
/// <see cref="AvailablePower.Enough"/> keeps them in that order, and never reads one for the other.
/// </para>
/// <para>
/// <b>It is stated, never inferred and never defaulted.</b> The two members below are the
/// condition's two sides and a caller picks one. There is no absent third case that the engine
/// reads as either: an aircraft the caller has not described is not an unpowered aircraft, and
/// defaulting it either way would decide, without being told, whether a paragraph of the corpus
/// reaches the operation. That is the shape <see cref="VisualObserverUse"/> uses for § 107.33's
/// chapeau and <see cref="SubpartDOperation"/> for § 107.49(f).
/// </para>
/// <para>
/// <b>The set is closed.</b> <see cref="All"/> is both values; the constructor is private and no
/// factory takes a designation, so a caller cannot make a third. "If the small unmanned aircraft is
/// powered" is a condition with two sides and no middle, and an engine that accepted "unknown"
/// would have to say what § 107.49(d) requires of an aircraft nobody has described.
/// </para>
/// </remarks>
public sealed record AircraftPower
{
    private AircraftPower(string designation) => Designation = designation;

    /// <summary>
    /// The small unmanned aircraft is powered, as the caller states it: § 107.49(d)'s condition is
    /// satisfied, so the paragraph states an obligation about this operation and the remote pilot in
    /// command's assertion is demanded.
    /// </summary>
    public static AircraftPower Powered { get; } = new("the small unmanned aircraft is powered");

    /// <summary>
    /// The small unmanned aircraft is not powered, as the caller states it: § 107.49(d)'s condition
    /// is not satisfied, so the paragraph states no obligation about this operation and no assertion
    /// about available power is demanded. That is not the same answer as there being enough
    /// available power, and it is not the same answer as there not being enough.
    /// </summary>
    public static AircraftPower NotPowered { get; } = new("the small unmanned aircraft is not powered");

    /// <summary>The condition as this engine states it, in § 107.49(d)'s own terms.</summary>
    public string Designation { get; }

    /// <summary>
    /// Both sides of § 107.49(d)'s condition, in the order this type declares them: powered, then
    /// not powered. There is no third.
    /// </summary>
    public static IReadOnlyList<AircraftPower> All { get; } =
    [
        Powered,
        NotPowered,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
