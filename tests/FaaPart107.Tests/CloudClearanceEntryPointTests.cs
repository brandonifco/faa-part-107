using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>cloud-clearance</c>, § 107.51(d), resolved through <see cref="EntryPoints.CloudClearance"/> only: the
/// minimum as printed, two distinct figures, the entry's open question declined rather than decided, and
/// the waiver gate.
/// </summary>
public class CloudClearanceEntryPointTests
{
    private const string Caller = nameof(CloudClearanceEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    [Fact]
    public void The_minimum_resolves_as_printed_500_feet_below_the_cloud_and_2000_feet_horizontally_from_it_citing_107_51_d()
    {
        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.CloudClearance.Resolve(new CloudClearanceRequest { Waiver = NoWaiver }));

        var clearance = Assert.IsType<CloudClearance>(resolved.Value);
        Assert.Equal(500m, clearance.BelowCloudFeet);
        Assert.Equal(2000m, clearance.HorizontallyFromCloudFeet);
        Assert.Equal("cfr-14-107", clearance.Authority.SourceId);
        Assert.Equal("§ 107.51(d)", clearance.Authority.Citation);
        Assert.Equal(EntryPoints.CloudClearance.Registered.Locator, clearance.Authority);
        Assert.Same(NoWaiver, clearance.Waiver);
    }

    [Fact]
    public void Asked_for_the_minimum_as_one_distance_it_declines_RequiresInterpretation_citing_107_51_d()
    {
        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.CloudClearance.Resolve(new CloudClearanceRequest { Waiver = NoWaiver, AsOneDistance = true })).Result;

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("§ 107.51(d)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.CloudClearance.Registered.Locator, unresolved.Locator);
        Assert.Contains("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not say whether both minimums must hold or either suffices", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("states no clearance above a cloud", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller, certificate: "107W-2026-00004");

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.CloudClearance.Resolve(new CloudClearanceRequest { Waiver = waiver })).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00004", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.CloudClearance.Resolve(new CloudClearanceRequest()));

        Assert.Equal(nameof(CloudClearanceRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.CloudClearance.Resolve(
            new CloudClearanceRequest { Waiver = WaiverStatement.Held("§ 107.41", Caller) }));

        Assert.Contains("§ 107.41", error.Message, StringComparison.Ordinal);
    }
}
