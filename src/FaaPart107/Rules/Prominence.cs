using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.51(c): <see cref="MapEntries.ProminentObjects"/>, "For purposes of this section, flight
/// visibility means the average slant distance from the control station at which prominent
/// unlighted objects may be seen and identified by day and prominent lighted objects may be seen
/// and identified by night."
/// </summary>
/// <remarks>
/// <para>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: part 107
/// does not define "prominent", and the term carries the whole visibility metric — the distance
/// reported as flight visibility is the distance at which prominent objects may be seen and
/// identified, so the degree of prominence fixes the number § 107.51(c)'s 3-statute-mile threshold
/// is compared against. § 107.51(c) states no measure for it in its own constituent — "unlighted
/// … by day and … lighted … by night" says when each kind of object counts, not what prominence is
/// measured against — and names nobody to decide it. So the engine declines, whatever the caller
/// supplies: a size threshold, a contrast rule or a list of object types would be this engine
/// answering a question the published map says is open.
/// </para>
/// <para>
/// The entry is <c>suspendedBy: [waivable-regulations]</c>, so the gate is read first and its
/// decline is a different one: § 107.205(i) lists the whole of § 107.51, and while the caller
/// states a waiver of it is in force the entry is unreachable rather than open (decision 0001, and
/// rules-factory decision 0021). The figure this definition serves is
/// <c>visibility-minimum</c>'s, and nothing here states or measures a visibility.
/// </para>
/// </remarks>
public static class Prominence
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary>
    /// <see cref="MapEntries.ProminentObjects"/>: the entry's decline, once past the waiver gate.
    /// </summary>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in
    /// force; otherwise <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this entry's
    /// own locator, § 107.51(c), and naming the term the corpus leaves undefined.
    /// </returns>
    public static Resolution<object> Objects(WaiverStatement waiver) =>
        Waivers.Suspension(MapEntries.ProminentObjects, Regulation, waiver) is { } suspended
            ? Resolution<object>.FromUnresolved(suspended)
            : Resolution<object>.FromUnresolved(new UnresolvedResult(
                UnresolvedReason.RequiresInterpretation,
                $"decide which objects the map entry '{MapEntries.ProminentObjects.Id}' counts: part 107 does not "
                + "define \"prominent\", the term carries the whole visibility metric — the flight visibility is the "
                + "average slant distance at which prominent objects may be seen and identified — and § 107.51(c) "
                + "states no measure for it and names nobody to decide it",
                MapEntries.ProminentObjects.Locator));
}
