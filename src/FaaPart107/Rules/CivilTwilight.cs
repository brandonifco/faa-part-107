using System.Globalization;
using RulesKernel.Provenance;

namespace FaaPart107;

/// <summary>
/// The official event § 107.29(c)(1)-(2) states a civil twilight boundary against: "official
/// sunrise" in (c)(1), "official sunset" in (c)(2).
/// </summary>
/// <remarks>
/// When either event occurs is a fact about a place and a date, not a rule of part 107; the corpus
/// states neither, and this engine does not compute one.
/// </remarks>
public enum OfficialEvent
{
    /// <summary>Official sunrise, the event § 107.29(c)(1) states both its boundaries against.</summary>
    Sunrise,

    /// <summary>Official sunset, the event § 107.29(c)(2) states both its boundaries against.</summary>
    Sunset,
}

/// <summary>
/// One boundary of a period § 107.29(c)(1)-(2) calls civil twilight: an offset in minutes from an
/// <see cref="OfficialEvent"/>.
/// </summary>
/// <remarks>
/// The sign carries the corpus's own word. Negative is "before" ("30 minutes before official
/// sunrise", -30); zero is "at" ("ends at official sunrise", "begins at official sunset"), where the
/// corpus prints no offset at all; positive is "after" ("30 minutes after official sunset", 30).
/// </remarks>
/// <param name="OffsetMinutes">The offset from <paramref name="Event"/>, in minutes.</param>
/// <param name="Event">The official event the boundary is stated against.</param>
public readonly record struct TwilightBoundary(decimal OffsetMinutes, OfficialEvent Event)
{
    /// <summary>A boundary at the event itself, the corpus printing no offset: <c>0</c> minutes.</summary>
    /// <param name="moment">The official event.</param>
    /// <returns>The boundary.</returns>
    public static TwilightBoundary At(OfficialEvent moment) => new(0m, moment);

    /// <summary>A boundary <paramref name="minutes"/> minutes before <paramref name="moment"/>.</summary>
    /// <param name="minutes">The offset, in minutes, as the corpus prints it.</param>
    /// <param name="moment">The official event.</param>
    /// <returns>The boundary.</returns>
    public static TwilightBoundary MinutesBefore(decimal minutes, OfficialEvent moment) => new(-minutes, moment);

    /// <summary>A boundary <paramref name="minutes"/> minutes after <paramref name="moment"/>.</summary>
    /// <param name="minutes">The offset, in minutes, as the corpus prints it.</param>
    /// <param name="moment">The official event.</param>
    /// <returns>The boundary.</returns>
    public static TwilightBoundary MinutesAfter(decimal minutes, OfficialEvent moment) => new(minutes, moment);

    /// <inheritdoc/>
    public override string ToString()
    {
        var moment = Event == OfficialEvent.Sunrise ? "official sunrise" : "official sunset";
        return OffsetMinutes == 0m
            ? $"at {moment}"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{Math.Abs(OffsetMinutes)} minutes {(OffsetMinutes < 0m ? "before" : "after")} {moment}");
    }
}

/// <summary>
/// One of the two periods § 107.29(c)(1)-(2) calls civil twilight outside Alaska: "a period of time
/// that begins … and ends …".
/// </summary>
/// <param name="Begins">Where the period begins.</param>
/// <param name="Ends">Where the period ends.</param>
public sealed record CivilTwilightPeriod(TwilightBoundary Begins, TwilightBoundary Ends)
{
    /// <inheritdoc/>
    public override string ToString() => $"a period of time that begins {Begins} and ends {Ends}";
}

/// <summary>
/// Civil twilight as § 107.29(c)(1)-(2) prints it: <see cref="MapEntries.CivilTwilightWindow"/>,
/// "(1) Except for Alaska, a period of time that begins 30 minutes before official sunrise and ends
/// at official sunrise; (2) Except for Alaska, a period of time that begins at official sunset and
/// ends 30 minutes after official sunset;".
/// </summary>
/// <remarks>
/// <para>
/// Two periods, each of 30 minutes, each stated against its own official event. They are not one
/// window: (c)(1) runs up to official sunrise and (c)(2) runs on from official sunset, and the two
/// 30-minute figures are printed separately.
/// </para>
/// <para>
/// This value is the periods as printed, not an interval on a clock. When official sunrise and
/// official sunset occur is not in this corpus, and computing them from an astronomical algorithm
/// would be this engine answering from outside it.
/// </para>
/// <para>
/// Alaska is another entry's. Both paragraphs open "Except for Alaska"; § 107.29(c)(3) defers the
/// Alaskan period to the Air Almanac, which this map has not admitted, and the map states that as
/// <c>civil-twilight-alaska</c> with <c>definedElsewhere</c> — correspondence row 3, which this
/// engine already answers <c>MissingRulesData</c> through <c>EntryPoints.CivilTwilightAlaska</c>.
/// This entry's <c>crossReferences</c> is empty, so it does not reach that entry and does not
/// answer for Alaska; it records the exclusion the corpus prints, and nothing more.
/// </para>
/// </remarks>
public sealed record CivilTwilightWindows
{
    /// <summary>§ 107.29(c)(1): 30 minutes before official sunrise, ending at official sunrise.</summary>
    public CivilTwilightPeriod BeforeSunrise { get; } = new(
        TwilightBoundary.MinutesBefore(30m, OfficialEvent.Sunrise),
        TwilightBoundary.At(OfficialEvent.Sunrise));

    /// <summary>§ 107.29(c)(2): from official sunset, ending 30 minutes after official sunset.</summary>
    public CivilTwilightPeriod AfterSunset { get; } = new(
        TwilightBoundary.At(OfficialEvent.Sunset),
        TwilightBoundary.MinutesAfter(30m, OfficialEvent.Sunset));

    /// <summary>The place both paragraphs except, in the corpus's own word: <c>Alaska</c>.</summary>
    public string ExceptFor { get; } = "Alaska";

    /// <summary>Where the value is stated: <c>§ 107.29(c)(1)-(2)</c>.</summary>
    public SourceLocator Authority => MapEntries.CivilTwilightWindow.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        $"except for {ExceptFor}, {BeforeSunrise}; and, except for {ExceptFor}, {AfterSunset} [{Authority}]";
}

/// <summary>§ 107.29(c)(1)-(2): civil twilight outside Alaska.</summary>
public static class CivilTwilight
{
    /// <summary><see cref="MapEntries.CivilTwilightWindow"/>: the two periods as printed.</summary>
    /// <remarks>
    /// The rule returns the value rather than a resolution because it has no decline to express. The
    /// entry is <c>scope: in</c> and <c>clarity: clear</c>, with no <c>dependsOn</c>, no
    /// <c>enabledBy</c>, no <c>suspendedBy</c>, no <c>crossReferences</c>, no <c>definedElsewhere</c>
    /// and no recorded ambiguity: every correspondence row that would produce one is absent. In
    /// particular it carries no waiver gate — § 107.51's entries are suspended by
    /// <c>waivable-regulations</c> and this one is not, and gating it by analogy would be the engine
    /// suspending a rule the map does not suspend.
    /// </remarks>
    /// <returns>The value.</returns>
    public static CivilTwilightWindows Windows() => new();
}
