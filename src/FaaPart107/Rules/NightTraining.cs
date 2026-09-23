using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.29(a)(1): <see cref="MapEntries.NightTrainingCompleted"/>, "(1) The remote pilot in
/// command of the small unmanned aircraft has completed an initial knowledge test or training, as
/// applicable, under § 107.65 after April 6, 2021; and".
/// </summary>
/// <remarks>
/// <para>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: whether
/// "initial" qualifies "training" as well as "knowledge test" is not settled, and "as applicable,
/// under § 107.65" points at a section that names an initial aeronautical knowledge test (a),
/// recurrent training (b) and training for part 61 certificate holders (c) without saying which
/// satisfies § 107.29(a)(1), while § 107.65(d) treats some completions before April 6, 2021 as
/// compliant with (b) or (c).
/// </para>
/// <para>
/// So the engine declines, whatever the caller supplies. Whether a particular remote pilot in
/// command has completed a knowledge test or a training is a fact about a person, and the entry's
/// note says the completion date against April 6, 2021 is computable <em>once the qualifying
/// completion is known</em> — which completion qualifies is exactly what the corpus does not say.
/// There is no side of the line left answerable here, unlike <see cref="Clouds"/>, whose two
/// figures are printed and whose open question is only how they combine: a rule that took a
/// completion date, a test record or a part 61 certificate and answered met or not met would be
/// this engine choosing among the readings the published map holds open.
/// </para>
/// <para>
/// The entry's one dependency, <see cref="MapEntries.KnowledgeRecency"/> (§ 107.65), is
/// <c>scope: out</c> and answers <see cref="UnresolvedReason.OutsideCurrentScope"/> on its own
/// terms; this entry does not borrow that answer. Its own correspondence row is row 6,
/// <c>ambiguity.fate: unresolved</c> — row 5, an operation whose <em>value</em> dependency is
/// unimplemented, does not reach it, because <c>knowledge-recency</c> is <c>kind: operation</c>
/// and not a value. So the reason is <see cref="UnresolvedReason.RequiresInterpretation"/> and the
/// locator cited is this entry's own, § 107.29(a)(1).
/// </para>
/// </remarks>
public static class NightTraining
{
    /// <summary>
    /// <see cref="MapEntries.NightTrainingCompleted"/>: the entry's decline, always.
    /// </summary>
    /// <returns>
    /// <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this entry's own locator,
    /// § 107.29(a)(1), and naming what the corpus leaves open about the qualifying completion.
    /// </returns>
    public static Resolution<object> Completed() =>
        Resolution<object>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.NightTrainingCompleted.Id}' is met: § 107.29(a)(1) does not "
            + "settle whether \"initial\" qualifies \"training\" as well as \"knowledge test\", and \"as applicable, "
            + "under § 107.65\" points at a section naming an initial aeronautical knowledge test, recurrent training "
            + "and training for part 61 certificate holders without saying which of them satisfies it, while "
            + "§ 107.65(d) treats some completions before April 6, 2021 as compliant with two of those",
            MapEntries.NightTrainingCompleted.Locator));
}
