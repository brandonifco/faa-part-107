using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>participant-briefing</c>: <see cref="Assertions.Stated"/>, the remote pilot in
        /// command's assertion, answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.49(b) tells the remote pilot in command to "Ensure that all persons directly
        /// participating in the small unmanned aircraft operation are informed about the operating
        /// conditions, emergency procedures, contingency procedures, roles and responsibilities,
        /// and potential hazards". The map records the whole of that paragraph as one entry,
        /// <c>kind: assertion</c>, asserted by the remote pilot in command. The five matters are
        /// what "informed about" is measured against — the entry's <c>note</c> calls them "the set
        /// stated in the same constituent … the delegated choice among values the corpus fixes" —
        /// and not five obligations with five verdicts: the map gives none of the five an entry of
        /// its own, so one asserted fact answers the whole enumeration.
        /// </para>
        /// <para>
        /// The fact is determined outside this engine and reported to it. The entry therefore
        /// carries no inputs — no roster of participants, no briefing record, no checklist over the
        /// five matters — and its request keeps the generated shape alone. An engine that took any
        /// of those and decided from them whether the persons had been informed would be doing the
        /// briefing's accounting, which § 107.49(b) gives to the remote pilot in command.
        /// </para>
        /// <para>
        /// <b>The audience term is open, and this entry still answers on its own row.</b> Who "all
        /// persons directly participating" are is the same undefined term as § 107.39(a),
        /// <see cref="MapEntries.DirectParticipation"/>, which the map records as a gap and this
        /// engine declines <see cref="UnresolvedReason.RequiresInterpretation"/> over
        /// (<see cref="Participation.Direct"/>); this entry names it in <c>dependsOn</c> for that
        /// reason. It does not borrow that decline. <c>dependsOn</c> orders implementation and is
        /// not a runtime precondition, and the correspondence table changes an entry's answer for a
        /// dependency in one place only — row 5, an <c>operation</c> whose <c>value</c> dependency
        /// is unimplemented — which reaches neither an assertion entry nor a dependency that is
        /// implemented. This entry's own first row is row 8,
        /// <see cref="CorrespondenceRow.Assertion"/>, and the decline one edge away is not
        /// inherited: the same reading <see cref="NightTraining"/> records for
        /// <see cref="MapEntries.KnowledgeRecency"/>. What the remote pilot in command asserts is
        /// that the directly participating persons were informed — whoever, on the reading nobody
        /// has made, those persons are. A caller who needs that term settled resolves
        /// <c>direct-participation</c> and is told, citing § 107.39(a), that it is open.
        /// </para>
        /// <para>
        /// The caller asserts the fact through <c>ParticipantBriefingRequest.Asserting</c> or
        /// <see cref="RuleRequest.Assert"/>, and asserting nothing throws
        /// <see cref="AssertionRequiredException"/> rather than declining: the corpus left the
        /// engine nothing to interpret <em>about this entry's own term</em>, so there is nothing
        /// here to be unresolved about. What the caller does not get to supply is the citation: the
        /// answer is built on <see cref="MapEntries.ParticipantBriefing"/>, so it cites
        /// § 107.49(b) whatever entry the caller's own <see cref="Assertion"/> was carrying.
        /// </para>
        /// <para>
        /// § 107.205 lists the regulations a certificate of waiver may authorize a deviation from
        /// and does not list § 107.49, so this entry has no <c>suspendedBy</c> in the map and no
        /// waiver gate runs before the demand.
        /// </para>
        /// </remarks>
        static partial void ParticipantBriefing(Requests.ParticipantBriefingRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Assertions.Stated(MapEntries.ParticipantBriefing, request.Assertions));
    }
}
