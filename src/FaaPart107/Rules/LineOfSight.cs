using System.Collections.Immutable;
using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Who exercised "the ability described in paragraph (a) of this section" throughout the entire
/// flight, as the caller states it: the three persons § 107.31 names, one flag each.
/// </summary>
/// <remarks>
/// <para>
/// This is § 107.31(b)'s subject and nothing else. Whether a given person exercised the ability
/// throughout the entire flight is a fact about the operation that the caller states, the same
/// class of input as a groundspeed or an airspace class; the engine does not compute it, and it
/// does not compute "throughout the entire flight" either — the temporal qualifier is part of what
/// each flag means, because (b) states it once, of the whole exercise, and not of any one person.
/// </para>
/// <para>
/// The names are § 107.31(b)'s own, and (b)(1) says "the person manipulating the flight
/// <em>controls</em>" where § 107.31(a) says "the person manipulating the flight
/// <em>control</em>". The corpus genuinely differs between the two paragraphs, so
/// <see cref="PersonManipulatingTheFlightControls"/> is plural here and
/// <c>unaided-visual-contact</c>'s <c>assertedBy</c> is singular there, and neither is normalised
/// into the other.
/// </para>
/// <para>
/// There is no fourth flag and no free text. § 107.31(b) enumerates two combinations drawn from
/// exactly these three persons, so a fourth person is not somebody the paragraph's enumeration can
/// be satisfied by, and recording one would invite <see cref="LineOfSight.Maintained"/> to weigh
/// somebody the corpus does not name.
/// </para>
/// </remarks>
/// <param name="RemotePilotInCommand">
/// True when the remote pilot in command exercised the ability throughout the entire flight.
/// Named by § 107.31(b)(1).
/// </param>
/// <param name="PersonManipulatingTheFlightControls">
/// True when the person manipulating the flight controls of the small unmanned aircraft system
/// exercised the ability throughout the entire flight. Named by § 107.31(b)(1), in its plural.
/// </param>
/// <param name="VisualObserver">
/// True when a visual observer exercised the ability throughout the entire flight. This is the
/// whole of § 107.31(b)(2).
/// </param>
public sealed record ExerciseOfTheAbility(
    bool RemotePilotInCommand,
    bool PersonManipulatingTheFlightControls,
    bool VisualObserver)
{
    /// <summary>Nobody exercised the ability: neither of § 107.31(b)'s two combinations.</summary>
    public static ExerciseOfTheAbility Nobody { get; } =
        new(RemotePilotInCommand: false, PersonManipulatingTheFlightControls: false, VisualObserver: false);

    /// <inheritdoc/>
    public override string ToString()
    {
        var who = new List<string>(3);
        if (RemotePilotInCommand)
        {
            who.Add("the remote pilot in command");
        }

        if (PersonManipulatingTheFlightControls)
        {
            who.Add("the person manipulating the flight controls of the small unmanned aircraft system");
        }

        if (VisualObserver)
        {
            who.Add("a visual observer");
        }

        return who.Count == 0
            ? "the ability was exercised by nobody"
            : string.Create(CultureInfo.InvariantCulture, $"the ability was exercised by {string.Join(" and ", who)}");
    }
}

/// <summary>
/// § 107.31 as a whole — <see cref="MapEntries.VisualLineOfSight"/>: the ability paragraph (a)
/// describes, as <c>unaided-visual-contact</c> answers it, together with paragraph (b)'s
/// requirement that it be exercised by one of two named combinations.
/// </summary>
/// <remarks>
/// <para>
/// The two halves are kept apart deliberately. <see cref="Ability"/> is the dependency's finding,
/// carried unchanged, and this record neither re-asks nor re-decides it; <see cref="Exercise"/> is
/// what the caller states about § 107.31(b), and it is only ever membership-tested against the
/// paragraph's own two rows. <see cref="Maintained"/> is their conjunction, which is what the
/// entry's locator — the whole of § 107.31, not either paragraph alone — asks.
/// </para>
/// <para>
/// A stated exercise is recorded and tested even where <see cref="UnaidedVisualContactFinding.Holds"/>
/// is false, and <see cref="Maintained"/> is then false whichever combination was stated: an ability
/// the asserter says is not there cannot be exercised by anybody, and paragraph (b) requires
/// "<em>the ability described in paragraph (a)</em>" to be exercised, not some other watching. The
/// engine does not turn that into a decline and does not call the caller's two statements
/// inconsistent — it reports both halves, and what each says.
/// </para>
/// </remarks>
/// <param name="Exercise">Who exercised the ability throughout the entire flight, as the caller states it.</param>
/// <param name="Ability">§ 107.31(a)'s ability, as <c>unaided-visual-contact</c> answered it, and the waiver statement it was answered under.</param>
public sealed record VisualLineOfSightFinding(ExerciseOfTheAbility Exercise, UnaidedVisualContactFinding Ability)
{
    /// <summary>What the caller stated about § 107.31(b), checked to be present.</summary>
    public ExerciseOfTheAbility Exercise { get; } = Exercise ?? throw new ArgumentNullException(nameof(Exercise));

    /// <summary>The dependency's finding, checked to be present.</summary>
    public UnaidedVisualContactFinding Ability { get; } = Ability ?? throw new ArgumentNullException(nameof(Ability));

    /// <summary>
    /// § 107.31(b)(1), "The remote pilot in command and the person manipulating the flight controls
    /// of the small unmanned aircraft system": true only when both of them exercised the ability.
    /// The row is a conjunction of two persons, and one of them alone does not satisfy it.
    /// </summary>
    public bool ExercisedByTheRemotePilotInCommandAndThePersonManipulatingTheFlightControls =>
        Exercise.RemotePilotInCommand && Exercise.PersonManipulatingTheFlightControls;

    /// <summary>
    /// § 107.31(b)(2), "A visual observer": true when a visual observer exercised the ability. The
    /// row names one person and is satisfied by that person alone.
    /// </summary>
    public bool ExercisedByAVisualObserver => Exercise.VisualObserver;

    /// <summary>
    /// § 107.31(b)'s membership test: true when at least one of the paragraph's two rows is
    /// satisfied, false when neither is.
    /// </summary>
    /// <remarks>
    /// "must be exercised by either: (1) … or (2) …" states a requirement met by a disjunction, not
    /// a prohibition on anybody else also exercising the ability. So a row is satisfied when the
    /// persons it names exercised the ability, whoever else did as well — and both rows satisfied at
    /// once is met, not neither. § 107.31(a) itself contemplates all three persons seeing the
    /// aircraft, so reading (b) as an exact match would make the paragraph forbid what the paragraph
    /// before it describes.
    /// </remarks>
    public bool Exercised =>
        ExercisedByTheRemotePilotInCommandAndThePersonManipulatingTheFlightControls || ExercisedByAVisualObserver;

    /// <summary>
    /// Whether § 107.31 as a whole is met: the ability paragraph (a) describes is there, and
    /// paragraph (b)'s requirement that it be exercised by one of the two named combinations is
    /// satisfied.
    /// </summary>
    public bool Maintained => Ability.Holds && Exercised;

    /// <summary>
    /// The two combinations § 107.31(b) permits the ability to be exercised by, verbatim:
    /// <see cref="LineOfSight.Combinations"/>.
    /// </summary>
    public ImmutableArray<string> Combinations => LineOfSight.Combinations;

    /// <summary>Where the rule is stated: <c>§ 107.31</c>, the whole section.</summary>
    public SourceLocator Authority => MapEntries.VisualLineOfSight.Locator;

    /// <summary>The caller's waiver statement, as the dependency recorded it.</summary>
    public WaiverStatement Waiver => Ability.Waiver;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"visual line of sight is {(Maintained ? "maintained" : "not maintained")}: {Exercise}, and {Ability} [{Authority}]");
}

/// <summary>
/// § 107.31(b): <see cref="MapEntries.VisualLineOfSight"/>, "Throughout the entire flight of the
/// small unmanned aircraft, the ability described in paragraph (a) of this section must be
/// exercised by either: (1) The remote pilot in command and the person manipulating the flight
/// controls of the small unmanned aircraft system; or (2) A visual observer."
/// </summary>
/// <remarks>
/// <para>
/// <b>What is this entry's, and what is its dependency's.</b> Paragraph (a) and its four numbered
/// purposes are <c>unaided-visual-contact</c>'s, a <c>kind: assertion</c> entry whose value the
/// caller supplies and <see cref="UnaidedVision.SeenThroughoutTheFlight"/> answers; nothing here
/// restates it, re-asks it or re-decides it, and the entry's own <c>crossReferences</c> row says as
/// much — "the ability described in paragraph (a) of this section" is <c>resolvedBy</c> that entry.
/// Paragraph (b) is this entry's: its two enumerated combinations, and its requirement that the
/// ability be exercised by one of them throughout the entire flight. The entry's locator is the
/// whole of § 107.31, so what it answers is the conjunction of the two.
/// </para>
/// <para>
/// <b>Why it computes rather than declines.</b> The entry's note says the split left this entry "a
/// membership test against a fixed pair of combinations, which is computable". The uncomputable
/// half — whether anyone could in fact see the aircraft — went to the assertion entry, which is
/// exactly what the note records: the earlier note that described an assertion while the entry
/// stayed <c>kind: operation</c> "recorded [it] nowhere", and the split fixed that. So this entry
/// demands the assertion through its dependency and never re-derives it, and applies (b) to what
/// the caller states about who exercised the ability.
/// </para>
/// <para>
/// <b>The gate is this entry's own.</b> <c>visual-line-of-sight</c> carries
/// <c>suspendedBy: ["waivable-regulations"]</c> in its own map row, and § 107.205(c) lists the whole
/// of § 107.31. So the gate is consulted here, against this entry, before the dependency is reached
/// at all: the decline then names <c>visual-line-of-sight</c> and § 107.31, and not the dependency's
/// § 107.31(a), and no fact the waiver has made irrelevant is demanded. That the dependency
/// consults the same gate a moment later against its own entry is not a second gate — by then the
/// caller has already stated that no waiver is in force.
/// </para>
/// </remarks>
public static class LineOfSight
{
    /// <summary>
    /// The regulation § 107.205(c) lists that states the entry: the whole of § 107.31 — the same
    /// regulation, and the same § 107.205(c) row, that suspends <c>unaided-visual-contact</c>. One
    /// waiver statement therefore reaches both entries, and the two are told apart by which entry
    /// the decline names, not by which regulation was waived.
    /// </summary>
    public const string Regulation = UnaidedVision.Regulation;

    /// <summary>
    /// Both rows of § 107.31(b)'s enumeration, verbatim, in the paragraph's own order and under the
    /// paragraph's own numbers.
    /// </summary>
    /// <remarks>
    /// Quoted, not interpreted, and carried so that what the membership test is against is legible
    /// beside the answer — as <see cref="UnaidedVision.Purposes"/> carries paragraph (a)'s four.
    /// Row (1) keeps § 107.31(b)(1)'s plural "flight controls", which is not § 107.31(a)'s singular
    /// "flight control"; the corpus differs between the two paragraphs and this engine prints each
    /// where the corpus prints it.
    /// </remarks>
    public static ImmutableArray<string> Combinations { get; } =
    [
        "(1) The remote pilot in command and the person manipulating the flight controls of the small unmanned aircraft system; or",
        "(2) A visual observer.",
    ];

    /// <summary>
    /// <see cref="MapEntries.VisualLineOfSight"/>: whether § 107.31 is maintained — paragraph (a)'s
    /// ability, as the dependency answers it, exercised throughout the entire flight by one of
    /// paragraph (b)'s two combinations.
    /// </summary>
    /// <remarks>
    /// The gate is consulted first, against this entry; then the dependency is resolved, which
    /// demands the assertion <c>unaided-visual-contact</c> owns and declines what it declines. Only
    /// then is § 107.31(b) applied, and applying it is the whole of this entry's own arithmetic:
    /// <see cref="ExerciseOfTheAbility"/> tested against <see cref="Combinations"/>. A negative
    /// assertion is answered, not declined, and leaves § 107.31 not maintained.
    /// </remarks>
    /// <param name="exercise">Who exercised the ability throughout the entire flight, as the caller states it. Required once the entry is reachable, and demanded after the gate (<c>docs/decisions/0008</c>).</param>
    /// <param name="waiver">Whether a waiver of § 107.31 is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts, carrying <c>unaided-visual-contact</c>'s value. Never defaulted.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver of § 107.31 is in force, naming this entry; otherwise whatever the dependency
    /// declines, unchanged.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// No waiver is in force and the caller asserted nothing for <c>unaided-visual-contact</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and
    /// <paramref name="exercise"/> was not stated; or the value asserted for
    /// <c>unaided-visual-contact</c> is not an <see cref="Assertion"/>, is about another entry, or is
    /// attributed to somebody § 107.31(a) does not name.
    /// </exception>
    public static Resolution<VisualLineOfSightFinding> Maintained(
        ExerciseOfTheAbility? exercise,
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.VisualLineOfSight, Regulation, waiver) is { } suspended)
        {
            return Resolution<VisualLineOfSightFinding>.FromUnresolved(suspended);
        }

        var stated = Demands.Of(
            exercise, MapEntries.VisualLineOfSight, nameof(Requests.VisualLineOfSightRequest.Exercise));

        return UnaidedVision.SeenThroughoutTheFlight(waiver, assertions).Match(
            ability => Resolution<VisualLineOfSightFinding>.FromValue(new VisualLineOfSightFinding(stated, ability)),
            Resolution<VisualLineOfSightFinding>.FromUnresolved);
    }
}
