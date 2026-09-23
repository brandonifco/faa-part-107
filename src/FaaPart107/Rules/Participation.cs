using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.39(a): <see cref="MapEntries.DirectParticipation"/>, "(a) That human being is directly
/// participating in the operation of the small unmanned aircraft;".
/// </summary>
/// <remarks>
/// <para>
/// The map records this entry's question as unresolved, <c>RequiresInterpretation</c>: part 107
/// does not define "directly participating", and the term carries the whole of the § 107.39
/// exception it states — whether the aircraft may be flown over a human being turns on it. "In the
/// operation of the small unmanned aircraft" states what the participation is in, not how direct it
/// must be, and the determination is vested in nobody. So the engine declines, whatever the caller
/// supplies: a list of the roles that count as participating, or a measure of how direct the
/// participation must be, would be this engine answering from practice a question the published map
/// says is open.
/// </para>
/// <para>
/// Compare § 107.39(b), <see cref="MapEntries.ReasonableProtection"/>, which the map records as an
/// assertion because it states its own measure: two paragraphs of one sentence go opposite ways.
/// The same undefined term delimits who must be briefed under § 107.49(b),
/// <see cref="MapEntries.ParticipantBriefing"/>, which depends on this entry for that reason; the
/// entry's locator names § 107.39(a) alone because the map's grammar allows an entry one section,
/// so the decline cites § 107.39(a) although the gap reaches § 107.49(b) too.
/// </para>
/// <para>
/// § 107.205(g) lists § 107.39, so the entry is suspended while a waiver of it is in force
/// (<c>suspendedBy</c>: <see cref="MapEntries.WaivableRegulations"/>), and that gate is this
/// entry's own: the caller states whether a waiver is held, and the engine never infers it.
/// </para>
/// </remarks>
public static class Participation
{
    /// <summary>The regulation § 107.205(g) lists that states the entry: the whole of § 107.39.</summary>
    public const string Regulation = "§ 107.39";

    /// <summary>
    /// <see cref="MapEntries.DirectParticipation"/>: the entry's decline, whenever it is reached.
    /// </summary>
    /// <param name="waiver">Whether a waiver of § 107.39 is in force, as the caller states it.</param>
    /// <returns>
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in force;
    /// otherwise <see cref="UnresolvedReason.RequiresInterpretation"/>, citing this entry's own locator,
    /// § 107.39(a), and naming the term the corpus leaves undefined.
    /// </returns>
    public static Resolution<object> Direct(WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.DirectParticipation, Regulation, waiver) is { } suspended)
        {
            return Resolution<object>.FromUnresolved(suspended);
        }

        return Resolution<object>.FromUnresolved(new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.DirectParticipation.Id}' is met: part 107 does not define "
            + "\"directly participating\", the term carries the whole of the § 107.39 exception it states, and "
            + "\"in the operation of the small unmanned aircraft\" states what the participation is in and not how "
            + "direct it must be, with the determination vested in nobody",
            MapEntries.DirectParticipation.Locator));
    }
}
