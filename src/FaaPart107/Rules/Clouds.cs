using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The minimum distance from clouds as § 107.51(d) prints it: <see cref="MapEntries.CloudClearance"/>,
/// "(d) The minimum distance of the small unmanned aircraft from clouds must be no less than: (1) 500
/// feet below the cloud; and (2) 2,000 feet horizontally from the cloud."
/// </summary>
/// <remarks>
/// Two figures, each in feet, as printed, and kept apart: the entry's note asks for both, because a
/// single combined value would lose the distinction between the vertical minimum and the horizontal
/// one. Neither is a clearance above a cloud — § 107.51(d) states none — and how the two combine is
/// the entry's open question, which <see cref="Clouds.Clearance"/> declines rather than decides.
/// </remarks>
/// <param name="Waiver">The caller's waiver statement the minimum was resolved under, recorded with it.</param>
public sealed record CloudClearance(WaiverStatement Waiver)
{
    /// <summary>How far below the cloud paragraph (d)(1) prints, in feet: 500.</summary>
    public decimal BelowCloudFeet { get; } = 500m;

    /// <summary>How far horizontally from the cloud paragraph (d)(2) prints, in feet: 2,000.</summary>
    public decimal HorizontallyFromCloudFeet { get; } = 2000m;

    /// <summary>Where the minimum is stated: <c>§ 107.51(d)</c>.</summary>
    public SourceLocator Authority => MapEntries.CloudClearance.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{BelowCloudFeet} feet below the cloud; {HorizontallyFromCloudFeet} feet horizontally from the cloud [{Authority}]");
}

/// <summary>§ 107.51(d): the minimum distance from clouds.</summary>
public static class Clouds
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary>
    /// <see cref="MapEntries.CloudClearance"/>: the minimum as printed, or, asked for it as one
    /// distance, the entry's decline.
    /// </summary>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <param name="asOneDistance">
    /// False for the minimum as printed, both figures. True asks for "the minimum distance … from
    /// clouds" as the single distance § 107.51(d)'s own sentence promises before printing two, which
    /// turns on the entry's open question: the two minimums are joined by "and" and no clearance
    /// above a cloud is stated, so read literally both must hold and read as a region around the
    /// cloud either suffices, and the corpus does not say which.
    /// </param>
    /// <returns>
    /// The minimum; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is
    /// in force; <see cref="UnresolvedReason.RequiresInterpretation"/> citing § 107.51(d) for one distance.
    /// </returns>
    public static Resolution<CloudClearance> Clearance(WaiverStatement waiver, bool asOneDistance = false)
    {
        if (Waivers.Suspension(MapEntries.CloudClearance, Regulation, waiver) is { } suspended)
        {
            return Resolution<CloudClearance>.FromUnresolved(suspended);
        }

        if (asOneDistance)
        {
            return Resolution<CloudClearance>.FromUnresolved(new UnresolvedResult(
                UnresolvedReason.RequiresInterpretation,
                $"state the minimum of the map entry '{MapEntries.CloudClearance.Id}' as one distance from a cloud: "
                + "§ 107.51(d) joins 500 feet below the cloud and 2,000 feet horizontally from the cloud with \"and\" "
                + "and states no clearance above a cloud, and the corpus does not say whether both minimums must hold "
                + "or either suffices",
                MapEntries.CloudClearance.Locator));
        }

        return Resolution<CloudClearance>.FromValue(new CloudClearance(waiver));
    }
}
