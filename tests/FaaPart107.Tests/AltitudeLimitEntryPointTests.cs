using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>altitude-limit</c>, § 107.51(b), resolved through <see cref="EntryPoints.AltitudeLimit"/> only: the
/// limit as printed, three distinct figures, and the waiver gate.
/// </summary>
public class AltitudeLimitEntryPointTests
{
    private const string Caller = nameof(AltitudeLimitEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    [Fact]
    public void The_limit_resolves_as_printed_400_feet_above_ground_level_a_400_foot_structure_radius_and_400_feet_above_the_structure_citing_107_51_b()
    {
        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.AltitudeLimit.Resolve(new AltitudeLimitRequest { Waiver = NoWaiver }));

        var limit = Assert.IsType<AltitudeLimit>(resolved.Value);
        Assert.Equal(400m, limit.AboveGroundLevelFeet);
        Assert.Equal(400m, limit.StructureRadiusFeet);
        Assert.Equal(400m, limit.AboveStructureUppermostLimitFeet);
        Assert.Equal("cfr-14-107", limit.Authority.SourceId);
        Assert.Equal("§ 107.51(b)", limit.Authority.Citation);
        Assert.Equal(EntryPoints.AltitudeLimit.Registered.Locator, limit.Authority);
        Assert.Same(NoWaiver, limit.Waiver);
    }

    [Fact]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller, certificate: "107W-2026-00002");

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.AltitudeLimit.Resolve(new AltitudeLimitRequest { Waiver = waiver })).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("altitude-limit", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00002", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AltitudeLimit.Resolve(new AltitudeLimitRequest()));

        Assert.Equal(nameof(AltitudeLimitRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AltitudeLimit.Resolve(
            new AltitudeLimitRequest { Waiver = WaiverStatement.Held("§ 107.41", Caller) }));

        Assert.Contains("§ 107.41", error.Message, StringComparison.Ordinal);
    }
}
