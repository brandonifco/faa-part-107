using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>visibility-minimum</c>'s rule reads.</summary>
    public sealed partial class VisibilityMinimumRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>visibility-minimum</c>: <see cref="Visibility.Minimum"/>, the minimum as printed, or the rule's decline.</summary>
        internal static partial Resolution<object> VisibilityMinimum(Requests.VisibilityMinimumRequest request) =>
            Answer(Visibility.Minimum(Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
