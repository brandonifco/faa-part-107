using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// What was asserted for <see cref="MapEntries.ReasonableProtection"/> (§ 107.39(b)), which of the
/// paragraph's two places it was asserted about, and the waiver statement it was resolved under,
/// recorded together.
/// </summary>
/// <remarks>
/// <para>
/// The fact is the caller's and this engine neither computes it nor checks it: § 107.39(b) states a
/// standard — a covered structure or a stationary vehicle "that can provide reasonable protection
/// from a falling small unmanned aircraft" — and the map records it as <c>kind: assertion</c>
/// (correspondence row 8). An engine that decided it from a roof material, a wall thickness, a
/// vehicle's construction or an aircraft's mass would be answering a question the corpus gives away,
/// and the corpus states no measure for it to use.
/// </para>
/// <para>
/// What this record adds over the bare <see cref="Assertion"/> is the two further caller facts the
/// entry needs, each demanded and never inferred. <see cref="Shelter"/> is which of § 107.39(b)'s
/// two the assertion is about, because the entry's note distributes the assertion "for each of a
/// covered structure and a stationary vehicle" and an answer that did not say which would let an
/// assertion about one stand as an answer about the other. <see cref="Waiver"/> is the waiver
/// statement, because <c>reasonable-protection</c> carries
/// <c>suspendedBy: ["waivable-regulations"]</c> — § 107.205(g) lists § 107.39 — and a suspended
/// entry owes it the same debt every suspended entry does: demand it, attribute it, record it
/// alongside the outcome, and never infer it (rules-factory decision 0021, this engine's decision
/// 0001, and <see cref="WaiverStatement"/>).
/// </para>
/// </remarks>
/// <param name="Shelter">
/// Which of § 107.39(b)'s two the assertion is about, as the caller stated it. Recorded so that the
/// answer is an answer about that one and not about the other;
/// <see cref="MapEntries.OverHumanBeings"/> depends on this entry and must check the place the human
/// being is located against it.
/// </param>
/// <param name="Protection">
/// What was asserted, as <see cref="Assertions.Stated"/> answered it: on the map's own entry, so the
/// name it carries and the locator it cites are the map's, carrying the caller's
/// <see cref="Assertion.Holds"/> and <see cref="Assertion.AssertedBy"/> unchanged.
/// </param>
/// <param name="Waiver">The caller's waiver statement the assertion was answered under, recorded with it.</param>
public sealed record ReasonableProtectionFinding(Shelter Shelter, Assertion Protection, WaiverStatement Waiver)
{
    /// <summary>Which of § 107.39(b)'s two it is about, checked to be present.</summary>
    public Shelter Shelter { get; } = Shelter ?? throw new ArgumentNullException(nameof(Shelter));

    /// <summary>The assertion, checked to be present.</summary>
    public Assertion Protection { get; } = Protection ?? throw new ArgumentNullException(nameof(Protection));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>
    /// True when the asserter reports that <see cref="Shelter"/> can provide reasonable protection
    /// from a falling small unmanned aircraft. It says nothing about the other of the two.
    /// </summary>
    public bool Holds => Protection.Holds;

    /// <summary>Who is answerable for the assertion, as the caller attributed it.</summary>
    public string AssertedBy => Protection.AssertedBy;

    /// <summary>Where the assertion is stated: <c>§ 107.39(b)</c>.</summary>
    public SourceLocator Authority => MapEntries.ReasonableProtection.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Shelter}: {Protection}; {Waiver}");
}

/// <summary>
/// § 107.39(b): whether the covered structure, or the stationary vehicle, the caller names can
/// provide reasonable protection from a falling small unmanned aircraft —
/// <see cref="MapEntries.ReasonableProtection"/>, a <c>kind: assertion</c> entry the caller answers
/// and this engine records.
/// </summary>
/// <remarks>
/// <para>
/// "(b) That human being is located under a covered structure or inside a stationary vehicle that
/// can provide reasonable protection from a falling small unmanned aircraft".
/// </para>
/// <para>
/// <b>The entry is the standard, not the exception.</b> The map's note draws that line itself: "The
/// standard qualifies the covered-structure exception; the exception itself is computable once it is
/// asserted." So what is asserted here is <see cref="Standard"/> — that the place in hand can
/// provide reasonable protection from a falling small unmanned aircraft. Whether a human being is in
/// fact located under or inside it is <c>over-human-beings</c>' (§ 107.39), which depends on this
/// entry; that entry's note keeps the "plain overflight with none of them" case too.
/// </para>
/// <para>
/// <b>The assertion distributes over the paragraph's two.</b> The entry's note says it "is asserted
/// and withheld <b>for each of</b> a covered structure and a stationary vehicle", and the map uses
/// that phrase only where a proposition distributes: <see cref="MapEntries.ObserverCoordination"/>,
/// which also names three actors, says "attributed to the three named persons" instead, because
/// coordinating is one joint fact. Whether a carport can provide reasonable protection is a
/// different fact from whether a parked van can. So the caller states which of the two the assertion
/// is about (<see cref="Shelter"/>, demanded and never inferred) and asserts the standard for that
/// one; the other is untouched, and stating nothing about it leaves it unasserted rather than
/// answered — which is what the same sentence's "never defaulted to true when absent" requires. The
/// place is recorded on the answer, so an answer about a stationary vehicle cannot be read as an
/// answer about a covered structure.
/// </para>
/// <para>
/// § 107.205(g) lists § 107.39, so this entry is suspended while a waiver of it is in force, as
/// <c>direct-participation</c> — § 107.39(a), and <see cref="Participation"/> — is. The two
/// paragraphs of that one sentence go opposite ways: (a) states an undefined term and declines
/// <see cref="UnresolvedReason.RequiresInterpretation"/>, (b) states its own measure and is asserted.
/// This entry borrows neither the decline nor its locator.
/// </para>
/// </remarks>
public static class Protection
{
    /// <summary>The regulation § 107.205(g) lists that states the entry: the whole of § 107.39.</summary>
    public const string Regulation = Participation.Regulation;

    /// <summary>The standard § 107.39(b) states, verbatim: what the caller asserts, and all of it.</summary>
    public const string Standard = "can provide reasonable protection from a falling small unmanned aircraft";

    /// <summary>
    /// <see cref="MapEntries.ReasonableProtection"/>: what the caller asserts about
    /// <paramref name="shelter"/> meeting <see cref="Standard"/>, answered unchanged on the map's own
    /// entry — unless a waiver of § 107.39 is stated in force, when the entry declines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate comes first, before either of the other two facts.</b> § 107.205(g) lists
    /// § 107.39, so while the caller states a waiver of it is in force this entry is unreachable and
    /// both which place is in hand and what was asserted about it are beside the point: asking for
    /// either first would demand a fact the waiver has made irrelevant, and a refusal is what the
    /// caller would get for an entry that had nothing to ask. So <see cref="Waivers.Suspension"/> is
    /// consulted before <paramref name="shelter"/> is demanded and before
    /// <see cref="Assertions.Stated"/> is reached, and the three caller facts are owed in that fixed
    /// order: the waiver statement always, then the place and the assertion only when the statement
    /// says no waiver is in force.
    /// </para>
    /// <para>
    /// That argument is not this entry's. Nothing in it is about § 107.39(b), and it holds of every
    /// entry the map gives <c>suspendedBy: ["waivable-regulations"]</c>, which is what
    /// <c>docs/decisions/0008-the-waiver-gate-precedes-every-other-demand.md</c> records: a handler
    /// demands the waiver statement and nothing else, and the rule demands the rest after the gate.
    /// This entry is one of the two that already read that way, and it is where the argument was
    /// first written down.
    /// </para>
    /// <para>
    /// With no waiver in force the assertion is demanded and answered as row 8 requires: taken
    /// unchanged in either direction, a "no" included, and never defaulted, inferred or computed.
    /// Asserting nothing throws rather than declining — the corpus gave the engine the means to
    /// proceed and the caller owes the value. What is answered is the one place the caller named,
    /// and nothing is concluded about the other.
    /// </para>
    /// <para>
    /// Who asserts it is the caller's to say. § 107.39 names the subject of the prohibition, "No
    /// person may operate a small unmanned aircraft over a human being", and the human being
    /// protected, and nobody whose determination settles the protection, so the map's
    /// <c>assertedBy</c> is the marker <c>caller</c> (rules-factory decision 0025, and
    /// <c>docs/decisions/0003-caller-is-not-a-name-to-match.md</c>). This entry inherits that from
    /// the shared <see cref="Assertions"/> and adds nothing to it: the attribution is recorded rather
    /// than matched against a list of people, and it is recorded — <see cref="Assertions.Stated"/>
    /// carries <see cref="Assertion.AssertedBy"/> through unchanged onto the answer.
    /// </para>
    /// </remarks>
    /// <param name="shelter">
    /// Which of § 107.39(b)'s two the assertion is about, as the caller states it. Required once the
    /// entry is reachable, and never inferred: the engine does not pick one, and does not read an
    /// assertion about one as an assertion about the other.
    /// </param>
    /// <param name="waiver">Whether a waiver of § 107.39 is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// What was asserted about <paramref name="shelter"/>, with the place and the statement recorded
    /// beside it; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver
    /// of § 107.39 is in force.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// No waiver is in force and the caller asserted nothing for the entry.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; no waiver is in force and
    /// <paramref name="shelter"/> was not stated; or the asserted value is not an
    /// <see cref="Assertion"/> or is about another entry.
    /// </exception>
    public static Resolution<ReasonableProtectionFinding> Reasonable(
        Shelter? shelter,
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.ReasonableProtection, Regulation, waiver) is { } suspended)
        {
            return Resolution<ReasonableProtectionFinding>.FromUnresolved(suspended);
        }

        var stated = shelter ?? throw new ArgumentException(
            $"resolving the map entry '{MapEntries.ReasonableProtection.Id}' "
            + $"[{MapEntries.ReasonableProtection.Locator.Citation}] needs its request's "
            + $"{nameof(Requests.ReasonableProtectionRequest.Shelter)}, and it was not set: the assertion is "
            + "made for each of a covered structure and a stationary vehicle, and the engine does not pick one",
            nameof(Requests.ReasonableProtectionRequest.Shelter));

        return Assertions.Stated(MapEntries.ReasonableProtection, assertions).Match(
            protection => Resolution<ReasonableProtectionFinding>.FromValue(
                new ReasonableProtectionFinding(stated, protection, waiver)),
            Resolution<ReasonableProtectionFinding>.FromUnresolved);
    }
}
