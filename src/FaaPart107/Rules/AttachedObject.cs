using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.49(e): <see cref="MapEntries.AttachedObjectSecure"/>, "Ensure that any object attached or
/// carried by the small unmanned aircraft is secure".
/// </summary>
/// <remarks>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: part 107
/// does not define "secure", and § 107.49(e) states no measure for it. The sentence's second
/// conjunct — "and does not adversely affect the flight characteristics or controllability of the
/// aircraft" — is a coordinate predicate of the same copula, not the measure of the first, and the
/// map carries it as a separate entry, <see cref="MapEntries.AttachedObjectNoAdverseEffect"/>. So
/// nothing in this constituent says what secure is measured against, and the engine declines
/// whatever the caller supplies: a fastening standard, a load figure or an industry practice would
/// be this engine answering a question the published map says is open.
/// </remarks>
public static class AttachedObject
{
    /// <summary>
    /// <see cref="MapEntries.AttachedObjectSecure"/>: the entry's decline, always.
    /// </summary>
    /// <returns>
    /// <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this entry's own locator,
    /// § 107.49(e), and naming the term the corpus leaves undefined.
    /// </returns>
    public static Resolution<object> Secure() =>
        Resolution<object>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.AttachedObjectSecure.Id}' is met: part 107 does not define "
            + "\"secure\" and § 107.49(e) states no measure for it — the second conjunct, \"and does not adversely "
            + "affect the flight characteristics or controllability of the aircraft\", is a coordinate predicate of "
            + "the same copula rather than the measure of the first",
            MapEntries.AttachedObjectSecure.Locator));
}
