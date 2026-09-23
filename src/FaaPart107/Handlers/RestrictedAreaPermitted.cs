using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>restricted-area-permitted</c>'s rule reads.</summary>
    /// <remarks>
    /// Both are facts about one operation that no published text states, so both are the caller's and
    /// neither is defaulted. There is no waiver input: the entry's <c>suspendedBy</c> is empty, so
    /// § 107.45 has no waiver gate in this map, and an input the rule never reads would be surface
    /// this engine cannot justify from the map.
    /// </remarks>
    public sealed partial class RestrictedAreaPermittedRequest
    {
        /// <summary>The designation of the area the operation is in, as the caller states it. Required.</summary>
        public AreaDesignation? Area { get; init; }

        /// <summary>Whether the permission § 107.45 names is held, as the caller states it. Required.</summary>
        public PermissionStatement? Permission { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>restricted-area-permitted</c>: <see cref="ProhibitedAndRestrictedAreas.Permitted"/>, the finding.
        /// </summary>
        /// <remarks>
        /// The designation and the permission are handed to the rule unchanged. Neither is inferred:
        /// a request that omits one refuses to resolve rather than let the engine decide whether an
        /// area is prohibited or restricted, or whether an agency granted permission.
        /// </remarks>
        internal static partial Resolution<object> RestrictedAreaPermitted(Requests.RestrictedAreaPermittedRequest request) =>
            Answer(ProhibitedAndRestrictedAreas.Permitted(
                Demand(request.Area, request.EntryId, nameof(request.Area)),
                Demand(request.Permission, request.EntryId, nameof(request.Permission))));
    }
}
