using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>well-clear</c>'s rule reads.</summary>
    /// <remarks>
    /// The waiver statement, and nothing else. The entry declines whatever else it is told, so an
    /// input the rule never reads — a separation distance, a closing rate, the other aircraft's
    /// position — would be surface this engine cannot justify from the map, and would suggest the
    /// engine has a well-clear measure to apply to it. The statement is not such an input: the map
    /// suspends this entry by <c>waivable-regulations</c>, and which decline the entry answers
    /// turns on it.
    /// </remarks>
    public sealed partial class WellClearRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.37(a) is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>well-clear</c>: <see cref="Yielding.WellClear"/>, the rule's decline.</summary>
        /// <remarks>
        /// The decline is the rule's own — the gate's while a waiver is stated in force, and
        /// otherwise the one naming the term the corpus leaves undefined — rather than the
        /// correspondence row's bare default. Nothing the caller asserts for this entry is read:
        /// it is <c>kind: operation</c>, not <c>assertion</c>.
        /// </remarks>
        internal static partial Resolution<object> WellClear(Requests.WellClearRequest request) =>
            Yielding.WellClear(Demand(request.Waiver, request.EntryId, nameof(request.Waiver)));
    }
}
