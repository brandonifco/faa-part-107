using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>flash-rate-sufficient</c>'s rule reads.</summary>
    /// <remarks>
    /// The fact itself is not among them. § 107.29(a)(2) and (b) state a standard the map records as
    /// <c>kind: assertion</c>, so the value arrives through the assertions dictionary —
    /// <see cref="FlashRateSufficientRequest.Asserting"/> or <see cref="RuleRequest.Assert"/> — and
    /// never as an input the engine could default. Nor is a flash rate among them: the corpus states
    /// no rate, so there is no figure for an input to carry and nothing for the engine to compare one
    /// against. What is an input is the other caller fact the entry needs: whether a waiver of
    /// § 107.29(a)(2) and (b) is in force.
    /// </remarks>
    public sealed partial class FlashRateSufficientRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.29(a)(2) and (b) is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>flash-rate-sufficient</c>: <see cref="FlashRate.SufficientToAvoidACollision"/> — the
        /// waiver gate, then the assertion answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.29(a)(2) and (b) both require anti-collision lighting "that has a flash rate
        /// sufficient to avoid a collision", and the map records that clause as
        /// <c>kind: assertion</c>: the fact is determined outside this engine and reported to it. So
        /// the entry carries no inputs of its own about the lighting — no flash rate, no period, no
        /// duty cycle — and the entry's note says why there could be none: "the corpus states no
        /// rate, so no figure is evidenced".
        /// </para>
        /// <para>
        /// The one input it does carry is the waiver statement, because § 107.205(b) lists
        /// "Section 107.29(a)(2) and (b)" and the entry's <c>suspendedBy</c> names the gate. It is
        /// demanded, never defaulted, and it is read before the assertion is: a waiver in force makes
        /// the entry unreachable, and demanding a fact about an unreachable entry would be asking the
        /// caller for something the waiver has already made irrelevant.
        /// </para>
        /// <para>
        /// Correspondence row 8 has a default, and that default neither consults the waiver gate nor
        /// checks what was asserted about which entry — it hands back the caller's own object, with
        /// the caller's own citation on it. This handler is what keeps the entry from answering that
        /// way, and it is what keeps § 107.29(a)(2) and (b)'s two assertion entries apart: the
        /// sibling on this very locator, <c>intensity-reduction-in-interest-of-safety</c>, is the
        /// remote pilot in command's determination about reducing the lighting's intensity, and is
        /// not this entry.
        /// </para>
        /// </remarks>
        static partial void FlashRateSufficient(Requests.FlashRateSufficientRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(FlashRate.SufficientToAvoidACollision(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
