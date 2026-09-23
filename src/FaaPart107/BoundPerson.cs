namespace FaaPart107;

/// <summary>
/// Which of the two people § 107.51's introductory text binds the caller is asking about, as the
/// caller states it. It is a fact the rule records, not a rule, so the map has no entry for it
/// (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// The introductory text names exactly two: "A remote pilot in command <b>and</b> the person
/// manipulating the flight controls of the small unmanned aircraft system must comply with all of
/// the following operating limitations when operating a small unmanned aircraft system". The two
/// members below are those two, and <see cref="MapEntries.OperatingLimitations"/>'s note asks for
/// both — "it binds both the remote pilot in command and the person manipulating the flight
/// controls … Cases: all limitations met; each one failing; and each of the two named persons."
/// </para>
/// <para>
/// <b>The set is closed, and it has no "somebody else".</b> <see cref="All"/> is every person this
/// engine names; the constructor is private and no factory takes a designation, so a caller cannot
/// make another. That follows <see cref="EncounteredObject"/>, and stops short of where that type
/// goes: § 107.37(a)'s note is explicit that an engine told the object is none of the three decides
/// the case, so <see cref="EncounteredObject.NoneOfThem"/> is a value the map authorises. Nothing
/// in this entry's evidence or note says what § 107.51's introductory text requires of a person it
/// does not name, so this engine offers no value for that and answers nothing about it.
/// </para>
/// <para>
/// The two are joined by "and", so the obligation is the same obligation and the answer does not
/// turn on which of them asks. Which one is asking is recorded on the outcome, because the
/// introductory text's whole content beyond the conjunction of limitations is who must comply.
/// </para>
/// </remarks>
public sealed record BoundPerson
{
    private BoundPerson(string designation) => Designation = designation;

    /// <summary>"A remote pilot in command", the first person § 107.51's introductory text names.</summary>
    public static BoundPerson RemotePilotInCommand { get; } = new("a remote pilot in command");

    /// <summary>
    /// "the person manipulating the flight controls of the small unmanned aircraft system", the
    /// second person § 107.51's introductory text names.
    /// </summary>
    public static BoundPerson PersonManipulatingTheFlightControls { get; } =
        new("the person manipulating the flight controls of the small unmanned aircraft system");

    /// <summary>The person as § 107.51's introductory text names them.</summary>
    public string Designation { get; }

    /// <summary>
    /// Every person this engine names, in the order the introductory text prints them. There is no
    /// third.
    /// </summary>
    public static IReadOnlyList<BoundPerson> All { get; } =
    [
        RemotePilotInCommand,
        PersonManipulatingTheFlightControls,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
