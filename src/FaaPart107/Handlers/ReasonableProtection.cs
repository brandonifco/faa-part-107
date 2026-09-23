using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>reasonable-protection</c>'s rule reads.</summary>
    /// <remarks>
    /// The fact itself is not among them. § 107.39(b) is <c>kind: assertion</c>, so the value arrives
    /// through the assertions dictionary — <see cref="ReasonableProtectionRequest.Asserting"/> or
    /// <see cref="RuleRequest.Assert"/> — and never as an input the engine could default. What are
    /// inputs are the two other caller facts the entry needs: which of § 107.39(b)'s two the
    /// assertion is about, and whether a waiver of § 107.39 is in force. Neither is a measure of
    /// anything, and there is no third: a roof material, a wall thickness or a vehicle's construction
    /// would be surface for a judgement the corpus hands to the caller outright.
    /// </remarks>
    public sealed partial class ReasonableProtectionRequest
    {
        /// <summary>
        /// Which of § 107.39(b)'s two the assertion is about — a covered structure, or a stationary
        /// vehicle — as the caller states it. Required, and never inferred: the entry's note makes
        /// the assertion "for each of" the two, so the engine neither picks one nor lets an assertion
        /// about one answer for the other.
        /// </summary>
        public Shelter? Shelter { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.39 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>reasonable-protection</c>: <see cref="Protection.Reasonable"/> — the waiver gate, then
        /// the place the caller names, then the assertion answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.39(b) states a standard — a covered structure or a stationary vehicle "that can
        /// provide reasonable protection from a falling small unmanned aircraft" — and no measure of
        /// one. The map records it as <c>kind: assertion</c>, and the fact is determined outside this
        /// engine and reported to it. So the entry carries no inputs of its own about protection: no
        /// roof material, no wall thickness, no vehicle construction, no aircraft mass. Surface of
        /// that kind would suggest this engine has a measure to apply, and it has none to apply.
        /// </para>
        /// <para>
        /// <see cref="Requests.ReasonableProtectionRequest.Shelter"/> is not such a measure: it is
        /// which of the paragraph's own two the assertion is about, a closed pair in the corpus's own
        /// words, demanded because the entry's note distributes the assertion "for each of a covered
        /// structure and a stationary vehicle". It is passed in unresolved so the rule can demand it
        /// <i>after</i> the gate: a waiver in force makes it as irrelevant as the assertion.
        /// </para>
        /// <para>
        /// The waiver statement is demanded here, never defaulted, because
        /// <see cref="Waivers.Suspension"/> is what reads it. Correspondence row 8 has a default, and
        /// that default neither consults the gate nor checks who asserted what — it hands back the
        /// caller's own object, with the caller's own citation on it, and drops both the place and
        /// the waiver statement the resolved answer must record. This handler is what keeps the entry
        /// from answering that way.
        /// </para>
        /// </remarks>
        static partial void ReasonableProtection(Requests.ReasonableProtectionRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Protection.Reasonable(
                request.Shelter,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
