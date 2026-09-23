using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// What was asserted for <see cref="MapEntries.IntensityReductionInInterestOfSafety"/>
/// (§ 107.29(a)(2), (b)), and the waiver statement it was resolved under, recorded together.
/// </summary>
/// <remarks>
/// <para>
/// The fact is the caller's and this engine neither computes it nor checks it: § 107.29(a)(2) and
/// (b) give the judgement to the remote pilot in command — "if he or she determines that, because
/// of operating conditions, it would be in the interest of safety to do so" — and the map records
/// it as <c>kind: assertion</c> (correspondence row 8).
/// </para>
/// <para>
/// What this record adds over the bare <see cref="Assertion"/> is the second caller fact the entry
/// needs. <c>intensity-reduction-in-interest-of-safety</c> carries
/// <c>suspendedBy: ["waivable-regulations"]</c> — § 107.205(b) lists "Section 107.29(a)(2) and (b)"
/// — so the waiver statement is owed here for the same reason it is owed by every suspended entry:
/// demand it, attribute it, record it alongside the outcome, and never infer it (rules-factory
/// decision 0021, and <see cref="WaiverStatement"/>). Decision 0001 states the contract for the
/// resolved side of that gate — "A statement that none is in force evaluates the rule normally and
/// travels with the result", recorded "as a typed field" — and it is not speed-specific: it holds
/// for every entry the map marks <c>suspendedBy</c>. An answer that dropped the statement would
/// leave a caller unable to say which statement, or whose, the determination was answered under.
/// </para>
/// </remarks>
/// <param name="Determination">
/// What was asserted, as <see cref="Assertions.Stated"/> answered it: on the map's own entry, so the
/// name it carries and the locator it cites are the map's, carrying the caller's
/// <see cref="Assertion.Holds"/> and <see cref="Assertion.AssertedBy"/> unchanged.
/// </param>
/// <param name="Waiver">The caller's waiver statement the determination was answered under, recorded with it.</param>
public sealed record IntensityReductionFinding(Assertion Determination, WaiverStatement Waiver)
{
    /// <summary>The assertion, checked to be present.</summary>
    public Assertion Determination { get; } = Determination ?? throw new ArgumentNullException(nameof(Determination));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>
    /// True when the remote pilot in command determined that, because of operating conditions,
    /// reducing the intensity would be in the interest of safety.
    /// </summary>
    public bool Holds => Determination.Holds;

    /// <summary>Who is answerable for the determination, as the caller attributed it.</summary>
    public string AssertedBy => Determination.AssertedBy;

    /// <summary>Where the determination is stated: <c>§ 107.29(a)(2), (b)</c>.</summary>
    public SourceLocator Authority => MapEntries.IntensityReductionInInterestOfSafety.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Determination}; {Waiver}");
}

/// <summary>
/// § 107.29(a)(2) and (b), the anti-collision lighting sentence: "The remote pilot in command may
/// reduce the intensity of, but may not extinguish, the anti-collision lighting if he or she
/// determines that, because of operating conditions, it would be in the interest of safety to do
/// so."
/// </summary>
/// <remarks>
/// <para>
/// The sentence has two halves and the map splits them. The computable half — that intensity may be
/// reduced but the lighting may not be extinguished — is <c>anti-collision-lighting</c>'s, and that
/// entry depends on this one. The half here is the one the corpus hands away: whether reducing the
/// intensity would be in the interest of safety is what the remote pilot in command "determines",
/// so the map records it <c>kind: assertion</c> and this engine demands it (correspondence row 8).
/// </para>
/// <para>
/// So there is nothing here to compute, and no figure to compare against. The corpus states no
/// intensity, no candela, no lumens and no proportion by which intensity may be reduced; it states
/// a determination and names who makes it. An engine that derived the determination from operating
/// conditions would be answering the question § 107.29 gives to the remote pilot in command, which
/// is the one thing row 8 forbids.
/// </para>
/// <para>
/// The entry is suspended by <see cref="MapEntries.WaivableRegulations"/>: § 107.205(b) lists
/// "Section 107.29(a)(2) and (b)" among the regulations a certificate of waiver may authorize
/// deviation from. The gate is read <em>before</em> the assertion is demanded, which is the whole of
/// <see cref="IntensityReduction"/>'s shape: while a waiver is in force the entry is unreachable, and
/// demanding a fact about a rule the caller has stated does not apply to it would be asking for
/// something it does not owe.
/// </para>
/// <para>
/// The gate's other side is owed too. A statement that no waiver is in force evaluates the rule and
/// travels with the result as a typed field (decision 0001, which states that for every entry the
/// map marks <c>suspendedBy</c> and not for speed alone), so the answer is an
/// <see cref="IntensityReductionFinding"/> and not a bare <see cref="Assertion"/>. The shared
/// assertion record carries no waiver field and is not changed to acquire one: it is also the answer
/// of entries with no gate at all, such as <see cref="MapEntries.ParticipantBriefing"/>, which
/// § 107.205 does not list. Wrapping is what <see cref="UnaidedVisualContactFinding"/> does for
/// § 107.31(a), the other gated assertion entry, and this entry matches it.
/// </para>
/// </remarks>
public static class Lighting
{
    /// <summary>
    /// The regulation § 107.205(b) lists that states the entry: <c>§ 107.29(a)(2) and (b)</c>, as
    /// that paragraph designates it.
    /// </summary>
    public const string Regulation = "§ 107.29(a)(2) and (b)";

    /// <summary>
    /// <see cref="MapEntries.IntensityReductionInInterestOfSafety"/>: what the remote pilot in
    /// command determined, unchanged, on the map's own entry, with the waiver statement it was
    /// answered under recorded beside it in an <see cref="IntensityReductionFinding"/>.
    /// </summary>
    /// <remarks>
    /// The two caller facts are read in a fixed order, and the order is the behaviour. The waiver
    /// statement is read first: while it says a certificate of waiver of
    /// <see cref="Regulation"/> is in force the entry declines
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 and
    /// <paramref name="assertions"/> is never consulted, so a suspended entry does not throw
    /// <see cref="AssertionRequiredException"/> for a determination the waiver has made beside the
    /// point. Only while no waiver is in force is the assertion demanded, and then nothing about it
    /// is defaulted or inferred in either direction.
    /// </remarks>
    /// <param name="waiver">Whether a waiver of <see cref="Regulation"/> is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// What was asserted, built on the map's entry so it cites § 107.29(a)(2), (b), with the waiver
    /// statement it was answered under recorded beside it;
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in
    /// force.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// No waiver is in force and the caller asserted nothing. The corpus gave the engine the means
    /// to proceed and the caller owes the value, so this is a demand and not an unresolved result.
    /// </exception>
    public static Resolution<IntensityReductionFinding> IntensityReduction(
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.IntensityReductionInInterestOfSafety, Regulation, waiver) is { } suspended)
        {
            return Resolution<IntensityReductionFinding>.FromUnresolved(suspended);
        }

        return Assertions.Stated(MapEntries.IntensityReductionInInterestOfSafety, assertions).Match(
            determination => Resolution<IntensityReductionFinding>.FromValue(
                new IntensityReductionFinding(determination, waiver)),
            Resolution<IntensityReductionFinding>.FromUnresolved);
    }
}
