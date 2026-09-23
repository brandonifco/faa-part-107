using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>prominent-objects</c>'s rule reads.</summary>
    /// <remarks>
    /// The waiver statement, and nothing else. The entry declines whatever object it is asked
    /// about, so an object to judge would be an input the rule never reads — surface this engine
    /// cannot justify from the map, and surface that would suggest the engine judges prominence.
    /// The statement is here because the map marks the entry <c>suspendedBy</c> the § 107.205 gate.
    /// </remarks>
    public sealed partial class ProminentObjectsRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>prominent-objects</c>: <see cref="Prominence.Objects"/>, the gate's decline or the rule's own.
        /// </summary>
        /// <remarks>
        /// The decline is the rule's own, naming the term the corpus leaves undefined, rather than
        /// the correspondence row's bare default.
        /// </remarks>
        internal static partial Resolution<object> ProminentObjects(Requests.ProminentObjectsRequest request) =>
            Prominence.Objects(Demand(request.Waiver, request.EntryId, nameof(request.Waiver)));
    }
}
