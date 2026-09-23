using System.Globalization;

namespace FaaPart107;

/// <summary>
/// What a caller states about the anti-collision lighting § 107.29(a)(2) and (b) require: whether
/// the small unmanned aircraft has any, whether it is lighted, whether its intensity has been
/// reduced, how far it is visible for, and who is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// None of it is in the corpus. What lighting an aircraft carries, how far that lighting can be
/// seen and what has been done to it are facts about one flight, so they are facts the caller
/// states, on the same terms as a waiver statement and a <see cref="StructureStatement"/>
/// (rules-factory decision 0021, and this engine's <c>docs/decisions/0001</c>): demanded,
/// attributed, recorded alongside the outcome, and never inferred. In particular the engine does
/// not assume an aircraft is lit and does not assume it is dark — assuming the first excuses an
/// operator who flew unlit, and assuming the second convicts one who did not.
/// </para>
/// <para>
/// <b>The four states are the four the entry's note asks for</b>: "Fitted and unfitted … and the
/// bound the same clause states — the intensity may be reduced but the lighting may not be
/// extinguished". They are made by factory, so a statement cannot carry a distance for lighting the
/// aircraft does not have, or say that extinguished lighting is visible for three miles. What the
/// caller never states is whether the requirement is met: the distance is stated, and
/// <see cref="Lights"/> applies § 107.29(a)(2) and (b)'s printed figure to it.
/// </para>
/// <para>
/// <b>No intensity is carried here, and no flash rate.</b> The corpus states neither figure — it
/// says the intensity "may" be reduced, and that the flash rate must be "sufficient to avoid a
/// collision" — so there is nothing for this type to hold and nothing for the engine to compare one
/// against. Whether a reduction is in the interest of safety is the remote pilot in command's
/// determination, the map entry <c>intensity-reduction-in-interest-of-safety</c>, and whether the
/// flash rate is sufficient is <c>flash-rate-sufficient</c>. Both are assertions, and neither is
/// stated here.
/// </para>
/// </remarks>
public sealed record LightingStatement
{
    private LightingStatement(Condition state, decimal? visibleForStatuteMiles, string statedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statedBy);
        State = state;
        VisibleForStatuteMiles = visibleForStatuteMiles;
        StatedBy = statedBy;
    }

    /// <summary>The condition of the aircraft's anti-collision lighting, as the caller states it.</summary>
    private enum Condition
    {
        /// <summary>The small unmanned aircraft has no anti-collision lighting.</summary>
        NoneFitted,

        /// <summary>It has anti-collision lighting, lighted, at an intensity that has not been reduced.</summary>
        Lighted,

        /// <summary>It has anti-collision lighting, lighted, with the intensity reduced.</summary>
        IntensityReduced,

        /// <summary>It has anti-collision lighting and the lighting has been extinguished.</summary>
        Extinguished,
    }

    /// <summary>
    /// How far the anti-collision lighting is visible for, in statute miles, as the caller states
    /// it — of the lighting as it now is, so of the reduced lighting where the intensity has been
    /// reduced. Null where the aircraft has no anti-collision lighting or the lighting has been
    /// extinguished, neither of which is lighting visible for any distance.
    /// </summary>
    /// <remarks>
    /// § 107.29(a)(2) and (b) state the distance the lighting must be visible for and do not say
    /// how that distance is established, so the figure is the caller's and this engine does not
    /// re-derive it. It is not § 107.51(c)'s "flight visibility": that is a quantity that section
    /// defines, turning on which objects are "prominent", and it belongs to other entries.
    /// </remarks>
    public decimal? VisibleForStatuteMiles { get; }

    /// <summary>Who is answerable for the statement. Free text; the engine does not parse it.</summary>
    public string StatedBy { get; }

    /// <summary>True where the small unmanned aircraft has anti-collision lighting at all.</summary>
    public bool Fitted => State != Condition.NoneFitted;

    /// <summary>
    /// True where the lighting is lighted. The clause requires "lighted anti-collision lighting",
    /// and lighting the aircraft does not have, or has extinguished, is not lighted.
    /// </summary>
    public bool Lighted => State is Condition.Lighted or Condition.IntensityReduced;

    /// <summary>
    /// True where the intensity has been reduced — what the clause's second sentence permits the
    /// remote pilot in command to do, on a determination the map holds as its own entry.
    /// </summary>
    public bool IntensityReduced => State == Condition.IntensityReduced;

    /// <summary>
    /// True where the lighting has been extinguished, which the same sentence says the remote pilot
    /// in command "may not" do.
    /// </summary>
    public bool Extinguished => State == Condition.Extinguished;

    private Condition State { get; }

    /// <summary>A statement that the small unmanned aircraft has no anti-collision lighting.</summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static LightingStatement NoneFitted(string statedBy) => new(Condition.NoneFitted, null, statedBy);

    /// <summary>
    /// A statement that the small unmanned aircraft has lighted anti-collision lighting, at an
    /// intensity that has not been reduced, visible for the stated distance.
    /// </summary>
    /// <param name="visibleForStatuteMiles">How far the lighting is visible for, in statute miles, not negative.</param>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static LightingStatement LightedAndVisibleFor(decimal visibleForStatuteMiles, string statedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(visibleForStatuteMiles);
        return new(Condition.Lighted, visibleForStatuteMiles, statedBy);
    }

    /// <summary>
    /// A statement that the lighting is lighted with its intensity reduced, visible for the stated
    /// distance as reduced.
    /// </summary>
    /// <param name="visibleForStatuteMiles">How far the reduced lighting is visible for, in statute miles, not negative.</param>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static LightingStatement IntensityReducedAndVisibleFor(decimal visibleForStatuteMiles, string statedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(visibleForStatuteMiles);
        return new(Condition.IntensityReduced, visibleForStatuteMiles, statedBy);
    }

    /// <summary>
    /// A statement that the aircraft has anti-collision lighting and it has been extinguished. No
    /// distance accompanies it: extinguished lighting is visible for none.
    /// </summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static LightingStatement FittedButExtinguished(string statedBy) =>
        new(Condition.Extinguished, null, statedBy);

    /// <inheritdoc/>
    public override string ToString() =>
        State switch
        {
            Condition.NoneFitted =>
                $"the small unmanned aircraft has no anti-collision lighting, as stated by {StatedBy}",
            Condition.Extinguished =>
                $"the small unmanned aircraft's anti-collision lighting has been extinguished, as stated by {StatedBy}",
            Condition.IntensityReduced => string.Create(
                CultureInfo.InvariantCulture,
                $"the small unmanned aircraft has lighted anti-collision lighting, its intensity reduced, visible for {VisibleForStatuteMiles} statute miles, as stated by {StatedBy}"),
            _ => string.Create(
                CultureInfo.InvariantCulture,
                $"the small unmanned aircraft has lighted anti-collision lighting visible for {VisibleForStatuteMiles} statute miles, as stated by {StatedBy}"),
        };
}
