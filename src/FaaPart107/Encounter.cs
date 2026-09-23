namespace FaaPart107;

/// <summary>
/// What the small unmanned aircraft passed, as the caller states it: one of the three kinds
/// § 107.37(a) names, or an object that is none of them. It is a fact the rule tests, not a rule,
/// so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// § 107.37(a) prints the kinds as one closed list — "all aircraft, airborne vehicles, and launch
/// and reentry vehicles" — of exactly three: "aircraft", "airborne vehicles" and "launch and
/// reentry vehicles", the last a single kind the serial comma joins to the other two. The three
/// members below are those three, and <see cref="NoneOfThem"/> is the fourth case the caller
/// states when the object is none of them, which <see cref="MapEntries.RightOfWay"/>'s note is
/// explicit the engine decides: "An engine told the object is none of the three … decides the case
/// without the open term at all".
/// </para>
/// <para>
/// <b>The set is closed.</b> <see cref="All"/> is every object this engine names; the constructor
/// is private and no factory takes a designation, so a caller cannot make another. That follows
/// <see cref="AirspaceClass"/>, and for the same reason: an engine that accepted an arbitrary
/// designation would have to say what § 107.37(a) does about a string it cannot identify, and
/// answering "the section does not name it" the moment a caller writes something the engine does
/// not recognise is a permission the section does not give. Which of the four an object is, is the
/// caller's fact; the engine holds no register of aircraft or vehicles and classifies nothing.
/// </para>
/// </remarks>
public sealed record EncounteredObject
{
    private EncounteredObject(string designation) => Designation = designation;

    /// <summary>"aircraft", the first kind § 107.37(a) names.</summary>
    public static EncounteredObject Aircraft { get; } = new("an aircraft");

    /// <summary>"airborne vehicles", the second kind § 107.37(a) names.</summary>
    public static EncounteredObject AirborneVehicle { get; } = new("an airborne vehicle");

    /// <summary>
    /// "launch and reentry vehicles", the third kind § 107.37(a) names — one kind, not two: the
    /// section's serial comma joins it to the other two as the last of three.
    /// </summary>
    public static EncounteredObject LaunchOrReentryVehicle { get; } = new("a launch or reentry vehicle");

    /// <summary>
    /// An object that is none of the three § 107.37(a) names, as the caller states it. The section
    /// does not reach it, so nothing in it is yielded to.
    /// </summary>
    public static EncounteredObject NoneOfThem { get; } =
        new("an object that is neither an aircraft, an airborne vehicle, nor a launch or reentry vehicle");

    /// <summary>The object as this engine names it.</summary>
    public string Designation { get; }

    /// <summary>
    /// Every object this engine names, in the order this type declares them: the three
    /// § 107.37(a) names, then the case it does not reach. There is no fifth.
    /// </summary>
    public static IReadOnlyList<EncounteredObject> All { get; } =
    [
        Aircraft,
        AirborneVehicle,
        LaunchOrReentryVehicle,
        NoneOfThem,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}

/// <summary>
/// Where the small unmanned aircraft passed the object, relative to it, as the caller states it:
/// one of the three relative positions § 107.37(a) prohibits, or none of them. It is a fact the
/// rule tests, not a rule, so the map has no entry for it.
/// </summary>
/// <remarks>
/// <para>
/// § 107.37(a) prints the positions as one closed list — "may not pass over, under, or ahead of
/// it" — of exactly three: over, under, ahead. The three members below are those three, and
/// <see cref="NoneOfThem"/> is the fourth case the caller states when the pass is none of them,
/// which <see cref="MapEntries.RightOfWay"/>'s note is explicit the engine decides: "An engine told
/// … that the manoeuvre is none of the three, decides the case without the open term at all".
/// </para>
/// <para>
/// <b>The set is closed</b>, for the reason <see cref="EncounteredObject"/> gives. Where the pass
/// was is the caller's fact: this engine reads no position, no track and no closing geometry, and
/// derives none.
/// </para>
/// </remarks>
public sealed record RelativePosition
{
    private RelativePosition(string designation) => Designation = designation;

    /// <summary>"over", the first position § 107.37(a) prohibits passing in.</summary>
    public static RelativePosition Over { get; } = new("over it");

    /// <summary>"under", the second position § 107.37(a) prohibits passing in.</summary>
    public static RelativePosition Under { get; } = new("under it");

    /// <summary>"ahead", the third position § 107.37(a) prohibits passing in.</summary>
    public static RelativePosition Ahead { get; } = new("ahead of it");

    /// <summary>
    /// A pass that is none of the three § 107.37(a) prohibits, as the caller states it. The
    /// section's prohibition does not reach it.
    /// </summary>
    public static RelativePosition NoneOfThem { get; } = new("neither over, under, nor ahead of it");

    /// <summary>The position as this engine names it.</summary>
    public string Designation { get; }

    /// <summary>
    /// Every position this engine names, in the order this type declares them: the three
    /// § 107.37(a) prohibits, then the case it does not reach. There is no fifth.
    /// </summary>
    public static IReadOnlyList<RelativePosition> All { get; } =
    [
        Over,
        Under,
        Ahead,
        NoneOfThem,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
