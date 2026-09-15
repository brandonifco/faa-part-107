using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>speed-within-limit</c>'s rule reads.</summary>
    public sealed partial class SpeedWithinLimitRequest
    {
        /// <summary>The small unmanned aircraft's groundspeed. Required.</summary>
        public Groundspeed? Groundspeed { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>speed-within-limit</c>: <see cref="Speed.Within"/>, the finding, or the rule's decline.</summary>
        internal static partial Resolution<object> SpeedWithinLimit(Requests.SpeedWithinLimitRequest request) =>
            Answer(Speed.Within(
                Demand(request.Groundspeed, request.EntryId, nameof(request.Groundspeed)),
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
