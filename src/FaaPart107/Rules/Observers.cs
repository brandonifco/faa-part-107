using System.Collections.Immutable;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// One of the requirements § 107.33 conjoins, as the entry that answers it answered it on this
/// operation: met, not met, or undetermined.
/// </summary>
/// <remarks>
/// <para>
/// Every field but <see cref="Paragraph"/> is the answering entry's own. <see cref="Met"/> is what
/// that entry resolved, never a verdict re-derived here, and <see cref="Account"/> is what it said
/// — its resolved outcome, or, where it did not resolve, what it recorded as attempted.
/// </para>
/// <para>
/// <see cref="Answering"/> is not always the entry that <em>states</em> the requirement.
/// § 107.33(a) and (c) are their own map entries and answer themselves; § 107.33(b) is stated by
/// <see cref="MapEntries.VisualObserverConditions"/> itself — no entry in the map carries that
/// paragraph's locator — and what answers it is the entry its own cross-reference points at,
/// <see cref="MapEntries.VisualLineOfSight"/>. So a (b) outcome cites § 107.31, which is where the
/// question that decided it lives.
/// </para>
/// </remarks>
/// <param name="Paragraph">The paragraph of § 107.33 this requirement is, under the section's own letter.</param>
/// <param name="Answering">The map entry this engine asked, as the map has it.</param>
/// <param name="Met">
/// What that entry answered: true where the requirement is met, false where it is not, and null
/// where that entry did not answer it.
/// </param>
/// <param name="Account">
/// What that entry said: its outcome where it resolved, and what it recorded as attempted where it
/// did not.
/// </param>
/// <param name="Reason">Why that entry did not resolve, as it gave it; null where it resolved.</param>
public sealed record RequirementOutcome(
    string Paragraph,
    MapEntry Answering,
    bool? Met,
    string Account,
    UnresolvedReason? Reason)
{
    /// <summary>This requirement's verdict in words: met, not met, or undetermined.</summary>
    public string Verdict => Met switch
    {
        true => "met",
        false => "not met",
        null => "undetermined",
    };

    /// <summary>Where this requirement is stated: <c>§ 107.33</c> plus the paragraph's letter.</summary>
    public string Citation => $"§ 107.33{Paragraph}";

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Citation} {Verdict}, on the map entry '{Answering.Id}' [{Answering.Locator.Citation}]";
}

/// <summary>
/// Whether § 107.33's requirements are met: <see cref="MapEntries.VisualObserverConditions"/>, "If
/// a visual observer is used during the aircraft operation, all of the following requirements must
/// be met: …".
/// </summary>
/// <remarks>
/// The section states nothing of its own but the condition it applies under, the conjunction of its
/// three paragraphs, and paragraph (b). This finding is exactly that: what the caller stated about
/// the condition, and each requirement as the entry that answers it answered it.
/// </remarks>
/// <param name="Use">Whether a visual observer is used during the aircraft operation, as the caller stated it.</param>
/// <param name="SectionApplies">
/// True when the chapeau's condition is satisfied, so § 107.33 states requirements about this
/// operation. Computed from <paramref name="Use"/> by <see cref="Observers.Conditions"/>, and never
/// from an absence.
/// </param>
/// <param name="Requirements">
/// The requirements the section conjoins, in the section's own paragraph order, each as the entry
/// that answers it answered it. Empty where the section does not apply, because the section then
/// states none.
/// </param>
/// <param name="Waiver">The caller's § 107.33 waiver statement the finding was resolved under, recorded with it.</param>
public sealed record VisualObserverConditionsFinding(
    VisualObserverUse Use,
    bool SectionApplies,
    IReadOnlyList<RequirementOutcome> Requirements,
    WaiverStatement Waiver)
{
    /// <summary>What the caller stated about the chapeau's condition, checked to be present.</summary>
    public VisualObserverUse Use { get; } = Use ?? throw new ArgumentNullException(nameof(Use));

    /// <summary>The requirements, checked to be present.</summary>
    public IReadOnlyList<RequirementOutcome> Requirements { get; } =
        Requirements ?? throw new ArgumentNullException(nameof(Requirements));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>
    /// Whether all of the requirements § 107.33 states are met — <b>null where the section does not
    /// apply</b>, because it then states no requirement to meet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three answers, and the third is not either of the other two. Where no visual observer is
    /// used, § 107.33 imposes nothing on the operation: that is not "the requirements are met", and
    /// it is not "they are not met" either. <see cref="SectionApplies"/> stands beside this property
    /// and says which case it is, so a null here is never the "undetermined" that
    /// <see cref="RequirementOutcome.Met"/>'s null means: a requirement this engine could not
    /// answer makes the whole entry decline and produces no finding at all.
    /// </para>
    /// <para>
    /// Where the section does apply, this is computed from what the answering entries returned on
    /// this operation and is not a constant of this entry. No situation reachable today makes it
    /// true, because § 107.33(a) is answered by <c>effective-communication</c>, whose question the
    /// map holds open; that is a fact about that entry and not about this one, and if it ever
    /// resolves a requirement met this property follows it with nothing changed here.
    /// </para>
    /// </remarks>
    public bool? AllRequirementsMet =>
        SectionApplies ? Requirements.All(requirement => requirement.Met == true) : null;

    /// <summary>Where the rule is stated: <c>§ 107.33</c>, the whole section.</summary>
    public SourceLocator Authority => MapEntries.VisualObserverConditions.Locator;

    /// <summary>Whether this finding is the same as <paramref name="other"/>, comparing the requirements by element.</summary>
    /// <param name="other">The other finding.</param>
    /// <returns>True when both state the same thing about the chapeau's condition under the same waiver statement, and carry the same requirements answered the same way, in the same order.</returns>
    /// <remarks>
    /// A record's generated equality would compare <see cref="Requirements"/> with
    /// <c>EqualityComparer&lt;IReadOnlyList&lt;RequirementOutcome&gt;&gt;.Default</c>, which is the
    /// identity of the list object, so two resolutions of the same request would be unequal.
    /// Determinism is about what the engine says, so equality is by element (<c>AGENTS.md</c> §8) —
    /// the same reason <see cref="MultipleAircraftFinding.Equals(MultipleAircraftFinding)"/> gives
    /// for overriding its own.
    /// </remarks>
    public bool Equals(VisualObserverConditionsFinding? other) =>
        other is not null
        && Use == other.Use
        && SectionApplies == other.SectionApplies
        && Waiver == other.Waiver
        && Requirements.SequenceEqual(other.Requirements);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Use);
        hash.Add(SectionApplies);
        hash.Add(Waiver);
        foreach (var requirement in Requirements)
        {
            hash.Add(requirement);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        SectionApplies
            ? $"{Use}, and all of the requirements § 107.33 states are "
                + $"{(AllRequirementsMet == true ? "met" : "not met")} [{Authority}]: "
                + $"{string.Join(", ", Requirements)}; {Waiver}"
            : $"{Use}, so § 107.33 states no requirement about this operation [{Authority}]; {Waiver}";
}

/// <summary>
/// § 107.33, "Visual observer": the condition the section applies under, paragraph (b), and the
/// conjunction of its three paragraphs.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for what the section is about rather than for the entry, because a type named
/// <c>VisualObserverConditions</c> collides with the <c>VisualObserverConditions</c> the generated
/// <see cref="Handlers"/> partial declares — the reason <see cref="Yielding"/> is named as it is.
/// </para>
/// <para>
/// <b>The chapeau is this entry's, and it is a condition.</b> "If a visual observer is used during
/// the aircraft operation, all of the following requirements must be met:" is in this entry's
/// evidence and in no other's. <c>effective-communication</c> (§ 107.33(a)) and
/// <c>observer-coordination</c> (§ 107.33(c)) each state their own paragraph and answer it as
/// though it applied; neither quotes the condition, and neither could, because the condition is
/// stated once for all three. So this entry carries it over all of them: where no visual observer
/// is used, the section states no requirement and the constituents are never asked — which is a
/// different answer from the requirements being met, and <see cref="VisualObserverConditionsFinding"/>
/// keeps the two apart.
/// </para>
/// <para>
/// <b>Paragraph (b) is this entry's too, and it is the only requirement this entry states.</b> The
/// entry's note: "What survives the split is (b) and the condition of application: if a visual
/// observer is used, the remote pilot in command must ensure the observer can see the aircraft in
/// the manner § 107.31 specifies — <b>computable once unaided-visual-contact is asserted, reached
/// through visual-line-of-sight</b>." No entry in the map carries § 107.33(b)'s locator, and the
/// entry's own <c>crossReferences</c> row resolves "in the manner specified in § 107.31" to
/// <c>visual-line-of-sight</c>, "the entry that states § 107.31 whole; it is the same edge as
/// dependsOn". So (b) is answered here, from that entry, and from the half of its finding the note
/// names: § 107.31(a)'s ability, which is <c>unaided-visual-contact</c>'s assertion carried
/// unchanged — the paragraph that says who "must be able to see the unmanned aircraft", and which
/// names "the visual observer (if one is used)" among them. § 107.31(b)'s enumeration of who must
/// exercise that ability is § 107.31's own requirement, which <c>visual-line-of-sight</c> answers
/// on its own account; (b) here asks whether the observer <em>is able to see</em>, which is (a).
/// Nothing about the ability is re-asked or re-decided: the assertion arrives under
/// <c>unaided-visual-contact</c>'s own entry id and this entry never declares surface for it.
/// </para>
/// <para>
/// <b>Every requirement is asked, and the answer follows what each one returned.</b> Nothing here
/// records in advance which of them can resolve. § 107.33(a) declines today because part 107 does
/// not define "effective", and this entry does not assert that: it calls the entry and reads what
/// came back, so a question the map later settles changes this entry's answer with nothing changed
/// here. Where a requirement's openness decides the outcome, the decline emitted is <b>this
/// entry's own</b>, naming the entry the caller asked about and the entry whose question blocks it,
/// and citing that entry's locator — the shape <c>docs/decisions/0001</c> records and #78 settles.
/// </para>
/// <para>
/// <b>"All of the following requirements must be met", so one requirement resolved unmet settles
/// it.</b> A requirement the engine resolved not met makes the conjunction false whatever an
/// undetermined one would have said. That is the chapeau's own word "all", not a reading of a
/// question the map holds open, and it is the reading <see cref="Weather"/> already applies to a
/// conjunction with an open conjunct: "one conjunct that is false on every reading settles it,
/// whatever the other is".
/// </para>
/// <para>
/// <b>The gate is this entry's own, and it runs first.</b> § 107.205(d) lists the whole of
/// § 107.33, so this entry and both of its § 107.33 constituents are suspended by the same row and
/// answer under the same statement. Consulting it here, before the condition is read and before any
/// constituent is asked, is what keeps the decline in this entry's name rather than
/// <c>effective-communication</c>'s: the two would otherwise cite the same § 107.205 and only what
/// was attempted would tell them apart. § 107.31 is a <em>different</em> listed regulation
/// (§ 107.205(c)), so the statement <c>visual-line-of-sight</c> is asked under is a second statement
/// the caller makes, demanded and never defaulted, and a waiver of § 107.31 leaves § 107.33(b)
/// undetermined rather than waiving anything in § 107.33.
/// </para>
/// </remarks>
public static class Observers
{
    /// <summary>
    /// The regulation § 107.205(d) lists that states the entry: the whole of § 107.33 — the same
    /// regulation, and the same § 107.205(d) row, that suspends <c>effective-communication</c> and
    /// <c>observer-coordination</c>. One waiver statement therefore reaches all three, and they are
    /// told apart by which entry the decline names, not by which regulation was waived.
    /// </summary>
    public const string Regulation = Communication.Regulation;

    /// <summary>
    /// The regulation § 107.205(c) lists that states § 107.33(b)'s cross-reference target,
    /// <c>visual-line-of-sight</c>: the whole of § 107.31. It is not the same regulation as
    /// <see cref="Regulation"/>, and a statement about one is never read as a statement about the
    /// other.
    /// </summary>
    public const string CrossReferencedRegulation = LineOfSight.Regulation;

    /// <summary>
    /// § 107.33(b), verbatim — the one requirement this entry states itself, and the only paragraph
    /// of the section no other map entry carries.
    /// </summary>
    /// <remarks>
    /// Quoted, not interpreted, and carried so that what this entry answers on its own account is
    /// legible beside the answer — as <see cref="LineOfSight.Combinations"/> and
    /// <see cref="Coordination.Purposes"/> carry their paragraphs' enumerations. (a) and (c) are not
    /// quoted here: each is its own entry's evidence, and that entry prints it.
    /// </remarks>
    public const string ParagraphB =
        "(b) The remote pilot in command must ensure that the visual observer is able to see the "
        + "unmanned aircraft in the manner specified in § 107.31.";

    /// <summary>
    /// The three paragraphs § 107.33's chapeau conjoins, under the section's own letters and in the
    /// section's own order.
    /// </summary>
    /// <remarks>
    /// The order is the section's, not this entry's <c>dependsOn</c> order. <c>dependsOn</c> orders
    /// the work — <c>visual-line-of-sight</c>, <c>effective-communication</c>,
    /// <c>observer-coordination</c> — while "all of the following requirements" enumerates the
    /// paragraphs as the corpus prints them. Either is deterministic; this one is what a reader of
    /// § 107.33 sees, and it fixes which entry a decline cites when more than one is undetermined.
    /// </remarks>
    public static ImmutableArray<string> Paragraphs { get; } = ["(a)", "(b)", "(c)"];

    /// <summary>
    /// <see cref="MapEntries.VisualObserverConditions"/>: whether § 107.33's requirements are met on
    /// the operation the caller states.
    /// </summary>
    /// <remarks>
    /// The gate first, against this entry; then the chapeau's condition, which the caller states and
    /// this engine never infers; then each requirement, in the section's paragraph order, answered
    /// by the entry the map points at. The conjunction is read off those answers: any requirement
    /// resolved not met makes the whole not met; all of them resolved met makes it met; otherwise
    /// the first requirement that was not answered blocks, and this entry declines naming itself and
    /// that entry.
    /// </remarks>
    /// <param name="use">Whether a visual observer is used during the aircraft operation, as the caller states it. Never inferred; required once the entry is reachable, and demanded after the gate (<c>docs/decisions/0008</c>).</param>
    /// <param name="exercise">Who exercised § 107.31(a)'s ability throughout the entire flight, as the caller states it: <c>visual-line-of-sight</c>'s input, for § 107.33(b). Required once the entry is reachable, and demanded after the gate.</param>
    /// <param name="waiver">Whether a waiver of § 107.33 is in force, as the caller states it.</param>
    /// <param name="visualLineOfSightWaiver">Whether a waiver of § 107.31 is in force, as the caller states it: the statement <c>visual-line-of-sight</c> is asked under, for § 107.33(b). It is § 107.31's gate and not this entry's, so it too is required only once <em>this</em> entry is reachable, and demanded after this entry's gate.</param>
    /// <param name="assertions">What the caller asserts, carrying <c>unaided-visual-contact</c>'s and <c>observer-coordination</c>'s values. Never defaulted.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver of § 107.33 is in force, naming this entry; otherwise, where no requirement is
    /// resolved unmet and one was not answered, this entry's own decline, carrying that entry's
    /// reason and citing its locator.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// A visual observer is used, no waiver is in force, and the caller asserted nothing for
    /// <c>unaided-visual-contact</c> or <c>observer-coordination</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A waiver statement is about another regulation, or an asserted value is not an
    /// <see cref="Assertion"/>, is about another entry, or is attributed to somebody the map does
    /// not name.
    /// </exception>
    public static Resolution<VisualObserverConditionsFinding> Conditions(
        VisualObserverUse? use,
        ExerciseOfTheAbility? exercise,
        WaiverStatement waiver,
        WaiverStatement? visualLineOfSightWaiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        // This entry's own gate, before anything else. § 107.205(d) lists § 107.33 whole, so the
        // same statement would suspend (a) and (c) a moment later; running it here is what keeps
        // the decline in this entry's name, and stops a fact the waiver has made irrelevant from
        // being demanded — including the three demanded below, which is what
        // docs/decisions/0008 settles for every gated entry of this engine.
        if (Waivers.Suspension(MapEntries.VisualObserverConditions, Regulation, waiver) is { } suspended)
        {
            return Resolution<VisualObserverConditionsFinding>.FromUnresolved(suspended);
        }

        var stated = Demands.Of(
            use, MapEntries.VisualObserverConditions, nameof(Requests.VisualObserverConditionsRequest.Use));
        var exercised = Demands.Of(
            exercise, MapEntries.VisualObserverConditions, nameof(Requests.VisualObserverConditionsRequest.Exercise));
        var sightWaiver = Demands.Of(
            visualLineOfSightWaiver,
            MapEntries.VisualObserverConditions,
            nameof(Requests.VisualObserverConditionsRequest.VisualLineOfSightWaiver));

        // The chapeau's condition. Where it is not satisfied the section states no requirement, so
        // there is nothing to ask and nothing to conjoin — and the finding says that rather than
        // saying the requirements are met.
        if (stated != VisualObserverUse.Used)
        {
            return Resolution<VisualObserverConditionsFinding>.FromValue(
                new VisualObserverConditionsFinding(stated, SectionApplies: false, [], waiver));
        }

        // Every requirement is asked, in the section's paragraph order, and the answer below is read
        // off what each one actually returned. Nothing here records in advance which of them can
        // resolve: an entry whose question the map later settles is followed from these calls, and
        // no test of this entry would have to change for that to happen.
        RequirementOutcome[] requirements =
        [
            Outcome(
                "(a)",
                MapEntries.EffectiveCommunication,
                Communication.Effective(waiver),
                value => value as bool?),
            Outcome(
                "(b)",
                MapEntries.VisualLineOfSight,
                LineOfSight.Maintained(exercised, sightWaiver, assertions),
                finding => finding.Ability.Holds),
            Outcome(
                "(c)",
                MapEntries.ObserverCoordination,
                Coordination.Coordinate(waiver, assertions),
                finding => finding.Holds),
        ];

        var blocking = Array.Find(requirements, requirement => requirement.Met is null);

        // "all of the following requirements must be met": a requirement resolved not met settles
        // the whole of it, whatever an unanswered one would have said, and every requirement
        // answered leaves nothing open.
        return Array.Exists(requirements, requirement => requirement.Met == false) || blocking is null
            ? Resolution<VisualObserverConditionsFinding>.FromValue(
                new VisualObserverConditionsFinding(stated, SectionApplies: true, requirements, waiver))
            : Resolution<VisualObserverConditionsFinding>.FromUnresolved(Undetermined(stated, requirements, blocking));
    }

    /// <summary>
    /// One requirement's answer, as this entry records it: the verdict the answering entry resolved
    /// and the account it gave, or the reason and the account of its decline.
    /// </summary>
    /// <remarks>
    /// The predicate reads the verdict off the entry's own resolved value and never recomputes it.
    /// Where an entry resolves a value this engine has no verdict to read from it — which is what
    /// <c>effective-communication</c> would do if its open question were ever settled into something
    /// other than a boolean — the predicate answers null and the requirement is undetermined, which
    /// declines rather than inventing a verdict or throwing.
    /// </remarks>
    private static RequirementOutcome Outcome<T>(
        string paragraph,
        MapEntry answering,
        Resolution<T> resolution,
        Func<T, bool?> met)
        where T : notnull =>
        resolution.Match(
            value => new RequirementOutcome(paragraph, answering, met(value), value.ToString() ?? string.Empty, null),
            unresolved => new RequirementOutcome(paragraph, answering, null, unresolved.Attempted, unresolved.Reason));

    /// <summary>
    /// This entry's own decline for an operation no requirement settled: it names the entry the
    /// caller asked about and every requirement that went unanswered, with the entry that was asked
    /// for each, and it carries the first of those entries' reason and cites that entry's locator.
    /// </summary>
    /// <remarks>
    /// It is not the constituent's decline handed back. The reason and the locator are the blocking
    /// entry's, because the question that blocks the answer is that entry's question and a citation
    /// should lead to where that question is; what was attempted is this entry's, so that a caller
    /// who asked about § 107.33 is told so by name. Today the blocking entry is almost always
    /// <c>effective-communication</c>, whose own decline is <c>RequiresInterpretation</c> citing
    /// § 107.33(a) — the same reason and the same locator this one carries. So the citation cannot
    /// tell the two apart and <see cref="UnresolvedResult.Attempted"/> is what does, which is the
    /// blind spot <c>docs/decisions/0001</c> records for speed, entered here by the same choice.
    /// </remarks>
    private static UnresolvedResult Undetermined(
        VisualObserverUse use,
        IReadOnlyList<RequirementOutcome> requirements,
        RequirementOutcome blocking)
    {
        var unanswered = requirements
            .Where(requirement => requirement.Met is null)
            .Select(requirement =>
                $"the map entry '{requirement.Answering.Id}' [{requirement.Answering.Locator.Citation}] "
                + $"did not answer {requirement.Citation}")
            .ToList();

        return new UnresolvedResult(
            blocking.Reason ?? UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.VisualObserverConditions.Id}' is met where {use}: "
            + "§ 107.33 requires all of the requirements it states, no requirement this engine resolved is "
            + $"unmet, and {string.Join(" and ", unanswered)}",
            blocking.Answering.Locator);
    }
}
