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
/// <para>
/// The third of those refusals is the one the map decides the reach of. An entry whose
/// <see cref="MapEntry.AssertedBy"/> is exactly <c>["caller"]</c> is one the corpus does not
/// narrow, so there is nobody for the engine to hold the attribution against and the check does
/// not constrain it; every other entry's list is the people the corpus names, compared exactly.
/// <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c> records the reading, and
/// rules-factory decision 0025 is its authority.
/// </para>
/// </remarks>
public static class Assertions
{
    /// <summary>
    /// The map's word for "the corpus names nobody" — rules-factory decision 0025's
    /// <c>assertedBy</c> marker, not a person and not a name to match a caller's attribution
    /// against.
    /// </summary>
    private const string Caller = "caller";

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
    /// attributed to somebody the map's <see cref="MapEntry.AssertedBy"/> does not name — the
    /// last only where that list names people; an entry whose list is exactly <c>["caller"]</c>
    /// takes whatever attribution the caller gives.
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

        // Who may assert is the map's to narrow, and on three entries it narrows nobody. Where
        // assertedBy names people, their exact strings are the check and are not normalized: the
        // corpus really does say "the person manipulating the flight control" in § 107.31(a) and
        // "flight controls" in § 107.33(c), and an engine that smoothed that over would be papering
        // a distinction the corpus makes. Where assertedBy is the marker, there is no name to
        // compare against — the attribution is recorded, not checked.
        if (!NamesNobody(entry)
            && !entry.AssertedBy.Any(who => string.Equals(who, assertion.AssertedBy, StringComparison.Ordinal)))
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

    /// <summary>
    /// Whether the map records that the corpus names nobody who may assert
    /// <paramref name="entry"/>: its <see cref="MapEntry.AssertedBy"/> is exactly the one marker
    /// <c>caller</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rules-factory decision 0025 created <c>assertedBy</c> and states what the marker means:
    /// <c>caller</c> "does not mean 'anyone'. It means that the corpus does not narrow who may
    /// assert, so the engine attributes the assertion to whoever the caller says made it." Three
    /// Part 107 entries carry it — <see cref="MapEntries.CollisionHazardProximity"/>,
    /// <see cref="MapEntries.ReasonableProtection"/> and
    /// <see cref="MapEntries.FlashRateSufficient"/> — and 0025's consequences table names those
    /// three. So the marker turns the attribution check off for them and changes nothing for the
    /// other seven, whose lists are people.
    /// </para>
    /// <para>
    /// "Exactly" is the whole test, and it is the map's own rule rather than this engine's
    /// convenience: 0025 requires that <c>["caller"]</c> stand alone, so a list that named the
    /// marker alongside a person would be a map defect, and this engine would rather refuse the
    /// attribution than read a defect as permission. <c>caller</c> is itself an ordinary
    /// attribution to hand in for such an entry — <see cref="Assertion.AssertedBy"/> records
    /// whatever the caller says, that word included. Refusing it would be a constraint 0025 does
    /// not state, and the engine does not add constraints to the map.
    /// </para>
    /// </remarks>
    /// <param name="entry">The assertion entry, as the map has it.</param>
    /// <returns><see langword="true"/> where the corpus narrows nobody.</returns>
    private static bool NamesNobody(MapEntry entry) =>
        entry.AssertedBy.Length == 1 && string.Equals(entry.AssertedBy[0], Caller, StringComparison.Ordinal);
}
