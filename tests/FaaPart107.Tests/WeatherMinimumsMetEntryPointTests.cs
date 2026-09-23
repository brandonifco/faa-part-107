using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>weather-minimums-met</c>, § 107.51(c)-(d), resolved through
/// <see cref="EntryPoints.WeatherMinimumsMet"/> only: each cloud minimum at, just below and just
/// above its printed figure and each independently of the other, a stated flight visibility at,
/// just below and just above 3 statute miles, and the waiver gate in both directions (the entry's
/// note, and rules-factory decision 0021).
/// </summary>
/// <remarks>
/// The entry resolves exactly one outcome, the minimums not met, and only where neither cloud
/// minimum is met — § 107.51(d) is then broken on either reading of how its two figures combine,
/// so the conjunction is false whatever the visibility is. Everywhere else § 107.51(c) is reached
/// and the answer turns on which objects are "prominent", which <c>prominent-objects</c> holds
/// open, so the engine declines rather than reading the caller's stated figure as the section's
/// defined flight visibility. The tests below pin both halves of that, and the figures compared
/// against are never written here as literals expected to come from the entry: they are
/// <c>visibility-minimum</c>'s and <c>cloud-clearance</c>'s, resolved through their own entry
/// points.
/// </remarks>
public class WeatherMinimumsMetEntryPointTests
{
    private const string Caller = nameof(WeatherMinimumsMetEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    private static Resolution<object> Resolve(
        decimal flightVisibilityStatuteMiles,
        decimal feetBelowCloud,
        decimal feetHorizontallyFromCloud,
        WaiverStatement waiver) =>
        EntryPoints.WeatherMinimumsMet.Resolve(new WeatherMinimumsMetRequest
        {
            FlightVisibilityStatuteMiles = flightVisibilityStatuteMiles,
            FeetBelowCloud = feetBelowCloud,
            FeetHorizontallyFromCloud = feetHorizontallyFromCloud,
            Waiver = waiver,
        });

    private static WeatherMinimumsFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<WeatherMinimumsFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    /// <summary>§ 107.51(c)'s figure, from <c>visibility-minimum</c> and not restated here.</summary>
    private static VisibilityMinimum Minimum =>
        Assert.IsType<VisibilityMinimum>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.VisibilityMinimum.Resolve(new VisibilityMinimumRequest { Waiver = NoWaiver })).Value);

    /// <summary>§ 107.51(d)'s two figures, from <c>cloud-clearance</c> and not restated here.</summary>
    private static CloudClearance Clearance =>
        Assert.IsType<CloudClearance>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.CloudClearance.Resolve(new CloudClearanceRequest { Waiver = NoWaiver })).Value);

    /// <summary>
    /// Stated flight visibilities spanning § 107.51(c)'s figure: well below it, just below it, at
    /// it, just above it and well above it. None of them changes what the entry answers.
    /// </summary>
    public static TheoryData<decimal> StatedVisibilities => new() { 0m, 2.99m, 3m, 3.01m, 10m };

    public static TheoryData<decimal, decimal, decimal> NeitherCloudMinimumMet => new()
    {
        // Nowhere near either figure.
        { 10m, 0m, 0m },

        // Just below both figures, with the visibility just below its own figure.
        { 2.99m, 499.99m, 1999.99m },

        // Just below both figures, with the visibility well above its own figure.
        { 10m, 499.99m, 1999.99m },
    };

    [Theory]
    [MemberData(nameof(NeitherCloudMinimumMet))]
    public void Neither_cloud_minimum_met_resolves_the_minimums_not_met_whatever_the_stated_visibility_citing_107_51_c_d(
        decimal visibility,
        decimal below,
        decimal horizontal)
    {
        var finding = Finding(Resolve(visibility, below, horizontal, NoWaiver));

        Assert.False(finding.MinimumsMet);
        Assert.False(finding.BelowCloudMinimumMet);
        Assert.False(finding.HorizontallyFromCloudMinimumMet);
        Assert.Equal(visibility, finding.FlightVisibilityStatuteMiles);
        Assert.Equal(below, finding.FeetBelowCloud);
        Assert.Equal(horizontal, finding.FeetHorizontallyFromCloud);

        // The figures it applied are its dependencies', reached through dependsOn.
        Assert.Equal(Clearance.BelowCloudFeet, finding.Clearance.BelowCloudFeet);
        Assert.Equal(Clearance.HorizontallyFromCloudFeet, finding.Clearance.HorizontallyFromCloudFeet);
        Assert.Equal(Minimum.StatuteMiles, finding.Minimum.StatuteMiles);

        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.51(c)-(d)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.WeatherMinimumsMet.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Theory]
    [MemberData(nameof(StatedVisibilities))]
    public void Both_cloud_minimums_met_declines_RequiresInterpretation_citing_prominent_objects_107_51_c_whatever_the_stated_visibility(
        decimal visibility)
    {
        // At both printed figures exactly: "no less than" is met at the figure.
        var atTheFigures = Declined(Resolve(visibility, Clearance.BelowCloudFeet, Clearance.HorizontallyFromCloudFeet, NoWaiver));

        // And well beyond them.
        var wellClear = Declined(Resolve(visibility, 1000m, 5000m, NoWaiver));

        foreach (var unresolved in new[] { atTheFigures, wellClear })
        {
            Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.51(c)", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, unresolved.Locator);
            Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("\"prominent\"", unresolved.Attempted, StringComparison.Ordinal);

            // The cloud half is settled, so cloud-clearance's question is not named.
            Assert.DoesNotContain("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
        }
    }

    public static TheoryData<decimal, decimal> ExactlyOneCloudMinimumMet => new()
    {
        // At 500 feet below the cloud, just short of 2,000 feet horizontally from it.
        { 500m, 1999.99m },

        // Just short of 500 feet below, at 2,000 feet horizontally.
        { 499.99m, 2000m },

        // Directly below the cloud at the vertical minimum, and nowhere near the horizontal one.
        { 500m, 0m },

        // Level with the cloud at the horizontal minimum, and nowhere near the vertical one.
        { 0m, 2000m },
    };

    [Theory]
    [MemberData(nameof(ExactlyOneCloudMinimumMet))]
    public void Exactly_one_cloud_minimum_met_declines_and_names_cloud_clearances_open_question_too(decimal below, decimal horizontal)
    {
        var unresolved = Declined(Resolve(10m, below, horizontal, NoWaiver));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("§ 107.51(c)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, unresolved.Locator);
        Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("one of § 107.51(d)'s two minimums and not the other", unresolved.Attempted, StringComparison.Ordinal);
    }

    public static TheoryData<decimal, decimal, decimal> EverySituation => new()
    {
        { 10m, 0m, 0m },
        { 10m, 500m, 1999.99m },
        { 10m, 1000m, 5000m },
        { 0m, 1000m, 5000m },
    };

    [Theory]
    [MemberData(nameof(EverySituation))]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement(
        decimal visibility,
        decimal below,
        decimal horizontal)
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller);

        var unresolved = Declined(Resolve(visibility, below, horizontal, waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("weather-minimums-met", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.51", "the remote pilot in command");

        var finding = Finding(Resolve(10m, 0m, 0m, waiver));
        var unresolved = Declined(Resolve(10m, 1000m, 5000m, waiver));

        Assert.False(finding.MinimumsMet);
        Assert.Same(waiver, finding.Waiver);
        Assert.Same(waiver, finding.Minimum.Waiver);
        Assert.Same(waiver, finding.Clearance.Waiver);
        Assert.Equal("the remote pilot in command", finding.Waiver.StatedBy);
        Assert.False(finding.Waiver.InForce);
        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest
            {
                FlightVisibilityStatuteMiles = 10m,
                FeetBelowCloud = 0m,
                FeetHorizontallyFromCloud = 0m,
            }));

        Assert.Equal(nameof(WeatherMinimumsMetRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_a_stated_figure_it_refuses_rather_than_assume_one()
    {
        var withoutVisibility = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest { FeetBelowCloud = 0m, FeetHorizontallyFromCloud = 0m, Waiver = NoWaiver }));
        var withoutBelow = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest { FlightVisibilityStatuteMiles = 10m, FeetHorizontallyFromCloud = 0m, Waiver = NoWaiver }));
        var withoutHorizontal = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest { FlightVisibilityStatuteMiles = 10m, FeetBelowCloud = 0m, Waiver = NoWaiver }));

        Assert.Equal(nameof(WeatherMinimumsMetRequest.FlightVisibilityStatuteMiles), withoutVisibility.ParamName);
        Assert.Equal(nameof(WeatherMinimumsMetRequest.FeetBelowCloud), withoutBelow.ParamName);
        Assert.Equal(nameof(WeatherMinimumsMetRequest.FeetHorizontallyFromCloud), withoutHorizontal.ParamName);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(10, -1, 0)]
    [InlineData(10, 0, -1)]
    public void A_negative_stated_figure_is_refused(int visibility, int below, int horizontal) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Resolve(visibility, below, horizontal, NoWaiver));

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_answering_from_defaults()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Registry.Resolve("weather-minimums-met", RuleRequest.Empty));

        Assert.Equal(nameof(WeatherMinimumsMetRequest.FlightVisibilityStatuteMiles), error.ParamName);
    }
}
