using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>anti-collision-lighting</c>'s rule reads.</summary>
    /// <remarks>
    /// Two, and no figure of the lighting beyond the distance it is visible for. What the caller
    /// states about the lighting is one <see cref="LightingStatement"/> — fitted or not, lighted or
    /// extinguished, its intensity reduced or not, and how far it is visible — and the other is the
    /// waiver statement § 107.205(b) makes necessary. The clause's two questions the corpus gives
    /// away arrive as assertions instead, through <see cref="RuleRequest.Assert"/>: the flash rate
    /// being sufficient (<c>flash-rate-sufficient</c>) and the remote pilot in command's
    /// determination about reducing the intensity
    /// (<c>intensity-reduction-in-interest-of-safety</c>). No flash rate, no intensity and no period
    /// of the day is declared here: the corpus states neither figure, and which period an operation
    /// is in belongs to the entries that turn on it, <c>night-operation</c> and
    /// <c>civil-twilight-operation</c>.
    /// </remarks>
    public sealed partial class AntiCollisionLightingRequest
    {
        /// <summary>
        /// What the caller states about the small unmanned aircraft's anti-collision lighting.
        /// Required: the engine does not assume an aircraft is lit, and does not assume it is dark.
        /// </summary>
        public LightingStatement? Lighting { get; init; }

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
        /// <c>anti-collision-lighting</c>: <see cref="Lights.AsRequired"/> — this entry's own waiver
        /// gate, then the requirement § 107.29(a)(2) and (b) state, read off what the caller stated
        /// about the lighting and what the clause's two assertion entries answered.
        /// </summary>
        /// <remarks>
        /// The handler decides nothing. Neither of the two inputs the rule needs is ever defaulted,
        /// and the caller's assertions are handed through unchanged so that the two entries this
        /// one depends on answer on their own terms and cite their own locators — which are this
        /// entry's locator too, since all three of § 107.29(a)(2) and (b)'s entries carry it.
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it;
        /// the lighting statement is handed over as the caller left it, so the rule demands it
        /// after the gate (<c>docs/decisions/0007</c>).
        /// </remarks>
        internal static partial Resolution<object> AntiCollisionLighting(Requests.AntiCollisionLightingRequest request) =>
            Answer(Lights.AsRequired(
                request.Lighting,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
