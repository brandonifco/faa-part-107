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
/// attributed to against the map, and answers with the fact unchanged. There is nothing here to
/// compute, and computing something would be this engine answering a question the corpus gives away.
/// </para>
/// <para>
/// The division of ownership is the whole of the care needed here. The caller owns the fact —
/// whether it holds, and whose fact it is — and the engine takes it as given, in either direction.
/// The map owns the entry — its name, and the locator the answer cites. So the answer is built
/// on the map's <see cref="MapEntry"/> and never on the one the caller's <see cref="Assertion"/>
/// carries: <see cref="MapEntry"/> and <c>SourceLocator</c> are publicly constructible, and a
/// caller who could supply the citation could have § 107.49(d) answered under § 107.51(b).
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
    /// What the caller asserted for <paramref name="entry"/>: the fact unchanged, on the map's
    /// entry.
    /// </summary>
    /// <param name="entry">
    /// The assertion entry being resolved, as the map has it. The answer is built on this, so the
    /// name it carries and the locator it cites are the map's and cannot be supplied by a caller.
    /// </param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// An <see cref="Assertion"/> on <paramref name="entry"/> carrying the caller's
    /// <see cref="Assertion.Holds"/> and <see cref="Assertion.AssertedBy"/> unchanged. An assertion
    /// entry has no unresolved outcome of its own — that is what row 8 means — so this resolves to
    /// a value or throws; the <see cref="Resolution{T}"/> is what an entry with a
    /// <c>suspendedBy</c> gate declines through.
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

        // Built on the map's own entry, never on the caller's. What the caller owns is the fact —
        // whether it holds, and whose fact it is — and the engine takes that unchanged, a "no"
        // included. What the map owns is the entry: the name the answer carries and the locator it
        // cites. MapEntry and SourceLocator are publicly constructible, so answering with the
        // caller's object would let a caller choose the citation § 107.49(d)'s answer is made
        // under. This is the line the attribution check already draws by reading entry.AssertedBy
        // and never assertion.Entry.AssertedBy; rebuilding draws it structurally rather than by a
        // fourth refusal, and without comparing object identity, which is not something a decision
        // in this engine may see (AGENTS.md section 8).
        return Resolution<Assertion>.FromValue(new Assertion(entry, assertion.Holds, assertion.AssertedBy));
    }
}
