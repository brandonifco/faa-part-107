using System.Collections.Immutable;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// One of the obligations § 107.49's lead-in conjoins, as the map entry that states it answered it
/// on this operation: done, not done, or undetermined.
/// </summary>
/// <remarks>
/// <para>
/// Every field but <see cref="Paragraph"/> is the answering entry's own. <see cref="Done"/> is what
/// that entry answered, never a verdict re-derived here, and <see cref="Account"/> is what it said —
/// its resolved outcome, or, where it did not resolve, what it recorded as attempted. So nothing the
/// constituents state is restated by <see cref="Preflight"/>, and a constituent whose question the
/// map later settles moves this outcome with nothing changed here.
/// </para>
/// <para>
/// <see cref="Paragraph"/> is here because the citation cannot carry it. § 107.49(e) is two map
/// entries under one locator — <see cref="MapEntries.AttachedObjectSecure"/> and
/// <see cref="MapEntries.AttachedObjectNoAdverseEffect"/> both cite § 107.49(e) — and
/// <see cref="MapEntries.SubpartDCategories"/> is located at <c>subpart D</c> rather than at
/// § 107.49(f), so a reader of a citation alone could not tell which paragraph an outcome is for.
/// </para>
/// <para>
/// An outcome is undetermined exactly where it carries a <see cref="Reason"/>: the two are set
/// together by the factories below and never apart. So a decline built from these never has to
/// invent a reason for an obligation it could not read, which is the fallback <c>#92</c> names in
/// four merged composites.
/// </para>
/// <para>
/// <b>Three verdicts, and "the paragraph states no obligation" is not a fourth.</b> Done, not done
/// and undetermined are the three answers to one question — did the remote pilot in command do what
/// this paragraph requires <em>of this operation</em>. § 107.49(d) and § 107.49(f) each state their
/// obligation under a condition, and where the caller states that condition is not satisfied the
/// paragraph requires nothing of the operation at all: there is no obligation for any of the three
/// verdicts to be about, so no outcome is built for it and the paragraph is simply <b>not
/// conjoined</b> (<see cref="Preflight.Actions"/>, and <c>#99</c>). A fourth verdict here would put a
/// conjunct in <see cref="PreflightActionsFinding.Obligations"/> that
/// <see cref="PreflightActionsFinding.AllDone"/> and the blocking search in
/// <see cref="Preflight.Actions"/> would each then have to except; and folding the case into
/// <see cref="Verdict"/> "undetermined" instead is the one answer that is certainly wrong, because
/// an undetermined conjunct makes this entry decline — so § 107.49 would decline on an operation the
/// caller described completely, over a paragraph nobody was ever owed anything under.
/// </para>
/// </remarks>
public sealed record ObligationOutcome
{
    private ObligationOutcome(MapEntry entry, string paragraph, bool? done, string account, UnresolvedReason? reason)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        Paragraph = paragraph;
        Done = done;
        Account = account;
        Reason = reason;
    }

    /// <summary>The map entry that states this obligation, as the map has it.</summary>
    public MapEntry Entry { get; }

    /// <summary>Which paragraph of § 107.49 the obligation is, in the section's own lettering.</summary>
    public string Paragraph { get; }

    /// <summary>
    /// What the answering entry answered: true where the obligation is done, false where it is not,
    /// and null where that entry did not settle it.
    /// </summary>
    public bool? Done { get; }

    /// <summary>
    /// What that entry said: its outcome where it resolved, and what it recorded as attempted where
    /// it did not.
    /// </summary>
    public string Account { get; }

    /// <summary>
    /// Why the obligation was not settled; null exactly where <see cref="Done"/> is not. It is the
    /// answering entry's own reason and is never widened here:
    /// <see cref="MapEntries.SubpartDCategories"/>' <see cref="UnresolvedReason.OutsideCurrentScope"/>
    /// and <see cref="MapEntries.ControlLinksWorking"/>'s
    /// <see cref="UnresolvedReason.RequiresInterpretation"/> are different answers, and this entry
    /// keeps them apart.
    /// </summary>
    public UnresolvedReason? Reason { get; }

    /// <summary>Where this obligation is stated: <c>§ 107.49</c> plus the paragraph's letter.</summary>
    public string Citation => $"§ 107.49{Paragraph}";

    /// <summary>This obligation's verdict in words: done, not done, or undetermined.</summary>
    public string Verdict => Done switch
    {
        true => "done",
        false => "not done",
        null => "undetermined",
    };

    /// <summary>
    /// The obligation as the answering entry settled it: it resolved a verdict this entry reads.
    /// </summary>
    /// <param name="entry">The map entry that states the obligation.</param>
    /// <param name="paragraph">The paragraph of § 107.49 it is.</param>
    /// <param name="done">What that entry answered.</param>
    /// <param name="account">What that entry said.</param>
    /// <returns>The outcome, carrying no reason.</returns>
    public static ObligationOutcome Answered(MapEntry entry, string paragraph, bool done, string account) =>
        new(entry, paragraph, done, account, null);

    /// <summary>
    /// The obligation as an entry that did not resolve left it: that entry's own reason, and what it
    /// recorded as attempted.
    /// </summary>
    /// <param name="entry">The map entry that states the obligation.</param>
    /// <param name="paragraph">The paragraph of § 107.49 it is.</param>
    /// <param name="account">What that entry recorded as attempted.</param>
    /// <param name="reason">Why it did not resolve, as that entry gave it.</param>
    /// <returns>The outcome, undetermined, carrying that entry's reason.</returns>
    public static ObligationOutcome Unanswered(
        MapEntry entry,
        string paragraph,
        string account,
        UnresolvedReason reason) =>
        new(entry, paragraph, null, account, reason);

    /// <summary>
    /// The obligation as an entry that resolved a value this engine has no verdict to read from left
    /// it: undetermined, <see cref="UnresolvedReason.UnsupportedRule"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Unreachable today, and named rather than defaulted.</b> Three of § 107.49's constituents
    /// declare no finding this entry can read a verdict from —
    /// <see cref="MapEntries.ControlLinksWorking"/>, <see cref="MapEntries.AttachedObjectSecure"/>
    /// and <see cref="MapEntries.SubpartDCategories"/> — and all three resolve nothing at all today:
    /// the first two decline over a term the corpus leaves undefined, and the third is
    /// <c>scope: out</c>. So no request reaches this factory. It exists because those entries
    /// resolve <c>object</c>, and an entry that begins resolving a value whose shape this entry does
    /// not know is a possibility the engine must answer rather than guess at or crash on.
    /// </para>
    /// <para>
    /// The reason is the kernel's own: <see cref="UnresolvedReason.UnsupportedRule"/> is "the corpus
    /// defines this; the engine has not implemented it yet", which is exactly the case — a
    /// constituent answered and this composite cannot read the answer. It is deliberately <b>not</b>
    /// <see cref="UnresolvedReason.RequiresInterpretation"/>, which is "the corpus is genuinely
    /// ambiguous here": four merged composites substitute that reason for this case, and <c>#92</c>
    /// records it as naming a gap in the engine as a gap in the corpus. That issue owns the sweep and
    /// the decision record; this entry does not pre-empt it, and does not add a fifth instance of
    /// what it names.
    /// </para>
    /// </remarks>
    /// <param name="entry">The map entry that states the obligation.</param>
    /// <param name="paragraph">The paragraph of § 107.49 it is.</param>
    /// <param name="account">What that entry resolved, as it printed it.</param>
    /// <returns>The outcome, undetermined.</returns>
    public static ObligationOutcome Unreadable(MapEntry entry, string paragraph, string account) =>
        new(entry, paragraph, null, account, UnresolvedReason.UnsupportedRule);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Citation} {Verdict}, on the map entry '{Entry.Id}' [{Entry.Locator.Citation}]"
        + (Reason is { } reason ? $" ({reason})" : string.Empty);
}

/// <summary>
/// Whether the remote pilot in command did, prior to flight, all of what § 107.49 requires:
/// <see cref="MapEntries.PreflightActions"/>, "Prior to flight, the remote pilot in command must:
/// (a) … (b) … (c) … (d) … (e) … and (f) …".
/// </summary>
/// <remarks>
/// The section states no obligation of its own. It conjoins the obligations its paragraphs state,
/// each of which the map carries as its own entry, and this finding is exactly that: what the caller
/// stated about the one condition § 107.49(f) applies under, and each obligation as the entry that
/// states it answered it.
/// </remarks>
/// <param name="Operation">
/// Whether the operation will be conducted over human beings under subpart D of this part, as the
/// caller stated it: § 107.49(f)'s condition. § 107.49(d)'s condition is deliberately <b>not</b>
/// beside it — that one is <see cref="MapEntries.SufficientAvailablePower"/>'s, and
/// <see cref="SufficientAvailablePowerFinding.Power"/> is where it is recorded, the same way
/// <see cref="VisualObserverConditionsFinding"/> carries § 107.33's own chapeau and not
/// <c>visual-line-of-sight</c>'s waiver statement.
/// </param>
/// <param name="Obligations">
/// The obligations the section conjoins, in the section's own paragraph order, each as the entry
/// that states it answered it. The two paragraphs that state a condition are among them exactly
/// where that condition is satisfied, because each states no obligation otherwise: § 107.49(f) on
/// the condition this entry carries, and § 107.49(d) on the one its own entry carries and tests.
/// </param>
public sealed record PreflightActionsFinding(SubpartDOperation Operation, IReadOnlyList<ObligationOutcome> Obligations)
{
    /// <summary>What the caller stated about § 107.49(f)'s condition, checked to be present.</summary>
    public SubpartDOperation Operation { get; } = Operation ?? throw new ArgumentNullException(nameof(Operation));

    /// <summary>The obligations, checked to be present.</summary>
    public IReadOnlyList<ObligationOutcome> Obligations { get; } =
        Obligations ?? throw new ArgumentNullException(nameof(Obligations));

    /// <summary>
    /// Whether the remote pilot in command did all of them prior to flight: true only where every
    /// obligation the section states <em>about this operation</em> was answered done.
    /// </summary>
    /// <remarks>
    /// This is computed from what the constituents answered on this operation, and is not a constant
    /// of this entry. No situation this engine can resolve makes it true today, because
    /// <see cref="MapEntries.ControlLinksWorking"/> and <see cref="MapEntries.AttachedObjectSecure"/>
    /// resolve nothing at all — part 107 defines neither "working properly" nor "secure" — and an
    /// obligation left undetermined makes this entry decline rather than produce a finding. That is a
    /// fact about those entries rather than about this one, and if they ever answer an obligation
    /// done this property follows them with nothing changed here.
    /// </remarks>
    public bool AllDone => Obligations.All(obligation => obligation.Done == true);

    /// <summary>Where the rule is stated: <c>§ 107.49</c>.</summary>
    public SourceLocator Authority => MapEntries.PreflightActions.Locator;

    /// <summary>Whether this finding is the same as <paramref name="other"/>, comparing the obligations by element.</summary>
    /// <param name="other">The other finding.</param>
    /// <returns>True when both state the same thing about § 107.49(f)'s condition and carry the same obligations answered the same way, in the same order.</returns>
    /// <remarks>
    /// A record's generated equality would compare <see cref="Obligations"/> with
    /// <c>EqualityComparer&lt;IReadOnlyList&lt;ObligationOutcome&gt;&gt;.Default</c>, which is the
    /// identity of the list object, so two resolutions of the same request would be unequal.
    /// Determinism is about what the engine says, so equality is by element (<c>AGENTS.md</c> §8) —
    /// the same reason <see cref="MultipleAircraftFinding.Equals(MultipleAircraftFinding)"/> gives
    /// for overriding its own.
    /// </remarks>
    public bool Equals(PreflightActionsFinding? other) =>
        other is not null
        && Operation == other.Operation
        && Obligations.SequenceEqual(other.Obligations);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Operation);
        foreach (var obligation in Obligations)
        {
            hash.Add(obligation);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Operation}, and the remote pilot in command {(AllDone ? "did" : "did not do")} all of what § 107.49 "
        + $"requires prior to flight [{Authority}]: {string.Join(", ", Obligations)}";
}

/// <summary>
/// § 107.49, "Preflight familiarization, inspection and actions":
/// <see cref="MapEntries.PreflightActions"/>, the conjunction its lead-in states over the
/// obligations its paragraphs make.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for what the section is about rather than for the entry, because a type named
/// <c>PreflightActions</c> collides with the <c>PreflightActions</c> the generated
/// <see cref="Handlers"/> partial declares — the reason <see cref="Yielding"/>,
/// <see cref="Compliance"/>, <see cref="Overflight"/> and <see cref="Observers"/> are named as they
/// are.
/// </para>
/// <para>
/// <b>This entry states nothing of its own but the conjunction and § 107.49(f)'s condition.</b> The
/// map splits every obligation out, and its note says what is left: "(a), (b) and (d) are
/// assertions, (c) is a gap, and (e) is one of each … What is left here is the conjunction — prior
/// to flight the remote pilot in command must do all of them — plus (f), which is reachable only
/// through subpart D and is a dependency on <c>subpart-d-categories</c> rather than a second reason
/// on this entry." So nothing here restates a constituent: no list of what an assessment must
/// include, no set of matters a participant is informed about, no measure of a working control link,
/// no endurance figure, no fastening standard, and no subpart D category.
/// </para>
/// <para>
/// <b>The lead-in adds no input.</b> "Prior to flight, the remote pilot in command must:" names one
/// person and one time, and neither is a fact this entry takes from a caller. The person is already
/// the constituents': the map reads the lead-in into each assertion entry's <c>assertedBy</c> —
/// <c>preflight-risk-assessment</c>, <c>participant-briefing</c>, <c>sufficient-available-power</c>
/// and <c>attached-object-no-adverse-effect</c> each say "Who asserts it is named in § 107.49's
/// lead-in" (rules-factory decision 0025) — so <see cref="Assertions.Stated"/> refuses an assertion
/// attributed to anybody else, on each constituent's own row. Unlike § 107.51's introductory text,
/// which names two people and which <see cref="Compliance"/> therefore asks the caller to choose
/// between, this lead-in names exactly one and leaves nothing to choose. "Prior to flight" is the
/// time the obligations it conjoins are owed by, and each of those obligations is reported done or
/// not done by the person the lead-in binds; an input beside that would let a caller assert both
/// that an obligation was done and that it was not done in time, which is a second verdict the map
/// gives no entry and this entry does not invent. The section's <c>clarity</c> is <c>clear</c> and
/// it records no ambiguity, so there is no open question here for an input to stand in for.
/// </para>
/// <para>
/// <b>§ 107.49(f) is a condition, and it is this entry's own to test.</b> "If the operation will be
/// conducted over human beings under subpart D of this part" is in this entry's evidence and in no
/// constituent's — <see cref="MapEntries.SubpartDCategories"/>' evidence is subpart D's own
/// applicability sentence — so, like § 107.33's chapeau in <see cref="Observers"/>, the condition is
/// this entry's to carry, stated by the caller and never inferred (<see cref="SubpartDOperation"/>).
/// Where it is not satisfied the paragraph states no obligation about the operation, so there is
/// nothing to conjoin and the entry it defers to is not asked at all; where it is satisfied, what
/// § 107.49(f) then requires is that entry's, and this engine asks it rather than deciding it.
/// </para>
/// <para>
/// <b>§ 107.49(d) is a condition too, and it is emphatically not this entry's to test.</b> "If the
/// small unmanned aircraft is powered" stands in this entry's evidence as well — the map quotes
/// § 107.49 entire — but it also stands in <see cref="MapEntries.SufficientAvailablePower"/>'s, and
/// it is the only antecedent of that kind in this map that is in two entries' evidence
/// (<see cref="AvailablePower"/>). Being in the constituent's evidence is what made it the
/// constituent's to implement (<c>#95</c>), and an antecedent implemented in one place must not be
/// implemented a second time in another: two implementations of one condition are two answers that
/// can disagree, and the defect <c>#99</c> reports is exactly that disagreement — this entry reached
/// § 107.49(d) through the shared <see cref="Assertions.Stated"/>, never saw the condition, and
/// demanded an assertion of an unpowered aircraft that <c>sufficient-available-power</c> had already
/// said nobody owes. So (d) is reached here through <see cref="AvailablePower.Enough"/>, that
/// entry's own rule, condition and all, and nothing in this class compares an
/// <see cref="AircraftPower"/> with anything. The caller's statement of it is an input to this
/// entry's request only because the caller has to state it somewhere and this is the entry being
/// asked; it is handed to that rule unread, the way <see cref="Observers.Conditions"/> hands
/// <c>visual-line-of-sight</c>'s § 107.31 waiver statement to <see cref="LineOfSight.Maintained"/>
/// without reading it.
/// </para>
/// <para>
/// <b>What the two conditions do to the conjunction is the same, and it is not a verdict.</b> Where
/// either condition is not satisfied the paragraph states no obligation about the operation, so
/// there is nothing of it to conjoin and no <see cref="ObligationOutcome"/> is built for it at all.
/// That is not an obligation left undetermined: an undetermined conjunct makes this entry decline,
/// and a paragraph that asks nothing of the operation must not make a completely described operation
/// decline. What differs between them is only <em>where</em> the condition is read — this entry for
/// (f), because there is no constituent whose evidence carries it; that constituent's own rule for
/// (d), because there is.
/// </para>
/// <para>
/// <b>What (f) being out of scope does to this entry, the map settles.</b> Its note calls (f) "a
/// dependency on <c>subpart-d-categories</c> rather than a second reason on this entry", the same
/// words <c>over-human-beings</c>' note uses of the same entry. So
/// <see cref="UnresolvedReason.OutsideCurrentScope"/> stays that entry's: it is recorded on (f)'s
/// outcome and named in the account of any decline, and it never becomes this entry's reason while
/// an in-scope obligation is also unsettled. The obligations are held in § 107.49's own paragraph
/// order, so the obligation that blocks — and whose reason and locator the decline carries — is the
/// first unsettled one in that order, which is (c) and its
/// <see cref="UnresolvedReason.RequiresInterpretation"/>. A caller is not handed
/// <see cref="UnresolvedReason.OutsideCurrentScope"/> for a section this engine maps.
/// </para>
/// <para>
/// <b>Every obligation is asked, and the answer follows what each one returned.</b> Nothing here
/// records in advance which of them can answer. (c) and the first conjunct of (e) decline today
/// because part 107 defines neither "working properly" nor "secure", and this entry does not assert
/// that: it calls each entry and reads what came back, so a question the map later settles changes
/// this entry's answer with nothing changed here and no test of this entry rewritten.
/// </para>
/// <para>
/// <b>"must do all of them", so one obligation answered not done settles it.</b> An obligation the
/// engine resolved not done makes the conjunction false whatever an undetermined one would have
/// said. That is the lead-in's own "must" over its whole list, and it is the reading
/// <see cref="Compliance"/> applies to § 107.51's "all of the following" and <see cref="Observers"/>
/// to § 107.33's "all of the following requirements must be met".
/// </para>
/// <para>
/// <b>An obligation that does not answer is reported as this entry's own decline.</b> The decline
/// emitted is this entry's, naming the entry the caller asked about and every obligation that went
/// unanswered with the reason that entry gave, and citing the blocking entry's locator — the shape
/// <c>docs/decisions/0001</c> records and <c>#78</c> settles. A constituent's
/// <see cref="UnresolvedResult"/> is never handed back: a decline of this entry must be
/// distinguishable from a decline of <c>control-links-working</c>, and what carries that distinction
/// is <see cref="UnresolvedResult.Attempted"/>, since the reason and the citation are deliberately
/// the blocking entry's.
/// </para>
/// <para>
/// <b>There is no waiver gate.</b> § 107.205 lists the regulations a certificate of waiver may
/// authorize a deviation from and does not list § 107.49, so this entry has no <c>suspendedBy</c> in
/// the map, exactly as its six in-scope constituents have none, and nothing about a waiver is
/// demanded or read here.
/// </para>
/// </remarks>
public static class Preflight
{
    /// <summary>
    /// § 107.49's lead-in, verbatim — the conjunction this entry states, and the sentence no
    /// constituent's evidence carries.
    /// </summary>
    /// <remarks>
    /// Quoted, not interpreted, and carried so that what this entry states on its own account is
    /// legible beside the answer — as <see cref="Observers.ParagraphB"/> carries § 107.33(b). The
    /// paragraphs are not quoted here: each is its own entry's evidence, and that entry prints it.
    /// </remarks>
    public const string LeadIn = "Prior to flight, the remote pilot in command must:";

    /// <summary>
    /// The obligations § 107.49's lead-in conjoins, under the section's own letters and in the
    /// section's own order.
    /// </summary>
    /// <remarks>
    /// The order is the section's, not this entry's <c>dependsOn</c> order. The two coincide here —
    /// the map lists the dependencies as the corpus prints the paragraphs — but what fixes the order
    /// below is "(a) … (b) … (c) … (d) … (e) … (f)", because that is what a reader of § 107.49 sees
    /// and because it is what decides which entry a decline cites when more than one is unsettled.
    /// (e) appears twice: the map splits the paragraph's two coordinate conjuncts into
    /// <c>attached-object-secure</c> and <c>attached-object-no-adverse-effect</c>, which share the
    /// locator § 107.49(e), and the section prints "is secure" before "does not adversely affect".
    /// </remarks>
    public static ImmutableArray<string> Paragraphs { get; } = ["(a)", "(b)", "(c)", "(d)", "(e)", "(e)", "(f)"];

    /// <summary>
    /// <see cref="MapEntries.PreflightActions"/>: whether the remote pilot in command did, prior to
    /// flight, all of what § 107.49 requires on the operation the caller states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each obligation is asked, in the section's own paragraph order, and each is answered by the
    /// entry the map gives it. The conjunction is then read off those answers: any obligation
    /// answered not done makes the whole not done; all of them answered done makes it done;
    /// otherwise the first obligation that was not answered blocks, and this entry declines naming
    /// itself and every obligation that went unanswered.
    /// </para>
    /// <para>
    /// Two paragraphs state their obligation under a condition, and where the caller states that
    /// condition is not satisfied the paragraph is not among the obligations at all: § 107.49(f) on
    /// the condition this entry's own evidence carries and this rule tests, and § 107.49(d) on the
    /// condition <see cref="MapEntries.SufficientAvailablePower"/>'s evidence carries and
    /// <see cref="AvailablePower.Enough"/> tests. Not conjoined is not undetermined — see
    /// <see cref="ObligationOutcome"/>.
    /// </para>
    /// </remarks>
    /// <param name="operation">
    /// Whether the operation will be conducted over human beings under subpart D of this part, as
    /// the caller states it: § 107.49(f)'s own condition. Required, and never inferred — this map
    /// reads subpart B, and an engine that decided which operations subpart D reaches would be
    /// working a subpart the map declares out of scope.
    /// </param>
    /// <param name="power">
    /// Whether the small unmanned aircraft is powered, as the caller states it: § 107.49(d)'s
    /// condition, which is <see cref="MapEntries.SufficientAvailablePower"/>'s own and is carried
    /// here only to be handed to that entry's rule. Nothing in this class reads it. Required, and
    /// never inferred — an aircraft the caller has not described is not an unpowered aircraft.
    /// </param>
    /// <param name="assertions">
    /// What the caller asserts. The obligations § 107.49 leaves to the remote pilot in command to
    /// report arrive here under their own entries' ids, and each is demanded by its own entry and
    /// never defaulted in either direction — § 107.49(d)'s only where that entry's own condition is
    /// satisfied, because a paragraph that states no obligation demands no assertion.
    /// </param>
    /// <returns>
    /// The finding; otherwise, where no obligation is answered not done and one went unanswered,
    /// this entry's own decline, carrying the blocking obligation's reason and citing its locator.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// The caller asserted nothing for one of the obligations § 107.49 gives the remote pilot in
    /// command to report <em>and states about this operation</em>. There are four of them on a
    /// powered aircraft and three on an unpowered one, because § 107.49(d) asks nothing of an
    /// aircraft its own condition does not reach.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// An asserted value is not an <see cref="Assertion"/>, is about another entry, or is attributed
    /// to somebody § 107.49's lead-in does not name.
    /// </exception>
    public static Resolution<PreflightActionsFinding> Actions(
        SubpartDOperation operation,
        AircraftPower power,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(power);
        ArgumentNullException.ThrowIfNull(assertions);

        // Every obligation is asked, in § 107.49's own lettering, and the answer below is read off
        // what each one actually returned. Nothing here records in advance which of them can answer:
        // an entry whose question the map later settles is followed from these calls.
        var obligations = new List<ObligationOutcome>
        {
            Stated(MapEntries.PreflightRiskAssessment, "(a)", assertions),
            Stated(MapEntries.ParticipantBriefing, "(b)", assertions),
            Untyped(MapEntries.ControlLinksWorking, "(c)", ControlLinks.Working()),
        };

        // § 107.49(d), through the entry whose evidence carries the paragraph's own condition. That
        // entry tests "If the small unmanned aircraft is powered" and this one does not: the
        // condition is asked once, where #95 put it, so the section and the paragraph cannot answer
        // one operation two ways (#99). Where the paragraph states no obligation about the
        // operation, Conditional returns none and there is nothing of (d) to conjoin.
        if (Conditional(
            MapEntries.SufficientAvailablePower,
            "(d)",
            AvailablePower.Enough(power, assertions),
            availability => availability.Holds) is { } availablePower)
        {
            obligations.Add(availablePower);
        }

        obligations.Add(Untyped(MapEntries.AttachedObjectSecure, "(e)", AttachedObject.Secure()));
        obligations.Add(Stated(MapEntries.AttachedObjectNoAdverseEffect, "(e)", assertions));

        // § 107.49(f)'s condition. Where the caller states it is not satisfied the paragraph states
        // no obligation about this operation, so there is nothing to conjoin for it and the entry it
        // defers to is not asked — the same shape § 107.33's chapeau has in Observers, one paragraph
        // wide rather than one section wide.
        if (operation == SubpartDOperation.OverHumanBeings)
        {
            obligations.Add(Untyped(
                MapEntries.SubpartDCategories,
                "(f)",
                Registry.Resolve(MapEntries.SubpartDCategories.Id, assertions)));
        }

        var blocking = obligations.Find(obligation => obligation.Done is null);

        // "Prior to flight, the remote pilot in command must" all of them: an obligation answered
        // not done settles the whole of it, whatever an unanswered one would have said, and every
        // obligation answered leaves nothing open.
        return obligations.Exists(obligation => obligation.Done == false) || blocking is null
            ? Resolution<PreflightActionsFinding>.FromValue(new PreflightActionsFinding(operation, obligations))
            : Resolution<PreflightActionsFinding>.FromUnresolved(Undetermined(operation, obligations, blocking));
    }

    /// <summary>
    /// One of the obligations § 107.49 leaves to the remote pilot in command to report and states
    /// unconditionally — § 107.49(a), § 107.49(b) and the second conjunct of § 107.49(e) — as that
    /// obligation's own entry answers it: the fact unchanged, on the map's entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Assertions.Stated"/> is the mechanism each of those entries' own handler uses, and
    /// it is what is called here: the assertion is demanded under that entry's id, refused if it is
    /// about another entry or attributed to somebody the map does not name, and answered unchanged.
    /// An assertion entry has no unresolved outcome of its own — correspondence row 8 — so this
    /// reads the verdict straight off the fact the caller reported, in either direction.
    /// </para>
    /// <para>
    /// <b>The fourth such obligation, § 107.49(d), is not read this way, and that is the point of
    /// <c>#99</c>.</b> Calling the shared mechanism reaches the assertion past the entry that states
    /// it, so a paragraph whose own evidence carries an antecedent has that antecedent skipped: an
    /// unpowered aircraft was asked here for a fact <c>sufficient-available-power</c> had already
    /// answered nobody owes. Those three paragraphs state no antecedent at all — their evidence is
    /// "(a) Assess …", "(b) Ensure that all persons directly participating …" and "does not adversely
    /// affect …" — so for them the shared mechanism and the entry's own rule cannot differ, and this
    /// stays the way they are read. § 107.49(d) goes through <see cref="Conditional"/>.
    /// </para>
    /// </remarks>
    private static ObligationOutcome Stated(MapEntry entry, string paragraph, RuleRequest assertions) =>
        Outcome(entry, paragraph, Assertions.Stated(entry, assertions), assertion => assertion.Holds);

    /// <summary>
    /// One of the obligations answered by an entry that resolves an untyped value: § 107.49(c), the
    /// first conjunct of § 107.49(e), and § 107.49(f).
    /// </summary>
    /// <remarks>
    /// None of the three declares a finding, because none of them resolves one: the first two over a
    /// term part 107 does not define, the third because the map puts it <c>scope: out</c>. All three
    /// are still asked, and the verdict is whatever a resolved value turns out to be where it is a
    /// plain answer to the obligation's yes-or-no question. Where it is not, the obligation is
    /// undetermined and says so (<see cref="ObligationOutcome.Unreadable"/>): this entry does not
    /// guess a verdict out of an <see cref="object"/>, and it does not fail.
    /// </remarks>
    private static ObligationOutcome Untyped(MapEntry entry, string paragraph, Resolution<object> resolution) =>
        Outcome(entry, paragraph, resolution, value => value as bool?);

    /// <summary>
    /// An obligation whose own paragraph states the condition it is owed under, answered by the
    /// entry whose evidence carries that condition: the outcome where the paragraph states an
    /// obligation about this operation, and <see langword="null"/> where it states none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Null is "not conjoined", and it is the only honest answer this type can give.</b> An
    /// <see cref="ObligationOutcome"/> is a verdict on an obligation, and where the paragraph's
    /// antecedent fails there is no obligation to have one: not done would convict an operation the
    /// paragraph says nothing about, done would excuse it, and undetermined would make § 107.49
    /// decline on facts the caller stated completely. So the conjunction is over the obligations the
    /// section states <em>about this operation</em>, which is what its lead-in's "must" ranges over.
    /// </para>
    /// <para>
    /// <b>The condition is the answering entry's and is never re-tested here.</b> What this reads is
    /// <see cref="IConditionalAssertion.ParagraphApplies"/> — the rule's own declaration that its
    /// paragraph did not reach the operation — and never the caller's statement of the condition.
    /// That is the same test <c>Evaluation.OperationEvaluator</c> makes, for the same reason
    /// <c>docs/decisions/0004</c> gives: it names no entry and no finding type, so a constituent a
    /// later map version states under an antecedent of its own is conjoined correctly the day it is
    /// built. § 107.49(d) is the one such constituent today (<c>#95</c>, <c>#99</c>).
    /// </para>
    /// <para>
    /// A decline is still an unanswered obligation and is conjoined as one. An entry that could not
    /// answer has not said its paragraph fails to reach the operation; it has said it could not say,
    /// and those are different, which is the whole of why this returns null only on the resolved
    /// branch.
    /// </para>
    /// </remarks>
    private static ObligationOutcome? Conditional<T>(
        MapEntry entry,
        string paragraph,
        Resolution<T> resolution,
        Func<T, bool?> done)
        where T : notnull, IConditionalAssertion =>
        resolution.Match<ObligationOutcome?>(
            value => value.ParagraphApplies ? Read(entry, paragraph, value, done) : null,
            unresolved => ObligationOutcome.Unanswered(
                entry, paragraph, unresolved.Attempted, unresolved.Reason));

    /// <summary>
    /// One obligation's answer, as this entry records it: the verdict <paramref name="done"/> reads
    /// off what the answering entry resolved, or the reason and the account of its decline.
    /// </summary>
    private static ObligationOutcome Outcome<T>(
        MapEntry entry,
        string paragraph,
        Resolution<T> resolution,
        Func<T, bool?> done)
        where T : notnull =>
        resolution.Match(
            value => Read(entry, paragraph, value, done),
            unresolved => ObligationOutcome.Unanswered(
                entry, paragraph, unresolved.Attempted, unresolved.Reason));

    /// <summary>
    /// The verdict read off a value an answering entry resolved: what <paramref name="done"/> found,
    /// or an obligation this entry has no verdict to read from.
    /// </summary>
    /// <remarks>
    /// The account is the answering entry's own <c>ToString()</c> and is never restated in this
    /// entry's words, which is what keeps § 107.49 from speaking for its constituents.
    /// </remarks>
    private static ObligationOutcome Read<T>(MapEntry entry, string paragraph, T value, Func<T, bool?> done)
        where T : notnull =>
        done(value) is { } verdict
            ? ObligationOutcome.Answered(entry, paragraph, verdict, value.ToString() ?? string.Empty)
            : ObligationOutcome.Unreadable(entry, paragraph, value.ToString() ?? string.Empty);

    /// <summary>
    /// This entry's own decline for an operation no obligation settled: it names the entry the
    /// caller asked about and every obligation that went unanswered, with that obligation's own
    /// reason, and it carries the first such obligation's reason and cites that entry's locator.
    /// </summary>
    /// <remarks>
    /// It is not a constituent's decline handed back. The reason and the locator are the blocking
    /// obligation's, because the question that blocks the answer is that entry's question and a
    /// citation should lead to where that question is; what was attempted is this entry's, so that a
    /// caller who asked about § 107.49 can tell this decline from the decline of the entry it names.
    /// Each unanswered obligation is named with the reason it gave, so that
    /// <see cref="MapEntries.SubpartDCategories"/>' <see cref="UnresolvedReason.OutsideCurrentScope"/>
    /// stays visible as that entry's rather than being flattened into the one reason an
    /// <see cref="UnresolvedResult"/> can carry.
    /// </remarks>
    private static UnresolvedResult Undetermined(
        SubpartDOperation operation,
        IReadOnlyList<ObligationOutcome> obligations,
        ObligationOutcome blocking)
    {
        var unanswered = obligations
            .Where(obligation => obligation.Done is null)
            .Select(obligation =>
                $"'{obligation.Entry.Id}' [{obligation.Entry.Locator.Citation}] did not answer "
                + $"{obligation.Citation} ({obligation.Reason})")
            .ToList();

        return new UnresolvedResult(
            blocking.Reason ?? throw new ArgumentException(
                "a blocking obligation is one that was not answered, and every such obligation carries the reason "
                + "the entry that did not answer it gave",
                nameof(blocking)),
            $"decide whether the map entry '{MapEntries.PreflightActions.Id}' is met where {operation}: § 107.49 "
            + "requires all of the obligations its paragraphs state, no obligation this engine answered is not "
            + $"done, and the map {(unanswered.Count == 1 ? "entry" : "entries")} {string.Join(" and ", unanswered)}",
            blocking.Entry.Locator);
    }
}
