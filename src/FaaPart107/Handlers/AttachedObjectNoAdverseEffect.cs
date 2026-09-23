using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>attached-object-no-adverse-effect</c>: <see cref="Assertions.Stated"/>, the remote
        /// pilot in command's assertion, answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.49(e)'s evidence, as the map quotes it, is "does not adversely affect the flight
        /// characteristics or controllability of the aircraft". The map records that as
        /// <c>kind: assertion</c> with <c>clarity: clear</c>, asserted by the remote pilot in
        /// command — § 107.49's lead-in is "Prior to flight, the remote pilot in command must:".
        /// The fact is determined outside this engine and reported to it, so the entry carries no
        /// inputs of its own: no mass, no centre of gravity, no aerodynamic effect and no model of
        /// handling. Anything computed here would be this engine answering a question the corpus
        /// hands to a person.
        /// </para>
        /// <para>
        /// The caller asserts the fact through
        /// <c>AttachedObjectNoAdverseEffectRequest.Asserting</c> or <see cref="RuleRequest.Assert"/>,
        /// and asserting nothing throws <see cref="AssertionRequiredException"/> rather than
        /// declining: the corpus left the engine nothing to interpret, so there is nothing here to
        /// be unresolved about. An assertion that the object <em>does</em> adversely affect the
        /// aircraft is an answer like any other and is returned as given, not converted into a
        /// decline.
        /// </para>
        /// <para>
        /// One sentence of § 107.49(e) carries two map entries, and this handler answers exactly
        /// one of them. <see cref="MapEntries.AttachedObjectSecure"/> — the "is secure" conjunct —
        /// has the identical locator, so the citation on an answer cannot tell the two apart: what
        /// tells them apart is the entry id, and the entry is the map's. Resolving on
        /// <see cref="MapEntries.AttachedObjectNoAdverseEffect"/> is what makes the answer this
        /// constituent's, refuses an assertion made about the other one, and stops a caller's own
        /// <see cref="Assertion"/> from choosing the name and the paragraph the answer is made
        /// under.
        /// </para>
        /// </remarks>
        static partial void AttachedObjectNoAdverseEffect(Requests.AttachedObjectNoAdverseEffectRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Assertions.Stated(MapEntries.AttachedObjectNoAdverseEffect, request.Assertions));
    }
}
