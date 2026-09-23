using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Whether § 107.29(b) permits the operation the caller states:
/// <see cref="MapEntries.CivilTwilightOperation"/>, "No person may operate a small unmanned
/// aircraft system during periods of civil twilight unless the small unmanned aircraft has lighted
/// anti-collision lighting …", with § 107.29(c)'s definition of the periods it says that of.
/// </summary>
/// <remarks>
/// <para>
/// Every element is kept where the map puts it. <see cref="During"/> is
/// <see cref="MapEntries.CivilTwilightWindow"/>'s own period, taken from that entry's value rather
/// than restated here, and <see cref="Lighting"/> is what
/// <see cref="MapEntries.AntiCollisionLighting"/> answered about this aircraft. What is this
/// entry's own is which of them applies: § 107.29(c) says what civil twilight refers to "for
/// purposes of paragraph (b)", and § 107.29(b) says its requirement of an operation during such a
/// period and of no other.
/// </para>
/// </remarks>
/// <param name="Place">Where the operation is, as the caller stated it: what § 107.29(c) selects the definition of civil twilight by.</param>
/// <param name="Period">Which of § 107.29(c)(1)-(2)'s periods the operation is during, as the caller stated it.</param>
/// <param name="During">
/// The period of civil twilight the operation is during, as <c>civil-twilight-window</c> prints it;
/// null where the caller stated neither, which is an operation § 107.29(b) says nothing about.
/// </param>
/// <param name="Lighting">
/// What <c>anti-collision-lighting</c> answered about the aircraft's lighting, or null where the
/// operation is during neither period and that entry was not asked.
/// </param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record CivilTwilightOperationFinding(
    OperationPlace Place,
    OperationPeriod Period,
    CivilTwilightPeriod? During,
    AntiCollisionLightingFinding? Lighting,
    WaiverStatement Waiver)
{
    /// <summary>Where the operation is, as the caller stated it, checked to be present.</summary>
    public OperationPlace Place { get; } = Place ?? throw new ArgumentNullException(nameof(Place));

    /// <summary>Which period the operation is during, as the caller stated it, checked to be present.</summary>
    public OperationPeriod Period { get; } = Period ?? throw new ArgumentNullException(nameof(Period));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>
    /// What <c>anti-collision-lighting</c> answered, present exactly where <see cref="During"/> is:
    /// § 107.29(b) states its requirement of an operation during a period of civil twilight and of
    /// no other, so that entry is asked for those operations and for no others.
    /// </summary>
    public AntiCollisionLightingFinding? Lighting { get; } = (During is null) == (Lighting is null)
        ? Lighting
        : throw new ArgumentException(
            "§ 107.29(b) states its requirement of an operation during a period of civil twilight, so the "
            + "anti-collision-lighting finding is present exactly where the period of civil twilight is",
            nameof(Lighting));

    /// <summary>
    /// True where the operation is during one of the periods § 107.29(c) calls civil twilight, so
    /// that § 107.29(b) states a prohibition about it at all.
    /// </summary>
    public bool DuringCivilTwilight => During is not null;

    /// <summary>
    /// Whether § 107.29(b) permits the operation — <b>null where it states no prohibition about
    /// it</b>, because the operation is during neither period of civil twilight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three answers, and the third is not either of the other two. An operation outside both
    /// periods is one this paragraph says nothing about: that is not "the lighting requirement is
    /// met", and it is not "the operation is prohibited" either. It is not a permission granted by
    /// any other section of part 107 either — § 107.29(a) states the night prohibition and is
    /// <c>night-operation</c>'s. <see cref="DuringCivilTwilight"/> stands beside this property and
    /// says which case a null is, so a null here is never "undetermined": a constituent this engine
    /// could not resolve makes the whole entry decline and produces no finding at all.
    /// </para>
    /// <para>
    /// Where the paragraph does reach the operation, this is what <c>anti-collision-lighting</c>
    /// resolved on this aircraft and is not a verdict re-derived here. The unless-clause's terms —
    /// lighted lighting, visible for at least the printed 3 statute miles, at a flash rate
    /// sufficient to avoid a collision, not extinguished — are that entry's and its two
    /// constituents', and not one of them is restated in this file.
    /// </para>
    /// </remarks>
    public bool? Permitted => Lighting?.Met;

    /// <summary>Where the rule is stated: <c>§ 107.29(b)-(c)</c>.</summary>
    public SourceLocator Authority => MapEntries.CivilTwilightOperation.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        During is null
            ? $"{Place}, {Period}, so § 107.29(b) states no prohibition about this operation [{Authority}]; {Waiver}"
            : $"{Place}, {Period} ({During}), and § 107.29(b) {(Permitted == true ? "permits" : "prohibits")} "
                + $"the operation [{Authority}]: {Lighting}; {Waiver}";
}

/// <summary>
/// § 107.29(b) and (c): the prohibition on operating during periods of civil twilight without the
/// anti-collision lighting the paragraph requires, and what those periods are.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for what the paragraphs are about rather than for the entry, because a type
/// named <c>CivilTwilightOperation</c> collides with the <c>CivilTwilightOperation</c> the
/// generated <see cref="Handlers"/> partial declares — the reason <see cref="Yielding"/> is named
/// as it is — and because <see cref="CivilTwilight"/> is already the sibling entry on (c)(1)-(2).
/// </para>
/// <para>
/// <b>What this entry states of its own is (c)'s chapeau: which definition applies.</b>
/// <see cref="ParagraphC"/> — "For purposes of paragraph (b) of this section, civil twilight refers
/// to the following:" — is in this entry's evidence and in no other's: <c>civil-twilight-window</c>
/// quotes items (1) and (2), <c>civil-twilight-alaska</c> quotes item (3), and neither quotes the
/// sentence that says they are a definition of the term paragraph (b) turns on. So the selection
/// among them is this entry's, over both, and it turns on the place: "Except for Alaska" in the
/// first two, "In Alaska" in the third. The map's note says the same in terms — "(c)(3) is part of
/// the definition of civil twilight this entry's rule turns on: without civil-twilight-alaska the
/// entry cannot say whether <b>an Alaskan operation</b> is in civil twilight".
/// </para>
/// <para>
/// <b>Alaska is therefore a condition and not a conjunct.</b> <c>civil-twilight-alaska</c> is asked
/// for an operation in Alaska and for no other — an unconditional conjunction with it would decline
/// every operation everywhere, because the Air Almanac is not an admitted source, and the two items
/// that would answer an operation outside Alaska say "Except for Alaska" in their own words. The
/// place is a fact the caller states positively, <see cref="OperationPlace"/>, never an absence:
/// the shape <see cref="VisualObserverUse"/> uses for § 107.33's chapeau.
/// </para>
/// <para>
/// <b>The three answers its constituents can give are three different answers, and this entry keeps
/// them apart.</b> <c>civil-twilight-alaska</c> declines
/// <see cref="UnresolvedReason.MissingRulesData"/>: the corpus is clear and the text that would
/// settle it is in a book this map has not admitted, which is not an open question.
/// <c>anti-collision-lighting</c> can decline <see cref="UnresolvedReason.OutsideCurrentScope"/>
/// through the § 107.205 gate. Either may one day resolve. So every decline this entry emits
/// carries <see cref="UnresolvedResult.Reason"/> from the constituent that actually blocked it and
/// substitutes none of its own — a composite that flattened them into one reason would lose the
/// distinction a caller decides what to do next by.
/// </para>
/// <para>
/// <b>The gate is this entry's own and runs first, which matters more here than usual.</b>
/// § 107.205(b) lists "Section 107.29(a)(2) and (b)" — the only row of § 107.205 that reaches
/// § 107.29 — and this entry's <c>suspendedBy</c> names <c>waivable-regulations</c>, so the
/// statement this entry is asked under is the same one <c>anti-collision-lighting</c> and its two
/// constituents are asked under, <see cref="Regulation"/>. <c>civil-twilight-window</c>, by
/// contrast, has no <c>suspendedBy</c> at all and cannot decline for a waiver, and (c) is not a
/// listed paragraph. Two things follow. A waived operation would otherwise be answered differently
/// depending on when it was: outside civil twilight the definition entry would resolve a finding,
/// because nothing suspends it; inside it, <c>anti-collision-lighting</c>'s own gate would decline
/// under <em>that</em> entry's name. Reading the gate here first gives one answer, in this entry's
/// name, for every operation. And the gate precedes the definition rather than following it: (c)
/// defines civil twilight "for purposes of paragraph (b)", so once (b) is suspended there is
/// nothing left for (c) to define — which is why a waived Alaskan operation is answered by the
/// gate and not by the Air Almanac's absence.
/// </para>
/// <para>
/// <b>What the gate precedes is the assertion, not the caller's typed inputs.</b> The four inputs
/// this rule reads are demanded by <see cref="Handlers"/> as the arguments of the call, so all four
/// are demanded before this method is entered and before the gate is read: a caller who states a
/// waiver in force but leaves the place unset is refused with <see cref="ArgumentException"/>
/// naming the place, not answered <see cref="UnresolvedReason.OutsideCurrentScope"/>. What a waived
/// operation is spared is the assertion: no constituent is asked, so nothing is demanded through
/// <see cref="RuleRequest"/>. That is <c>operating-limitations</c>' ordering, and
/// <c>visual-observer-conditions</c>' and <c>anti-collision-lighting</c>', which is why this
/// composite takes it. <b>The engine also does it the other way</b>: <c>reasonable-protection</c>
/// passes its <see cref="Shelter"/> in unresolved so that <see cref="Protection.Reasonable"/> can
/// demand it <em>after</em> the gate, on the argument that a waiver in force makes the place as
/// irrelevant as the assertion. The two orderings coexist and nothing in this repository records
/// which is right; settling that is not this entry's to do (<c>AGENTS.md</c> §6), and what is
/// recorded here is which one this entry takes and what it therefore does.
/// </para>
/// <para>
/// <b>Its other consequence is worth stating plainly</b>, as <see cref="Lights"/> states it: this
/// gate and <c>anti-collision-lighting</c>'s are the same gate on the same statement, so once this
/// one has passed that entry cannot decline for a waiver, and the decline
/// <see cref="Undetermined"/> emits for it is not reachable through the public API today. It is
/// written all the same, and the constituent's actual result is what the answer is read off, so
/// that a question the map later opens or settles moves this entry's answer with nothing changed
/// here (issue #78's shape).
/// </para>
/// </remarks>
public static class Twilight
{
    /// <summary>
    /// The regulation § 107.205 lists that reaches this entry, as § 107.205(b) designates it:
    /// "Section 107.29(a)(2) and (b)" — the same string, and the same row,
    /// <see cref="Lights.Regulation"/> is.
    /// </summary>
    /// <remarks>
    /// It is deliberately not this entry's locator. The locator cites the passage,
    /// <c>§ 107.29(b)-(c)</c>; § 107.205(b) designates what a certificate may authorize deviation
    /// from, and of § 107.29 it lists paragraphs (a)(2) and (b) and nothing else. Paragraph (b) is
    /// the prohibition this entry applies, so the listed regulation reaches it; paragraph (c) is a
    /// definition § 107.205 does not list, and this engine does not invent a designation for it.
    /// One caller statement therefore covers this entry and the three entries on § 107.29(a)(2) and
    /// (b), and they are told apart by which entry a decline names, not by which regulation was
    /// waived.
    /// </remarks>
    public const string Regulation = Lights.Regulation;

    /// <summary>
    /// § 107.29(c)'s chapeau, verbatim — the sentence this entry states and no other entry carries.
    /// </summary>
    /// <remarks>
    /// Quoted, not interpreted, and carried so that what this entry answers on its own account is
    /// legible beside the answer — as <see cref="Observers.ParagraphB"/> carries § 107.33(b). The
    /// three items are not quoted here: (1) and (2) are <c>civil-twilight-window</c>'s evidence and
    /// (3) is <c>civil-twilight-alaska</c>'s, and each of those entries prints its own.
    /// </remarks>
    public const string ParagraphC =
        "(c) For purposes of paragraph (b) of this section, civil twilight refers to the following:";

    /// <summary>
    /// <see cref="MapEntries.CivilTwilightOperation"/>: whether § 107.29(b) permits the operation
    /// the caller states.
    /// </summary>
    /// <remarks>
    /// The gate first, against this entry; then § 107.29(c)'s definition, from the entry the stated
    /// place selects — <c>civil-twilight-alaska</c> in Alaska, which is asked at runtime and whose
    /// answer this entry reports rather than assuming, and <c>civil-twilight-window</c> outside it;
    /// then, and only for an operation during one of that entry's periods, § 107.29(b)'s
    /// requirement, as <c>anti-collision-lighting</c> answers it on this aircraft. An operation
    /// during neither period resolves a finding that says the paragraph states no prohibition about
    /// it: that entry is not asked, so nothing about the flash rate or the remote pilot in command's
    /// determination is demanded through <paramref name="assertions"/>. The caller's own
    /// <paramref name="lighting"/> statement is demanded whatever the period, by the handler, before
    /// this method is entered.
    /// </remarks>
    /// <param name="place">Where the operation is, as the caller states it. Never inferred.</param>
    /// <param name="period">Which of § 107.29(c)(1)-(2)'s periods the operation is during, as the caller states it. Never inferred.</param>
    /// <param name="lighting">What the caller states about the aircraft's anti-collision lighting: <c>anti-collision-lighting</c>'s input.</param>
    /// <param name="waiver">Whether a waiver of § 107.29(a)(2) and (b) is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts, for the entries this one reaches. Never defaulted.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver of § 107.29(a)(2) and (b) is in force; and, where the constituent that would supply
    /// the definition or answer the requirement did not resolve, this entry's own decline carrying
    /// <em>that</em> constituent's reason and citing its locator.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// An entry this one reached was asked for an assertion the caller did not make.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation, or an asserted value is not an
    /// <see cref="Assertion"/> or is about another entry.
    /// </exception>
    public static Resolution<CivilTwilightOperationFinding> Operation(
        OperationPlace place,
        OperationPeriod period,
        LightingStatement lighting,
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(place);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(lighting);
        ArgumentNullException.ThrowIfNull(assertions);

        // This entry's own gate, before the definition as well as before the requirement:
        // § 107.205(b) reaches paragraph (b), and (c) defines civil twilight only "for purposes of
        // paragraph (b)". It is not before everything — the four inputs above were demanded by the
        // handler to make this call at all.
        if (Waivers.Suspension(MapEntries.CivilTwilightOperation, Regulation, waiver) is { } suspended)
        {
            return Resolution<CivilTwilightOperationFinding>.FromUnresolved(suspended);
        }

        // § 107.29(c): what civil twilight refers to here. In Alaska that is (c)(3)'s, so the entry
        // that holds it is asked, and what it answers — not what this engine expects it to answer —
        // is what this entry reports.
        if (place == OperationPlace.InAlaska)
        {
            return Resolution<CivilTwilightOperationFinding>.FromUnresolved(
                EntryPoints.CivilTwilightAlaska
                    .Resolve(new Requests.CivilTwilightAlaskaRequest(assertions))
                    .Match(
                        answered => Undetermined(
                            MapEntries.CivilTwilightAlaska,
                            null,
                            place.ToString(),
                            InAlaska($"answered with a value this engine can read no period of civil twilight from: {answered}")),
                        open => Undetermined(
                            MapEntries.CivilTwilightAlaska,
                            open.Reason,
                            place.ToString(),
                            InAlaska("did not resolve it"))));
        }

        // Outside Alaska it is (c)(1)-(2)'s, which is civil-twilight-window's value: the periods are
        // read off that entry and no figure of them is stated here.
        var during = Stated(period, CivilTwilight.Windows());
        if (during is null)
        {
            return Resolution<CivilTwilightOperationFinding>.FromValue(
                new CivilTwilightOperationFinding(place, period, null, null, waiver));
        }

        // "No person may operate … during periods of civil twilight unless the small unmanned
        // aircraft has lighted anti-collision lighting …": the unless-clause is the requirement
        // anti-collision-lighting states, and the verdict below is that entry's own.
        return Lights.AsRequired(lighting, waiver, assertions).Match(
            required => Resolution<CivilTwilightOperationFinding>.FromValue(
                new CivilTwilightOperationFinding(place, period, during, required, waiver)),
            open => Resolution<CivilTwilightOperationFinding>.FromUnresolved(
                Undetermined(
                    MapEntries.AntiCollisionLighting,
                    open.Reason,
                    $"{place}, {period}",
                    $"§ 107.29(b) states its requirement of the anti-collision lighting the map entry "
                    + $"'{MapEntries.AntiCollisionLighting.Id}' [{MapEntries.AntiCollisionLighting.Locator.Citation}] "
                    + $"speaks for — {MapEntries.AntiCollisionLighting.Name} — and that entry did not resolve")));
    }

    /// <summary>
    /// The period of civil twilight the operation is during, as <c>civil-twilight-window</c> prints
    /// it: the item of § 107.29(c) the caller named, or null where the caller named neither.
    /// </summary>
    /// <remarks>
    /// This is the whole of what this entry does with the definition, and it deliberately restates
    /// none of it: the two periods, both 30-minute figures and both official events are that
    /// entry's value, and a period this entry reports is the very object that entry resolved.
    /// </remarks>
    /// <param name="period">Which period the caller states the operation is during.</param>
    /// <param name="windows">What <c>civil-twilight-window</c> answered.</param>
    /// <returns>The period, or null.</returns>
    private static CivilTwilightPeriod? Stated(OperationPeriod period, CivilTwilightWindows windows) =>
        period == OperationPeriod.BeforeOfficialSunrise ? windows.BeforeSunrise
        : period == OperationPeriod.AfterOfficialSunset ? windows.AfterSunset
        : null;

    /// <summary>What an operation in Alaska turns on, and what the entry that holds it did.</summary>
    /// <param name="outcome">What <c>civil-twilight-alaska</c> did, in words.</param>
    /// <returns>The explanation this entry's decline carries.</returns>
    private static string InAlaska(string outcome) =>
        $"§ 107.29(c)(3) is what civil twilight refers to there, and the map entry "
        + $"'{MapEntries.CivilTwilightAlaska.Id}' [{MapEntries.CivilTwilightAlaska.Locator.Citation}] "
        + $"— {MapEntries.CivilTwilightAlaska.Name} — {outcome}";

    /// <summary>
    /// This entry's own decline for an operation a constituent could not settle: it names the entry
    /// the caller asked about and the constituent whose question blocks it, and it carries that
    /// constituent's reason and cites that constituent's locator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is not the constituent's decline handed back (issue #78, and the shape
    /// <c>docs/decisions/0001</c> records for <c>speed-within-limit</c> → <c>speed-limit</c>). The
    /// reason and the locator are the constituent's, because the question that blocks the answer is
    /// the constituent's and a citation should lead to where that question is stated; what was
    /// attempted is this entry's, so that a caller who asked whether § 107.29(b) permits an
    /// operation is told so by name.
    /// </para>
    /// <para>
    /// <b>The reason is carried, never chosen.</b> The two constituents that can block this entry
    /// give different reasons — <c>civil-twilight-alaska</c> gives
    /// <see cref="UnresolvedReason.MissingRulesData"/>, because the corpus is clear and the text
    /// that would settle it is elsewhere, and <c>anti-collision-lighting</c> gives
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> through the § 107.205 gate — and a caller
    /// does different things about them. The fallback is reached only where a constituent resolved
    /// a value this engine can read no answer from, which is the case
    /// <see cref="Observers"/> records the same fallback for.
    /// </para>
    /// </remarks>
    /// <param name="constituent">The entry whose question blocks the answer, as the map has it.</param>
    /// <param name="reason">Why that entry did not settle it, as it gave it; null where it resolved a value this engine cannot read.</param>
    /// <param name="situation">The operation the question was asked about, as the caller stated it.</param>
    /// <param name="explanation">What was needed, from which entry, and what that entry did.</param>
    /// <returns>The decline.</returns>
    private static UnresolvedResult Undetermined(
        MapEntry constituent,
        UnresolvedReason? reason,
        string situation,
        string explanation) =>
        new(
            reason ?? UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.CivilTwilightOperation.Id}' "
            + $"[{MapEntries.CivilTwilightOperation.Locator.Citation}] permits an operation where {situation}: "
            + explanation,
            constituent.Locator);
}
