using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Whether the anti-collision lighting § 107.29(a)(2) and (b) require is there, as
/// <see cref="MapEntries.AntiCollisionLighting"/> states the requirement: what the caller stated
/// about the aircraft's lighting, the printed distance it must be visible for, and what the clause's
/// two assertion entries answered about it.
/// </summary>
/// <remarks>
/// <para>
/// Each element is kept where the map puts it. The distance is this entry's, because the figure is
/// printed in this entry's own evidence; the flash rate is <c>flash-rate-sufficient</c>'s and the
/// determination about reducing the intensity is
/// <c>intensity-reduction-in-interest-of-safety</c>'s, and neither is restated here. Where a
/// constituent was not asked its finding is null, which records that its question was not reached
/// — never that it was answered no.
/// </para>
/// </remarks>
/// <param name="LightingStated">What the caller stated about the aircraft's anti-collision lighting.</param>
/// <param name="FlashRate">
/// What <c>flash-rate-sufficient</c> answered, or null where the aircraft's own lighting settled the
/// conjunction and that entry was not asked.
/// </param>
/// <param name="Reduction">
/// What <c>intensity-reduction-in-interest-of-safety</c> answered, or null where the caller stated no
/// reduction and that entry was not asked.
/// </param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record AntiCollisionLightingFinding(
    LightingStatement LightingStated,
    FlashRateSufficientFinding? FlashRate,
    IntensityReductionFinding? Reduction,
    WaiverStatement Waiver)
{
    /// <summary>What the caller stated about the lighting, checked to be present.</summary>
    public LightingStatement LightingStated { get; } =
        LightingStated ?? throw new ArgumentNullException(nameof(LightingStated));

    /// <summary>The waiver statement, checked to be present.</summary>
    public WaiverStatement Waiver { get; } = Waiver ?? throw new ArgumentNullException(nameof(Waiver));

    /// <summary>
    /// The distance the lighting must be visible for, in statute miles, as § 107.29(a)(2) and (b)
    /// print it: 3, in both paragraphs, "visible for at least 3 statute miles".
    /// </summary>
    /// <remarks>
    /// The figure is this entry's own. § 107.51(c)'s minimum flight visibility is also 3 statute
    /// miles and is <see cref="MapEntries.VisibilityMinimum"/>'s, stated by another section about
    /// another quantity; the two figures are equal and neither is read from the other.
    /// </remarks>
    public decimal VisibleForAtLeastStatuteMiles { get; } = 3m;

    /// <summary>True where the small unmanned aircraft has anti-collision lighting, as stated.</summary>
    public bool Fitted => LightingStated.Fitted;

    /// <summary>
    /// True where the lighting is lighted, as the clause's "lighted anti-collision lighting"
    /// requires: false where the aircraft has none, and false where it has been extinguished.
    /// </summary>
    public bool Lighted => LightingStated.Lighted;

    /// <summary>
    /// Whether the lighting is visible for at least <see cref="VisibleForAtLeastStatuteMiles"/>.
    /// "At least" is met at the figure and broken below it. Null where no distance was stated,
    /// because the aircraft has no lighting or the lighting has been extinguished.
    /// </summary>
    public bool? VisibleFarEnough => LightingStated.VisibleForStatuteMiles is { } miles
        ? miles >= VisibleForAtLeastStatuteMiles
        : null;

    /// <summary>
    /// What <c>flash-rate-sufficient</c> answered about the flash rate, or null where that entry
    /// was not asked.
    /// </summary>
    public bool? FlashRateSufficient => FlashRate?.Holds;

    /// <summary>
    /// What <c>intensity-reduction-in-interest-of-safety</c> answered about the reduction, or null
    /// where no reduction was stated and that entry was not asked.
    /// </summary>
    public bool? ReductionInInterestOfSafety => Reduction?.Holds;

    /// <summary>
    /// Whether what was done to the lighting is within the bound the clause's second sentence
    /// states: the intensity "may" be reduced, on the remote pilot in command's determination, and
    /// the lighting "may not" be extinguished. Extinguished lighting is outside it whatever that
    /// determination was, and a reduction the determination does not carry is outside it too.
    /// </summary>
    public bool WithinTheBound =>
        Lighted && (!LightingStated.IntensityReduced || Reduction is { Holds: true });

    /// <summary>
    /// Whether the aircraft has the anti-collision lighting § 107.29(a)(2) and (b) require: lighted
    /// lighting — so fitted, and not extinguished — visible for at least the printed figure, whose
    /// flash rate <c>flash-rate-sufficient</c> answered sufficient, with anything done to it within
    /// the clause's bound.
    /// </summary>
    public bool Met =>
        Lighted && VisibleFarEnough == true && FlashRate is { Holds: true } && WithinTheBound;

    /// <summary>Where the requirement is stated: <c>§ 107.29(a)(2), (b)</c>.</summary>
    public SourceLocator Authority => MapEntries.AntiCollisionLighting.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"the anti-collision lighting § 107.29(a)(2) and (b) require is {(Met ? "there" : "not there")} "
            + $"[{Authority}]: {LightingStated}, against at least {VisibleForAtLeastStatuteMiles} statute miles"
            + $"{Constituent(FlashRate)}{Constituent(Reduction)}; {Waiver}");

    private static string Constituent(object? answered) =>
        answered is null ? string.Empty : $"; {answered}";
}

/// <summary>
/// § 107.29(a)(2) and (b): the anti-collision lighting the small unmanned aircraft must have,
/// <see cref="MapEntries.AntiCollisionLighting"/>.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for the lighting rather than for the entry, because a type named
/// <c>AntiCollisionLighting</c> collides with the <c>AntiCollisionLighting</c> the generated
/// <see cref="Handlers"/> partial declares — the reason <see cref="Yielding"/> is named as it is —
/// and because <see cref="Lighting"/> is already the sibling entry on this locator.
/// </para>
/// <para>
/// <b>This entry states the requirement, and the clause's two questions belong to other entries.</b>
/// The evidence is two paragraphs stating one sentence twice, "The small unmanned aircraft has
/// lighted anti-collision lighting visible for at least 3 statute miles that has a flash rate
/// sufficient to avoid a collision", with a second sentence bounding what may be done to it. Of
/// that, the map gives the flash rate to <c>flash-rate-sufficient</c> and the determination about
/// reducing the intensity to <c>intensity-reduction-in-interest-of-safety</c>, both
/// <c>kind: assertion</c> and both reached here through <c>dependsOn</c>. What is left, and what
/// this entry answers, is the requirement itself: that the aircraft <em>has</em> the lighting, that
/// it is <em>lighted</em>, that it is visible for at least the distance the clause prints, and that
/// what has been done to it is within the clause's bound.
/// </para>
/// <para>
/// <b>The 3 statute miles are this entry's figure, as printed.</b> They are in this entry's own
/// evidence, twice, and the entry's note says so: "the 3 statute mile visibility figure the corpus
/// states plainly". So the figure is stated here and the caller's stated distance is compared
/// against it, the way <see cref="VisibilityMinimum"/> states § 107.51(c)'s figure. It is not that
/// entry's figure borrowed: § 107.51(c) states a minimum flight visibility, a quantity that section
/// defines and whose definition turns on a term the map holds open, and this clause states how far
/// a light must be visible. The two figures are equal and independent.
/// </para>
/// <para>
/// <b>No rate and no intensity are modelled</b>, for the reason the two assertion entries give: the
/// corpus states neither. The engine takes no flashes per minute, no candela and no proportion by
/// which an intensity was reduced, and compares none of them against anything.
/// </para>
/// <para>
/// <b>One requirement, stated twice.</b> § 107.29(a)(2) states it for operation at night and
/// § 107.29(b) states it, word for word, for operation during periods of civil twilight — which is
/// why this entry's locator cites both and why the entry's note asks for it "for night and for
/// civil twilight". Which period an operation is in is not asked here and is not an input: that is
/// what <c>night-operation</c> (§ 107.29(a)) and <c>civil-twilight-operation</c> (§ 107.29(b)-(c))
/// turn on, and both of them depend on this entry for the lighting.
/// </para>
/// <para>
/// <b>The gate is this entry's own and runs first.</b> § 107.205(b) lists "Section 107.29(a)(2) and
/// (b)", which is this entry's <c>suspendedBy</c> and both constituents' as well, so one caller
/// statement covers all three. Reading it here first means a stated waiver declines under
/// <em>this</em> entry's name rather than under whichever constituent would have been asked first,
/// and that no fact is demanded for an entry the caller has said is out of reach. Its other
/// consequence is worth stating plainly: once this gate has passed, neither constituent can decline
/// — their only decline is the same gate on the same statement — so
/// <see cref="Undetermined(MapEntry, UnresolvedResult, LightingStatement)"/> is not reachable
/// through the public API today. It is written all the same, and the constituents' actual results
/// are what the answer is read off, so that a question the map later opens or settles moves this
/// entry's answer with nothing changed here (issue #78's shape).
/// </para>
/// </remarks>
public static class Lights
{
    /// <summary>
    /// The regulation § 107.205 lists that states the entry, as § 107.205(b) designates it:
    /// "Section 107.29(a)(2) and (b)".
    /// </summary>
    /// <remarks>
    /// It is deliberately not the entry's locator. The locator cites the passage,
    /// <c>§ 107.29(a)(2), (b)</c>; the list designates what a certificate may authorize deviation
    /// from, and § 107.205(b) covers those two paragraphs and nothing else of § 107.29. It is the
    /// same string both constituents check a statement against, because § 107.205 lists the
    /// paragraphs once for all three entries.
    /// </remarks>
    public const string Regulation = "§ 107.29(a)(2) and (b)";

    /// <summary>
    /// <see cref="MapEntries.AntiCollisionLighting"/>: whether the small unmanned aircraft has the
    /// anti-collision lighting § 107.29(a)(2) and (b) require, on what the caller states about the
    /// lighting and what the clause's two assertion entries answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is the behaviour. The waiver gate is read first, as this entry's own. Then the
    /// aircraft's own lighting: where it has none, or has extinguished what it has, the requirement
    /// is a conjunction with a false conjunct and is settled whatever the flash rate would have been
    /// — so the finding is resolved there, and a fact about lighting the aircraft does not have is
    /// not demanded of the caller. That is <see cref="Weather"/>'s reading of a conjunction, and the
    /// same care the two constituents take in reading their gate before demanding their assertion.
    /// </para>
    /// <para>
    /// Where there is lighted lighting, <c>flash-rate-sufficient</c> is asked, because the clause's
    /// first sentence conjoins it — the lighting must be lighting "that has a flash rate sufficient
    /// to avoid a collision" — and it is asked however the stated distance compares with the printed
    /// figure, so that what the caller owes does not shift with the other conjunct's answer. Then,
    /// and only where the caller states the intensity was reduced,
    /// <c>intensity-reduction-in-interest-of-safety</c> is asked, because that is the situation the
    /// clause's second sentence conditions: a determination about a reduction nobody made is beside
    /// the point, and demanding it would be asking for a fact the caller does not owe.
    /// </para>
    /// </remarks>
    /// <param name="lighting">What the caller states about the aircraft's anti-collision lighting. Never inferred; required once the entry is reachable, and demanded after the gate (<c>docs/decisions/0008</c>).</param>
    /// <param name="waiver">Whether a waiver of § 107.29(a)(2) and (b) is in force, as the caller states it.</param>
    /// <param name="assertions">What the caller asserts, for the two entries this one depends on. Never defaulted.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver of § 107.29(a)(2) and (b) is in force; and, where a constituent this entry asked did
    /// not resolve, this entry's own decline carrying that constituent's reason and citing its
    /// locator.
    /// </returns>
    /// <exception cref="AssertionRequiredException">
    /// A constituent was asked and the caller asserted nothing for it.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and
    /// <paramref name="lighting"/> was not stated; or an asserted value is not an
    /// <see cref="Assertion"/> or is about another entry.
    /// </exception>
    public static Resolution<AntiCollisionLightingFinding> AsRequired(
        LightingStatement? lighting,
        WaiverStatement waiver,
        RuleRequest assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);

        if (Waivers.Suspension(MapEntries.AntiCollisionLighting, Regulation, waiver) is { } suspended)
        {
            return Resolution<AntiCollisionLightingFinding>.FromUnresolved(suspended);
        }

        var stated = Demands.Of(
            lighting, MapEntries.AntiCollisionLighting, nameof(Requests.AntiCollisionLightingRequest.Lighting));

        // "has lighted anti-collision lighting …": an aircraft with none, and one whose lighting has
        // been extinguished, has no lighted anti-collision lighting, and the conjunction is false
        // whatever its other conjuncts would say. Neither constituent is asked, because neither
        // question is reached: there is no lighting here whose flash rate could avoid a collision,
        // and extinguishing is outside the second sentence's bound however the remote pilot in
        // command determined.
        if (!stated.Lighted)
        {
            return Resolution<AntiCollisionLightingFinding>.FromValue(
                new AntiCollisionLightingFinding(stated, null, null, waiver));
        }

        return FlashRate.SufficientToAvoidACollision(waiver, assertions).Match(
            rate => stated.IntensityReduced
                ? Lighting.IntensityReduction(waiver, assertions).Match(
                    reduction => Resolution<AntiCollisionLightingFinding>.FromValue(
                        new AntiCollisionLightingFinding(stated, rate, reduction, waiver)),
                    open => Resolution<AntiCollisionLightingFinding>.FromUnresolved(
                        Undetermined(MapEntries.IntensityReductionInInterestOfSafety, open, stated)))
                : Resolution<AntiCollisionLightingFinding>.FromValue(
                    new AntiCollisionLightingFinding(stated, rate, null, waiver)),
            open => Resolution<AntiCollisionLightingFinding>.FromUnresolved(
                Undetermined(MapEntries.FlashRateSufficient, open, stated)));
    }

    /// <summary>
    /// This entry's own decline for a situation a constituent could not answer: it names the entry
    /// the caller asked about and the constituent whose question blocks it, and it carries that
    /// constituent's reason and cites that constituent's locator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is not the constituent's decline handed back (issue #78, and the shape
    /// <c>docs/decisions/0001</c> records for <c>speed-within-limit</c> → <c>speed-limit</c>). The
    /// reason and the locator are the constituent's, because the question that blocks the answer is
    /// the constituent's question and a citation should lead to where that question is stated; what
    /// was attempted is this entry's, so that a caller who asked about the anti-collision lighting
    /// is told so by name.
    /// </para>
    /// <para>
    /// Here the citation distinguishes nothing at all, and that is worth being explicit about: all
    /// three entries — this one and both constituents — carry the locator
    /// <c>§ 107.29(a)(2), (b)</c>, so citing the constituent's locator cites this entry's too.
    /// <see cref="UnresolvedResult.Attempted"/> is the only thing that says which entry was asked
    /// and which could not answer, and the kernel's <see cref="UnresolvedResult"/> has no field for
    /// an entry id (<c>docs/decisions/0001</c> records the same blind spot for § 107.51(a)'s two
    /// entries, where there were two rather than three).
    /// </para>
    /// </remarks>
    /// <param name="constituent">The entry whose question blocks the answer, as the map has it.</param>
    /// <param name="blocking">What that entry answered.</param>
    /// <param name="lighting">What the caller stated about the lighting, recorded in what was attempted.</param>
    /// <returns>The decline.</returns>
    private static UnresolvedResult Undetermined(
        MapEntry constituent,
        UnresolvedResult blocking,
        LightingStatement lighting) =>
        new(
            blocking.Reason,
            $"decide whether the map entry '{MapEntries.AntiCollisionLighting.Id}' "
            + $"[{MapEntries.AntiCollisionLighting.Locator.Citation}] is met, where {lighting}: "
            + $"§ 107.29(a)(2) and (b) state it of lighting the map entry '{constituent.Id}' "
            + $"[{constituent.Locator.Citation}] speaks for — {constituent.Name} — and that entry did not resolve",
            constituent.Locator);
}
