using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>sufficient-available-power</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// One, and it is neither the fact nor a verdict. § 107.49(d) states its obligation under a
    /// condition — "If the small unmanned aircraft is powered" — which is in this entry's own
    /// evidence, so the caller states it here (<see cref="AircraftPower"/>, and <c>#95</c>).
    /// </para>
    /// <para>
    /// There is nothing here about the power itself: no battery state, no endurance, no intended
    /// operational time. That fact is <c>kind: assertion</c>, asserted by the remote pilot in
    /// command, so it arrives the way an assertion entry's value always arrives — through the
    /// assertions dictionary, under this entry's own id (<see cref="RuleRequest.Assert"/>) — and an
    /// input of that kind would be surface suggesting the engine weighed it.
    /// </para>
    /// <para>
    /// The declared input is demanded. An aircraft the caller has not described is not an unpowered
    /// aircraft (rules-factory decision 0021, and <c>docs/decisions/0001</c>).
    /// </para>
    /// </remarks>
    public sealed partial class SufficientAvailablePowerRequest
    {
        /// <summary>
        /// Whether the small unmanned aircraft is powered, as the caller states it: § 107.49(d)'s
        /// own condition. Required, and never inferred in either direction.
        /// </summary>
        public AircraftPower? Power { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>sufficient-available-power</c>: <see cref="AvailablePower.Enough"/> — § 107.49(d)'s
        /// own condition, then the remote pilot in command's assertion answered unchanged on the
        /// map's own entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// § 107.49(d) tells the remote pilot in command, "If the small unmanned aircraft is
        /// powered", to "ensure that there is enough available power for the small unmanned aircraft
        /// system to operate for the intended operational time". The map records the obligation as
        /// <c>kind: assertion</c>, asserted by the remote pilot in command: the fact is determined
        /// outside this engine and reported to it. So the caller asserts it through
        /// <c>SufficientAvailablePowerRequest.Asserting</c> or <see cref="RuleRequest.Assert"/>, and
        /// asserting nothing throws <see cref="AssertionRequiredException"/> rather than declining:
        /// the corpus left the engine nothing to interpret, so there is nothing here to be
        /// unresolved about. What the caller does not get to supply is the citation: the answer is
        /// built on <see cref="MapEntries.SufficientAvailablePower"/>, so it cites § 107.49(d)
        /// whatever entry the caller's own <see cref="Assertion"/> was carrying.
        /// </para>
        /// <para>
        /// The one input it carries is the paragraph's condition, and it is demanded, never
        /// defaulted, and read before the assertion is. Where the caller states the aircraft is not
        /// powered, § 107.49(d) states no obligation about the operation and no assertion is asked
        /// for. Correspondence row 8 has a default, and that default reads no condition and checks
        /// nothing about who asserted what — it hands back the caller's own object, with the
        /// caller's own citation on it. This handler is what keeps the entry from answering that
        /// way.
        /// </para>
        /// </remarks>
        static partial void SufficientAvailablePower(Requests.SufficientAvailablePowerRequest request, ref Resolution<object>? resolution) =>
            resolution = Answer(AvailablePower.Enough(
                Demand(request.Power, request.EntryId, nameof(request.Power)),
                request.Assertions));
    }
}
