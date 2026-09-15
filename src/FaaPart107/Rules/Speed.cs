using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The groundspeed limit as § 107.51(a) prints it: <see cref="MapEntries.SpeedLimit"/>, "(a) The
/// groundspeed of the small unmanned aircraft may not exceed 87 knots (100 miles per hour)."
/// </summary>
/// <remarks>
/// Both figures, both units, as printed, and neither is chosen. They are not equal (87 knots is
/// about 100.12 miles per hour), and the corpus does not say which governs; that is the entry's
/// open question, which the engine declines rather than decides.
/// </remarks>
/// <param name="Waiver">The caller's waiver statement the limit was resolved under, recorded with it.</param>
public sealed record GroundspeedLimit(WaiverStatement Waiver)
{
    /// <summary>The figure printed in knots.</summary>
    public Groundspeed Knots { get; } = Groundspeed.InKnots(87m);

    /// <summary>The figure printed in parentheses, in miles per hour.</summary>
    public Groundspeed MilesPerHour { get; } = Groundspeed.InMilesPerHour(100m);

    /// <summary>Where the limit is stated: <c>§ 107.51(a)</c>.</summary>
    public SourceLocator Authority => MapEntries.SpeedLimit.Locator;

    /// <inheritdoc/>
    public override string ToString() => $"{Knots} ({MilesPerHour}) [{Authority}]";
}

/// <summary>
/// Whether a groundspeed is within § 107.51(a)'s limit: <see cref="MapEntries.SpeedWithinLimit"/>.
/// </summary>
/// <param name="Groundspeed">The groundspeed the caller supplied.</param>
/// <param name="WithinLimit">True when it exceeds neither printed figure; false when it exceeds both.</param>
/// <param name="Limit">The limit it was compared with, and the waiver statement it was resolved under.</param>
public sealed record GroundspeedFinding(Groundspeed Groundspeed, bool WithinLimit, GroundspeedLimit Limit)
{
    /// <summary>Where the rule is stated: <c>§ 107.51(a)</c>.</summary>
    public SourceLocator Authority => MapEntries.SpeedWithinLimit.Locator;

    /// <summary>The caller's waiver statement, recorded with the outcome.</summary>
    public WaiverStatement Waiver => Limit.Waiver;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Groundspeed} is {(WithinLimit ? "within" : "beyond")} {Limit.Knots} ({Limit.MilesPerHour}) [{Authority}]; {Waiver}";
}

/// <summary>§ 107.51(a): the groundspeed limit, and whether a groundspeed is within it.</summary>
public static class Speed
{
    /// <summary>The regulation § 107.205(i) lists that states both entries: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary>
    /// <see cref="MapEntries.SpeedLimit"/>: the limit as printed, or, asked for it as one figure in one
    /// unit, the entry's decline.
    /// </summary>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <param name="asOneFigureIn">
    /// Null for the limit as printed. A unit asks for the limit as a single figure in that unit, which
    /// turns on which printed figure governs: 87 knots if the knots figure does, about 86.9 knots if the
    /// miles-per-hour figure does.
    /// </param>
    /// <returns>
    /// The limit; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in
    /// force; <see cref="UnresolvedReason.RequiresInterpretation"/> citing § 107.51(a) for one figure.
    /// </returns>
    public static Resolution<GroundspeedLimit> Limit(WaiverStatement waiver, SpeedUnit? asOneFigureIn = null)
    {
        if (Waivers.Suspension(MapEntries.SpeedLimit, Regulation, waiver) is { } suspended)
        {
            return Resolution<GroundspeedLimit>.FromUnresolved(suspended);
        }

        if (asOneFigureIn is { } unit)
        {
            return Resolution<GroundspeedLimit>.FromUnresolved(new UnresolvedResult(
                UnresolvedReason.RequiresInterpretation,
                $"state the limit of the map entry '{MapEntries.SpeedLimit.Id}' as one figure in {unit}, "
                + "which turns on whether 87 knots or 100 miles per hour governs",
                MapEntries.SpeedLimit.Locator));
        }

        return Resolution<GroundspeedLimit>.FromValue(new GroundspeedLimit(waiver));
    }

    /// <summary>
    /// <see cref="MapEntries.SpeedWithinLimit"/>: whether <paramref name="groundspeed"/> "may not exceed
    /// 87 knots (100 miles per hour)" is met.
    /// </summary>
    /// <remarks>
    /// A groundspeed exceeding neither printed figure is within the limit, and one exceeding both is
    /// beyond it, whichever governs. One that exceeds 100 miles per hour and not 87 knots both exceeds
    /// and does not exceed the limit, which is <see cref="MapEntries.SpeedLimit"/>'s open question
    /// reached through <c>dependsOn</c>, so the decline cites that entry.
    /// </remarks>
    /// <param name="groundspeed">The groundspeed.</param>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in
    /// force; <see cref="UnresolvedReason.RequiresInterpretation"/> citing <c>speed-limit</c>'s § 107.51(a)
    /// between the two figures.
    /// </returns>
    public static Resolution<GroundspeedFinding> Within(Groundspeed groundspeed, WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.SpeedWithinLimit, Regulation, waiver) is { } suspended)
        {
            return Resolution<GroundspeedFinding>.FromUnresolved(suspended);
        }

        return Limit(waiver).Match(
            limit =>
            {
                var beyondKnots = groundspeed.Exceeds(limit.Knots);
                var beyondMilesPerHour = groundspeed.Exceeds(limit.MilesPerHour);
                return beyondKnots == beyondMilesPerHour
                    ? Resolution<GroundspeedFinding>.FromValue(new GroundspeedFinding(groundspeed, !beyondKnots, limit))
                    : Resolution<GroundspeedFinding>.FromUnresolved(new UnresolvedResult(
                        UnresolvedReason.RequiresInterpretation,
                        $"decide whether {groundspeed} exceeds {limit.Knots} ({limit.MilesPerHour}): it exceeds one printed figure "
                        + $"and not the other, and the map entry '{MapEntries.SpeedLimit.Id}' does not say which governs",
                        MapEntries.SpeedLimit.Locator));
            },
            Resolution<GroundspeedFinding>.FromUnresolved);
    }
}
