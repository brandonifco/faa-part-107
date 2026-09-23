using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>effective-communication</c>'s rule reads.</summary>
    /// <remarks>
    /// Only the waiver statement. The entry declines whatever else it is asked — the map records
    /// its question as unresolved — so an input describing the communication would be surface this
    /// engine cannot justify from the map, and would invite the caller to think the engine weighed
    /// it.
    /// </remarks>
    public sealed partial class EffectiveCommunicationRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.33 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>effective-communication</c>: <see cref="Communication.Effective"/>, the rule's decline.</summary>
        internal static partial Resolution<object> EffectiveCommunication(Requests.EffectiveCommunicationRequest request) =>
            Communication.Effective(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)));
    }
}
