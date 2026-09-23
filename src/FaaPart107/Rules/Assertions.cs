using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// How a <c>kind: assertion</c> entry is answered: correspondence row 8, "nothing; the engine
/// demands the value" (the correspondence table beside <c>docs/corpus-map.md</c>, and
/// <see cref="CorrespondenceRow.Assertion"/>).
/// </summary>
/// <remarks>
/// <para>
/// This is the mechanism for every assertion entry, as <see cref="Waivers"/> is the mechanism for
/// every suspended one. It states no rule of its own: an assertion entry's whole content is that
/// the corpus puts the fact in somebody else's hands, so the engine demands it, checks who it is
/// attributed to against the map, and answers with it unchanged. There is nothing here to compute,
/// and computing something would be this engine answering a question the corpus gives away.
/// </para>
/// <para>
/// Three things are the caller's error rather than a gap in the corpus, and each is an
/// <see cref="ArgumentException"/> rather than an unresolved result, exactly as a missing waiver
/// statement is: a value that is not an <see cref="Assertion"/>, an assertion about a different
/// entry, and an assertion attributed to somebody the corpus does not let assert it. Asserting
/// nothing at all is <see cref="AssertionRequiredException"/>, which the kernel's request raises.
/// </para>
/// </remarks>
public static class Assertions
{
    /// <summary>
    /// What the caller asserted for <paramref name="entry"/>, checked and returned unchanged.
    /// </summary>
    /// <param name="entry">The assertion entry being resolved.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// The caller's <see cref="Assertion"/>. An assertion entry has no unresolved outcome of its
    /// own — that is what row 8 means — so this resolves to a value or throws; the
    /// <see cref="Resolution{T}"/> is what an entry with a <c>suspendedBy</c> gate declines through.
    /// </returns>
    /// <exception cref="AssertionRequiredException">The caller asserted nothing for the entry.</exception>
    /// <exception cref="ArgumentException">
    /// The asserted value is not an <see cref="Assertion"/>, is about another entry, or is
    /// attributed to somebody the map's <see cref="MapEntry.AssertedBy"/> does not name.
    /// </exception>
    public static Resolution<Assertion> Stated(MapEntry entry, RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(assertions);

        var asserted = assertions.Asserted(entry.Id);
        if (asserted is not Assertion assertion)
        {
            throw new ArgumentException(
                $"the map entry '{entry.Id}' [{entry.Locator.Citation}] is asserted, and its value must be an "
                + $"{nameof(Assertion)} saying who asserts it; the caller asserted a {asserted.GetType()}",
                nameof(assertions));
        }

        if (!string.Equals(assertion.Entry.Id, entry.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"the map entry '{entry.Id}' [{entry.Locator.Citation}] was asserted with an assertion about "
                + $"'{assertion.Entry.Id}' [{assertion.Entry.Locator.Citation}]",
                nameof(assertions));
        }

        if (!entry.AssertedBy.Any(who => string.Equals(who, assertion.AssertedBy, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                $"the map entry '{entry.Id}' [{entry.Locator.Citation}] is asserted by "
                + $"{string.Join(" or ", entry.AssertedBy)}, and the assertion is attributed to {assertion.AssertedBy}",
                nameof(assertions));
        }

        return Resolution<Assertion>.FromValue(assertion);
    }
}
