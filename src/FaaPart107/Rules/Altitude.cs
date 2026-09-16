using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The altitude limit as § 107.51(b) prints it: <see cref="MapEntries.AltitudeLimit"/>, "(b) The
/// altitude of the small unmanned aircraft cannot be higher than 400 feet above ground level, unless
/// the small unmanned aircraft: (1) Is flown within a 400-foot radius of a structure; and (2) Does not
/// fly higher than 400 feet above the structure's immediate uppermost limit."
/// </summary>
/// <remarks>
/// Three figures, each in feet, as printed. They happen to be equal and are not interchangeable: the
/// entry's note asks for them as three distinct figures. Whether an altitude is within the limit is
/// <c>altitude-within-limit</c>'s, not this value's.
/// </remarks>
/// <param name="Waiver">The caller's waiver statement the limit was resolved under, recorded with it.</param>
public sealed record AltitudeLimit(WaiverStatement Waiver)
{
    /// <summary>The altitude above ground level the aircraft cannot be higher than, in feet: 400.</summary>
    public decimal AboveGroundLevelFeet { get; } = 400m;

    /// <summary>The radius of a structure, in feet, within which paragraph (b)(1) is met: 400.</summary>
    public decimal StructureRadiusFeet { get; } = 400m;

    /// <summary>How far above the structure's immediate uppermost limit paragraph (b)(2) allows, in feet: 400.</summary>
    public decimal AboveStructureUppermostLimitFeet { get; } = 400m;

    /// <summary>Where the limit is stated: <c>§ 107.51(b)</c>.</summary>
    public SourceLocator Authority => MapEntries.AltitudeLimit.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{AboveGroundLevelFeet} feet above ground level; within a {StructureRadiusFeet}-foot radius of a structure, {AboveStructureUppermostLimitFeet} feet above its immediate uppermost limit [{Authority}]");
}

/// <summary>§ 107.51(b): the altitude limit.</summary>
public static class Altitude
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary><see cref="MapEntries.AltitudeLimit"/>: the limit as printed.</summary>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The limit; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in force.
    /// </returns>
    public static Resolution<AltitudeLimit> Limit(WaiverStatement waiver) =>
        Waivers.Suspension(MapEntries.AltitudeLimit, Regulation, waiver) is { } suspended
            ? Resolution<AltitudeLimit>.FromUnresolved(suspended)
            : Resolution<AltitudeLimit>.FromValue(new AltitudeLimit(waiver));
}
