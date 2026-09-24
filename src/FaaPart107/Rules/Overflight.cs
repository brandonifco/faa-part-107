using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// One of the three cases § 107.39's "unless—" excepts from its prohibition, as the map entry that
/// states it answered it on this operation: met, not met, or undetermined.
/// </summary>
/// <remarks>
/// Every field but <see cref="Paragraph"/> is the answering entry's own. <see cref="Met"/> is what
/// that entry's answer came to on this operation, never a verdict re-derived here, and
/// <see cref="Account"/> is what it said — its outcome where it resolved, or what it recorded as
/// attempted where it did not. So nothing the constituents state is restated by
/// <see cref="Overflight"/>, and a constituent whose question the map later settles moves this
/// outcome with nothing changed here.
/// </remarks>
/// <param name="Entry">The map entry that answers this case, as the map has it.</param>
/// <param name="Paragraph">
/// Which of § 107.39's three the case is, in the section's own lettering. It is here because the
/// citation cannot carry it: <see cref="MapEntries.SubpartDCategories"/> is located at
/// <c>subpart D</c> and not at § 107.39(c), so a reader of a citation alone could not tell which
/// paragraph deferred to it.
/// </param>
/// <param name="Met">
/// What that entry's answer came to: true where the case § 107.39 excepts is met, false where it is
/// not, and null where it was not settled.
/// </param>
/// <param name="Account">
/// What that entry said: its outcome where it resolved, and what it recorded as attempted where it
/// did not.
/// </param>
/// <param name="Reason">
/// Why it was not settled, as that entry gave it; null where it was settled. It is that entry's own
/// reason and is never widened here: <see cref="MapEntries.SubpartDCategories"/>'
/// <see cref="UnresolvedReason.OutsideCurrentScope"/> and
/// <see cref="MapEntries.DirectParticipation"/>'s <see cref="UnresolvedReason.RequiresInterpretation"/>
/// are different answers, and this entry keeps them apart.
/// </param>
public sealed record ExceptedCaseOutcome(
    MapEntry Entry,
    string Paragraph,
    bool? Met,
    string Account,
    UnresolvedReason? Reason)
{
    /// <summary>This case's verdict in words: met, not met, or undetermined.</summary>
    public string Verdict => Met switch
    {
        true => "met",
        false => "not met",
        null => "undetermined",
    };

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Paragraph} '{Entry.Id}' [{Entry.Locator.Citation}] {Verdict}"
        + (Reason is { } reason ? $" ({reason})" : string.Empty);
}

/// <summary>
/// Whether § 107.39 prohibits operating a small unmanned aircraft over the human being the caller
/// describes: <see cref="MapEntries.OverHumanBeings"/>, "No person may operate a small unmanned
/// aircraft over a human being unless— (a) … (b) … or (c) …".
/// </summary>
/// <remarks>
/// <para>
/// The finding is this section's and no wider. § 107.39 not prohibiting the operation is not a
/// permission to fly it: every other rule in the part still applies, and none of them is answered
/// here.
/// </para>
/// </remarks>
/// <param name="Location">
/// Where the human being is located, as the caller stated it. It is this entry's own fact and half
/// of § 107.39(b); see <see cref="HumanBeingLocation"/>.
/// </param>
/// <param name="ExceptedCases">
/// The three cases § 107.39's "unless—" excepts, in the section's own lettering and order, each as
/// the entry that states it answered it.
/// </param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record OverHumanBeingsFinding(
    HumanBeingLocation Location,
    IReadOnlyList<ExceptedCaseOutcome> ExceptedCases,
    WaiverStatement Waiver)
{
    /// <summary>Where the human being is, checked to be present.</summary>
    public HumanBeingLocation Location { get; } = Location ?? throw new ArgumentNullException(nameof(Location));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>
    /// Whether § 107.39 leaves the operation to the rest of the part: true only where one of the
    /// three cases its "unless—" excepts was met on this operation.
    /// </summary>
    /// <remarks>
    /// This is computed from what the constituents answered on this operation, and is not a constant
    /// of this entry. The one situation this engine can resolve it true on today is § 107.39(b)'s,
    /// because (a)'s term is undefined and (c) is out of scope; that is a fact about those entries
    /// rather than about this one, and if either ever answers a case met this property follows it
    /// with nothing changed here.
    /// </remarks>
    public bool MayOperate => ExceptedCases.Any(excepted => excepted.Met == true);

    /// <summary>Where the rule is stated: <c>§ 107.39</c>.</summary>
    public SourceLocator Authority => MapEntries.OverHumanBeings.Locator;

    /// <summary>Whether this finding is the same as <paramref name="other"/>, comparing the excepted cases by element.</summary>
    /// <param name="other">The other finding.</param>
    /// <returns>True when both are about a human being in the same place under the same waiver statement, and carry the same excepted cases answered the same way, in the same order.</returns>
    /// <remarks>
    /// A record's generated equality would compare <see cref="ExceptedCases"/> with
    /// <c>EqualityComparer&lt;IReadOnlyList&lt;ExceptedCaseOutcome&gt;&gt;.Default</c>, which is the
    /// identity of the list object, so two resolutions of the same request would be unequal.
    /// Determinism is about what the engine says, so equality is by element (<c>AGENTS.md</c> §8) —
    /// the same reason <see cref="MultipleAircraftFinding.Equals(MultipleAircraftFinding)"/> gives
    /// for overriding its own.
    /// </remarks>
    public bool Equals(OverHumanBeingsFinding? other) =>
        other is not null
        && Location == other.Location
        && Waiver == other.Waiver
        && ExceptedCases.SequenceEqual(other.ExceptedCases);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Location);
        hash.Add(Waiver);
        foreach (var excepted in ExceptedCases)
        {
            hash.Add(excepted);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"a human being {Location}: § 107.39 {(MayOperate ? "does not prohibit" : "prohibits")} operating a small "
        + $"unmanned aircraft over that human being [{Authority}]: {string.Join(", ", ExceptedCases)}; {Waiver}";
}

/// <summary>
/// § 107.39, "Operation over human beings": <see cref="MapEntries.OverHumanBeings"/>, the one
/// prohibition its opening states and the three cases its "unless—" excepts from it.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for what the section is about rather than for the entry, because a type named
/// <c>OverHumanBeings</c> collides with the <c>OverHumanBeings</c> the generated
/// <see cref="Handlers"/> partial declares — the reason <see cref="Yielding"/> and
/// <see cref="Compliance"/> are named as they are.
/// </para>
/// <para>
/// <b>The opening is this entry's alone, and it is what is implemented here.</b> "No person may
/// operate a small unmanned aircraft over a human being unless—" is in this entry's evidence and in
/// no constituent's: <see cref="MapEntries.DirectParticipation"/>'s evidence is exactly "(a) …",
/// <see cref="MapEntries.ReasonableProtection"/>'s is exactly "(b) …", and
/// <see cref="MapEntries.SubpartDCategories"/>' is subpart D's own applicability sentence. Two
/// things in it are therefore nobody else's to state. The first is the prohibition: each constituent
/// states a condition and not one of them prohibits anything, so the verdict
/// <see cref="OverHumanBeingsFinding.MayOperate"/> exists only here. The second is "unless—", read
/// with the "or" this entry's evidence carries at the end of (b): the three are alternatives, so
/// <b>one of them met is enough</b> and the others need not be settled. That is the disjunctive
/// mirror of the reading <see cref="Compliance"/> applies to § 107.51's "all of the following",
/// and like it, it is the section's own word rather than a reading of a question the map holds open.
/// </para>
/// <para>
/// The opening's subject is "No person", which binds everybody and names nobody, so — unlike
/// § 107.51's introductory text, whose two named people <see cref="Compliance"/> carries as
/// <see cref="BoundPerson"/> — there is nothing here for a caller to choose among and this entry
/// carries no person.
/// </para>
/// <para>
/// <b>Three cases, three entries, each asked at runtime.</b> Nothing here records in advance which
/// of them can answer. (a) is <see cref="MapEntries.DirectParticipation"/>, whose term part 107 does
/// not define and which declines <see cref="UnresolvedReason.RequiresInterpretation"/> citing
/// § 107.39(a); (b) is § 107.39(b), half of it this entry's and half of it
/// <see cref="MapEntries.ReasonableProtection"/>'s assertion; (c) is
/// <see cref="MapEntries.SubpartDCategories"/>, which the map puts <c>scope: out</c>. Each is called
/// and the answer below is read off what it actually returned, so an entry whose question the map
/// later settles is followed from that call and no test of this entry would have to change for it.
/// </para>
/// <para>
/// <b>What (c) being out of scope does to this entry, the map settles.</b> Its note: "The deferral
/// in (c) is subpart-d-categories, which is <c>scope: out</c> and carries
/// <see cref="UnresolvedReason.OutsideCurrentScope"/> itself — <b>a dependency, not a second reason
/// on this entry</b>." So that reason stays that entry's: it is recorded on (c)'s outcome and named
/// in the account of any decline, and it never becomes this entry's reason while an in-scope case is
/// also unsettled. The cases are held in § 107.39's own paragraph order, so the case that blocks —
/// and whose reason and locator the decline carries — is the first unsettled one in that order,
/// which is (a) and its <see cref="UnresolvedReason.RequiresInterpretation"/>. A caller is not
/// handed <see cref="UnresolvedReason.OutsideCurrentScope"/> for a section this engine maps.
/// </para>
/// <para>
/// <b>A case that does not answer is reported as this entry's own decline.</b> The decline emitted
/// is this entry's, naming the entry the caller asked about and every case that did not answer, and
/// citing the blocking case's locator — the shape <c>docs/decisions/0001</c> records for
/// <c>speed-within-limit</c> → <c>speed-limit</c> and <see cref="Compliance"/> applies for
/// § 107.51. A constituent's <see cref="UnresolvedResult"/> is never handed back: a decline of this
/// entry must be distinguishable from a decline of <c>direct-participation</c>, and what carries
/// that distinction is <see cref="UnresolvedResult.Attempted"/>, since the reason and the citation
/// are deliberately the blocking entry's.
/// </para>
/// <para>
/// § 107.205(g) lists § 107.39, so this entry is suspended by
/// <see cref="MapEntries.WaivableRegulations"/>, exactly as <c>direct-participation</c> and
/// <c>reasonable-protection</c> are — one caller statement about § 107.39 reaches all three. The
/// gate runs here as this entry's own, <b>before</b> any case is asked and before any fact is
/// demanded, so that the decline names <c>over-human-beings</c> and not whichever constituent would
/// have been asked first; the three share the § 107.205 citation that decline carries, so once again
/// only what was attempted tells them apart.
/// </para>
/// </remarks>
public static class Overflight
{
    /// <summary>The regulation § 107.205(g) lists that states the entry: the whole of § 107.39.</summary>
    public const string Regulation = Participation.Regulation;

    /// <summary>§ 107.39(a), as the section letters it.</summary>
    private const string ParagraphA = "(a)";

    /// <summary>§ 107.39(b), as the section letters it.</summary>
    private const string ParagraphB = "(b)";

    /// <summary>§ 107.39(c), as the section letters it.</summary>
    private const string ParagraphC = "(c)";

    /// <summary>
    /// <see cref="MapEntries.OverHumanBeings"/>: whether § 107.39 prohibits operating a small
    /// unmanned aircraft over a human being the caller states is <paramref name="location"/>.
    /// </summary>
    /// <remarks>
    /// Each of the three cases § 107.39 excepts is asked, in the section's own paragraph order, and
    /// each answers on its own terms. The disjunction is then read off those answers: any case met
    /// makes § 107.39 not prohibit the operation; every case answered and none met makes it prohibit
    /// it; otherwise the first case that did not answer blocks it and this entry declines, naming
    /// itself and every case that did not answer.
    /// </remarks>
    /// <param name="location">
    /// Where the human being is located, as the caller states it. Required once the entry is
    /// reachable, and never inferred: § 107.39(b) requires the human being to be located under or
    /// inside the very thing that can provide reasonable protection, and the engine locates nobody.
    /// </param>
    /// <param name="shelter">
    /// Which of § 107.39(b)'s two places the caller's assertion is about, for
    /// <see cref="MapEntries.ReasonableProtection"/>. Passed to that entry unchanged; it is that
    /// entry's fact and this engine neither picks it nor derives it from
    /// <paramref name="location"/>.
    /// </param>
    /// <param name="waiver">Whether a waiver of § 107.39 is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver of § 107.39 is in force; otherwise, where no case is met and a case did not answer,
    /// this entry's own decline, carrying the blocking case's reason and citing its locator.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// The human being is located under one of § 107.39(b)'s two places and the caller asserted
    /// nothing for <see cref="MapEntries.ReasonableProtection"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; no waiver is in force and
    /// <paramref name="location"/> was not stated; or the standard was asserted about the place the
    /// human being is not located.
    /// </exception>
    public static Resolution<OverHumanBeingsFinding> OverAHumanBeing(
        HumanBeingLocation? location,
        Shelter? shelter,
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.OverHumanBeings, Regulation, waiver) is { } suspended)
        {
            return Resolution<OverHumanBeingsFinding>.FromUnresolved(suspended);
        }

        var located = location ?? throw new ArgumentException(
            $"resolving the map entry '{MapEntries.OverHumanBeings.Id}' "
            + $"[{MapEntries.OverHumanBeings.Locator.Citation}] needs its request's "
            + $"{nameof(Requests.OverHumanBeingsRequest.Location)}, and it was not set: § 107.39(b) requires the "
            + "human being to be located under or inside the very thing that can provide reasonable protection, "
            + "and the engine locates nobody",
            nameof(Requests.OverHumanBeingsRequest.Location));

        // Every case is asked, in § 107.39's own lettering, and the answer below is read off what
        // each one actually returned.
        ExceptedCaseOutcome[] excepted =
        [
            Outcome(MapEntries.DirectParticipation, ParagraphA, Participation.Direct(waiver), NoVerdictToRead),
            LocatedUnderAPlaceThatProtects(located, shelter, waiver, assertions),
            Outcome(
                MapEntries.SubpartDCategories,
                ParagraphC,
                Registry.Resolve(MapEntries.SubpartDCategories.Id, assertions),
                NoVerdictToRead),
        ];

        var blocking = Array.Find(excepted, outcome => outcome.Met is null);

        // "unless— (a) … (b) … or (c) …": one case met settles the whole of it, whatever an
        // unsettled case would have said, and every case answered leaves nothing open.
        return Array.Exists(excepted, outcome => outcome.Met == true) || blocking is null
            ? Resolution<OverHumanBeingsFinding>.FromValue(new OverHumanBeingsFinding(located, excepted, waiver))
            : Resolution<OverHumanBeingsFinding>.FromUnresolved(Undetermined(located, excepted, blocking));
    }

    /// <summary>
    /// § 107.39(b), both halves of it: whether the human being is located under one of the
    /// paragraph's two places, which is this entry's, and whether that place can provide reasonable
    /// protection from a falling small unmanned aircraft, which is
    /// <see cref="MapEntries.ReasonableProtection"/>'s assertion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The paragraph's relative clause binds the two, so the answer is about <b>one</b> place: the
    /// one the human being is located under or inside. A human being located under neither is a case
    /// the paragraph does not reach at all — the first conjunct is false, so (b) is not met whatever
    /// anybody asserts about a structure or a vehicle elsewhere — and the assertion is then not
    /// asked, so a plain overflight is answerable by a caller who has none to make. That is the same
    /// shape <see cref="Yielding"/> gives § 107.37(a): an engine told the object is none of the
    /// three named kinds decides the case without reaching the open term at all.
    /// </para>
    /// <para>
    /// Where the human being <i>is</i> located under one of the two, the assertion is asked, and it
    /// must be about that same place. An assertion about the other place is not an answer about this
    /// one: <see cref="MapEntries.ReasonableProtection"/>'s note distributes the standard "for each
    /// of a covered structure and a stationary vehicle" and forbids defaulting it "to true when
    /// absent", so a caller who asserts the standard for a stationary vehicle on site while the
    /// human being is under a carport has not answered this question. The engine will not read the
    /// one as the other, will not default the unasserted place in either direction, and will not
    /// pick a place for the caller — which is why the refusal is an
    /// <see cref="ArgumentException"/> naming both places rather than a verdict or an unresolved
    /// result: the corpus gave the engine the means to proceed and the caller owes the value for the
    /// place in hand (correspondence row 8).
    /// </para>
    /// </remarks>
    private static ExceptedCaseOutcome LocatedUnderAPlaceThatProtects(
        HumanBeingLocation located,
        Shelter? shelter,
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        if (located.Place is not { } place)
        {
            return new ExceptedCaseOutcome(
                MapEntries.ReasonableProtection,
                ParagraphB,
                Met: false,
                $"the human being is {located}, so § 107.39(b) does not reach this operation and the standard it "
                + "states was not asked",
                Reason: null);
        }

        return Protection.Reasonable(shelter, waiver, assertions).Match(
            protection => protection.Shelter == place
                ? new ExceptedCaseOutcome(
                    MapEntries.ReasonableProtection, ParagraphB, protection.Holds, protection.ToString(), null)
                : throw new ArgumentException(
                    $"resolving the map entry '{MapEntries.OverHumanBeings.Id}' "
                    + $"[{MapEntries.OverHumanBeings.Locator.Citation}] needs the map entry "
                    + $"'{MapEntries.ReasonableProtection.Id}' "
                    + $"[{MapEntries.ReasonableProtection.Locator.Citation}] asserted about the place the human "
                    + $"being is located: the human being is {located}, and the standard was asserted about "
                    + $"{protection.Shelter}. § 107.39(b) requires the human being to be located under or inside "
                    + "the very thing that can provide reasonable protection, and the assertion is made for each "
                    + "of a covered structure and a stationary vehicle, so the engine does not read an assertion "
                    + "about one as an assertion about the other",
                    nameof(Requests.OverHumanBeingsRequest.Shelter)),
            unresolved => new ExceptedCaseOutcome(
                MapEntries.ReasonableProtection, ParagraphB, null, unresolved.Attempted, unresolved.Reason));
    }

    /// <summary>
    /// One case's answer, as this entry records it: the verdict <paramref name="met"/> reads off
    /// what the answering entry resolved, or the reason and the account of its decline.
    /// </summary>
    private static ExceptedCaseOutcome Outcome<T>(
        MapEntry entry,
        string paragraph,
        Resolution<T> resolution,
        Func<T, bool?> met)
        where T : notnull =>
        resolution.Match(
            value => new ExceptedCaseOutcome(entry, paragraph, met(value), value.ToString() ?? string.Empty, null),
            unresolved => new ExceptedCaseOutcome(entry, paragraph, null, unresolved.Attempted, unresolved.Reason));

    /// <summary>
    /// The verdict this entry reads off an entry that resolves an untyped value: none.
    /// </summary>
    /// <remarks>
    /// <see cref="MapEntries.DirectParticipation"/> and <see cref="MapEntries.SubpartDCategories"/>
    /// declare no finding, because neither resolves one: the first over an undefined term, the
    /// second because the map puts it out of scope. Both are still asked, and if one ever resolves
    /// something this entry records the case as unsettled with that entry's own account beside it —
    /// it does not guess a verdict out of an <see cref="object"/>, and it does not fail. The map
    /// would have to give the entry a verdict to read before this entry could read one.
    /// </remarks>
    private static bool? NoVerdictToRead(object value) => null;

    /// <summary>
    /// This entry's own decline for an operation no case settled: it names the entry the caller
    /// asked about and every case that did not answer, with that case's own reason, and it carries
    /// the first such case's reason and cites that case's locator.
    /// </summary>
    /// <remarks>
    /// It is not a constituent's decline handed back. The reason and the locator are the blocking
    /// case's, because the question that blocks the answer is that entry's question and a citation
    /// should lead to where that question is; what was attempted is this entry's, so that a caller
    /// who asked about § 107.39 can tell this decline from the decline of the entry it names. Each
    /// unsettled case is named with the reason it gave, so that (c)'s
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> stays visible as that entry's rather than
    /// being flattened into the one reason an <see cref="UnresolvedResult"/> can carry.
    /// </remarks>
    private static UnresolvedResult Undetermined(
        HumanBeingLocation located,
        IReadOnlyList<ExceptedCaseOutcome> excepted,
        ExceptedCaseOutcome blocking)
    {
        var unsettled = excepted
            .Where(outcome => outcome.Met is null)
            .Select(outcome =>
                $"'{outcome.Entry.Id}' [{outcome.Entry.Locator.Citation}]"
                + (outcome.Reason is { } reason ? $" ({reason})" : string.Empty))
            .ToList();

        return new UnresolvedResult(
            blocking.Reason ?? UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.OverHumanBeings.Id}' prohibits operating a small unmanned "
            + $"aircraft over a human being {located}: § 107.39 prohibits it unless one of the three cases it "
            + "states is met, no case this engine answered is met, and the map "
            + $"{(unsettled.Count == 1 ? "entry" : "entries")} {string.Join(" and ", unsettled)} did not answer "
            + $"whether the {(unsettled.Count == 1 ? "case it states is" : "cases they state are")} met",
            blocking.Entry.Locator);
    }
}
