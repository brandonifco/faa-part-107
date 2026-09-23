using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The minimum flight visibility as § 107.51(c) prints it:
/// <see cref="MapEntries.VisibilityMinimum"/>, "(c) The minimum flight visibility, as observed from
/// the location of the control station must be no less than 3 statute miles. For purposes of this
/// section, flight visibility means the average slant distance from the control station at which
/// prominent unlighted objects may be seen and identified by day and prominent lighted objects may
/// be seen and identified by night."
/// </summary>
/// <remarks>
/// One figure, in statute miles, as printed. The paragraph's second sentence defines the quantity
/// the figure is a threshold on — the average slant distance from the control station — and states
/// no threshold of its own; the entry's note is explicit that this entry states the figure and that
/// "prominent", which that definition turns on and the corpus never fixes, is
/// <c>prominent-objects</c>' and not this entry's. So nothing here measures a visibility: whether a
/// stated visibility meets the figure is <c>weather-minimums-met</c>'s.
/// </remarks>
/// <param name="Waiver">The caller's waiver statement the minimum was resolved under, recorded with it.</param>
public sealed record VisibilityMinimum(WaiverStatement Waiver)
{
    /// <summary>The flight visibility the operation must be no less than, in statute miles: 3.</summary>
    public decimal StatuteMiles { get; } = 3m;

    /// <summary>Where the minimum is stated: <c>§ 107.51(c)</c>.</summary>
    public SourceLocator Authority => MapEntries.VisibilityMinimum.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"no less than {StatuteMiles} statute miles of flight visibility, observed from the control station [{Authority}]");
}

/// <summary>§ 107.51(c): the minimum flight visibility.</summary>
public static class Visibility
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary><see cref="MapEntries.VisibilityMinimum"/>: the minimum as printed.</summary>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The minimum; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver is in force.
    /// </returns>
    public static Resolution<VisibilityMinimum> Minimum(WaiverStatement waiver) =>
        Waivers.Suspension(MapEntries.VisibilityMinimum, Regulation, waiver) is { } suspended
            ? Resolution<VisibilityMinimum>.FromUnresolved(suspended)
            : Resolution<VisibilityMinimum>.FromValue(new VisibilityMinimum(waiver));
}
