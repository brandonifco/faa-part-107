using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// What was asserted for <see cref="MapEntries.FlashRateSufficient"/> (§ 107.29(a)(2), (b)), and the
/// waiver statement it was resolved under, recorded together.
/// </summary>
/// <remarks>
/// <para>
/// The fact is the caller's and this engine neither computes it nor checks it: the clause requires
/// anti-collision lighting "that has a flash rate sufficient to avoid a collision", and the map
/// records that as <c>kind: assertion</c> (correspondence row 8). The entry's note is explicit about
/// what that forbids — "the corpus states no rate, so no figure is evidenced" — so an engine that
/// decided it from a frequency, a period or a duty cycle would be answering a question the corpus
/// gives away, against a figure the corpus does not state.
/// </para>
/// <para>
/// What this record adds over the bare <see cref="Assertion"/> is the second caller fact the entry
/// needs. <c>flash-rate-sufficient</c> carries <c>suspendedBy: ["waivable-regulations"]</c> —
/// § 107.205(b) lists "Section 107.29(a)(2) and (b)" — so the waiver statement is owed here for the
/// same reason it is owed by every suspended entry: demand it, attribute it, record it alongside the
/// outcome, and never infer it (rules-factory decision 0021, decision 0001, and
/// <see cref="WaiverStatement"/>).
/// </para>
/// </remarks>
/// <param name="Sufficiency">
/// What was asserted, as <see cref="Assertions.Stated"/> answered it: on the map's own entry, so the
/// name it carries and the locator it cites are the map's, carrying the caller's
/// <see cref="Assertion.Holds"/> and <see cref="Assertion.AssertedBy"/> unchanged.
/// </param>
/// <param name="Waiver">The caller's waiver statement the assertion was answered under, recorded with it.</param>
public sealed record FlashRateSufficientFinding(Assertion Sufficiency, WaiverStatement Waiver)
{
    /// <summary>The assertion, checked to be present.</summary>
    public Assertion Sufficiency { get; } = Sufficiency ?? throw new ArgumentNullException(nameof(Sufficiency));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>True when the asserter reports the flash rate is sufficient to avoid a collision.</summary>
    public bool Holds => Sufficiency.Holds;

    /// <summary>Who is answerable for the assertion, as the caller attributed it.</summary>
    public string AssertedBy => Sufficiency.AssertedBy;

    /// <summary>Where the assertion is stated: <c>§ 107.29(a)(2), (b)</c>.</summary>
    public SourceLocator Authority => MapEntries.FlashRateSufficient.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Sufficiency}; {Waiver}");
}

/// <summary>
/// § 107.29(a)(2) and (b): whether the anti-collision lighting's flash rate is sufficient to avoid a
/// collision — <see cref="MapEntries.FlashRateSufficient"/>, a <c>kind: assertion</c> entry the
/// caller answers and this engine records.
/// </summary>
/// <remarks>
/// <para>
/// "(2) The small unmanned aircraft has lighted anti-collision lighting visible for at least 3
/// statute miles that has a flash rate sufficient to avoid a collision. … (b) No person may operate
/// a small unmanned aircraft system during periods of civil twilight unless the small unmanned
/// aircraft has lighted anti-collision lighting visible for at least 3 statute miles that has a
/// flash rate sufficient to avoid a collision."
/// </para>
/// <para>
/// <b>No figure is modelled, because the corpus states none.</b> The clause states a standard —
/// "sufficient to avoid a collision" — and not a rate. The entry's note says so in as many words,
/// and an engine that took a flashes-per-minute input, a period or a duty cycle and compared it
/// against something would be substituting its own rule for the corpus's. So the entry carries no
/// inputs of its own about the lighting. The one figure this clause does state plainly, the 3
/// statute miles the lighting must be visible for, is <see cref="MapEntries.AntiCollisionLighting"/>'s
/// and not this entry's; that entry depends on this one.
/// </para>
/// <para>
/// <b>Who asserts it is the caller's to say.</b> § 107.29(a) and (b) name the subject of the
/// prohibition, "no person may operate", and nobody whose determination settles the flash rate, so
/// the map's <c>assertedBy</c> is the marker <c>caller</c> (rules-factory decision 0025, and
/// <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>). The remote pilot in command the
/// same clause does name determines only whether reducing the lighting's intensity would be in the
/// interest of safety, which is <see cref="MapEntries.IntensityReductionInInterestOfSafety"/> — a
/// different entry, whose <c>assertedBy</c> names that person and is checked against them. The two
/// share this entry's locator and are not the same question.
/// </para>
/// </remarks>
public static class FlashRate
{
    /// <summary>
    /// The regulation § 107.205 lists that states the entry, as § 107.205(b) designates it:
    /// "Section 107.29(a)(2) and (b)".
    /// </summary>
    /// <remarks>
    /// § 107.205's own wording is what a caller's <see cref="WaiverStatement.Regulation"/> is checked
    /// against, and it is deliberately not the entry's locator: the locator cites the passage,
    /// <c>§ 107.29(a)(2), (b)</c>, and the list designates what a certificate may authorize deviation
    /// from. § 107.205(b) covers the two paragraphs together and nothing else of § 107.29, so a
    /// statement about § 107.29 whole is a statement about another regulation and is refused.
    /// </remarks>
    public const string Regulation = "§ 107.29(a)(2) and (b)";

    /// <summary>
    /// <see cref="MapEntries.FlashRateSufficient"/>: what the caller asserts about the flash rate
    /// being sufficient to avoid a collision, answered unchanged on the map's own entry — unless a
    /// waiver of § 107.29(a)(2) and (b) is stated in force, when the entry declines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate comes first.</b> § 107.205(b) lists § 107.29(a)(2) and (b), so while the caller
    /// states a waiver of it is in force this entry is unreachable and the assertion is beside the
    /// point: asking for it first would demand a fact the waiver has made irrelevant, and an
    /// <see cref="AssertionRequiredException"/> is what the caller would get for an entry that had
    /// nothing to ask. So <see cref="Waivers.Suspension"/> is consulted before
    /// <see cref="Assertions.Stated"/> is, and the two caller facts are owed in that fixed order: the
    /// waiver statement always, the assertion only when the statement says no waiver is in force.
    /// </para>
    /// <para>
    /// With no waiver in force the assertion is demanded and answered as row 8 requires: taken
    /// unchanged in either direction, a "no" included, and never defaulted, inferred or computed —
    /// the entry's note says the fact is "never defaulted to true when absent". Asserting nothing
    /// throws rather than declining: the corpus gave the engine the means to proceed and the caller
    /// owes the value.
    /// </para>
    /// <para>
    /// The attribution is recorded and not matched against a list of people, because the map's list
    /// for this entry is the marker rather than a person. That is the shared mechanism's reading of
    /// the map (<c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>) and not this entry's
    /// own: nothing here requires the word <c>caller</c> of a caller, and nothing here refuses it.
    /// </para>
    /// </remarks>
    /// <param name="waiver">Whether a waiver of § 107.29(a)(2) and (b) is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// What was asserted, with the statement recorded beside it;
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver of
    /// § 107.29(a)(2) and (b) is in force.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// No waiver is in force and the caller asserted nothing for the entry.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation, or the asserted value is not an
    /// <see cref="Assertion"/> or is about another entry.
    /// </exception>
    public static Resolution<FlashRateSufficientFinding> SufficientToAvoidACollision(
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.FlashRateSufficient, Regulation, waiver) is { } suspended)
        {
            return Resolution<FlashRateSufficientFinding>.FromUnresolved(suspended);
        }

        return Assertions.Stated(MapEntries.FlashRateSufficient, assertions).Match(
            sufficiency => Resolution<FlashRateSufficientFinding>.FromValue(
                new FlashRateSufficientFinding(sufficiency, waiver)),
            Resolution<FlashRateSufficientFinding>.FromUnresolved);
    }
}
