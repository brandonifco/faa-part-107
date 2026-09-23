using RulesKernel.Resolution;

namespace FaaPart107.Evaluation;

/// <summary>
/// What this engine was able to say about one map entry, for one operation. Twelve states, and no
/// two of them mean the same thing.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration exists so that a caller can always tell <b>"we did not say"</b> from <b>"we
/// said no"</b> from <b>"the engine cannot determine this"</b> from <b>"the engine has not built
/// this"</b> from <b>"this is outside the engine's scope"</b>. Collapsing any pair of those is the
/// defect this API exists to prevent, so there is no state here that stands for two of them, and
/// nothing in <see cref="OperationEvaluation"/> reduces a set of them to one answer.
/// </para>
/// <para>
/// No member is zero, so <c>default</c> is not a state — the same guard
/// <see cref="AreaDesignation"/> uses, and for the same reason: an uninitialised value must not
/// read as a verdict.
/// </para>
/// <para>
/// The states divide into three groups, which is what makes progressive disclosure possible
/// without collapsing anything: what the engine answered (<see cref="Satisfied"/>,
/// <see cref="Violated"/>, <see cref="Informational"/>, <see cref="HumanAssertionRecorded"/>),
/// what the caller still owes (<see cref="ActionRequired"/>,
/// <see cref="HumanAssertionRequired"/>, <see cref="FactRequired"/> — see
/// <see cref="OperationEvaluation.Outstanding"/>), and what this engine cannot answer at all
/// (<see cref="RequiresInterpretation"/>, <see cref="OutsideCurrentScope"/>,
/// <see cref="NotBuilt"/>, <see cref="MissingRulesData"/>, <see cref="UnresolvedInteraction"/> —
/// see <see cref="OperationEvaluation.Unanswered"/>). Each member of either group keeps its own
/// state, so the grouping is a filter and never a summary.
/// </para>
/// </remarks>
public enum RequirementState
{
    /// <summary>
    /// The entry's rule was evaluated on the facts supplied and the rule's own verdict property
    /// says it is met. It is this entry's paragraph and nothing wider: another rule of part 107,
    /// and the rest of the CFR, may say otherwise.
    /// </summary>
    Satisfied = 1,

    /// <summary>
    /// The entry's rule was evaluated on the facts supplied and the rule's own verdict property
    /// says it is not met.
    /// </summary>
    Violated = 2,

    /// <summary>
    /// The entry's rule names something obtainable that the caller has not stated, and says in a
    /// verdict of its own that the requirement is outstanding rather than broken.
    /// </summary>
    /// <remarks>
    /// <b>No rule this engine has built reports this today, and that is deliberate.</b> Every rule
    /// here that names an obtainable thing — prior ATC authorization under § 107.41, permission
    /// from a using or controlling agency under § 107.45 — also demands the caller's statement
    /// about it, so the caller has either not spoken (<see cref="FactRequired"/>) or has stated a
    /// non-possession the rule then reads as the section prohibiting the operation
    /// (<see cref="Violated"/>). Reporting that middle case here instead would read more favourably
    /// than the rule's own verdict property, which is not this orchestrator's to do;
    /// <c>docs/decisions/0005-the-rules-verdict-is-the-verdict.md</c> is that decision, and says
    /// what a rule would have to state for this state to be produced. It is kept in this
    /// enumeration rather than removed because issue #51 requires the state to be distinct from
    /// <see cref="Violated"/> and from <see cref="FactRequired"/>, and because a rule that draws
    /// the distinction should land in a state of its own rather than change what
    /// <see cref="Violated"/> means. The obtainability a caller wants to render is on the finding —
    /// <see cref="AirspaceFinding.AuthorizationRequired"/>,
    /// <see cref="AreaPermissionFinding.PermissionRequired"/> — which travels on
    /// <see cref="EvaluatedRequirement.Finding"/>.
    /// </remarks>
    ActionRequired = 3,

    /// <summary>
    /// The entry is <c>kind: assertion</c> and the caller asserted nothing for it
    /// (<see cref="AssertionRequiredException"/>). This is <b>not</b> an unresolved result: the
    /// corpus gave the engine the means to proceed and the caller owes the value.
    /// <see cref="EvaluatedRequirement.AssertionOwed"/> names which assertion,
    /// <see cref="EvaluatedRequirement.AssertionCites"/> where it is stated, and
    /// <see cref="EvaluatedRequirement.AssertedBy"/> who the corpus lets make it — all three of the
    /// demanded entry, which is not the evaluated one when a composite asks for a constituent's
    /// assertion.
    /// </summary>
    HumanAssertionRequired = 4,

    /// <summary>
    /// The entry is <c>kind: assertion</c>, the caller supplied the value, and the engine answered
    /// with it unchanged: the fact and who is answerable for it are on
    /// <see cref="EvaluatedRequirement.Finding"/>.
    /// </summary>
    /// <remarks>
    /// It is deliberately neither <see cref="Satisfied"/> nor <see cref="Violated"/>. Whether the
    /// asserted proposition <em>holding</em> is compliance differs entry by entry — § 107.49(d)'s
    /// proposition holding is the required state, § 107.37(b)'s holding is the prohibited one — and
    /// neither the map nor any rule of this engine records which. Scoring these would be the
    /// orchestrator reading the corpus, which <c>AGENTS.md</c> §5 forbids it. See
    /// <c>docs/decisions/0004-an-assertion-is-recorded-and-not-scored.md</c>.
    /// </remarks>
    HumanAssertionRecorded = 5,

    /// <summary>
    /// The entry refused the request: an input it demands was not supplied, or the value supplied
    /// is not one it accepts. <see cref="EvaluatedRequirement.MissingInput"/> names the input and
    /// <see cref="EvaluatedRequirement.Explanation"/> is the entry's own words. A missing input is
    /// the caller's error and not a gap in the corpus, so it is never an unresolved result — and
    /// never an answer the engine invented from a default.
    /// </summary>
    FactRequired = 6,

    /// <summary>
    /// The entry resolved to something that states what the rule says rather than whether an
    /// operation complies with it: a figure the corpus prints
    /// (<see cref="GroundspeedLimit"/>, <see cref="AltitudeLimit"/>,
    /// <see cref="VisibilityMinimum"/>, <see cref="CloudClearance"/>,
    /// <see cref="CivilTwilightWindows"/>), or a finding whose own documentation disclaims a
    /// compliance verdict (<see cref="NightWaiverTerminationFinding"/>), or a finding that says the
    /// paragraph does not reach this operation at all and so states no requirement to meet — the
    /// third answer a <c>bool?</c> verdict carries, where the rule's own documentation is explicit
    /// that the null is neither of the other two and is never "undetermined":
    /// <see cref="VisualObserverConditionsFinding.AllRequirementsMet"/> where no visual observer is
    /// used, and <see cref="CivilTwilightOperationFinding.Permitted"/> where the operation is during
    /// neither period of civil twilight. It must not be read as <see cref="Satisfied"/>.
    /// </summary>
    Informational = 7,

    /// <summary>
    /// The map records the question as unresolved and the rule declined
    /// <see cref="UnresolvedReason.RequiresInterpretation"/>: the corpus does not settle it, and a
    /// recorded decision is needed before this can resolve. The engine can determine this no
    /// further, and the absence of a verdict is not a verdict.
    /// </summary>
    RequiresInterpretation = 8,

    /// <summary>
    /// The entry is outside what this engine covers and declined
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/>: the map puts it out of scope, or the
    /// caller stated a certificate of waiver authorizing deviation from the regulation that states
    /// it, which suspends the entry (§ 107.205, and rules-factory decision 0021).
    /// </summary>
    OutsideCurrentScope = 9,

    /// <summary>
    /// The map has the entry and this engine has not implemented it; the rule declined
    /// <see cref="UnresolvedReason.UnsupportedRule"/>. Nothing whatever follows about the
    /// operation: this is the engine naming a hole in itself, and it is not
    /// <see cref="Satisfied"/> and not <see cref="Violated"/>.
    /// </summary>
    NotBuilt = 10,

    /// <summary>
    /// The rule exists and the structured data it needs is absent; the rule declined
    /// <see cref="UnresolvedReason.MissingRulesData"/>. In this map that is
    /// <c>definedElsewhere</c>, <c>beyondAdapter</c>, or an operation whose value dependency is
    /// unimplemented.
    /// </summary>
    MissingRulesData = 11,

    /// <summary>
    /// Both rules exist and their combination is not resolved; the rule declined
    /// <see cref="UnresolvedReason.UnsupportedInteraction"/>. No rule of this engine produces this
    /// today; the state exists so that the reason has somewhere of its own to land rather than
    /// being folded into one of the others.
    /// </summary>
    UnresolvedInteraction = 12,
}

/// <summary>The mechanical part of the classification: one kernel decline reason, one state.</summary>
/// <remarks>
/// There is no regulatory content here, and there is no judgement. The correspondence table in
/// rules-factory's <c>docs/corpus-map.md</c> fixes which reason an entry's row declines with, and
/// <see cref="Registry"/> applies it; this maps that reason onto the product-facing state and
/// nothing else. It is public so that a caller who resolved an entry directly through
/// <see cref="EntryPoints"/> classifies its decline exactly as <see cref="OperationEvaluator"/>
/// does, rather than writing the table a second time.
/// </remarks>
public static class RequirementStates
{
    /// <summary>The state <paramref name="reason"/> is reported as.</summary>
    /// <param name="reason">A kernel decline reason.</param>
    /// <returns>Its state. The mapping is total over <see cref="UnresolvedReason"/> and injective: no two reasons share a state.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a member of the enumeration.</exception>
    public static RequirementState For(UnresolvedReason reason) => reason switch
    {
        UnresolvedReason.UnsupportedRule => RequirementState.NotBuilt,
        UnresolvedReason.RequiresInterpretation => RequirementState.RequiresInterpretation,
        UnresolvedReason.OutsideCurrentScope => RequirementState.OutsideCurrentScope,
        UnresolvedReason.MissingRulesData => RequirementState.MissingRulesData,
        UnresolvedReason.UnsupportedInteraction => RequirementState.UnresolvedInteraction,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "the kernel has no such decline reason"),
    };
}
