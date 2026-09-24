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

/// <summary>
/// Whether an altitude is within § 107.51(b)'s limit: <see cref="MapEntries.AltitudeWithinLimit"/>.
/// </summary>
/// <remarks>
/// The paragraph is a ceiling with one exception of two parts, joined by "and". The ceiling is met
/// while the altitude is not higher than 400 feet above ground level. Above that, the aircraft is
/// within the limit only where both parts of the exception hold: it "(1) Is flown within a 400-foot
/// radius of a structure; and (2) Does not fly higher than 400 feet above the structure's immediate
/// uppermost limit." <see cref="WithinLimit"/> is that sentence, and nothing else.
/// </remarks>
/// <param name="AltitudeAboveGroundLevelFeet">The altitude the caller supplied, in feet above ground level.</param>
/// <param name="WithinGroundLevelCeiling">
/// Paragraph (b)'s own ceiling: true while the altitude is not higher than 400 feet above ground level.
/// </param>
/// <param name="WithinStructureRadius">
/// Paragraph (b)(1): true while the caller's statement names a structure and the stated distance is
/// within the printed 400-foot radius. False when the statement names no structure, because there is
/// then no structure it is flown within a radius of.
/// </param>
/// <param name="WithinStructureAllowance">
/// Paragraph (b)(2): true while the caller's statement names a structure and the altitude is not
/// higher than 400 feet above that structure's immediate uppermost limit. False when the statement
/// names no structure, because there is then no uppermost limit to measure against.
/// </param>
/// <param name="Structure">
/// What the caller stated about the structure, recorded with the outcome. A statement that names
/// none leaves both parts of the exception unmet, and the <b>constructor</b> holds that rather than
/// the rule's discipline holding it (<c>#101</c>).
/// </param>
/// <param name="Limit">The limit it was compared with, and the waiver statement it was resolved under.</param>
public sealed record AltitudeFinding(
    decimal AltitudeAboveGroundLevelFeet,
    bool WithinGroundLevelCeiling,
    bool WithinStructureRadius,
    bool WithinStructureAllowance,
    StructureStatement Structure,
    AltitudeLimit Limit)
{
    /// <summary>
    /// What the caller stated about the structure, checked against what this finding reports of
    /// § 107.51(b)'s exception.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both parts of the exception are claimed <em>against a structure</em>: (b)(1) is a radius of
    /// one, and (b)(2) is an allowance above one's immediate uppermost limit. A statement that names
    /// no structure measured neither, so neither part can be met, and
    /// <see cref="WithinStructureRadius"/> or <see cref="WithinStructureAllowance"/> true beside it
    /// would lift the ceiling on a structure nobody stated. False stands: it is not a comparison
    /// this finding could not make, it is the exception simply not satisfied, which is what
    /// <see cref="StructureStatement.NoneWithinRadius"/> says.
    /// </para>
    /// <para>
    /// This is the check <see cref="WeatherMinimumsFinding.Cloud"/> makes on the same shape one
    /// paragraph down, and the two differ only where § 107.51(b) and § 107.51(d) differ: there,
    /// neither of the two members has an honest value with no cloud stated — false would read the
    /// absence as a measurement, and true would decide a question the map holds open — so the
    /// finding itself is refused, rather than one value of each member.
    /// </para>
    /// </remarks>
    public StructureStatement Structure { get; } =
        CheckStructure(Structure, WithinStructureRadius, WithinStructureAllowance, nameof(Structure));

    /// <summary>
    /// Whether the altitude is within the limit: the ceiling is met, or both parts of the exception are.
    /// </summary>
    public bool WithinLimit => WithinGroundLevelCeiling || (WithinStructureRadius && WithinStructureAllowance);

    /// <summary>Where the rule is stated: <c>§ 107.51(b)</c>.</summary>
    public SourceLocator Authority => MapEntries.AltitudeWithinLimit.Locator;

    /// <summary>The caller's waiver statement, recorded with the outcome.</summary>
    public WaiverStatement Waiver => Limit.Waiver;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{AltitudeAboveGroundLevelFeet} feet above ground level is {(WithinLimit ? "within" : "beyond")} {Limit.AboveGroundLevelFeet} feet above ground level [{Authority}]; {Structure}; {Waiver}");

    private static StructureStatement CheckStructure(
        StructureStatement structure,
        bool withinStructureRadius,
        bool withinStructureAllowance,
        string name)
    {
        ArgumentNullException.ThrowIfNull(structure, name);
        return structure.NamesAStructure || !(withinStructureRadius || withinStructureAllowance)
            ? structure
            : throw new ArgumentException(
                "§ 107.51(b)'s exception is claimed against a structure, so a statement that names "
                + "none meets neither of its two parts: there is no structure the aircraft is flown "
                + "within a 400-foot radius of, and no immediate uppermost limit to measure 400 feet "
                + "above",
                name);
    }
}

/// <summary>§ 107.51(b): the altitude limit, and whether an altitude is within it.</summary>
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

    /// <summary>
    /// <see cref="MapEntries.AltitudeWithinLimit"/>: whether <paramref name="altitudeAboveGroundLevelFeet"/>
    /// meets "cannot be higher than 400 feet above ground level, unless the small unmanned aircraft: (1) Is
    /// flown within a 400-foot radius of a structure; and (2) Does not fly higher than 400 feet above the
    /// structure's immediate uppermost limit."
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every comparison is the paragraph's own wording. "Cannot be higher than 400 feet" and "does not fly
    /// higher than 400 feet above" are met at the figure and broken above it; "within a 400-foot radius" is
    /// met at the radius and broken beyond it. The two parts of the exception are joined by "and", so a
    /// structure too far away does not lift the ceiling however low the flight, and a flight higher than
    /// the structure's immediate uppermost limit plus 400 is not saved by standing next to it.
    /// </para>
    /// <para>
    /// The structure is <paramref name="structure"/>'s to state, never the engine's to assume: with no
    /// statement the entry refuses, and a statement that the aircraft is flown within a 400-foot radius of
    /// no structure leaves the ceiling governing alone. The figures compared against are
    /// <c>altitude-limit</c>'s, reached through <c>dependsOn</c> and not restated here.
    /// </para>
    /// </remarks>
    /// <param name="altitudeAboveGroundLevelFeet">The altitude, in feet above ground level, not negative.</param>
    /// <param name="structure">What the caller states about the structure the exception is claimed under.</param>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in
    /// force.
    /// </returns>
    public static Resolution<AltitudeFinding> Within(
        decimal altitudeAboveGroundLevelFeet,
        StructureStatement structure,
        WaiverStatement waiver)
    {
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentOutOfRangeException.ThrowIfNegative(altitudeAboveGroundLevelFeet);

        if (Waivers.Suspension(MapEntries.AltitudeWithinLimit, Regulation, waiver) is { } suspended)
        {
            return Resolution<AltitudeFinding>.FromUnresolved(suspended);
        }

        return Limit(waiver).Match(
            limit => Resolution<AltitudeFinding>.FromValue(new AltitudeFinding(
                altitudeAboveGroundLevelFeet,
                WithinGroundLevelCeiling: altitudeAboveGroundLevelFeet <= limit.AboveGroundLevelFeet,
                WithinStructureRadius: structure.DistanceFeet is { } distance && distance <= limit.StructureRadiusFeet,
                WithinStructureAllowance: structure.ImmediateUppermostLimitFeet is { } uppermost
                    && altitudeAboveGroundLevelFeet <= uppermost + limit.AboveStructureUppermostLimitFeet,
                structure,
                limit)),
            Resolution<AltitudeFinding>.FromUnresolved);
    }
}
