using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Whether § 107.25(b) prohibits an operation: <see cref="MapEntries.MovingVehicleOperation"/>,
/// "No person may operate a small unmanned aircraft system— ... (b) From a moving land or
/// water-borne vehicle unless the small unmanned aircraft is flown over a sparsely populated area
/// and is not transporting another person's property for compensation or hire."
/// </summary>
/// <remarks>
/// The finding is about paragraph (b) and nothing else. § 107.25(a), from a moving aircraft, is
/// <see cref="MapEntries.MovingAircraftOperation"/>, a separate entry sharing this locator, and
/// nothing here says whether any other rule of part 107 permits the operation.
/// </remarks>
/// <param name="FromMovingLandOrWaterBorneVehicle">Whether the system is operated from a moving land or water-borne vehicle, as the caller states it.</param>
/// <param name="TransportingAnotherPersonsPropertyForCompensationOrHire">Whether the small unmanned aircraft is transporting another person's property for compensation or hire, as the caller states it.</param>
/// <param name="Prohibited">True when § 107.25(b) prohibits the operation.</param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record MovingVehicleFinding(
    bool FromMovingLandOrWaterBorneVehicle,
    bool TransportingAnotherPersonsPropertyForCompensationOrHire,
    bool Prohibited,
    WaiverStatement Waiver)
{
    /// <summary>Where the prohibition is stated: <c>§ 107.25</c>.</summary>
    public SourceLocator Authority => MapEntries.MovingVehicleOperation.Locator;

    /// <inheritdoc/>
    public override string ToString()
    {
        var vehicle = FromMovingLandOrWaterBorneVehicle
            ? "operated from a moving land or water-borne vehicle"
            : "not operated from a moving land or water-borne vehicle";
        var property = TransportingAnotherPersonsPropertyForCompensationOrHire
            ? "transporting another person's property for compensation or hire"
            : "not transporting another person's property for compensation or hire";
        var verdict = Prohibited ? "§ 107.25(b) prohibits it" : "§ 107.25(b) does not prohibit it";
        return $"{vehicle}, {property}: {verdict} [{Authority}]; {Waiver}";
    }
}

/// <summary>§ 107.25(b): operation from a moving land or water-borne vehicle.</summary>
public static class MovingVehicle
{
    /// <summary>The regulation § 107.205(a) lists that states the entry: the whole of § 107.25.</summary>
    public const string Regulation = "§ 107.25";

    /// <summary>
    /// <see cref="MapEntries.MovingVehicleOperation"/>: whether § 107.25(b) prohibits the operation,
    /// where the answer does not turn on the entry's open question, and the entry's decline where it does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// (b) prohibits operating from a moving land or water-borne vehicle "unless" two things hold
    /// together: the aircraft "is flown over a sparsely populated area" <em>and</em> it "is not
    /// transporting another person's property for compensation or hire". Two situations the evidence
    /// names do not turn on the first of those. An operation that is not from a moving land or
    /// water-borne vehicle is not what (b) prohibits at all. An operation that is, and that is
    /// transporting another person's property for compensation or hire, fails the second conjunct, so
    /// the exception cannot hold however the first is read, and (b) prohibits it.
    /// </para>
    /// <para>
    /// What is left — from a moving land or water-borne vehicle, not carrying another's property for
    /// compensation or hire — turns entirely on whether the aircraft "is flown over a sparsely
    /// populated area", and the map records that as this entry's unresolved question: part 107 does
    /// not define "sparsely populated area", and the term carries the whole exception in (b). So the
    /// engine declines it. There is deliberately no input for the area: a caller's statement that it
    /// is, or is not, sparsely populated would be this engine giving effect to a term the published
    /// map says is undefined, and the answer would be an interpretation wearing the engine's citation.
    /// </para>
    /// </remarks>
    /// <param name="fromMovingLandOrWaterBorneVehicle">
    /// Whether the small unmanned aircraft system is operated from a moving land or water-borne vehicle,
    /// as the caller states it. The engine does not infer it, and (a)'s moving aircraft is not this entry.
    /// Required once the entry is reachable, and demanded after the gate (<c>docs/decisions/0007</c>).
    /// </param>
    /// <param name="transportingAnotherPersonsPropertyForCompensationOrHire">
    /// Whether the small unmanned aircraft is transporting another person's property for compensation or
    /// hire, as the caller states it. The engine does not infer the commercial character of a flight.
    /// Required once the entry is reachable, and demanded after the gate.
    /// </param>
    /// <param name="waiver">Whether a waiver of § 107.25 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is
    /// in force; <see cref="UnresolvedReason.RequiresInterpretation"/> citing § 107.25 where the answer
    /// turns on "sparsely populated area".
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and either fact
    /// about the operation was not stated.
    /// </exception>
    public static Resolution<MovingVehicleFinding> Operation(
        bool? fromMovingLandOrWaterBorneVehicle,
        bool? transportingAnotherPersonsPropertyForCompensationOrHire,
        WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.MovingVehicleOperation, Regulation, waiver) is { } suspended)
        {
            return Resolution<MovingVehicleFinding>.FromUnresolved(suspended);
        }

        var fromVehicle = Demands.Of(
            fromMovingLandOrWaterBorneVehicle,
            MapEntries.MovingVehicleOperation,
            nameof(Requests.MovingVehicleOperationRequest.FromMovingLandOrWaterBorneVehicle));
        var transporting = Demands.Of(
            transportingAnotherPersonsPropertyForCompensationOrHire,
            MapEntries.MovingVehicleOperation,
            nameof(Requests.MovingVehicleOperationRequest.TransportingAnotherPersonsPropertyForCompensationOrHire));

        if (!fromVehicle)
        {
            return Resolution<MovingVehicleFinding>.FromValue(new MovingVehicleFinding(
                fromVehicle,
                transporting,
                Prohibited: false,
                waiver));
        }

        if (transporting)
        {
            return Resolution<MovingVehicleFinding>.FromValue(new MovingVehicleFinding(
                fromVehicle,
                transporting,
                Prohibited: true,
                waiver));
        }

        return Resolution<MovingVehicleFinding>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.MovingVehicleOperation.Id}' prohibits an operation from a moving "
            + "land or water-borne vehicle that is not transporting another person's property for compensation or hire: "
            + "§ 107.25(b) then turns on whether the small unmanned aircraft is flown over a sparsely populated area, "
            + "part 107 does not define \"sparsely populated area\", and the term carries the whole exception",
            MapEntries.MovingVehicleOperation.Locator));
    }
}
