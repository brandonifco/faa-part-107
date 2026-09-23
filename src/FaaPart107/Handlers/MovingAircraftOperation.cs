using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>moving-aircraft-operation</c>'s rule reads.</summary>
    /// <remarks>
    /// Two, and no more. § 107.25's "unless ... a sparsely populated area ... compensation or hire"
    /// qualifies constituent (b) alone — the separate entry <c>moving-vehicle-operation</c> — so
    /// neither condition is an input here, and an input this entry's rule never reads would be
    /// surface this engine cannot justify from the map.
    /// </remarks>
    public sealed partial class MovingAircraftOperationRequest
    {
        /// <summary>
        /// Whether the small unmanned aircraft system is operated from a moving aircraft, as the caller
        /// states it. Required; the engine does not infer it, in either direction.
        /// </summary>
        public bool? FromAMovingAircraft { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.25 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>moving-aircraft-operation</c>: <see cref="MovingAircraft.Operation"/>, the finding, or the rule's decline.</summary>
        internal static partial Resolution<object> MovingAircraftOperation(Requests.MovingAircraftOperationRequest request) =>
            Answer(MovingAircraft.Operation(
                Demand(request.FromAMovingAircraft, request.EntryId, nameof(request.FromAMovingAircraft)),
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
