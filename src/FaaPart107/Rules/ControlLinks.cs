using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.49(c): <see cref="MapEntries.ControlLinksWorking"/>, "(c) Ensure that all control links
/// between ground control station and the small unmanned aircraft are working properly;".
/// </summary>
/// <remarks>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: part 107
/// does not define "working properly", and the term carries the whole obligation. § 107.49(c)
/// states no measure for it in its own constituent — "between ground control station and the small
/// unmanned aircraft" identifies which links, not what properly is measured against — and vests
/// the determination in nobody. So the engine declines, whatever the caller supplies: a
/// link-quality threshold, a telemetry check or a signal-strength rule would be this engine
/// answering a question the published map says is open.
/// </remarks>
public static class ControlLinks
{
    /// <summary>
    /// <see cref="MapEntries.ControlLinksWorking"/>: the entry's decline, always.
    /// </summary>
    /// <returns>
    /// <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this entry's own locator,
    /// § 107.49(c), and naming the term the corpus leaves undefined.
    /// </returns>
    public static Resolution<object> Working() =>
        Resolution<object>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.ControlLinksWorking.Id}' is met: part 107 does not define "
            + "\"working properly\", the term carries the whole obligation, and § 107.49(c) states no measure for "
            + "it and vests the determination in nobody",
            MapEntries.ControlLinksWorking.Locator));
}
