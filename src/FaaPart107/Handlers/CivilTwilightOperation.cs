using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>civil-twilight-operation</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// Four. Two are the facts § 107.29(c) makes its definition turn on and § 107.29(b) states its
    /// prohibition about — where the operation is, which selects which of (c)'s three items defines
    /// civil twilight, and which of (c)(1)-(2)'s periods the operation is during. Neither is a
    /// clock reading: when official sunrise and official sunset occur at a place on a date is not
    /// in this corpus, so the engine computes neither and takes the caller's statement of the
    /// period instead. The third is what the caller states about the aircraft's lighting, which is
    /// <c>anti-collision-lighting</c>'s own input, handed on unchanged. The fourth is the waiver
    /// statement § 107.205(b) makes necessary — one statement, about § 107.29(a)(2) and (b), which
    /// is the row that reaches this entry and the entries on the lighting alike.
    /// </para>
    /// <para>
    /// There is no figure of the lighting here and no date or time. The distance, the flash rate
    /// and the determination about reducing the intensity are the lighting entries', and the two
    /// 30-minute periods are <c>civil-twilight-window</c>'s value: this entry restates none of
    /// them.
    /// </para>
    /// <para>
    /// Every declared input is demanded. An operation the caller has not described is not an
    /// operation outside Alaska, and not one outside civil twilight either (rules-factory decision
    /// 0021, and <c>docs/decisions/0001</c>).
    /// </para>
    /// </remarks>
    public sealed partial class CivilTwilightOperationRequest
    {
        /// <summary>
        /// Where the operation is, as the caller states it: what § 107.29(c) selects the definition
        /// of civil twilight by. Required, and never inferred in either direction.
        /// </summary>
        public OperationPlace? Place { get; init; }

        /// <summary>
        /// Which of the periods § 107.29(c)(1)-(2) state the operation is during, as the caller
        /// states it, or neither of them. Required, and never inferred.
        /// </summary>
        public OperationPeriod? Period { get; init; }

        /// <summary>
        /// What the caller states about the small unmanned aircraft's anti-collision lighting:
        /// <c>anti-collision-lighting</c>'s input, for § 107.29(b)'s unless-clause. Required.
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
        /// <c>civil-twilight-operation</c>: <see cref="Twilight.Operation"/> — this entry's own
        /// § 107.205(b) waiver gate, then § 107.29(c)'s definition from the entry the stated place
        /// selects, then § 107.29(b)'s requirement as <c>anti-collision-lighting</c> answers it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The handler decides nothing. It demands the four inputs the rule needs, never defaulting
        /// any of them, and hands the caller's assertions through unchanged so that the entries
        /// this one reaches answer on their own terms and cite their own locators. The map gives
        /// this entry no <c>assertedBy</c>, so nothing asserted under its own id is an input here.
        /// </para>
        /// <para>
        /// <b>All four are demanded here, before the rule's waiver gate is read</b>, because they
        /// are the arguments of the call: a request that states a waiver in force and leaves one of
        /// them unset is refused naming that input rather than declined
        /// <see cref="UnresolvedReason.OutsideCurrentScope"/>. What the gate precedes is the
        /// assertion — no constituent is asked for a waived operation, so nothing is demanded
        /// through <see cref="RuleRequest"/>. This is <c>operating-limitations</c>',
        /// <c>visual-observer-conditions</c>' and <c>anti-collision-lighting</c>' ordering;
        /// <c>reasonable-protection</c> takes the other one, passing its input in unresolved so its
        /// rule can demand it after the gate. Which is right is not recorded anywhere in this
        /// repository and is not this entry's to settle (<c>AGENTS.md</c> §6).
        /// </para>
        /// </remarks>
        internal static partial Resolution<object> CivilTwilightOperation(Requests.CivilTwilightOperationRequest request) =>
            Answer(Twilight.Operation(
                Demand(request.Place, request.EntryId, nameof(request.Place)),
                Demand(request.Period, request.EntryId, nameof(request.Period)),
                Demand(request.Lighting, request.EntryId, nameof(request.Lighting)),
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
