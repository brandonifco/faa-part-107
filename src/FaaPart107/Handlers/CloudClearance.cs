using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>cloud-clearance</c>'s rule reads.</summary>
    public sealed partial class CloudClearanceRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }

        /// <summary>False for the minimum as printed, both figures; true to ask for it as one distance from a cloud.</summary>
        public bool AsOneDistance { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>cloud-clearance</c>: <see cref="Clouds.Clearance"/>, the minimum as printed, or the rule's decline.</summary>
        internal static partial Resolution<object> CloudClearance(Requests.CloudClearanceRequest request) =>
            Answer(Clouds.Clearance(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.AsOneDistance));
    }
}
