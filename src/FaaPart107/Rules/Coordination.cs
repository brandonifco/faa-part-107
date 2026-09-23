using System.Collections.Immutable;
using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// What was asserted for <see cref="MapEntries.ObserverCoordination"/> (§ 107.33(c)), and the waiver
/// statement it was resolved under, recorded together.
/// </summary>
/// <remarks>
/// <para>
/// The fact is the caller's and this engine neither computes it nor checks it: § 107.33(c) has three
/// named persons "coordinate to do the following", and the map records that as
/// <c>kind: assertion</c> (correspondence row 8). An engine that decided it from a position feed, a
/// traffic picture or a count of lookouts would be answering a question the corpus hands to the
/// people doing the flying.
/// </para>
/// <para>
/// What this record adds over the bare <see cref="Assertion"/> is the second caller fact the entry
/// needs. <c>observer-coordination</c> carries <c>suspendedBy: ["waivable-regulations"]</c> —
/// § 107.205(d) lists § 107.33 — so the waiver statement is owed here for the same reason it is owed
/// by every suspended entry: demand it, attribute it, <b>record it alongside the outcome</b>, and
/// never infer it (decision 0001, rules-factory decision 0021, and <see cref="WaiverStatement"/>).
/// A statement that none is in force travels with the result; an answer that dropped it could not
/// say under whose statement, or on whose word, the rule was evaluated normally at all.
/// </para>
/// <para>
/// It is local to this entry deliberately. Widening the shared <see cref="Assertion"/> record to
/// carry a waiver statement would reach the assertion entries that have no gate, where there is no
/// statement to carry and an optional one would be a place for the engine to default; the finding
/// belongs where the gate does.
/// </para>
/// </remarks>
/// <param name="Coordination">
/// What was asserted, as <see cref="Assertions.Stated"/> answered it: on the map's own entry, so the
/// name it carries and the locator it cites are the map's, carrying the caller's
/// <see cref="Assertion.Holds"/> and <see cref="Assertion.AssertedBy"/> unchanged.
/// </param>
/// <param name="Waiver">The caller's waiver statement the assertion was answered under, recorded with it.</param>
public sealed record ObserverCoordinationFinding(Assertion Coordination, WaiverStatement Waiver)
{
    /// <summary>The assertion, checked to be present.</summary>
    public Assertion Coordination { get; } = Coordination ?? throw new ArgumentNullException(nameof(Coordination));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>True when the asserter reports that the three named persons coordinate as § 107.33(c) requires.</summary>
    public bool Holds => Coordination.Holds;

    /// <summary>Who is answerable for the assertion, as the caller attributed it.</summary>
    public string AssertedBy => Coordination.AssertedBy;

    /// <summary>
    /// The two things § 107.33(c) states the coordination is for, verbatim and in the paragraph's own
    /// order: <see cref="FaaPart107.Coordination.Purposes"/>.
    /// </summary>
    public ImmutableArray<string> Purposes => FaaPart107.Coordination.Purposes;

    /// <summary>Where the assertion is stated: <c>§ 107.33(c)</c>.</summary>
    public SourceLocator Authority => MapEntries.ObserverCoordination.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Coordination}; {Waiver}");
}

/// <summary>
/// § 107.33(c): whether the three persons the paragraph names coordinate to scan the airspace and
/// maintain awareness — <see cref="MapEntries.ObserverCoordination"/>, a <c>kind: assertion</c>
/// entry the caller answers and this engine records.
/// </summary>
/// <remarks>
/// <para>
/// "(c) The remote pilot in command, the person manipulating the flight controls of the small
/// unmanned aircraft system, and the visual observer must coordinate to do the following: (1) Scan
/// the airspace where the small unmanned aircraft is operating for any potential collision hazard;
/// and (2) Maintain awareness of the position of the small unmanned aircraft through direct visual
/// observation."
/// </para>
/// <para>
/// <b>One proposition, not two.</b> The paragraph states one requirement — those three persons "must
/// coordinate" — and "to do the following:" introduces, in the same constituent, the closed two-item
/// enumeration that coordination is measured against, joined by "and". The entry's own note reads it
/// that way, and the map records one entry with one <c>kind: assertion</c>. So this engine demands
/// one value for it, does not split the paragraph into two assertions, and does not let one limb
/// stand for the other; <see cref="Purposes"/> prints both rather than either, because an assertion
/// answered about the scan alone would not be the paragraph's.
/// </para>
/// <para>
/// There is nothing here for the engine to compute. Whether a scan covers the airspace the aircraft
/// is operating in, whether a hazard is a potential collision hazard, and whether awareness of the
/// aircraft's position is maintained through direct visual observation are judgements the three
/// named persons make and report.
/// </para>
/// <para>
/// Who may assert it is the map's <see cref="MapEntry.AssertedBy"/>, in the corpus's own words: the
/// three persons (c) names, and nobody else. Those strings are compared ordinally — § 107.33(c) says
/// "the person manipulating the flight controls" where § 107.31(a) says "the person manipulating the
/// flight control", and the engine keeps that difference rather than papering over it.
/// </para>
/// </remarks>
public static class Coordination
{
    /// <summary>The regulation § 107.205(d) lists that states the entry: the whole of § 107.33.</summary>
    public const string Regulation = "§ 107.33";

    /// <summary>
    /// The two things § 107.33(c) states the coordination is for, verbatim, in the paragraph's own
    /// order and under the paragraph's own numbers.
    /// </summary>
    /// <remarks>
    /// These are quoted, not interpreted, and they are what the one assertion is about — not two
    /// assertions and not a summary of them. Nothing in this engine reads them: the fact they measure
    /// is the asserter's, and the engine records it and them together so that what was asserted is
    /// legible beside the answer.
    /// </remarks>
    public static ImmutableArray<string> Purposes { get; } =
    [
        "(1) Scan the airspace where the small unmanned aircraft is operating for any potential collision hazard; and",
        "(2) Maintain awareness of the position of the small unmanned aircraft through direct visual observation.",
    ];

    /// <summary>
    /// <see cref="MapEntries.ObserverCoordination"/>: what the caller asserts about the three named
    /// persons coordinating for <see cref="Purposes"/>, answered unchanged on the map's own entry and
    /// recorded beside the statement it was answered under — unless a waiver of § 107.33 is stated in
    /// force, when the entry declines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate comes first.</b> § 107.205(d) lists § 107.33, so while the caller states a waiver
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
    /// proceed and the caller owes the value. Either way the statement travels with the outcome: as
    /// text in <c>Attempted</c> on the decline, and as a typed field on the resolved value
    /// (decision 0001).
    /// </para>
    /// </remarks>
    /// <param name="waiver">Whether a waiver of § 107.33 is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// What was asserted, with the statement recorded beside it;
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver of § 107.33
    /// is in force.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// No waiver is in force and the caller asserted nothing for the entry.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation, or the asserted value is not an
    /// <see cref="Assertion"/>, is about another entry, or is attributed to somebody § 107.33(c) does
    /// not name.
    /// </exception>
    public static Resolution<ObserverCoordinationFinding> Coordinate(
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        // The gate first, and the assertion only past it. The two facts the caller owes are demanded
        // in that fixed order: a suspended entry that demanded the assertion first would throw for a
        // fact § 107.205 has made irrelevant, and would report that as the caller's omission.
        if (Waivers.Suspension(MapEntries.ObserverCoordination, Regulation, waiver) is { } suspended)
        {
            return Resolution<ObserverCoordinationFinding>.FromUnresolved(suspended);
        }

        return Assertions.Stated(MapEntries.ObserverCoordination, assertions).Match(
            coordination => Resolution<ObserverCoordinationFinding>.FromValue(
                new ObserverCoordinationFinding(coordination, waiver)),
            Resolution<ObserverCoordinationFinding>.FromUnresolved);
    }
}
