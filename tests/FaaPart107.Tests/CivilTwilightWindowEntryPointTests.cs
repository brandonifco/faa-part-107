using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>civil-twilight-window</c>, § 107.29(c)(1)-(2), resolved through
/// <see cref="EntryPoints.CivilTwilightWindow"/> only: the two periods as printed, each figure and
/// each boundary, the exception the corpus prints, and the absence of any gate on it.
/// </summary>
public class CivilTwilightWindowEntryPointTests
{
    private static CivilTwilightWindows Resolve() =>
        Assert.IsType<CivilTwilightWindows>(
            Assert.IsType<Resolution<object>.Resolved>(
                EntryPoints.CivilTwilightWindow.Resolve(new CivilTwilightWindowRequest())).Value);

    [Fact]
    public void The_two_periods_resolve_as_printed_30_minutes_before_official_sunrise_to_sunrise_and_sunset_to_30_minutes_after_citing_107_29_c_1_2()
    {
        var windows = Resolve();

        Assert.Equal(-30m, windows.BeforeSunrise.Begins.OffsetMinutes);
        Assert.Equal(30m, windows.AfterSunset.Ends.OffsetMinutes);
        Assert.Equal("cfr-14-107", windows.Authority.SourceId);
        Assert.Equal("§ 107.29(c)(1)-(2)", windows.Authority.Citation);
        Assert.Equal(EntryPoints.CivilTwilightWindow.Registered.Locator, windows.Authority);
    }

    [Fact]
    public void Each_period_is_bounded_by_its_own_official_event_and_the_corpus_prints_no_offset_at_sunrise_or_at_sunset()
    {
        var windows = Resolve();

        Assert.Equal(OfficialEvent.Sunrise, windows.BeforeSunrise.Begins.Event);
        Assert.Equal(OfficialEvent.Sunrise, windows.BeforeSunrise.Ends.Event);
        Assert.Equal(0m, windows.BeforeSunrise.Ends.OffsetMinutes);

        Assert.Equal(OfficialEvent.Sunset, windows.AfterSunset.Begins.Event);
        Assert.Equal(OfficialEvent.Sunset, windows.AfterSunset.Ends.Event);
        Assert.Equal(0m, windows.AfterSunset.Begins.OffsetMinutes);
    }

    [Fact]
    public void The_periods_are_stated_except_for_Alaska_and_the_value_prints_the_corpus_wording()
    {
        var windows = Resolve();

        Assert.Equal("Alaska", windows.ExceptFor);
        Assert.Equal(
            "except for Alaska, a period of time that begins 30 minutes before official sunrise and ends at official sunrise; "
            + "and, except for Alaska, a period of time that begins at official sunset and ends 30 minutes after official sunset "
            + "[cfr-14-107 / § 107.29(c)(1)-(2)]",
            windows.ToString());
    }

    [Fact]
    public void It_resolves_through_the_dictionary_dispatch_demanding_no_input_and_gating_on_no_waiver()
    {
        var dispatched = Assert.IsType<Resolution<object>.Resolved>(
            Registry.Resolve("civil-twilight-window", RuleRequest.Empty));

        Assert.Equal(Resolve(), Assert.IsType<CivilTwilightWindows>(dispatched.Value));
    }
}
