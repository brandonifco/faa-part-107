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
/// caller stated it: § 107.49(f)'s condition.
/// </param>
/// <param name="Obligations">
/// The obligations the section conjoins, in the section's own paragraph order, each as the entry
/// that states it answered it. § 107.49(f) is among them exactly where the caller stated the
/// condition it applies under is satisfied, because the paragraph states no obligation otherwise.
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
    /// obligation the section states was answered done.
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
/// <b>§ 107.49(f) is a condition, and it is this entry's one input.</b> "If the operation will be
/// conducted over human beings under subpart D of this part" is in this entry's evidence and in no
/// constituent's — <see cref="MapEntries.SubpartDCategories"/>' evidence is subpart D's own
/// applicability sentence — so, like § 107.33's chapeau in <see cref="Observers"/>, the condition is
/// this entry's to carry, stated by the caller and never inferred (<see cref="SubpartDOperation"/>).
/// Where it is not satisfied the paragraph states no obligation about the operation, so there is
/// nothing to conjoin and the entry it defers to is not asked at all; where it is satisfied, what
/// § 107.49(f) then requires is that entry's, and this engine asks it rather than deciding it.
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
    /// Each obligation is asked, in the section's own paragraph order, and each is answered by the
    /// entry the map gives it. The conjunction is then read off those answers: any obligation
    /// answered not done makes the whole not done; all of them answered done makes it done;
    /// otherwise the first obligation that was not answered blocks, and this entry declines naming
    /// itself and every obligation that went unanswered.
    /// </remarks>
    /// <param name="operation">
    /// Whether the operation will be conducted over human beings under subpart D of this part, as
    /// the caller states it: § 107.49(f)'s own condition. Required, and never inferred — this map
    /// reads subpart B, and an engine that decided which operations subpart D reaches would be
    /// working a subpart the map declares out of scope.
    /// </param>
    /// <param name="assertions">
    /// What the caller asserts. The four obligations § 107.49 leaves to the remote pilot in command
    /// to report arrive here under their own entries' ids, and each is demanded by its own entry and
    /// never defaulted in either direction.
    /// </param>
    /// <returns>
    /// The finding; otherwise, where no obligation is answered not done and one went unanswered,
    /// this entry's own decline, carrying the blocking obligation's reason and citing its locator.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// The caller asserted nothing for one of the four obligations § 107.49 gives the remote pilot in
    /// command to report.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// An asserted value is not an <see cref="Assertion"/>, is about another entry, or is attributed
    /// to somebody § 107.49's lead-in does not name.
    /// </exception>
    public static Resolution<PreflightActionsFinding> Actions(SubpartDOperation operation, RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(assertions);

        // Every obligation is asked, in § 107.49's own lettering, and the answer below is read off
        // what each one actually returned. Nothing here records in advance which of them can answer:
        // an entry whose question the map later settles is followed from these calls.
        var obligations = new List<ObligationOutcome>
        {
            Stated(MapEntries.PreflightRiskAssessment, "(a)", assertions),
            Stated(MapEntries.ParticipantBriefing, "(b)", assertions),
            Untyped(MapEntries.ControlLinksWorking, "(c)", ControlLinks.Working()),
            Stated(MapEntries.SufficientAvailablePower, "(d)", assertions),
            Untyped(MapEntries.AttachedObjectSecure, "(e)", AttachedObject.Secure()),
            Stated(MapEntries.AttachedObjectNoAdverseEffect, "(e)", assertions),
        };

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
    /// One of the four obligations § 107.49 leaves to the remote pilot in command to report, as that
    /// obligation's own entry answers it: the fact unchanged, on the map's entry.
    /// </summary>
    /// <remarks>
    /// <see cref="Assertions.Stated"/> is the mechanism each of those entries' own handler uses, and
    /// it is what is called here: the assertion is demanded under that entry's id, refused if it is
    /// about another entry or attributed to somebody the map does not name, and answered unchanged.
    /// An assertion entry has no unresolved outcome of its own — correspondence row 8 — so this
    /// reads the verdict straight off the fact the caller reported, in either direction.
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
            value => done(value) is { } verdict
                ? ObligationOutcome.Answered(entry, paragraph, verdict, value.ToString() ?? string.Empty)
                : ObligationOutcome.Unreadable(entry, paragraph, value.ToString() ?? string.Empty),
            unresolved => ObligationOutcome.Unanswered(
                entry, paragraph, unresolved.Attempted, unresolved.Reason));

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
