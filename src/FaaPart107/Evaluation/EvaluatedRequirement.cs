using System.Collections.Immutable;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107.Evaluation;

/// <summary>
/// What this engine said about one map entry, for one operation: the state, the entry that
/// produced it, the citation it rests on, and the engine's own words for it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not to be confused with <see cref="FaaPart107.RequirementOutcome"/></b>, which is
/// § 107.33's rule's own record of one of that section's three requirements. This type is the
/// product-facing result for one <em>map entry</em>, and it is what
/// <see cref="OperationEvaluation.Requirements"/> holds.
/// </para>
/// <para>
/// <b>Every outcome is traceable.</b> <see cref="EntryId"/> names the map entry and
/// <see cref="Citations"/> holds every locator that entry cites — a result a caller cannot trace
/// to a rule and a citation is not acceptable output from this engine. <see cref="Status"/> and
/// <see cref="Row"/> are the map's own, so a caller can see why an entry answered as it did
/// without holding the map.
/// </para>
/// <para>
/// <b>There is no boolean here.</b> <see cref="State"/> is the answer, and reading it means
/// handling the state it actually is. The rule's own finding, with the verdict property the state
/// was read from, is on <see cref="Finding"/> for a caller who wants the detail.
/// </para>
/// </remarks>
public sealed record EvaluatedRequirement
{
    private readonly ImmutableArray<SourceLocator> citations;
    private readonly ImmutableArray<string> assertedBy;

    /// <summary>Builds the outcome of evaluating <paramref name="entry"/>.</summary>
    /// <param name="entry">The map entry that was evaluated.</param>
    /// <param name="state">What this engine was able to say.</param>
    /// <param name="explanation">The engine's own words for it.</param>
    /// <param name="owed">
    /// The <c>kind: assertion</c> entry whose value the caller owes, where one was demanded. It is
    /// <paramref name="entry"/> itself when an assertion entry was evaluated directly, and a
    /// different entry when a composite demanded a constituent's assertion — § 107.39 asking for
    /// <c>reasonable-protection</c>, say. Null in every other case.
    /// </param>
    internal EvaluatedRequirement(
        RegisteredEntry entry,
        RequirementState state,
        string explanation,
        RegisteredEntry? owed = null)
    {
        EntryId = entry.Id;
        Status = entry.Status;
        Row = entry.Row;
        citations = entry.Locators;

        // Who may assert it is the *demanded* entry's, not the evaluated one's. A composite is not
        // itself kind: assertion, so taking this from `entry` left it empty on exactly the outcomes
        // that promise it.
        var attributed = owed ?? entry;
        assertedBy = attributed.AssertedBy.IsDefault ? [] : attributed.AssertedBy;
        AssertionOwed = owed?.Id;
        AssertionCites = owed?.Locator;
        State = state;
        Explanation = explanation;
    }

    /// <summary>The map entry this outcome is about.</summary>
    public string EntryId { get; }

    /// <summary>The entry's merged status, as the map and this engine's overlay record it.</summary>
    public EntryStatus Status { get; }

    /// <summary>The first correspondence row the entry matches, which fixes what its decline means.</summary>
    public CorrespondenceRow Row { get; }

    /// <summary>What this engine was able to say. Reading it is the whole of reading this outcome.</summary>
    public RequirementState State { get; }

    /// <summary>
    /// Every locator the entry cites: its own for a located entry, and for a derived entry the
    /// locator of every located entry it rests on (<see cref="Registry.Citations"/>).
    /// </summary>
    public ImmutableArray<SourceLocator> Citations => citations;

    /// <summary>The citation an answer or a decline carries, the first of <see cref="Citations"/>.</summary>
    public SourceLocator Locator => citations[0];

    /// <summary>
    /// Who the corpus lets assert the fact this outcome is about, the map's <c>assertedBy</c>, in
    /// the corpus's own words. On <see cref="RequirementState.HumanAssertionRequired"/> it is who
    /// may make the assertion the caller owes — which is <see cref="AssertionOwed"/>'s
    /// <c>assertedBy</c> and not this entry's, because a composite that demands a constituent's
    /// assertion is not itself <c>kind: assertion</c>. Empty where no assertion is in play.
    /// </summary>
    public ImmutableArray<string> AssertedBy => assertedBy;

    /// <summary>
    /// The map entry whose assertion the caller owes, on
    /// <see cref="RequirementState.HumanAssertionRequired"/>; null otherwise.
    /// </summary>
    /// <remarks>
    /// <b>This is not always <see cref="EntryId"/>, and the difference is the point</b> — the same
    /// shape as <see cref="DeclineCites"/> beside <see cref="Locator"/>. Asking § 107.39 whether an
    /// aircraft may be operated over a human being comes back owing
    /// <c>reasonable-protection</c>'s assertion at § 107.39(b); asking § 107.31 as a whole comes
    /// back owing <c>unaided-visual-contact</c>'s. <see cref="EntryId"/> stays the entry that was
    /// evaluated, because that is what the caller asked; this names what a person still has to
    /// attest, so that a product can name it without parsing the
    /// <see cref="Explanation"/>'s English.
    /// </remarks>
    public string? AssertionOwed { get; }

    /// <summary>
    /// Where the assertion <see cref="AssertionOwed"/> names is stated in the corpus; null
    /// otherwise. § 107.39(b) for <c>reasonable-protection</c>, § 107.31(a) for
    /// <c>unaided-visual-contact</c>, and so on.
    /// </summary>
    public SourceLocator? AssertionCites { get; }

    /// <summary>
    /// The engine's own words for this outcome: the rule's finding, the decline's
    /// <see cref="UnresolvedResult.Attempted"/>, or the entry's refusal. Never prose this
    /// orchestrator wrote about a regulation.
    /// </summary>
    public string Explanation { get; }

    /// <summary>
    /// Why the entry declined, where it did; null otherwise.
    /// <see cref="RequirementStates.For(UnresolvedReason)"/> is how it became
    /// <see cref="State"/>.
    /// </summary>
    public UnresolvedReason? Reason { get; init; }

    /// <summary>
    /// The locator the decline itself carries, where the entry declined; null otherwise.
    /// </summary>
    /// <remarks>
    /// <b>This is not always <see cref="Locator"/>, and the difference is the point.</b>
    /// <see cref="Citations"/> is what the <em>entry</em> cites; a decline cites where the real
    /// rule lives, which is often somewhere else: a waiver stated in force makes an entry decline
    /// citing § 107.205 (<c>waivable-regulations</c>) rather than its own paragraph;
    /// <c>civil-twilight-operation</c> in Alaska cites <c>civil-twilight-alaska</c>;
    /// <c>speed-within-limit</c> between the two printed figures cites <c>speed-limit</c>'s
    /// § 107.51(a), which is whose question it is (<c>docs/decisions/0001</c>). Dropping it would
    /// leave a caller holding a decline whose citation is the entry it asked about rather than the
    /// one that could not answer.
    /// </remarks>
    public SourceLocator? DeclineCites { get; init; }

    /// <summary>
    /// What the entry resolved to, exactly as the rule built it — a finding, a printed figure, or
    /// an <see cref="Assertion"/> — and null where the entry did not resolve.
    /// </summary>
    public object? Finding { get; init; }

    /// <summary>
    /// On <see cref="RequirementState.FactRequired"/>, the request input the entry demanded and did
    /// not get, as the entry named it; null otherwise.
    /// </summary>
    public string? MissingInput { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"{EntryId} [{Locator}]: {State} — {Explanation}";

    /// <summary>Whether this outcome is the same as <paramref name="other"/>: everything this engine said about the entry.</summary>
    /// <param name="other">The other outcome.</param>
    /// <returns>True when both say the same thing about the same entry.</returns>
    /// <remarks>
    /// <para>
    /// A record's generated equality would compare the two <see cref="ImmutableArray{T}"/> members
    /// by the identity of the array behind them, so two evaluations of the same facts would differ.
    /// Determinism is about what the engine says (<c>AGENTS.md</c> §8), so the citations and the
    /// attributions are compared by element — the same reason
    /// <see cref="MultipleAircraftFinding"/> gives for overriding its own.
    /// </para>
    /// <para>
    /// <b><see cref="Finding"/> is compared, and it is compared by value.</b> The rule's own
    /// finding is what the engine resolved, so it is what tells two outcomes apart — not the
    /// words the engine printed about it. Every finding this engine can resolve is a record, so
    /// its equality is its fields'; and every one of them that carries a collection overrides that
    /// equality to compare the collection by element rather than by the identity of the list
    /// object — <see cref="OperatingLimitationsFinding"/>, <see cref="OverHumanBeingsFinding"/>,
    /// <see cref="PreflightActionsFinding"/>, <see cref="VisualObserverConditionsFinding"/> and
    /// <see cref="MultipleAircraftFinding"/>, each for the reason
    /// <see cref="MultipleAircraftFinding.Equals(MultipleAircraftFinding)"/> gives and cites,
    /// <c>AGENTS.md</c> §8. So comparing findings here does not make two resolutions of one
    /// request unequal over which list object happened to carry them.
    /// </para>
    /// <para>
    /// <b>What that catches, and a rendered string did not.</b> A finding field the rule does not
    /// print is inside this comparison now. It was not while <see cref="Explanation"/> stood in
    /// for the finding: two outcomes could be equal while their findings differed in a field the
    /// rule kept to itself, and <see cref="WeatherMinimumsFinding"/> is the worked case —
    /// § 107.51(c)'s stated flight visibility is recorded on the finding and
    /// <see cref="WeatherMinimumsFinding.ToString"/> omits it, so two operations stating different
    /// visibilities produced the same words. They differ here now, because what is compared is the
    /// finding.
    /// </para>
    /// <para>
    /// <b><see cref="Explanation"/> stays beside it, and that is a decision rather than an
    /// oversight.</b> On a resolved outcome it is redundant: it is the finding's own
    /// <c>ToString()</c> and nothing else, so an equal finding forces an equal explanation. On
    /// every outcome that is not a resolution there is no finding at all — a decline carries
    /// <see cref="UnresolvedResult.Attempted"/>, a demanded assertion carries what was demanded,
    /// a missing fact carries the refusal — and <see cref="Explanation"/> is then the only member
    /// here holding what the engine said. Dropping it to be rid of the redundancy would make two
    /// declines that cite the same locator for the same reason in different words the same
    /// outcome, and those words are the engine's account of why it could not answer. Redundant
    /// where the finding speaks, load-bearing where nothing else does: it is kept.
    /// </para>
    /// <para>
    /// <b>One condition this rests on, which is not this type's to fix.</b> Comparing findings
    /// reduces, one level down, to record equality on the values a finding carries, and the only
    /// map-side value among them is <see cref="Assertion.Entry"/>, a <see cref="MapEntry"/>, whose
    /// <see cref="MapEntry.AssertedBy"/> is an <see cref="ImmutableArray{T}"/> and so is compared
    /// by the identity of the array behind it — the very defect this method overrides its own
    /// equality to avoid. It is sound today for two reasons and no others: every
    /// <see cref="MapEntry"/> this engine hands out is one of <see cref="MapEntries"/>' shared
    /// statics, and a record's generated equality short-circuits on reference identity; and where
    /// an entry names nobody, <c>AssertedBy</c> is the <c>ImmutableArray&lt;string&gt;.Empty</c>
    /// singleton, which is one array. <see cref="MapEntry"/> is generated, so the fix belongs to
    /// the generator and not here: it is <c>#110</c>, raised upstream as
    /// <c>rules-factory#462</c>.
    /// </para>
    /// </remarks>
    public bool Equals(EvaluatedRequirement? other) =>
        other is not null
        && string.Equals(EntryId, other.EntryId, StringComparison.Ordinal)
        && Status == other.Status
        && Row == other.Row
        && State == other.State
        && Reason == other.Reason
        && DeclineCites == other.DeclineCites
        && string.Equals(AssertionOwed, other.AssertionOwed, StringComparison.Ordinal)
        && AssertionCites == other.AssertionCites
        && string.Equals(Explanation, other.Explanation, StringComparison.Ordinal)
        && Equals(Finding, other.Finding)
        && string.Equals(MissingInput, other.MissingInput, StringComparison.Ordinal)
        && citations.SequenceEqual(other.citations)
        && assertedBy.SequenceEqual(other.assertedBy, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(EntryId, StringComparer.Ordinal);
        hash.Add(State);
        hash.Add(Reason);
        hash.Add(Explanation, StringComparer.Ordinal);
        hash.Add(Finding);
        foreach (var citation in citations)
        {
            hash.Add(citation);
        }

        return hash.ToHashCode();
    }
}
