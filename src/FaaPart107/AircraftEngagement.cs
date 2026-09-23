namespace FaaPart107;

/// <summary>
/// The three roles § 107.35 names: "manipulate flight controls or act as a remote pilot in command
/// or visual observer in the operation of" an unmanned aircraft.
/// </summary>
/// <remarks>
/// The list is the evidence's own, and it is closed. A person who is in none of these roles in the
/// operation of an aircraft is not doing what the sentence prohibits, so that aircraft is not one
/// the rule counts, and the caller does not state it.
/// </remarks>
public enum AircraftRole
{
    /// <summary>"manipulate flight controls".</summary>
    ManipulatingFlightControls,

    /// <summary>"act as a remote pilot in command".</summary>
    RemotePilotInCommand,

    /// <summary>"act as a ... visual observer".</summary>
    VisualObserver,
}

/// <summary>
/// One unmanned aircraft, and the role § 107.35 names that a person fills in its operation, as the
/// caller states it. It is a fact the rule tests, not a rule, so the map has no entry for it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Aircraft"/> is the caller's own designation for the aircraft, and the engine does not
/// parse it, look it up or normalise it: two engagements naming the same designation are the same
/// aircraft, and two designations are two aircraft. That is the caller's fact. The engine holds no
/// register of aircraft and infers none — § 107.35 counts unmanned aircraft, and the caller is who
/// knows which are which.
/// </para>
/// <para>
/// Nor is a time modelled. The caller states the engagements a person is in "at the same time";
/// which engagements are simultaneous is the caller's fact too, not something the engine derives
/// from clocks, flights or sorties.
/// </para>
/// </remarks>
/// <param name="Aircraft">The caller's designation for the unmanned aircraft, non-empty.</param>
/// <param name="Role">The role the person fills in its operation.</param>
public sealed record AircraftEngagement(string Aircraft, AircraftRole Role)
{
    /// <summary>The caller's designation for the unmanned aircraft, checked to be non-empty.</summary>
    public string Aircraft { get; } = CheckAircraft(Aircraft);

    /// <summary>The role the person fills in its operation, checked to be one § 107.35 names.</summary>
    public AircraftRole Role { get; } = CheckRole(Role);

    /// <summary>The role as § 107.35 words it.</summary>
    public string RoleAsStated => Role switch
    {
        AircraftRole.ManipulatingFlightControls => "manipulating flight controls",
        AircraftRole.RemotePilotInCommand => "acting as a remote pilot in command",
        _ => "acting as a visual observer",
    };

    /// <inheritdoc/>
    public override string ToString() => $"{RoleAsStated} in the operation of {Aircraft}";

    private static string CheckAircraft(string aircraft)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraft, nameof(aircraft));
        return aircraft;
    }

    private static AircraftRole CheckRole(AircraftRole role) =>
        Enum.IsDefined(role)
            ? role
            : throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "§ 107.35 names three roles: manipulating flight controls, acting as a remote pilot in command, acting as a visual observer");
}
