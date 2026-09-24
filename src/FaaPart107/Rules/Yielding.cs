using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Whether § 107.37(a) prohibits the pass the caller states: <see cref="MapEntries.RightOfWay"/>,
/// "(a) Each small unmanned aircraft must yield the right of way to all aircraft, airborne
/// vehicles, and launch and reentry vehicles. Yielding the right of way means that the small
/// unmanned aircraft must give way to the aircraft or vehicle and may not pass over, under, or
/// ahead of it unless well clear."
/// </summary>
/// <remarks>
/// The finding is this paragraph's and no wider: it says what § 107.37(a) does about this pass,
/// and another rule may prohibit the operation anyway. § 107.37(b), operating so close to another
/// aircraft as to create a collision hazard, is a separate entry
/// (<see cref="MapEntries.CollisionHazardProximity"/>), and nothing here answers it.
/// </remarks>
/// <param name="Object">What was passed, as the caller stated it.</param>
/// <param name="Position">Where the small unmanned aircraft passed it, as the caller stated it.</param>
/// <param name="ObjectIsOneTheSectionNames">True when the object is one of the three kinds § 107.37(a) names.</param>
/// <param name="PositionIsOneTheSectionProhibits">True when the pass is in one of the three relative positions § 107.37(a) prohibits.</param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record RightOfWayFinding(
    EncounteredObject Object,
    RelativePosition Position,
    bool ObjectIsOneTheSectionNames,
    bool PositionIsOneTheSectionProhibits,
    WaiverStatement Waiver)
{
    /// <summary>
    /// True when § 107.37(a) does not prohibit this pass, because one of its two enumerations does
    /// not reach it.
    /// </summary>
    /// <remarks>
    /// It is true in every finding this rule resolves, and that is the shape of the paragraph
    /// rather than a rule that always permits: where both enumerations do reach the pass, whether
    /// it is prohibited turns on the exception "unless well clear", and the rule declines on
    /// <see cref="MapEntries.WellClear"/>'s question instead of resolving to false.
    /// </remarks>
    public bool MayPass => !(ObjectIsOneTheSectionNames && PositionIsOneTheSectionProhibits);

    /// <summary>Where the rule is stated: <c>§ 107.37(a)</c>.</summary>
    public SourceLocator Authority => MapEntries.RightOfWay.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        $"passing {Position.Designation}, and it is {Object}: § 107.37(a) "
        + $"{(ObjectIsOneTheSectionNames ? "names it" : "does not name it")} and "
        + $"{(PositionIsOneTheSectionProhibits ? "prohibits that pass" : "does not prohibit that pass")}, "
        + $"so § 107.37(a) {(MayPass ? "does not prohibit" : "prohibits")} this pass [{Authority}]; {Waiver}";
}

/// <summary>
/// § 107.37(a), "Yielding the right of way": <see cref="MapEntries.RightOfWay"/>, the two
/// enumerations the paragraph prints, and <see cref="MapEntries.WellClear"/>, the exception it
/// states no measure for.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for what the paragraph is about rather than for the entry, because a type
/// named <c>RightOfWay</c> collides with the <c>RightOfWay</c> the generated
/// <see cref="Handlers"/> partial declares.
/// </para>
/// <para>
/// <see cref="MapEntries.WellClear"/>'s question the map records as unresolved,
/// <c>RequiresInterpretation</c>: part 107 does not define "well clear", and the term carries the
/// whole exception to the prohibition on passing over, under or ahead. The term occurs exactly
/// once in the corpus, § 107.3 does not define it and no other section does, and § 107.37(a)
/// states no measure for it in its own constituent — "must give way" is a coordinate obligation in
/// the definiens, not what the clearance is measured against.
/// </para>
/// <para>
/// So that entry declines, whatever the caller supplies. A separation distance, a time to closest
/// approach or any other quantitative well-clear threshold would be this engine answering a
/// question the published map says is open, however standard the figure. The entry is not
/// <c>kind: assertion</c> either, so the engine does not demand the answer from the caller and
/// does not take it when offered: it records what the engine would have to do and cannot.
/// </para>
/// <para>
/// <see cref="MapEntries.RightOfWay"/> is the same paragraph's two enumerations, which are
/// computable, and it reaches the open term only where they both reach the pass. Its note:
/// "Two enumerations, both computable: three protected kinds — aircraft, airborne vehicles, launch
/// and reentry vehicles — and three prohibited relative positions — over, under, ahead. An engine
/// told the object is none of the three, or that the manoeuvre is none of the three, decides the
/// case without the open term at all … What is not computed is the exception 'unless well clear',
/// which is well-clear."
/// </para>
/// <para>
/// § 107.37(a) is waivable — § 107.205(f) lists it — so both entries are suspended by
/// <see cref="MapEntries.WaivableRegulations"/>, and while the caller states a waiver of it is in
/// force the answer is that gate's <see cref="UnresolvedReason.OutsideCurrentScope"/> rather than
/// anything the paragraph says. Each entry runs that gate as its own, naming itself: the two share
/// the locator § 107.37(a), so where the decline cites cannot tell them apart and only what was
/// attempted does.
/// </para>
/// <para>
/// § 107.37(b), operating so close to another aircraft as to create a collision hazard, is a
/// separate entry (<see cref="MapEntries.CollisionHazardProximity"/>) which the caller asserts,
/// and nothing here answers it.
/// </para>
/// </remarks>
public static class Yielding
{
    /// <summary>The regulation § 107.205(f) lists that states both entries: <c>§ 107.37(a)</c>.</summary>
    public const string Regulation = "§ 107.37(a)";

    /// <summary>
    /// <see cref="MapEntries.RightOfWay"/>: whether § 107.37(a) prohibits passing
    /// <paramref name="position"/> an object the caller states is <paramref name="encountered"/>.
    /// </summary>
    /// <remarks>
    /// Two enumerations, each closed and each the paragraph's own. An object that is none of the
    /// three kinds § 107.37(a) names is one the paragraph does not reach, whatever the pass; an
    /// object it does name, passed in none of the three relative positions it prohibits, is a pass
    /// the prohibition does not reach. Either decides the case, and the exception is not consulted.
    /// Where both reach the pass, whether it is prohibited turns on "unless well clear", which is
    /// <see cref="MapEntries.WellClear"/> — reached through <c>dependsOn</c>, asked, and what it
    /// answers on the request in hand is what decides.
    /// </remarks>
    /// <param name="encountered">What was passed, as the caller states it. Never inferred; required once the entry is reachable, and demanded after the gate (<c>docs/decisions/0008</c>).</param>
    /// <param name="position">Where the small unmanned aircraft passed it, as the caller states it. Never inferred; required once the entry is reachable, and demanded after the gate.</param>
    /// <param name="waiver">Whether a waiver of § 107.37(a) is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver
    /// of § 107.37(a) is in force; otherwise, where both enumerations reach the pass, this entry's
    /// own decline, carrying the reason <see cref="MapEntries.WellClear"/> gave on this request and
    /// citing that entry's § 107.37(a).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and
    /// <paramref name="encountered"/> or <paramref name="position"/> was not stated.
    /// </exception>
    public static Resolution<RightOfWayFinding> RightOfWay(
        EncounteredObject? encountered,
        RelativePosition? position,
        WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.RightOfWay, Regulation, waiver) is { } suspended)
        {
            return Resolution<RightOfWayFinding>.FromUnresolved(suspended);
        }

        var passed = Demands.Of(encountered, MapEntries.RightOfWay, nameof(Requests.RightOfWayRequest.Encountered));
        var relative = Demands.Of(position, MapEntries.RightOfWay, nameof(Requests.RightOfWayRequest.Position));

        var named = Names(passed);
        var prohibited = Prohibits(relative);
        if (!named || !prohibited)
        {
            return Resolution<RightOfWayFinding>.FromValue(
                new RightOfWayFinding(passed, relative, named, prohibited, waiver));
        }

        // Both enumerations reach the pass, so the case turns on the exception "unless well
        // clear". That is the map entry this one dependsOn, and this rule asks it rather than
        // answering it: the decline below is built from what that entry actually returned on this
        // request, and this engine supplies no separation distance, no time to closest approach
        // and no other well-clear measure to decide it with.
        return Resolution<RightOfWayFinding>.FromUnresolved(
            Undetermined(passed, relative, WellClear(waiver)));
    }

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

    /// <summary>
    /// This entry's own decline for a pass both enumerations reach: it names the entry the caller
    /// asked about and the entry whose question blocks the answer, carries that entry's reason,
    /// cites that entry's locator, and quotes what that entry itself recorded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is not <see cref="MapEntries.WellClear"/>'s decline handed back. The reason and the
    /// locator are that entry's, because the question that blocks the answer is its question and a
    /// citation should lead to where that question is; what was attempted is this entry's, so that
    /// a caller who asked about § 107.37(a)'s two enumerations can tell this decline from the
    /// decline of the entry it reached. The two share the locator § 107.37(a), so the citation
    /// cannot tell them apart and only <see cref="UnresolvedResult.Attempted"/> does
    /// (<c>docs/decisions/0001</c> records the same blind spot for speed, and
    /// <c>docs/decisions/0006</c> the shape).
    /// </para>
    /// <para>
    /// The map gives <see cref="MapEntries.WellClear"/> no verdict type — it is
    /// <c>Resolution&lt;object&gt;</c> — so an entry that resolves is an ordinary case here and not
    /// an impossibility: the pass is recorded as unsettled with that entry's own account beside it,
    /// rather than a verdict being guessed out of an <see cref="object"/>. The map would have to
    /// give that entry a verdict to read before this one could read one.
    /// </para>
    /// </remarks>
    private static UnresolvedResult Undetermined(
        EncounteredObject encountered,
        RelativePosition position,
        Resolution<object> exception)
    {
        var (reason, answered, account) = exception.Match(
            value => (
                (UnresolvedReason?)null,
                "answered whether the pass is well clear with a value this entry has no verdict to read",
                value.ToString() ?? string.Empty),
            unresolved => (
                (UnresolvedReason?)unresolved.Reason,
                "did not answer whether the pass is well clear",
                unresolved.Attempted));

        return new UnresolvedResult(
            reason ?? UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.RightOfWay.Id}' "
            + $"[{MapEntries.RightOfWay.Locator.Citation}] prohibits passing {position.Designation}, and it is "
            + $"{encountered}: § 107.37(a) names it and prohibits that pass unless well clear, and the map entry "
            + $"'{MapEntries.WellClear.Id}' [{MapEntries.WellClear.Locator.Citation}] {answered}; what "
            + $"'{MapEntries.WellClear.Id}' recorded: {account}",
            MapEntries.WellClear.Locator);
    }

    /// <summary>
    /// The three kinds § 107.37(a) names, and nothing else. <see cref="EncounteredObject.NoneOfThem"/>
    /// falls through to false, and nothing outside <see cref="EncounteredObject.All"/> exists.
    /// </summary>
    private static bool Names(EncounteredObject encountered) =>
        encountered == EncounteredObject.Aircraft
        || encountered == EncounteredObject.AirborneVehicle
        || encountered == EncounteredObject.LaunchOrReentryVehicle;

    /// <summary>
    /// The three relative positions § 107.37(a) prohibits passing in, and nothing else.
    /// <see cref="RelativePosition.NoneOfThem"/> falls through to false, and nothing outside
    /// <see cref="RelativePosition.All"/> exists.
    /// </summary>
    private static bool Prohibits(RelativePosition position) =>
        position == RelativePosition.Over
        || position == RelativePosition.Under
        || position == RelativePosition.Ahead;
}
