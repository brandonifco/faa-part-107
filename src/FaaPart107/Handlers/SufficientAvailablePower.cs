using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>sufficient-available-power</c>: <see cref="Assertions.Stated"/>, the remote pilot in
        /// command's assertion, answered unchanged on the map's own entry.
        /// </summary>
        /// <remarks>
        /// § 107.49(d) tells the remote pilot in command to "ensure that there is enough available
        /// power for the small unmanned aircraft system to operate for the intended operational
        /// time". The map records that as <c>kind: assertion</c>, asserted by the remote pilot in
        /// command: the fact is determined outside this engine and reported to it. So the entry
        /// carries no inputs of its own — no battery state, no endurance, no intended operational
        /// time — and its request keeps the generated shape alone. The caller asserts the fact
        /// through <c>SufficientAvailablePowerRequest.Asserting</c> or
        /// <see cref="RuleRequest.Assert"/>, and asserting nothing throws
        /// <see cref="AssertionRequiredException"/> rather than declining: the corpus left the
        /// engine nothing to interpret, so there is nothing here to be unresolved about. What the
        /// caller does not get to supply is the citation: the answer is built on
        /// <see cref="MapEntries.SufficientAvailablePower"/>, so it cites § 107.49(d) whatever
        /// entry the caller's own <see cref="Assertion"/> was carrying.
        /// </remarks>
        static partial void SufficientAvailablePower(Requests.SufficientAvailablePowerRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(Assertions.Stated(MapEntries.SufficientAvailablePower, request.Assertions));
    }
}
