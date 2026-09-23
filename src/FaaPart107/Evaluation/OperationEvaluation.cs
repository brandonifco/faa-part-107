using System.Collections.Immutable;
using System.Globalization;
using RulesKernel.Identity;

namespace FaaPart107.Evaluation;

/// <summary>
/// What this engine says about one operation: one <see cref="RequirementOutcome"/> for every entry
/// of the map, in the map's order, and the identity of the engine that said it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a determination that an operation is lawful, and it cannot be made into one.</b>
/// The map covers a bounded slice of 14 CFR part 107 and none of the rest of the CFR, and several
/// of its entries are ones this engine has not built, has been told are outside its scope, or
/// declines because the corpus does not settle them. There is deliberately no aggregate verdict
/// here, no boolean anywhere on this type or on <see cref="RequirementOutcome"/>, and nothing that
/// reduces the outcomes to one answer: what the engine evaluated and what it did not are both part
/// of the answer, and <see cref="Unanswered"/> is how a caller reads the second half.
/// </para>
/// <para>
/// <b>Progressive disclosure.</b> <see cref="Requirements"/> is everything;
/// <see cref="InState(RequirementState)"/> is one state; <see cref="Outstanding"/> is what the
/// caller still owes, and <see cref="Unanswered"/> is what this engine cannot answer. Both
/// groupings are filters over outcomes that each keep their own
/// <see cref="RequirementOutcome.State"/>, so no distinction is lost by using one.
/// </para>
/// </remarks>
public sealed record OperationEvaluation
{
    private readonly ImmutableArray<RequirementOutcome> requirements;

    internal OperationEvaluation(ImmutableArray<RequirementOutcome> requirements) =>
        this.requirements = requirements;

    /// <summary>
    /// Every map entry, in the map's order, one outcome each — including the entries this engine
    /// has not built and the entries the map puts out of scope, which are part of the answer and
    /// not omissions from it.
    /// </summary>
    public ImmutableArray<RequirementOutcome> Requirements => requirements;

    /// <summary>
    /// The engine that evaluated this: the ruleset and its version, the replay schema, and the
    /// corpus baseline the map is true of — <see cref="Ruleset.Identity"/>, reused rather than
    /// restated.
    /// </summary>
    public ReplayCompatibilityIdentity EvaluatedBy => Ruleset.Identity;

    /// <summary>
    /// Everything the caller still owes this engine before it could say more: an authorization or
    /// permission the rule names as obtainable, an assertion the corpus leaves to a person, and a
    /// fact an entry demanded and did not get.
    /// </summary>
    public ImmutableArray<RequirementOutcome> Outstanding =>
        Where(RequirementState.ActionRequired, RequirementState.HumanAssertionRequired, RequirementState.FactRequired);

    /// <summary>
    /// Everything this engine cannot answer whatever the caller supplies: the corpus does not
    /// settle it, the map puts it out of scope or a waiver suspends it, this engine has not built
    /// it, the structured data is absent, or the combination is unresolved. None of these is a
    /// finding about the operation.
    /// </summary>
    public ImmutableArray<RequirementOutcome> Unanswered =>
        Where(
            RequirementState.RequiresInterpretation,
            RequirementState.OutsideCurrentScope,
            RequirementState.NotBuilt,
            RequirementState.MissingRulesData,
            RequirementState.UnresolvedInteraction);

    /// <summary>The outcomes in <paramref name="state"/>, in the map's order.</summary>
    /// <param name="state">The state to read.</param>
    /// <returns>The outcomes in it; empty when there are none.</returns>
    public ImmutableArray<RequirementOutcome> InState(RequirementState state) => Where(state);

    /// <summary>How many outcomes are in <paramref name="state"/>.</summary>
    /// <param name="state">The state to count.</param>
    /// <returns>The count.</returns>
    public int Count(RequirementState state)
    {
        var count = 0;
        foreach (var outcome in requirements)
        {
            if (outcome.State == state)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The outcome for <paramref name="entryId"/>.</summary>
    /// <param name="entryId">A map entry id.</param>
    /// <returns>Its outcome.</returns>
    /// <exception cref="KeyNotFoundException">The map has no such entry.</exception>
    public RequirementOutcome Requirement(string entryId)
    {
        foreach (var outcome in requirements)
        {
            if (string.Equals(outcome.EntryId, entryId, StringComparison.Ordinal))
            {
                return outcome;
            }
        }

        throw new KeyNotFoundException($"the map has no entry '{entryId}'");
    }

    /// <summary>The engine's <c>provenance.json</c>, byte for byte: the map package and its version, the factory, the kernel and the corpus pin.</summary>
    /// <returns>The file's bytes, from <see cref="EngineProvenance"/>.</returns>
    /// <exception cref="InvalidOperationException">The assembly was built without it.</exception>
    /// <remarks>
    /// The record is the engine's own and is not restated here: with
    /// <see cref="EvaluatedBy"/> it is what lets a caller say "evaluated by FaaPart107 engine X,
    /// from map package Y at version Z, against corpus baseline B". It is a method rather than a
    /// property because the bytes are not part of this value's identity.
    /// </remarks>
    public byte[] ProvenanceJson() => EngineProvenance.ReadBytes();

    /// <inheritdoc/>
    public override string ToString()
    {
        var counts = new List<string>();
        foreach (var state in Enum.GetValues<RequirementState>())
        {
            var count = Count(state);
            if (count > 0)
            {
                counts.Add(string.Create(CultureInfo.InvariantCulture, $"{count} {state}"));
            }
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{EvaluatedBy.Ruleset.Id} v{EvaluatedBy.Ruleset.Version} over {requirements.Length} map entries: "
            + $"{string.Join(", ", counts)}. This is what those entries say; it is not a determination that an operation is lawful.");
    }

    /// <summary>Whether this evaluation is the same as <paramref name="other"/>, comparing the outcomes by element.</summary>
    /// <param name="other">The other evaluation.</param>
    /// <returns>True when both say the same thing about the same entries, in the same order.</returns>
    /// <remarks>
    /// By element for the reason <see cref="RequirementOutcome.Equals(RequirementOutcome)"/> gives:
    /// a record's generated equality would compare the backing array by identity, and determinism
    /// is about what the engine says.
    /// </remarks>
    public bool Equals(OperationEvaluation? other) =>
        other is not null && requirements.SequenceEqual(other.requirements);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var outcome in requirements)
        {
            hash.Add(outcome);
        }

        return hash.ToHashCode();
    }

    private ImmutableArray<RequirementOutcome> Where(params RequirementState[] states)
    {
        var found = ImmutableArray.CreateBuilder<RequirementOutcome>();
        foreach (var outcome in requirements)
        {
            if (Array.IndexOf(states, outcome.State) >= 0)
            {
                found.Add(outcome);
            }
        }

        return found.ToImmutable();
    }
}
