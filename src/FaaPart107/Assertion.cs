using RulesKernel.Provenance;

namespace FaaPart107;

/// <summary>
/// What a caller asserts for a <c>kind: assertion</c> map entry: whether the entry's proposition
/// holds, and who is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// An assertion entry is one the corpus states as a fact somebody outside this engine determines
/// and reports. The engine's whole part in it is to demand the value, check who it is attributed
/// to against the map's <c>assertedBy</c>, and answer with what was asserted, unchanged. It does
/// not infer, compute, default or estimate the fact in either direction: an engine that decided
/// <see cref="MapEntries.SufficientAvailablePower"/> from a battery reading would be answering a
/// question § 107.49 gives to the remote pilot in command.
/// </para>
/// <para>
/// This is the same debt <see cref="WaiverStatement"/> records for the waiver gate — demand it,
/// attribute it, record it alongside the outcome, and never infer it — and it is recorded the same
/// way, because the reason is the same: a fact the engine did not determine is worth nothing
/// without whose fact it is.
/// </para>
/// <para>
/// There is no default assertion. An entry asked to resolve without one throws
/// <see cref="AssertionRequiredException"/>, which is not an unresolved result: the corpus gave the
/// engine the means to proceed and the caller owes the value (correspondence row 8).
/// </para>
/// </remarks>
/// <param name="Entry">
/// The map entry the assertion is about. An entry refuses an assertion about another entry.
/// </param>
/// <param name="Holds">True when the entry's proposition is so, as the asserter reports it.</param>
/// <param name="AssertedBy">
/// Who is answerable for the assertion, in the corpus's own words. The map names who the corpus
/// lets assert an entry, <see cref="MapEntry.AssertedBy"/>, and the entry refuses an assertion
/// attributed to anybody else; this engine does not widen that list from its own reading.
/// </param>
public sealed record Assertion(MapEntry Entry, bool Holds, string AssertedBy)
{
    /// <summary>The map entry the assertion is about, checked to be present.</summary>
    public MapEntry Entry { get; } = Check(Entry, nameof(Entry));

    /// <summary>Who asserted it, checked to be non-empty.</summary>
    public string AssertedBy { get; } = CheckText(AssertedBy, nameof(AssertedBy));

    /// <summary>Where the corpus states what was asserted: the entry's own locator.</summary>
    public SourceLocator Authority => Entry.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Entry.Name}: {(Holds ? "yes" : "no")}, as asserted by {AssertedBy} [{Authority}]";

    private static MapEntry Check(MapEntry entry, string name) =>
        entry ?? throw new ArgumentNullException(name);

    private static string CheckText(string text, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, name);
        return text;
    }
}
