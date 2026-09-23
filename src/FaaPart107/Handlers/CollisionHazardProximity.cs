using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>collision-hazard-proximity</c>: <see cref="Assertions.Stated"/>, the caller's
        /// assertion, answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.37(b) is one sentence — "No person may operate a small unmanned aircraft so close
        /// to another aircraft as to create a collision hazard" — and it states a standard, not a
        /// distance. The map records it as <c>kind: assertion</c>, and its note is explicit that an
        /// engine which resolved it to a number "would substitute its own rule for the corpus's".
        /// So the entry carries no inputs of its own: no separation, no closing rate, no
        /// collision-hazard threshold. Surface of that kind would suggest this engine has a measure
        /// to apply, and it has none to apply.
        /// </para>
        /// <para>
        /// Who asserts it is the caller's to say. § 107.37(b) names the subject of a prohibition,
        /// "No person", and nobody whose determination settles the hazard, so the map's
        /// <c>assertedBy</c> is the marker <c>caller</c> (rules-factory decision 0025, and
        /// <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>). The attribution is
        /// therefore recorded rather than checked against a list of people, and it is recorded:
        /// <see cref="Assertions.Stated"/> carries <see cref="Assertion.AssertedBy"/> through
        /// unchanged onto the answer.
        /// </para>
        /// <para>
        /// The neighbouring constituent, <c>well-clear</c> (§ 107.37(a)), is <c>kind: operation</c>
        /// and declines <see cref="UnresolvedReason.RequiresInterpretation"/>. This entry does not
        /// borrow that answer. They are different constituents of the same section, mapped
        /// differently, and row 8 leaves nothing here to be unresolved about: asserting nothing
        /// throws <see cref="AssertionRequiredException"/>, which is a demand on the caller and not
        /// a gap in the corpus.
        /// </para>
        /// </remarks>
        static partial void CollisionHazardProximity(Requests.CollisionHazardProximityRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Assertions.Stated(MapEntries.CollisionHazardProximity, request.Assertions));
    }
}
