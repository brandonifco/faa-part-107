namespace FaaPart107;

/// <summary>
/// The airspace an operation is in, as the caller states it. It is a parameter the rule tests, not
/// a rule, so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MapEntries.AirspaceAuthorized"/>'s note is explicit: "The airspace class is an input.
/// Determining it from a position requires a corpus this map has not admitted." So this engine never
/// derives the airspace from a latitude, a longitude, an altitude, a chart or a facility map. The
/// caller states it, and the engine tests what the caller stated.
/// </para>
/// <para>
/// <b>The set is closed.</b> <see cref="All"/> is every airspace this engine names; the constructor
/// is private and no factory takes a designation, so a caller cannot make another. That is
/// deliberate, and it is this type's whole safety property. An engine that accepted an arbitrary
/// designation would have to say what § 107.41 does about a string it cannot identify, and both
/// available answers are wrong: treating it as airspace the section does not reach permits an
/// unauthorized operation in Class B the moment a caller writes "Class B" rather than
/// "Class B airspace", and declining instead would invent an unresolved reason the map does not
/// record. Matching the engine's own fact or refusing is the pattern every other stated fact here
/// already follows — <see cref="WaiverStatement.Regulation"/> is read only to be compared with the
/// entry's own constant and to throw otherwise.
/// </para>
/// <para>
/// The designations are the ones § 107.41 prints — "Class B, Class C, or Class D airspace or within
/// the lateral boundaries of the surface area of Class E airspace designated for an airport" —
/// together with the two the entry's note adds: Class G, "which needs none", and, by the necessary
/// implication of the section's own restriction, Class E that is not within such a surface area.
/// Nothing else is enumerated, because nothing else is in the map. An airspace outside these six is
/// one the map has not put on the table, and the remedy is a new map version, not a guess here.
/// </para>
/// <para>
/// § 107.41 draws its Class E line at the surface area designated for an airport, so a bare
/// "Class E" is not a statement this type accepts: the caller states which side of that line the
/// operation is on, through <see cref="ClassESurfaceAreaDesignatedForAnAirport"/> or
/// <see cref="ClassEOutsideAnAirportSurfaceArea"/>.
/// </para>
/// </remarks>
public sealed record AirspaceClass
{
    private AirspaceClass(string designation) => Designation = designation;

    /// <summary>Class B airspace, the first § 107.41 names.</summary>
    public static AirspaceClass ClassB { get; } = new("Class B airspace");

    /// <summary>Class C airspace, the second § 107.41 names.</summary>
    public static AirspaceClass ClassC { get; } = new("Class C airspace");

    /// <summary>Class D airspace, the third § 107.41 names.</summary>
    public static AirspaceClass ClassD { get; } = new("Class D airspace");

    /// <summary>
    /// Within the lateral boundaries of the surface area of Class E airspace designated for an
    /// airport: the only Class E § 107.41 names.
    /// </summary>
    public static AirspaceClass ClassESurfaceAreaDesignatedForAnAirport { get; } =
        new("within the lateral boundaries of the surface area of Class E airspace designated for an airport");

    /// <summary>
    /// Class E airspace that is not within the lateral boundaries of a surface area designated for an
    /// airport: Class E § 107.41's own restriction leaves out.
    /// </summary>
    public static AirspaceClass ClassEOutsideAnAirportSurfaceArea { get; } =
        new("Class E airspace outside the lateral boundaries of a surface area designated for an airport");

    /// <summary>Class G airspace, which <see cref="MapEntries.AirspaceAuthorized"/>'s note records as needing no authorization.</summary>
    public static AirspaceClass ClassG { get; } = new("Class G airspace");

    /// <summary>The airspace as this engine names it.</summary>
    public string Designation { get; }

    /// <summary>
    /// Every airspace this engine names, in the order this type declares them. There is no seventh:
    /// a caller chooses from this list or does not resolve the entry.
    /// </summary>
    public static IReadOnlyList<AirspaceClass> All { get; } =
    [
        ClassB,
        ClassC,
        ClassD,
        ClassESurfaceAreaDesignatedForAnAirport,
        ClassEOutsideAnAirportSurfaceArea,
        ClassG,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
