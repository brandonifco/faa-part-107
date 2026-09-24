using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Whether § 107.25 prohibits an operation because it is from a moving aircraft:
/// <see cref="MapEntries.MovingAircraftOperation"/>, "No person may operate a small unmanned
/// aircraft system— (a) From a moving aircraft;".
/// </summary>
/// <param name="FromAMovingAircraft">
/// Whether the small unmanned aircraft system is operated from a moving aircraft, as the caller
/// stated it. The engine never infers it.
/// </param>
/// <param name="Prohibited">
/// True when constituent (a) prohibits the operation — that is, when it is from a moving aircraft.
/// False when it is not, which is the whole of what (a) says about it and not a finding that
/// anything else in part 107 permits it.
/// </param>
/// <param name="Waiver">The caller's waiver statement the entry was resolved under, recorded with it.</param>
public sealed record MovingAircraftFinding(bool FromAMovingAircraft, bool Prohibited, WaiverStatement Waiver)
{
    /// <summary>Where the rule is stated: <c>§ 107.25</c>.</summary>
    public SourceLocator Authority => MapEntries.MovingAircraftOperation.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        (FromAMovingAircraft
            ? "operation of a small unmanned aircraft system from a moving aircraft is prohibited"
            : "operation of a small unmanned aircraft system from an aircraft that is not moving is not prohibited")
        + $" [{Authority}]; {Waiver}";
}

/// <summary>
/// § 107.25(a): operation of a small unmanned aircraft system from a moving aircraft.
/// </summary>
/// <remarks>
/// <para>
/// The entry the map publishes for this limb is <see cref="MapEntries.MovingAircraftOperation"/>,
/// and its evidence is the whole of what is implemented here: "No person may operate a small
/// unmanned aircraft system— (a) From a moving aircraft;". The map records the entry as
/// <c>clear</c>, with no open question, so the engine answers it.
/// </para>
/// <para>
/// A prohibition without exception. The "unless the small unmanned aircraft is flown over a
/// sparsely populated area and is not transporting another person's property for compensation or
/// hire" in § 107.25 qualifies constituent (b) alone, which the map publishes as the separate
/// entry <c>moving-vehicle-operation</c> and records as <c>ambiguous</c>. Neither of those
/// conditions is read here, and this rule answers nothing about a land or water-borne vehicle.
/// </para>
/// <para>
/// Whether an operation is from a moving aircraft is a fact about one flight and is not in the
/// corpus, so the caller states it and the engine records it; it is not inferred from anything
/// else in the request.
/// </para>
/// </remarks>
public static class MovingAircraft
{
    /// <summary>The regulation § 107.205(a) lists that states this entry: the whole of § 107.25.</summary>
    public const string Regulation = "§ 107.25";

    /// <summary>
    /// <see cref="MapEntries.MovingAircraftOperation"/>: whether "(a) From a moving aircraft" prohibits
    /// the operation the caller describes.
    /// </summary>
    /// <param name="fromAMovingAircraft">
    /// Whether the small unmanned aircraft system is operated from a moving aircraft, as the caller
    /// states it. Never inferred; required once the entry is reachable, and demanded after the gate
    /// (<c>docs/decisions/0007</c>).
    /// </param>
    /// <param name="waiver">Whether a waiver of § 107.25 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver
    /// of § 107.25 is in force.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and
    /// <paramref name="fromAMovingAircraft"/> was not stated.
    /// </exception>
    public static Resolution<MovingAircraftFinding> Operation(bool? fromAMovingAircraft, WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.MovingAircraftOperation, Regulation, waiver) is { } suspended)
        {
            return Resolution<MovingAircraftFinding>.FromUnresolved(suspended);
        }

        var stated = Demands.Of(
            fromAMovingAircraft,
            MapEntries.MovingAircraftOperation,
            nameof(Requests.MovingAircraftOperationRequest.FromAMovingAircraft));

        return Resolution<MovingAircraftFinding>.FromValue(
            new MovingAircraftFinding(stated, Prohibited: stated, waiver));
    }
}
