using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// § 107.49(d) on one operation: what the caller stated about the condition the paragraph states its
/// obligation under, and — where that condition is satisfied — what was asserted for
/// <see cref="MapEntries.SufficientAvailablePower"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three answers, and the third is not either of the other two.</b> Where the small unmanned
/// aircraft is powered, the paragraph states an obligation and the remote pilot in command reports
/// whether it is met: <see cref="Holds"/> is that report, taken unchanged in either direction. Where
/// the aircraft is not powered, § 107.49(d) states no obligation about this operation at all:
/// <see cref="Holds"/> is null, <see cref="ParagraphApplies"/> says which case that null is, and
/// neither "there is enough available power" nor "there is not" has been said by anybody. That is
/// the shape <see cref="VisualObserverConditionsFinding.AllRequirementsMet"/> already has for
/// § 107.33's chapeau.
/// </para>
/// <para>
/// The null is <b>never "undetermined"</b>. This entry has no unresolved outcome of its own — that
/// is what correspondence row 8 means — so a caller who gets a finding at all has been answered, and
/// the only thing a null <see cref="Holds"/> can be is the antecedent failing.
/// </para>
/// <para>
/// <b>What the caller owns and what the map owns is unchanged.</b> The fact is the caller's, and it
/// is recorded and not scored (<c>docs/decisions/0004</c>): nothing here reads whether there being
/// enough power is compliance. The citation is the map's:
/// <see cref="Availability"/> is <see cref="Assertions.Stated"/>'s answer, built on
/// <see cref="MapEntries.SufficientAvailablePower"/>, so it cites § 107.49(d) whatever entry the
/// caller's own <see cref="Assertion"/> was carrying.
/// </para>
/// <para>
/// <see cref="Availability"/> is null exactly where <see cref="ParagraphApplies"/> is false: the two
/// are set together by the factories below and never apart, the invariant
/// <see cref="ObligationOutcome"/> keeps between its verdict and its reason.
/// </para>
/// </remarks>
public sealed record SufficientAvailablePowerFinding : IConditionalAssertion
{
    private SufficientAvailablePowerFinding(AircraftPower power, Assertion? availability)
    {
        Power = power ?? throw new ArgumentNullException(nameof(power));
        Availability = availability;
    }

    /// <summary>
    /// Whether the small unmanned aircraft is powered, as the caller stated it: § 107.49(d)'s own
    /// condition.
    /// </summary>
    public AircraftPower Power { get; }

    /// <summary>
    /// What was asserted for <see cref="MapEntries.SufficientAvailablePower"/>, as
    /// <see cref="Assertions.Stated"/> answered it — null exactly where § 107.49(d) states no
    /// obligation about this operation, because no assertion is demanded there and none was made.
    /// </summary>
    public Assertion? Availability { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// True where the caller stated the small unmanned aircraft is powered. It is read off
    /// <see cref="Power"/>, which the caller states, and never from an absence.
    /// </remarks>
    public bool ParagraphApplies => Power == AircraftPower.Powered;

    /// <summary>
    /// Whether the remote pilot in command reports that there is enough available power for the
    /// small unmanned aircraft system to operate for the intended operational time — <b>null where
    /// § 107.49(d) states no obligation about this operation</b>, because the aircraft is not
    /// powered.
    /// </summary>
    /// <remarks>
    /// The null is neither "there is enough" nor "there is not": nobody has been asked, because the
    /// paragraph's own condition is not satisfied. <see cref="ParagraphApplies"/> stands beside this
    /// property and says which case it is.
    /// </remarks>
    public bool? Holds => Availability?.Holds;

    /// <summary>
    /// Who is answerable for the assertion, as the caller attributed it; null where none was made.
    /// </summary>
    public string? AssertedBy => Availability?.AssertedBy;

    /// <summary>Where the rule is stated: <c>§ 107.49(d)</c>.</summary>
    public SourceLocator Authority => MapEntries.SufficientAvailablePower.Locator;

    /// <summary>
    /// The obligation as the entry's own answer: the aircraft is powered, and this is what was
    /// asserted for it.
    /// </summary>
    /// <param name="power">The condition, as the caller stated it.</param>
    /// <param name="availability">What was asserted, as <see cref="Assertions.Stated"/> answered it.</param>
    /// <returns>The finding, carrying the assertion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="availability"/> is null.</exception>
    public static SufficientAvailablePowerFinding Asserted(AircraftPower power, Assertion availability) =>
        new(power, availability ?? throw new ArgumentNullException(nameof(availability)));

    /// <summary>
    /// The paragraph as it stands where its condition is not satisfied: § 107.49(d) states no
    /// obligation about this operation, so nothing was demanded and nothing is recorded.
    /// </summary>
    /// <param name="power">The condition, as the caller stated it.</param>
    /// <returns>The finding, carrying no assertion.</returns>
    public static SufficientAvailablePowerFinding StatesNoObligation(AircraftPower power) => new(power, null);

    /// <inheritdoc/>
    public override string ToString() =>
        ParagraphApplies
            ? $"{Power}, and {Availability} [{Authority}]"
            : $"{Power}, so § 107.49(d) states no obligation about this operation [{Authority}]";
}

/// <summary>
/// § 107.49(d): whether there is enough available power for the small unmanned aircraft system to
/// operate for the intended operational time — <see cref="MapEntries.SufficientAvailablePower"/>, a
/// <c>kind: assertion</c> entry the remote pilot in command answers and this engine records, under
/// the one condition the paragraph states it.
/// </summary>
/// <remarks>
/// <para>
/// "(d) If the small unmanned aircraft is powered, ensure that there is enough available power for
/// the small unmanned aircraft system to operate for the intended operational time;"
/// </para>
/// <para>
/// <b>The antecedent is this entry's, because it is in this entry's evidence.</b> The map quotes the
/// paragraph whole, "If the small unmanned aircraft is powered" included. So the condition belongs
/// here (<c>#95</c>), and where it is not satisfied the paragraph states no obligation about the
/// operation: that is not the obligation being met, and it is not its being unmet.
/// </para>
/// <para>
/// <b>It is also in the composite's evidence, and that is the asymmetry with § 107.49(f)</b> — the
/// reverse of the one those two entries' notes are often read as drawing.
/// <see cref="MapEntries.PreflightActions"/>' evidence quotes § 107.49 entire, lead-in and (a)
/// through (f), so "If the small unmanned aircraft is powered" stands in the composite's evidence as
/// well as this constituent's: it is the only antecedent <em>of that kind</em> in this map that
/// appears in two entries. The qualifier is doing work and is not hedging. Two other protases do
/// appear in more than one entry's evidence — "the visual observer (if one is used)", in
/// <c>visual-line-of-sight</c> and <c>unaided-visual-contact</c>, and "if he or she determines
/// that … it would be in the interest of safety to do so", in four entries — but neither gates
/// whether its paragraph reaches the operation at all, which is what this kind of antecedent does
/// and what makes it the constituent's to implement.
/// § 107.49(f)'s runs the other way — <see cref="SubpartDOperation"/>'s condition is in the
/// composite's evidence and in no constituent's, <c>subpart-d-categories</c>' evidence being
/// subpart D's own scope sentence, which does not carry it — which is why the composite tests that
/// one itself: there is no constituent to route it through.
/// </para>
/// <para>
/// <b>That asymmetry is what settled how the composite follows this entry</b> (<c>#99</c>). Being in
/// the composite's evidence too does not make the condition the composite's to read a second time —
/// the composite's evidence is the whole section, so it carries every constituent's words, and
/// reading the antecedent there as well would put one condition in two implementations that can
/// disagree. They did disagree: <see cref="Preflight"/> reached § 107.49(d) through the shared
/// <see cref="Assertions.Stated"/>, never saw the condition, and demanded this assertion of an
/// unpowered aircraft that this entry had already answered nobody owes. So § 107.49(d) is now
/// reached through <see cref="Enough"/>, and the caller's statement of the condition travels on
/// <c>PreflightActionsRequest.Power</c> unread by that rule; § 107.49(f)'s condition stays the
/// composite's own input <em>and</em> the composite's own test, because no constituent's evidence
/// carries it. Where the paragraph states no obligation, the composite conjoins nothing for it,
/// which is not an undetermined conjunct.
/// </para>
/// <para>
/// <b>The condition comes first, and the assertion only past it</b>, the order
/// <see cref="Coordination.Coordinate"/> puts the § 107.205 gate and its assertion in and for the
/// same reason: demanding the fact first would demand a fact the condition has made irrelevant, and
/// an <see cref="AssertionRequiredException"/> is what the caller would get for a paragraph that had
/// nothing to ask. So the two caller facts are owed in a fixed order — the condition always, the
/// assertion only where the aircraft is powered.
/// </para>
/// <para>
/// <b>Row 8 is untouched by that.</b> Where the paragraph reaches the operation the value is
/// demanded and answered exactly as <see cref="Assertions.Stated"/> answers it: unchanged in either
/// direction, a "no" included, refused where it is about another entry or attributed to somebody
/// § 107.49's lead-in does not name, and never computed, defaulted or inferred. The condition is not
/// the assertion and is never read as one: whether the aircraft is powered is a fact about the
/// aircraft, and whether there is enough available power for the system to operate for the intended
/// operational time is the judgement the corpus hands to the remote pilot in command. This engine
/// holds no battery state, no endurance figure and no intended operational time, and derives none.
/// </para>
/// <para>
/// <b>There is no waiver gate.</b> § 107.205 lists the regulations a certificate of waiver may
/// authorize a deviation from and does not list § 107.49, so this entry has no <c>suspendedBy</c> in
/// the map and nothing about a waiver is demanded or read here.
/// </para>
/// </remarks>
public static class AvailablePower
{
    /// <summary>
    /// § 107.49(d), verbatim as the map quotes it — the paragraph this entry states, condition and
    /// all.
    /// </summary>
    /// <remarks>
    /// Quoted, not interpreted, and carried so that the condition the engine tests is legible beside
    /// the answer — as <see cref="Observers.ParagraphB"/> carries § 107.33(b) and
    /// <see cref="Preflight.LeadIn"/> carries § 107.49's lead-in.
    /// </remarks>
    public const string Paragraph =
        "(d) If the small unmanned aircraft is powered, ensure that there is enough available power "
        + "for the small unmanned aircraft system to operate for the intended operational time;";

    /// <summary>
    /// <see cref="MapEntries.SufficientAvailablePower"/>: what § 107.49(d) says about the operation
    /// the caller states — the remote pilot in command's assertion, answered unchanged on the map's
    /// own entry, where the small unmanned aircraft is powered; and no obligation at all where it is
    /// not.
    /// </summary>
    /// <remarks>
    /// The condition first, and the assertion only past it. Where the caller states the aircraft is
    /// not powered, nothing is demanded and the finding says the paragraph states no obligation
    /// about this operation, which is a different answer from the obligation being met.
    /// </remarks>
    /// <param name="power">
    /// Whether the small unmanned aircraft is powered, as the caller states it: § 107.49(d)'s own
    /// condition. Required, and never inferred — an aircraft the caller has not described is not an
    /// unpowered aircraft.
    /// </param>
    /// <param name="assertions">What the caller asserts. Never defaulted.</param>
    /// <returns>
    /// The finding: the assertion recorded where the aircraft is powered, and the paragraph's
    /// silence where it is not. This entry has no unresolved outcome of its own — correspondence
    /// row 8 — so this resolves a value or throws.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="power"/> or <paramref name="assertions"/> is null.</exception>
    /// <exception cref="AssertionRequiredException">
    /// The aircraft is powered and the caller asserted nothing for the entry.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The asserted value is not an <see cref="Assertion"/>, is about another entry, or is attributed
    /// to somebody § 107.49's lead-in does not name.
    /// </exception>
    public static Resolution<SufficientAvailablePowerFinding> Enough(AircraftPower power, RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(power);
        ArgumentNullException.ThrowIfNull(assertions);

        // The paragraph's own condition, and nothing asked past it. "If the small unmanned aircraft
        // is powered" is what § 107.49(d) states its obligation under, so an aircraft the caller
        // states is not powered is an aircraft the paragraph says nothing about — and the engine
        // demands no assertion for a fact the corpus never asked anybody to report.
        if (power != AircraftPower.Powered)
        {
            return Resolution<SufficientAvailablePowerFinding>.FromValue(
                SufficientAvailablePowerFinding.StatesNoObligation(power));
        }

        return Assertions.Stated(MapEntries.SufficientAvailablePower, assertions).Match(
            availability => Resolution<SufficientAvailablePowerFinding>.FromValue(
                SufficientAvailablePowerFinding.Asserted(power, availability)),
            Resolution<SufficientAvailablePowerFinding>.FromUnresolved);
    }
}
