using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>direct-participation</c>'s rule reads.</summary>
    /// <remarks>
    /// One, and it is not about the human being. The entry declines whatever it is asked, so an input
    /// describing what a person does in the operation would be surface this engine cannot justify from
    /// the map: it would invite a caller to think the engine weighs it. The waiver statement is here
    /// because the entry's map row names <c>waivable-regulations</c> in <c>suspendedBy</c>.
    /// </remarks>
    public sealed partial class DirectParticipationRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.39 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>direct-participation</c>: <see cref="Participation.Direct"/>, the rule's decline.
        /// </summary>
        /// <remarks>
        /// The decline is the rule's own, naming the term the corpus leaves undefined, rather than the
        /// correspondence row's bare default; the waiver statement is demanded and never defaulted
        /// (rules-factory decision 0021).
        /// </remarks>
        internal static partial Resolution<object> DirectParticipation(Requests.DirectParticipationRequest request) =>
            Participation.Direct(Demand(request.Waiver, request.EntryId, nameof(request.Waiver)));
    }
}
