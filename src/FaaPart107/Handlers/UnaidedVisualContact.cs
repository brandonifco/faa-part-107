using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>unaided-visual-contact</c>'s rule reads.</summary>
    /// <remarks>
    /// The fact itself is not among them. § 107.31(a) is <c>kind: assertion</c>, so the value arrives
    /// through the assertions dictionary — <see cref="UnaidedVisualContactRequest.Asserting"/> or
    /// <see cref="RuleRequest.Assert"/> — and never as an input the engine could default. What is an
    /// input is the other caller fact the entry needs: whether a waiver of § 107.31 is in force.
    /// </remarks>
    public sealed partial class UnaidedVisualContactRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.31 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>unaided-visual-contact</c>: <see cref="UnaidedVision.SeenThroughoutTheFlight"/> — the
        /// waiver gate, then the assertion answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.31(a) asks whether "the remote pilot in command, the visual observer (if one is
        /// used), and the person manipulating the flight control" can, with vision unaided by any
        /// device other than corrective lenses, "see the unmanned aircraft throughout the entire
        /// flight" for the four purposes it goes on to state. The map records that as
        /// <c>kind: assertion</c>, asserted by any of those three; the fact is determined outside this
        /// engine and reported to it. So the entry carries no inputs of its own about seeing — no
        /// distance, no visibility, no aircraft size — and the corpus states no figure for one to be
        /// compared against.
        /// </para>
        /// <para>
        /// The one input it does carry is the waiver statement, because § 107.205(c) lists § 107.31
        /// and the entry's <c>suspendedBy</c> names the gate. It is demanded, never defaulted, and it
        /// is read before the assertion is: a waiver in force makes the entry unreachable, and
        /// demanding a fact about an unreachable entry would be asking the caller for something the
        /// waiver has already made irrelevant. Correspondence row 8 has a default, and that default
        /// neither consults the gate nor checks who asserted what — it hands back the caller's own
        /// object, with the caller's own citation on it. This handler is what keeps the entry from
        /// answering that way.
        /// </para>
        /// </remarks>
        static partial void UnaidedVisualContact(Requests.UnaidedVisualContactRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(UnaidedVision.SeenThroughoutTheFlight(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
