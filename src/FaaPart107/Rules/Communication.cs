using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.33(a): <see cref="MapEntries.EffectiveCommunication"/>, "(a) The remote pilot in command,
/// the person manipulating the flight controls of the small unmanned aircraft system, and the
/// visual observer must maintain effective communication with each other at all times."
/// </summary>
/// <remarks>
/// <para>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: part 107
/// does not define "effective", and the term carries the whole of § 107.33(a). The paragraph
/// states no measure for it in its own constituent — "with each other at all times" fixes between
/// whom and when, not what effectiveness is measured against — and vests the determination in
/// nobody: it is an obligation on the three persons the paragraph names, not a judgement the
/// corpus gives them. So the engine declines, whatever the caller supplies: a radio check, a
/// latency threshold or an industry standard for effective communication would be this engine
/// answering a question the published map says is open.
/// </para>
/// <para>
/// The three persons are § 107.33(a)'s own, in its own words — "the person manipulating the flight
/// controls of the small unmanned aircraft system", plural, which is not § 107.31(a)'s "the person
/// manipulating the flight control of the small unmanned aircraft system". The corpus differs
/// between the two sections, and this engine prints each where the corpus prints it.
/// </para>
/// </remarks>
public static class Communication
{
    /// <summary>The regulation § 107.205(d) lists that states the entry: the whole of § 107.33.</summary>
    public const string Regulation = "§ 107.33";

    /// <summary>
    /// <see cref="MapEntries.EffectiveCommunication"/>: the entry's decline, and, while a waiver of
    /// § 107.33 is stated in force, the gate's.
    /// </summary>
    /// <param name="waiver">Whether a waiver of § 107.33 is in force, as the caller states it.</param>
    /// <returns>
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in force,
    /// naming this entry; otherwise <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this
    /// entry's own locator, § 107.33(a), and naming the term the corpus leaves undefined.
    /// </returns>
    public static Resolution<object> Effective(WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.EffectiveCommunication, Regulation, waiver) is { } suspended)
        {
            return Resolution<object>.FromUnresolved(suspended);
        }

        return Resolution<object>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.EffectiveCommunication.Id}' is met: part 107 does not "
            + "define \"effective\", the term carries the whole of § 107.33(a), and the paragraph states no "
            + "measure for it — \"with each other at all times\" fixes between whom and when, not what "
            + "effectiveness is measured against — and vests the determination in nobody: it is an obligation "
            + "on the remote pilot in command, the person manipulating the flight controls of the small "
            + "unmanned aircraft system, and the visual observer, not a judgement the corpus gives them",
            MapEntries.EffectiveCommunication.Locator));
    }
}
