using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>preflight-actions</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// One, and it is not a verdict. A caller does not tell this entry whether § 107.49 was complied
    /// with, whether the operating environment was assessed, whether the control links are working,
    /// or whether subpart D's requirements are met.
    /// <see cref="PreflightActionsRequest.Operation"/> is § 107.49(f)'s own condition — <b>which
    /// operation this is</b> — and nothing else.
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
    /// The declared input is demanded. An operation the caller has not described is not an operation
    /// outside subpart D (rules-factory decision 0021, and <c>docs/decisions/0001</c>).
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
        /// The assertions dictionary is handed on untouched, because the values it must carry are
        /// the four constituent assertion entries' and this entry asserts nothing of its own: the
        /// map gives <c>preflight-actions</c> no <c>assertedBy</c>, so a value asserted under this
        /// entry's own id is not an input to anything here.
        /// </remarks>
        internal static partial Resolution<object> PreflightActions(Requests.PreflightActionsRequest request) =>
            Answer(Preflight.Actions(
                Demand(request.Operation, request.EntryId, nameof(request.Operation)),
                request.Assertions));
    }
}
