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

    internal EvaluatedRequirement(RegisteredEntry entry, RequirementState state, string explanation)
    {
        EntryId = entry.Id;
        Status = entry.Status;
        Row = entry.Row;
        citations = entry.Locators;
        assertedBy = entry.AssertedBy.IsDefault ? [] : entry.AssertedBy;
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
    /// Who the corpus lets assert this entry, the map's <c>assertedBy</c>, in the corpus's own
    /// words. Empty on an entry that is not <c>kind: assertion</c>. On
    /// <see cref="RequirementState.HumanAssertionRequired"/> it is who may make the assertion the
    /// caller owes.
    /// </summary>
    public ImmutableArray<string> AssertedBy => assertedBy;

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
    /// <b><see cref="Finding"/> is deliberately not compared, and <see cref="Explanation"/> stands
    /// for it. That is a workaround, and it is meant to be undone.</b>
    /// <see cref="OperatingLimitationsFinding"/> holds an <c>IReadOnlyList</c> with no
    /// <c>Equals</c> override, so its generated equality compares that list by identity and two
    /// resolutions of the same request are unequal — the defect
    /// <see cref="MultipleAircraftFinding.Equals(MultipleAircraftFinding)"/> overrides its own
    /// equality to avoid, citing <c>AGENTS.md</c> §8. Comparing findings here would make two
    /// evaluations of identical facts unequal for a reason that is not about what either of them
    /// says. What the engine <em>says</em> about the finding is <see cref="Explanation"/>, the
    /// rule's own <c>ToString()</c>, compared ordinally — so a finding whose reported content
    /// differs still makes the outcomes differ.
    /// </para>
    /// <para>
    /// The fix belongs to that entry and not to this API, so it is filed as <b>#90</b> rather than
    /// made here: a rules change in a branch that closes #51 is barred by that issue's own
    /// non-goals and by <c>AGENTS.md</c> §4. <b>When #90 lands, <see cref="Finding"/> goes back
    /// into this comparison</b> — one line, <c>&amp;&amp; Equals(Finding, other.Finding)</c>, which
    /// was here before. Until then one consequence is live and worth knowing: a finding field the
    /// rule does not print is outside this comparison, so two outcomes can be equal while their
    /// findings differ in a field the rule kept to itself —
    /// <see cref="WeatherMinimumsFinding.ToString"/> omits the stated flight visibility, for
    /// instance.
    /// </para>
    /// </remarks>
    public bool Equals(EvaluatedRequirement? other) =>
        other is not null
        && string.Equals(EntryId, other.EntryId, StringComparison.Ordinal)
        && Status == other.Status
        && Row == other.Row
        && State == other.State
        && Reason == other.Reason
        && string.Equals(Explanation, other.Explanation, StringComparison.Ordinal)
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
        foreach (var citation in citations)
        {
            hash.Add(citation);
        }

        return hash.ToHashCode();
    }
}
