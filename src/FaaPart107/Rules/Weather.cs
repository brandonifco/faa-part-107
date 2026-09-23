using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The one finding § 107.51(c)-(d) lets this engine resolve:
/// <see cref="MapEntries.WeatherMinimumsMet"/> is not met, because neither of § 107.51(d)'s two
/// printed minimums is met.
/// </summary>
/// <remarks>
/// The entry is a conjunction — "(c) The minimum flight visibility … must be no less than 3 statute
/// miles" and "(d) The minimum distance … from clouds must be no less than: (1) 500 feet below the
/// cloud; and (2) 2,000 feet horizontally from the cloud" — so one conjunct that is false on every
/// reading settles it, whatever the other is. Neither cloud minimum met is that case: read
/// literally both must hold, read as a region around the cloud either suffices, and neither holds,
/// so <c>cloud-clearance</c>'s open question does not have to be answered to know § 107.51(d) is
/// not met. Every other situation reaches § 107.51(c), whose quantity <c>prominent-objects</c>
/// holds open, and <see cref="Weather.MinimumsMet"/> declines instead of constructing this.
/// </remarks>
/// <param name="FlightVisibilityStatuteMiles">The flight visibility the caller stated, in statute miles, recorded with the outcome.</param>
/// <param name="FeetBelowCloud">How far below the cloud the caller stated the aircraft is, in feet.</param>
/// <param name="FeetHorizontallyFromCloud">How far horizontally from the cloud the caller stated the aircraft is, in feet.</param>
/// <param name="Minimum">§ 107.51(c)'s figure, as <c>visibility-minimum</c> states it.</param>
/// <param name="Clearance">§ 107.51(d)'s two figures, as <c>cloud-clearance</c> states them, and the waiver statement this was resolved under.</param>
public sealed record WeatherMinimumsFinding(
    decimal FlightVisibilityStatuteMiles,
    decimal FeetBelowCloud,
    decimal FeetHorizontallyFromCloud,
    VisibilityMinimum Minimum,
    CloudClearance Clearance)
{
    /// <summary>
    /// Paragraph (d)(1): true while the aircraft is no less than <c>cloud-clearance</c>'s printed
    /// distance below the cloud. "No less than" is met at the figure and broken below it.
    /// </summary>
    public bool BelowCloudMinimumMet => FeetBelowCloud >= Clearance.BelowCloudFeet;

    /// <summary>
    /// Paragraph (d)(2): true while the aircraft is no less than <c>cloud-clearance</c>'s printed
    /// distance horizontally from the cloud. "No less than" is met at the figure and broken below it.
    /// </summary>
    public bool HorizontallyFromCloudMinimumMet => FeetHorizontallyFromCloud >= Clearance.HorizontallyFromCloudFeet;

    /// <summary>
    /// Whether § 107.51(c) and § 107.51(d) are both met: false, in every finding this engine can
    /// resolve. A finding is constructed only where neither cloud minimum is met, which makes the
    /// conjunction false on every reading; the engine can never resolve it true, because that would
    /// need § 107.51(c) met, and the quantity § 107.51(c)'s figure is compared against is
    /// <c>prominent-objects</c>' open question.
    /// </summary>
    public bool MinimumsMet => false;

    /// <summary>Where the rule is stated: <c>§ 107.51(c)-(d)</c>.</summary>
    public SourceLocator Authority => MapEntries.WeatherMinimumsMet.Locator;

    /// <summary>The caller's waiver statement, recorded with the outcome.</summary>
    public WaiverStatement Waiver => Clearance.Waiver;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"the minimums are not met: {FeetBelowCloud} feet below the cloud is less than {Clearance.BelowCloudFeet} and "
            + $"{FeetHorizontallyFromCloud} feet horizontally from it is less than {Clearance.HorizontallyFromCloudFeet} "
            + $"[{Authority}]; {Waiver}");
}

/// <summary>
/// § 107.51(c)-(d): whether the visibility and cloud-clearance minimums are met,
/// <see cref="MapEntries.WeatherMinimumsMet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entry states no figure of its own. The 3 statute miles are <c>visibility-minimum</c>'s and
/// the 500 and 2,000 feet are <c>cloud-clearance</c>'s, reached through <c>dependsOn</c> and read
/// from those entries' values, so a figure that moved there moves here.
/// </para>
/// <para>
/// Its third dependency, <c>prominent-objects</c>, answers nothing: the map records its question as
/// unresolved, and it declines <see cref="UnresolvedReason.RequiresInterpretation"/> on every
/// request. That is not an accident of build order, and this entry is where it bites.
/// <c>visibility-minimum</c>'s note says the gap is "carried by the entry that applies the
/// definition rather than by the one that states the figure", and this entry is the one that
/// applies it: § 107.51(c)'s threshold is on "flight visibility", which the section defines as the
/// average slant distance at which <em>prominent</em> objects may be seen and identified, and the
/// corpus fixes no degree of prominence. So a distance the caller states is a flight visibility
/// only once that degree is fixed, and comparing the stated figure with 3 statute miles and
/// answering met or not met would be this engine fixing it.
/// </para>
/// <para>
/// This entry does not borrow <c>prominent-objects</c>' decline either, the way
/// <see cref="NightTraining"/> does not borrow <c>knowledge-recency</c>'s. It answers on its own
/// terms, and its own terms are a conjunction: one conjunct false on every reading settles the
/// whole of it. Neither cloud minimum met is exactly that, and the entry resolves it — which is why
/// the map split <c>prominent-objects</c> out instead of reclassifying this entry, "classifying the
/// whole entry by one of its clauses would have thrown away the 500-foot and 2,000-foot figures".
/// Everywhere else § 107.51(c) is reached and the answer is undetermined, so the decline cites
/// <c>prominent-objects</c>' § 107.51(c) and names the term the corpus leaves undefined.
/// </para>
/// </remarks>
public static class Weather
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary>
    /// <see cref="MapEntries.WeatherMinimumsMet"/>: whether the operation the caller states meets
    /// "(c) The minimum flight visibility, as observed from the location of the control station must
    /// be no less than 3 statute miles" and "(d) The minimum distance of the small unmanned aircraft
    /// from clouds must be no less than: (1) 500 feet below the cloud; and (2) 2,000 feet
    /// horizontally from the cloud."
    /// </summary>
    /// <remarks>
    /// Resolves only where neither cloud minimum is met: § 107.51(d) is then broken on either
    /// reading of how its two figures combine, so the conjunction is false whatever the flight
    /// visibility is, and § 107.51(c) is never reached. Every other situation reaches § 107.51(c)
    /// and declines, including one whose stated visibility is far above or far below the figure:
    /// the stated distance is not § 107.51(c)'s flight visibility until "prominent" is fixed, and
    /// this engine does not fix it. Where the cloud half is itself undetermined — one minimum met
    /// and not the other — the decline names <c>cloud-clearance</c>'s question as well.
    /// </remarks>
    /// <param name="flightVisibilityStatuteMiles">The flight visibility the caller states, observed from the location of the control station, in statute miles, not negative.</param>
    /// <param name="feetBelowCloud">How far below the cloud the caller states the small unmanned aircraft is, in feet, not negative.</param>
    /// <param name="feetHorizontallyFromCloud">How far horizontally from the cloud the caller states the small unmanned aircraft is, in feet, not negative.</param>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding, the minimums not met, where neither cloud minimum is met;
    /// <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in
    /// force; otherwise <see cref="UnresolvedReason.RequiresInterpretation"/> citing
    /// <c>prominent-objects</c>' § 107.51(c).
    /// </returns>
    public static Resolution<WeatherMinimumsFinding> MinimumsMet(
        decimal flightVisibilityStatuteMiles,
        decimal feetBelowCloud,
        decimal feetHorizontallyFromCloud,
        WaiverStatement waiver)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(flightVisibilityStatuteMiles);
        ArgumentOutOfRangeException.ThrowIfNegative(feetBelowCloud);
        ArgumentOutOfRangeException.ThrowIfNegative(feetHorizontallyFromCloud);

        if (Waivers.Suspension(MapEntries.WeatherMinimumsMet, Regulation, waiver) is { } suspended)
        {
            return Resolution<WeatherMinimumsFinding>.FromUnresolved(suspended);
        }

        return Visibility.Minimum(waiver).Match(
            minimum => Clouds.Clearance(waiver).Match(
                clearance =>
                {
                    var finding = new WeatherMinimumsFinding(
                        flightVisibilityStatuteMiles,
                        feetBelowCloud,
                        feetHorizontallyFromCloud,
                        minimum,
                        clearance);

                    return !finding.BelowCloudMinimumMet && !finding.HorizontallyFromCloudMinimumMet
                        ? Resolution<WeatherMinimumsFinding>.FromValue(finding)
                        : Resolution<WeatherMinimumsFinding>.FromUnresolved(Undetermined(finding));
                },
                Resolution<WeatherMinimumsFinding>.FromUnresolved),
            Resolution<WeatherMinimumsFinding>.FromUnresolved);
    }

    /// <summary>
    /// The decline for a situation that reaches § 107.51(c): the visibility half is undetermined
    /// however the stated figure compares with the minimum, and the cloud half is named too when it
    /// is itself undetermined.
    /// </summary>
    private static UnresolvedResult Undetermined(WeatherMinimumsFinding finding)
    {
        var cloudHalf = finding.BelowCloudMinimumMet == finding.HorizontallyFromCloudMinimumMet
            ? string.Empty
            : string.Create(
                CultureInfo.InvariantCulture,
                $"; and {finding.FeetBelowCloud} feet below the cloud with {finding.FeetHorizontallyFromCloud} feet "
                + $"horizontally from it meets one of § 107.51(d)'s two minimums and not the other, which the map "
                + $"entry '{MapEntries.CloudClearance.Id}' holds open");

        return new UnresolvedResult(
            UnresolvedReason.RequiresInterpretation,
            string.Create(
                CultureInfo.InvariantCulture,
                $"decide whether the map entry '{MapEntries.WeatherMinimumsMet.Id}' is met on a stated flight "
                + $"visibility of {finding.FlightVisibilityStatuteMiles} statute miles: § 107.51(c) requires no less "
                + $"than {finding.Minimum.StatuteMiles} statute miles of the flight visibility it defines, and the "
                + $"map entry '{MapEntries.ProminentObjects.Id}' holds open which objects are \"prominent\" — the "
                + $"degree that fixes the distance the definition reports — so the stated figure is not yet that "
                + $"quantity{cloudHalf}"),
            MapEntries.ProminentObjects.Locator);
    }
}
