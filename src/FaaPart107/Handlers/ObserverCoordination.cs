using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>observer-coordination</c>'s rule reads.</summary>
    /// <remarks>
    /// The waiver statement, and nothing else. The fact § 107.33(c) states arrives through the
    /// assertions the request already carries (correspondence row 8), so there is no scan, no
    /// hazard and no aircraft position here: an input of that kind would be surface suggesting the
    /// engine judges whether the coordination happened, which is the one thing this entry says it
    /// does not. The statement is here because the map marks the entry <c>suspendedBy</c> the
    /// § 107.205 gate, and § 107.205(d) lists the whole of § 107.33.
    /// </remarks>
    public sealed partial class ObserverCoordinationRequest
    {
        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.33 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>observer-coordination</c>: <see cref="Coordination.Coordinate"/> — the waiver gate,
        /// then the assertion answered unchanged on the map's own entry and recorded beside the
        /// statement it was answered under.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.33(c) has the remote pilot in command, the person manipulating the flight controls
        /// of the small unmanned aircraft system and the visual observer coordinate to scan the
        /// airspace for any potential collision hazard and maintain awareness of the aircraft's
        /// position through direct visual observation. The map records that as
        /// <c>kind: assertion</c>, asserted by those three persons, so the caller asserts it through
        /// <c>ObserverCoordinationRequest</c>'s assertions and the engine answers it unchanged —
        /// including a "no", which is an answer and not a decline. What the caller does not get to
        /// supply is the citation: the answer is built on
        /// <see cref="MapEntries.ObserverCoordination"/>, so it cites § 107.33(c) whatever entry the
        /// caller's own <see cref="Assertion"/> was carrying.
        /// </para>
        /// <para>
        /// The one input it carries is the waiver statement, because § 107.205(d) lists § 107.33 and
        /// the entry's <c>suspendedBy</c> names the gate. It is demanded, never defaulted, read
        /// before the assertion is, and recorded on the answer either way — as text in
        /// <c>Attempted</c> on the decline, and as
        /// <see cref="ObserverCoordinationFinding.Waiver"/> on the resolved value (decision 0001).
        /// Correspondence row 8 has a default, and that default neither consults the gate nor checks
        /// who asserted what — it hands back the caller's own object, with the caller's own citation
        /// on it. This handler is what keeps the entry from answering that way.
        /// </para>
        /// </remarks>
        static partial void ObserverCoordination(Requests.ObserverCoordinationRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Coordination.Coordinate(
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
