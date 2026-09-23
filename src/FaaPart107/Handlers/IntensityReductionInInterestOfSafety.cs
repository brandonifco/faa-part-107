using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>intensity-reduction-in-interest-of-safety</c>'s rule reads.</summary>
    /// <remarks>
    /// The determination itself is not among them: it is the caller's assertion, and it arrives
    /// through <see cref="IEntryRequest.Assertions"/> rather than as an input of this type. What is
    /// declared here is the entry's waiver gate, which every suspended entry demands the same way.
    /// </remarks>
    public sealed partial class IntensityReductionInInterestOfSafetyRequest
    {
        /// <summary>
        /// Whether a certificate of waiver authorizing deviation from § 107.29(a)(2) and (b) is in
        /// force, as the caller states it. Required.
        /// </summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>intensity-reduction-in-interest-of-safety</c>: <see cref="Lighting.IntensityReduction"/>,
        /// the waiver gate first and then the remote pilot in command's determination, answered
        /// unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// § 107.29(a)(2) and (b) say "The remote pilot in command may reduce the intensity of, but
        /// may not extinguish, the anti-collision lighting if he or she determines that, because of
        /// operating conditions, it would be in the interest of safety to do so." The map records
        /// the determination as <c>kind: assertion</c>, asserted by the remote pilot in command, so
        /// the fact is settled outside this engine and reported to it: there is no intensity figure
        /// to read, nothing to derive from operating conditions, and no default in either
        /// direction. The entry carries one input of its own, the waiver statement § 107.205(b)
        /// makes necessary, demanded through <see cref="Demand{T}(T?, string, string)"/> and never
        /// inferred; the determination arrives through
        /// <c>IntensityReductionInInterestOfSafetyRequest.Asserting</c> or
        /// <see cref="RuleRequest.Assert"/>. With no waiver in force, asserting nothing throws
        /// <see cref="AssertionRequiredException"/> rather than declining. What the caller does not
        /// get to supply is the citation: the answer is built on
        /// <see cref="MapEntries.IntensityReductionInInterestOfSafety"/>, so it cites
        /// § 107.29(a)(2), (b) whatever entry the caller's own <see cref="Assertion"/> was carrying.
        /// </remarks>
        static partial void IntensityReductionInInterestOfSafety(Requests.IntensityReductionInInterestOfSafetyRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Lighting.IntensityReduction(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
