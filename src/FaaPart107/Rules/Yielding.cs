using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.37(a), "Yielding the right of way": <see cref="MapEntries.WellClear"/>, "Yielding the
/// right of way means that the small unmanned aircraft must give way to the aircraft or vehicle
/// and may not pass over, under, or ahead of it unless well clear."
/// </summary>
/// <remarks>
/// <para>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: part 107
/// does not define "well clear", and the term carries the whole exception to the prohibition on
/// passing over, under or ahead. The term occurs exactly once in the corpus, § 107.3 does not
/// define it and no other section does, and § 107.37(a) states no measure for it in its own
/// constituent — "must give way" is a coordinate obligation in the definiens, not what the
/// clearance is measured against.
/// </para>
/// <para>
/// So the engine declines, whatever the caller supplies. A separation distance, a time to closest
/// approach or any other quantitative well-clear threshold would be this engine answering a
/// question the published map says is open, however standard the figure. The entry is not
/// <c>kind: assertion</c> either, so the engine does not demand the answer from the caller and
/// does not take it when offered: it records what the engine would have to do and cannot.
/// </para>
/// <para>
/// § 107.37(a) is waivable — § 107.205(f) lists it — so the entry is suspended by
/// <see cref="MapEntries.WaivableRegulations"/>, and while the caller states a waiver of it is in
/// force the answer is that gate's <see cref="UnresolvedReason.OutsideCurrentScope"/> rather than
/// the open term's. The two declines say different things and cite different places, and the
/// entry is not reached at all while the gate holds.
/// </para>
/// <para>
/// § 107.37(b), operating so close to another aircraft as to create a collision hazard, is a
/// separate entry (<see cref="MapEntries.CollisionHazardProximity"/>) which the caller asserts,
/// and nothing here answers it.
/// </para>
/// </remarks>
public static class Yielding
{
    /// <summary>The regulation § 107.205(f) lists that states the entry: <c>§ 107.37(a)</c>.</summary>
    public const string Regulation = "§ 107.37(a)";

    /// <summary>
    /// <see cref="MapEntries.WellClear"/>: the entry's decline, always.
    /// </summary>
    /// <param name="waiver">Whether a waiver of § 107.37(a) is in force, as the caller states it.</param>
    /// <returns>
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/>, citing § 107.205, while the statement says a
    /// waiver is in force; otherwise <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this
    /// entry's own locator, § 107.37(a), and naming the term the corpus leaves undefined.
    /// </returns>
    public static Resolution<object> WellClear(WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.WellClear, Regulation, waiver) is { } suspended)
        {
            return Resolution<object>.FromUnresolved(suspended);
        }

        return Resolution<object>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.WellClear.Id}' is met: part 107 does not define "
            + "\"well clear\", the term carries the whole exception to the prohibition on passing over, under "
            + "or ahead, and § 107.37(a) states no measure for it",
            MapEntries.WellClear.Locator));
    }
}
