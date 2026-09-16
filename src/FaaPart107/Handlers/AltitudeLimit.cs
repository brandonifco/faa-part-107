using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>altitude-limit</c>'s rule reads.</summary>
    public sealed partial class AltitudeLimitRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>altitude-limit</c>: <see cref="Altitude.Limit"/>, the limit as printed, or the rule's decline.</summary>
        internal static partial Resolution<object> AltitudeLimit(Requests.AltitudeLimitRequest request) =>
            Answer(Altitude.Limit(Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
