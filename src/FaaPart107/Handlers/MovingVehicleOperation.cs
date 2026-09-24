using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>moving-vehicle-operation</c>'s rule reads.</summary>
    /// <remarks>
    /// Each is a fact about one operation, stated by the caller and never inferred by the engine.
    /// There is no input for whether the aircraft is flown over a sparsely populated area: that term
    /// is the entry's unresolved question, and the rule declines wherever the answer turns on it,
    /// whatever anyone supplies.
    /// </remarks>
    public sealed partial class MovingVehicleOperationRequest
    {
        /// <summary>Whether the small unmanned aircraft system is operated from a moving land or water-borne vehicle, as the caller states it. Required.</summary>
        public bool? FromMovingLandOrWaterBorneVehicle { get; init; }

        /// <summary>Whether the small unmanned aircraft is transporting another person's property for compensation or hire, as the caller states it. Required.</summary>
        public bool? TransportingAnotherPersonsPropertyForCompensationOrHire { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.25 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>moving-vehicle-operation</c>: <see cref="MovingVehicle.Operation"/>, the finding, or the rule's decline.</summary>
        /// <remarks>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The two facts about the operation are handed over as the caller left them, so the rule
        /// demands each after the gate (<c>docs/decisions/0007</c>).
        /// </remarks>
        internal static partial Resolution<object> MovingVehicleOperation(Requests.MovingVehicleOperationRequest request) =>
            Answer(MovingVehicle.Operation(
                request.FromMovingLandOrWaterBorneVehicle,
                request.TransportingAnotherPersonsPropertyForCompensationOrHire,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
