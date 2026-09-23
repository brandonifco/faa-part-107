using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>right-of-way</c>'s rule reads.</summary>
    /// <remarks>
    /// The two facts § 107.37(a)'s two enumerations are tested against, and the waiver statement.
    /// There is no third fact: a separation distance, a closing rate or a time to closest approach
    /// would be surface for the exception "unless well clear", which is <c>well-clear</c>'s open
    /// question and not this engine's to measure, and declaring one would suggest the engine has a
    /// well-clear threshold to apply to it.
    /// </remarks>
    public sealed partial class RightOfWayRequest
    {
        /// <summary>What the small unmanned aircraft passed, as the caller states it. Required, and never inferred: the engine classifies nothing.</summary>
        public EncounteredObject? Encountered { get; init; }

        /// <summary>Where the small unmanned aircraft passed it, relative to it, as the caller states it. Required, and never inferred.</summary>
        public RelativePosition? Position { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.37(a) is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>right-of-way</c>: <see cref="Yielding.RightOfWay"/>, the finding, or the rule's decline.</summary>
        internal static partial Resolution<object> RightOfWay(Requests.RightOfWayRequest request) =>
            Answer(Yielding.RightOfWay(
                Demand(request.Encountered, request.EntryId, nameof(request.Encountered)),
                Demand(request.Position, request.EntryId, nameof(request.Position)),
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
