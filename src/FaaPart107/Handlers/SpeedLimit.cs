using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>speed-limit</c>'s rule reads.</summary>
    public sealed partial class SpeedLimitRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }

        /// <summary>Null for the limit as printed; a unit to ask for it as one figure in that unit.</summary>
        public SpeedUnit? AsOneFigureIn { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>speed-limit</c>: <see cref="Speed.Limit"/>, the limit as printed, or the rule's decline.</summary>
        internal static partial Resolution<object> SpeedLimit(Requests.SpeedLimitRequest request) =>
            Answer(Speed.Limit(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.AsOneFigureIn));
    }
}
