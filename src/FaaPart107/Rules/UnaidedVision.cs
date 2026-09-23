using System.Collections.Immutable;
using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// What was asserted for <see cref="MapEntries.UnaidedVisualContact"/> (§ 107.31(a)), and the waiver
/// statement it was resolved under, recorded together.
/// </summary>
/// <remarks>
/// <para>
/// The fact is the caller's and this engine neither computes it nor checks it: § 107.31(a) asks
/// whether three named people "must be able to see the unmanned aircraft throughout the entire
/// flight", with vision "unaided by any device other than corrective lenses", and the map records
/// that as <c>kind: assertion</c> (correspondence row 8). An engine that decided it from a distance,
/// a visibility figure or an aircraft size would be answering a question the corpus hands to a
/// person, and the corpus states no distance for it to use.
/// </para>
/// <para>
/// What this record adds over the bare <see cref="Assertion"/> is the second caller fact the entry
/// needs. <c>unaided-visual-contact</c> carries <c>suspendedBy: ["waivable-regulations"]</c> —
/// § 107.205(c) lists § 107.31 — so the waiver statement is owed here for the same reason it is owed
/// by every suspended entry: demand it, attribute it, record it alongside the outcome, and never
/// infer it (rules-factory decision 0021, and <see cref="WaiverStatement"/>).
/// </para>
/// </remarks>
/// <param name="Contact">
/// What was asserted, as <see cref="Assertions.Stated"/> answered it: on the map's own entry, so the
/// name it carries and the locator it cites are the map's, carrying the caller's
/// <see cref="Assertion.Holds"/> and <see cref="Assertion.AssertedBy"/> unchanged.
/// </param>
/// <param name="Waiver">The caller's waiver statement the assertion was answered under, recorded with it.</param>
public sealed record UnaidedVisualContactFinding(Assertion Contact, WaiverStatement Waiver)
{
    /// <summary>The assertion, checked to be present.</summary>
    public Assertion Contact { get; } = Contact ?? throw new ArgumentNullException(nameof(Contact));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>True when the asserter reports that the aircraft can be seen unaided throughout the entire flight.</summary>
    public bool Holds => Contact.Holds;

    /// <summary>Who is answerable for the assertion, as the caller attributed it.</summary>
    public string AssertedBy => Contact.AssertedBy;

    /// <summary>
    /// The four purposes § 107.31(a) states the ability against, verbatim and in the paragraph's own
    /// order: <see cref="UnaidedVision.Purposes"/>.
    /// </summary>
    public ImmutableArray<string> Purposes => UnaidedVision.Purposes;

    /// <summary>Where the assertion is stated: <c>§ 107.31(a)</c>.</summary>
    public SourceLocator Authority => MapEntries.UnaidedVisualContact.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Contact}; {Waiver}");
}

/// <summary>
/// § 107.31(a): whether the aircraft can be seen unaided throughout the entire flight, for the four
/// purposes the paragraph states — <see cref="MapEntries.UnaidedVisualContact"/>, a
/// <c>kind: assertion</c> entry the caller answers and this engine records.
/// </summary>
/// <remarks>
/// <para>
/// "(a) With vision that is unaided by any device other than corrective lenses, the remote pilot in
/// command, the visual observer (if one is used), and the person manipulating the flight control of
/// the small unmanned aircraft system must be able to see the unmanned aircraft throughout the
/// entire flight in order to: (1) Know the unmanned aircraft's location; (2) Determine the unmanned
/// aircraft's attitude, altitude, and direction of flight; (3) Observe the airspace for other air
/// traffic or hazards; and (4) Determine that the unmanned aircraft does not endanger the life or
/// property of another."
/// </para>
/// <para>
/// <b>One proposition, not four.</b> The paragraph states one ability — "must be able to see the
/// unmanned aircraft throughout the entire flight" — and "in order to:" introduces, in the same
/// constituent, the four purposes that ability is measured against. The entry's own note reads it
/// that way: the "sufficient to avoid a collision" construction "with four adjuncts instead of one".
/// So the map records one entry with one <c>kind: assertion</c>, and this engine demands one value
/// for it. It does not split the paragraph into four assertions, because the corpus does not state
/// four abilities; and <see cref="Purposes"/> prints all four rather than any of them, because an
/// assertion answered about three of the paragraph's purposes would not be the paragraph's.
/// </para>
/// <para>
/// (a)(4)'s "endanger the life or property of another" sits inside that measure and is not a second
/// open term this entry leaves unresolved: § 107.31 does not prohibit endangering, it requires the
/// ability to see well enough to determine. Which of the two combinations in § 107.31(b) must
/// exercise the ability is <c>visual-line-of-sight</c>'s, which depends on this entry; it is not
/// decided here.
/// </para>
/// </remarks>
public static class UnaidedVision
{
    /// <summary>The regulation § 107.205(c) lists that states the entry: the whole of § 107.31.</summary>
    public const string Regulation = "§ 107.31";

    /// <summary>
    /// The four purposes § 107.31(a) states the ability against, verbatim, in the paragraph's own
    /// order and under the paragraph's own numbers.
    /// </summary>
    /// <remarks>
    /// These are quoted, not interpreted, and they are what the one assertion is about — not four
    /// assertions and not a summary of them. Nothing in this engine reads them: the fact they measure
    /// is the asserter's, and the engine records it and them together so that what was asserted is
    /// legible beside the answer.
    /// </remarks>
    public static ImmutableArray<string> Purposes { get; } =
    [
        "(1) Know the unmanned aircraft's location;",
        "(2) Determine the unmanned aircraft's attitude, altitude, and direction of flight;",
        "(3) Observe the airspace for other air traffic or hazards; and",
        "(4) Determine that the unmanned aircraft does not endanger the life or property of another.",
    ];

    /// <summary>
    /// <see cref="MapEntries.UnaidedVisualContact"/>: what the caller asserts about the aircraft being
    /// seen unaided throughout the entire flight, for <see cref="Purposes"/>, answered unchanged on
    /// the map's own entry — unless a waiver of § 107.31 is stated in force, when the entry declines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate comes first.</b> § 107.205(c) lists § 107.31, so while the caller states a waiver
    /// of it is in force this entry is unreachable and the assertion is beside the point: asking for
    /// it first would demand a fact the waiver has made irrelevant, and an
    /// <see cref="AssertionRequiredException"/> is what the caller would get for an entry that had
    /// nothing to ask. So <see cref="Waivers.Suspension"/> is consulted before
    /// <see cref="Assertions.Stated"/> is, and the two caller facts are owed in that fixed order: the
    /// waiver statement always, the assertion only when the statement says no waiver is in force.
    /// </para>
    /// <para>
    /// With no waiver in force the assertion is demanded and answered as row 8 requires: taken
    /// unchanged in either direction, a "no" included, and never defaulted, inferred or computed.
    /// Asserting nothing throws rather than declining — the corpus gave the engine the means to
    /// proceed and the caller owes the value.
    /// </para>
    /// </remarks>
    /// <param name="waiver">Whether a waiver of § 107.31 is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// What was asserted, with the statement recorded beside it;
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver of § 107.31
    /// is in force.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// No waiver is in force and the caller asserted nothing for the entry.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation, or the asserted value is not an
    /// <see cref="Assertion"/>, is about another entry, or is attributed to somebody § 107.31(a) does
    /// not name.
    /// </exception>
    public static Resolution<UnaidedVisualContactFinding> SeenThroughoutTheFlight(
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.UnaidedVisualContact, Regulation, waiver) is { } suspended)
        {
            return Resolution<UnaidedVisualContactFinding>.FromUnresolved(suspended);
        }

        return Assertions.Stated(MapEntries.UnaidedVisualContact, assertions).Match(
            contact => Resolution<UnaidedVisualContactFinding>.FromValue(
                new UnaidedVisualContactFinding(contact, waiver)),
            Resolution<UnaidedVisualContactFinding>.FromUnresolved);
    }
}
