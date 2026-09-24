using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>preflight-actions</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// Two, and neither is a verdict. A caller does not tell this entry whether § 107.49 was complied
    /// with, whether the operating environment was assessed, whether the control links are working,
    /// whether there is enough available power, or whether subpart D's requirements are met. Both are
    /// conditions a paragraph of § 107.49 states its obligation under — <b>which operation this is</b>
    /// and <b>which aircraft this is</b> — and nothing else.
    /// </para>
    /// <para>
    /// <b>The two conditions are not owned the same way, and the request is where that is easiest to
    /// miss.</b> <see cref="PreflightActionsRequest.Operation"/> is § 107.49(f)'s, which is in this
    /// entry's evidence and in no constituent's, so this entry reads it.
    /// <see cref="PreflightActionsRequest.Power"/> is § 107.49(d)'s, which is in this entry's evidence
    /// <em>and</em> in <c>sufficient-available-power</c>'s, so that entry reads it (<c>#95</c>) and
    /// <see cref="Preflight"/> hands it straight to <see cref="AvailablePower.Enough"/> without
    /// looking at it (<c>#99</c>). A caller has to state it here because this is the entry being
    /// asked; that is all its presence on this request means. It is the shape
    /// <c>VisualObserverConditionsRequest.VisualLineOfSightWaiver</c> already has — a constituent's
    /// caller fact, carried by the composite for the constituent to read.
    /// </para>
    /// <para>
    /// The four obligations § 107.49 leaves to the remote pilot in command to report are
    /// <c>kind: assertion</c> entries, so they arrive the way an assertion entry's value always
    /// arrives — through the assertions dictionary, under <em>that</em> entry's id
    /// (<see cref="RuleRequest.Assert"/>) — and never as an input this entry could default or
    /// restate. The other three obligations take no input at all: § 107.49(c) and the first conjunct
    /// of § 107.49(e) are questions the map holds open, so an input describing a control link or a
    /// fastening would be surface suggesting the engine weighed it, and § 107.49(f)'s requirements
    /// are <c>subpart-d-categories</c>', which the map puts <c>scope: out</c>.
    /// </para>
    /// <para>
    /// There is nothing here about a waiver: § 107.205 does not list § 107.49, so the map gives this
    /// entry no <c>suspendedBy</c> and there is no statement to demand. And there is nothing here
    /// for § 107.49's lead-in: it names one person, whom the map has already read into each
    /// constituent's <c>assertedBy</c>, and one time, which is when the obligations it conjoins are
    /// owed rather than a second fact about them.
    /// </para>
    /// <para>
    /// Both declared inputs are demanded. An operation the caller has not described is not an
    /// operation outside subpart D, and an aircraft the caller has not described is not an unpowered
    /// aircraft (rules-factory decision 0021, and <c>docs/decisions/0001</c>).
    /// </para>
    /// </remarks>
    public sealed partial class PreflightActionsRequest
    {
        /// <summary>
        /// Whether the operation will be conducted over human beings under subpart D of this part,
        /// as the caller states it: § 107.49(f)'s condition. Required, and never inferred in either
        /// direction.
        /// </summary>
        public SubpartDOperation? Operation { get; init; }

        /// <summary>
        /// Whether the small unmanned aircraft is powered, as the caller states it: § 107.49(d)'s
        /// condition, which is <c>sufficient-available-power</c>'s to read and is carried here only
        /// so that it can be handed to that entry's rule. Required, and never inferred in either
        /// direction.
        /// </summary>
        public AircraftPower? Power { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>preflight-actions</c>: <see cref="Preflight.Actions"/> — each of § 107.49's
        /// obligations as the entry that states it answers it, conjoined in the section's own
        /// paragraph order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The assertions dictionary is handed on untouched, because the values it must carry are
        /// the four constituent assertion entries' and this entry asserts nothing of its own: the
        /// map gives <c>preflight-actions</c> no <c>assertedBy</c>, so a value asserted under this
        /// entry's own id is not an input to anything here.
        /// </para>
        /// <para>
        /// This entry's own fact is demanded before the one it carries for a constituent —
        /// § 107.49(f)'s condition, which this entry reads, then § 107.49(d)'s, which
        /// <c>sufficient-available-power</c> reads — the order
        /// <c>Handlers.VisualObserverConditions</c> already puts § 107.33's own inputs and
        /// <c>visual-line-of-sight</c>'s waiver statement in. Both are demanded, so the order decides
        /// only which absence a caller who stated neither is told about first.
        /// </para>
        /// </remarks>
        internal static partial Resolution<object> PreflightActions(Requests.PreflightActionsRequest request) =>
            Answer(Preflight.Actions(
                Demand(request.Operation, request.EntryId, nameof(request.Operation)),
                Demand(request.Power, request.EntryId, nameof(request.Power)),
                request.Assertions));
    }
}
