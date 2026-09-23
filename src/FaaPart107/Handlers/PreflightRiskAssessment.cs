using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>preflight-risk-assessment</c>: <see cref="Assertions.Stated"/>, the remote pilot in
        /// command's assertion, answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.49(a) tells the remote pilot in command to "Assess the operating environment,
        /// considering risks to persons and property in the immediate vicinity both on the surface
        /// and in the air", and then fixes what that assessment "must include": "(1) Local weather
        /// conditions; (2) Local airspace and any flight restrictions; (3) The location of persons
        /// and property on the surface; and (4) Other ground hazards." The map records the whole of
        /// that paragraph as one entry, <c>kind: assertion</c>, asserted by the remote pilot in
        /// command — the closed four-item list is what the assessment is measured against, not four
        /// obligations with four verdicts, and the map gives none of the four an entry of its own.
        /// So one asserted fact answers the whole enumeration.
        /// </para>
        /// <para>
        /// The fact is determined outside this engine and reported to it. The entry therefore
        /// carries no inputs — no weather, no airspace or flight restrictions, no location of
        /// persons and property, no ground hazards, and no checklist over them — and its request
        /// keeps the generated shape alone. An engine that took any of those and decided from them
        /// whether the operating environment had been assessed would be doing the assessing, which
        /// § 107.49(a) gives to the remote pilot in command.
        /// </para>
        /// <para>
        /// The caller asserts the fact through <c>PreflightRiskAssessmentRequest.Asserting</c> or
        /// <see cref="RuleRequest.Assert"/>, and asserting nothing throws
        /// <see cref="AssertionRequiredException"/> rather than declining: the corpus left the
        /// engine nothing to interpret, so there is nothing here to be unresolved about. What the
        /// caller does not get to supply is the citation: the answer is built on
        /// <see cref="MapEntries.PreflightRiskAssessment"/>, so it cites § 107.49(a) whatever entry
        /// the caller's own <see cref="Assertion"/> was carrying.
        /// </para>
        /// <para>
        /// § 107.205 lists the regulations a certificate of waiver may authorize a deviation from
        /// and does not list § 107.49, so this entry has no <c>suspendedBy</c> in the map and no
        /// waiver gate runs before the demand.
        /// </para>
        /// </remarks>
        static partial void PreflightRiskAssessment(Requests.PreflightRiskAssessmentRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Assertions.Stated(MapEntries.PreflightRiskAssessment, request.Assertions));
    }
}
