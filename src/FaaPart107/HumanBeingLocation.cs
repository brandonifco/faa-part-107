namespace FaaPart107;

/// <summary>
/// Where the human being the small unmanned aircraft is operated over is located, as the caller
/// states it: under a covered structure, inside a stationary vehicle, or under neither. It is a
/// fact the rule tests, not a rule, so the map has no entry for it (rules-factory
/// <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the half of § 107.39(b) that is this entry's.</b> The paragraph reads "(b) That human
/// being is located under a covered structure or inside a stationary vehicle that can provide
/// reasonable protection from a falling small unmanned aircraft", and its relative clause binds two
/// facts into one: <i>where the human being is</i>, and <i>whether that very thing can provide
/// reasonable protection</i>. The second is <see cref="MapEntries.ReasonableProtection"/>, a
/// <c>kind: assertion</c> entry whose note says the standard "is asserted and withheld <b>for each
/// of</b> a covered structure and a stationary vehicle" — so the assertion records which of the two
/// it is about (<see cref="Shelter"/>). The first is this, and <see cref="Shelter"/>'s own remarks
/// say so in terms: "Nor is it where the human being is — § 107.39(b) requires the person to be
/// located under or inside the very thing that can provide reasonable protection, and whether they
/// are is <see cref="MapEntries.OverHumanBeings"/>'s, which depends on this entry and must check its
/// own limb against the one recorded here."
/// </para>
/// <para>
/// <b>So the two are compared, and an assertion about one place is not an answer about the other.</b>
/// <see cref="Place"/> is the <see cref="Shelter"/> this location is, and
/// <see cref="Overflight.OverAHumanBeing"/> requires the assertion in hand to be about that same
/// place. A caller who asserts the standard for a stationary vehicle on site while the human being
/// is under a carport has said nothing about the carport, and the engine neither reads the one as
/// the other nor defaults it — which is what the entry's note forbids in the same sentence as "for
/// each of": "never defaulted to true when absent".
/// </para>
/// <para>
/// <b>The set is closed, and the third member is stated rather than inferred from an absence.</b>
/// <see cref="All"/> is every member; the constructor is private and no factory takes a designation,
/// so a caller cannot make a fourth. <see cref="NeitherOfThem"/> is the case
/// <see cref="MapEntries.OverHumanBeings"/>'s note keeps — "a plain overflight with none of them" —
/// and it is a value the caller positively states, never a null and never a default, following
/// <see cref="EncounteredObject.NoneOfThem"/> and <see cref="RelativePosition.NoneOfThem"/>. This is
/// why <see cref="Shelter"/> deliberately has no "neither" member and this type does: the standard
/// is asserted over § 107.39(b)'s two printed places and nothing else, while where a human being is
/// includes the place the paragraph does not reach.
/// </para>
/// <para>
/// <b>What this is not.</b> It is not a measure of protection and not a claim that the place is
/// adequate: § 107.39(b)'s standard is the caller's assertion, and nothing is read off this value
/// about it. The engine holds no register of structures or vehicles, inspects neither, and locates
/// nobody: which of the three it is, is the caller's fact.
/// </para>
/// </remarks>
public sealed record HumanBeingLocation
{
    private HumanBeingLocation(string designation, Shelter? place)
    {
        Designation = designation;
        Place = place;
    }

    /// <summary>
    /// "located under a covered structure", the first of § 107.39(b)'s two places, as the place the
    /// human being is.
    /// </summary>
    public static HumanBeingLocation UnderACoveredStructure { get; } =
        new("located under a covered structure", Shelter.CoveredStructure);

    /// <summary>
    /// "located … inside a stationary vehicle", the second of § 107.39(b)'s two places, as the place
    /// the human being is.
    /// </summary>
    public static HumanBeingLocation InsideAStationaryVehicle { get; } =
        new("located inside a stationary vehicle", Shelter.StationaryVehicle);

    /// <summary>
    /// The human being is under neither of § 107.39(b)'s two places, as the caller states it: the
    /// "plain overflight with none of them" <see cref="MapEntries.OverHumanBeings"/>'s note keeps.
    /// § 107.39(b) does not reach it, whatever anyone asserts about a structure or a vehicle
    /// elsewhere.
    /// </summary>
    public static HumanBeingLocation NeitherOfThem { get; } =
        new("located neither under a covered structure nor inside a stationary vehicle", null);

    /// <summary>
    /// Every place this engine names, in the order this type declares them: § 107.39(b)'s two, in
    /// the paragraph's own order, then the case it does not reach. There is no fourth.
    /// </summary>
    public static IReadOnlyList<HumanBeingLocation> All { get; } =
    [
        UnderACoveredStructure,
        InsideAStationaryVehicle,
        NeitherOfThem,
    ];

    /// <summary>Where the human being is, as this engine names it.</summary>
    public string Designation { get; }

    /// <summary>
    /// Which of § 107.39(b)'s two places this is, as <see cref="Shelter"/> has them, so that the
    /// place the standard was asserted over and the place the human being is located can be
    /// compared. Null for <see cref="NeitherOfThem"/>, which is neither of them.
    /// </summary>
    public Shelter? Place { get; }

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
