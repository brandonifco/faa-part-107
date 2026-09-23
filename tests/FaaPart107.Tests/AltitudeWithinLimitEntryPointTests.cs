using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>altitude-within-limit</c>, § 107.51(b), resolved through <see cref="EntryPoints.AltitudeWithinLimit"/>
/// only: the plain 400 feet above ground level case; inside a 400-foot radius of a structure; the boundary
/// where the aircraft is within the radius but above the structure's immediate uppermost limit plus 400;
/// the two parts of the exception being joined by "and"; and the waiver gate in both directions (the
/// entry's note, and rules-factory decision 0021).
/// </summary>
public class AltitudeWithinLimitEntryPointTests
{
    private const string Caller = nameof(AltitudeWithinLimitEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    private static readonly StructureStatement NoStructure = StructureStatement.NoneWithinRadius(Caller);

    private static Resolution<object> Resolve(decimal altitudeFeet, StructureStatement structure, WaiverStatement waiver) =>
        EntryPoints.AltitudeWithinLimit.Resolve(new AltitudeWithinLimitRequest
        {
            AltitudeAboveGroundLevelFeet = altitudeFeet,
            Structure = structure,
            Waiver = waiver,
        });

    private static AltitudeFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<AltitudeFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    public static TheoryData<decimal> AtOrBelowTheCeiling => new()
    {
        0m,
        100m,

        // Just below the printed figure, and at it: "cannot be higher than 400 feet" is met at 400.
        399.99m,
        400m,
    };

    [Theory]
    [MemberData(nameof(AtOrBelowTheCeiling))]
    public void An_altitude_at_or_below_400_feet_above_ground_level_is_within_the_limit_citing_107_51_b(decimal altitudeFeet)
    {
        var finding = Finding(Resolve(altitudeFeet, NoStructure, NoWaiver));

        Assert.True(finding.WithinLimit);
        Assert.True(finding.WithinGroundLevelCeiling);
        Assert.False(finding.WithinStructureRadius);
        Assert.False(finding.WithinStructureAllowance);
        Assert.Equal(altitudeFeet, finding.AltitudeAboveGroundLevelFeet);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.51(b)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.AltitudeWithinLimit.Registered.Locator, finding.Authority);
        Assert.Same(NoStructure, finding.Structure);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    public static TheoryData<decimal> AboveTheCeiling => new()
    {
        // Just above the printed figure, and well above it.
        400.01m,
        401m,
        5000m,
    };

    [Theory]
    [MemberData(nameof(AboveTheCeiling))]
    public void An_altitude_higher_than_400_feet_above_ground_level_with_no_structure_is_beyond_the_limit_citing_107_51_b(decimal altitudeFeet)
    {
        var finding = Finding(Resolve(altitudeFeet, NoStructure, NoWaiver));

        Assert.False(finding.WithinLimit);
        Assert.False(finding.WithinGroundLevelCeiling);
        Assert.False(finding.WithinStructureRadius);
        Assert.False(finding.WithinStructureAllowance);
        Assert.Equal("§ 107.51(b)", finding.Authority.Citation);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    public static TheoryData<decimal, decimal, decimal> WithinTheRadiusAndTheAllowance => new()
    {
        // distance from the structure, the structure's immediate uppermost limit, the altitude.
        { 0m, 500m, 900m },       // at the allowance: 500 + 400.
        { 100m, 500m, 899.99m },  // just below it.
        { 400m, 500m, 900m },     // at the radius and at the allowance, both boundaries at once.
        { 400m, 1000m, 1400m },
    };

    [Theory]
    [MemberData(nameof(WithinTheRadiusAndTheAllowance))]
    public void Within_a_400_foot_radius_and_not_higher_than_400_feet_above_the_structure_is_within_the_limit_above_400_agl(
        decimal distanceFeet,
        decimal uppermostLimitFeet,
        decimal altitudeFeet)
    {
        var structure = StructureStatement.Near(distanceFeet, uppermostLimitFeet, Caller);

        var finding = Finding(Resolve(altitudeFeet, structure, NoWaiver));

        Assert.False(finding.WithinGroundLevelCeiling);
        Assert.True(finding.WithinStructureRadius);
        Assert.True(finding.WithinStructureAllowance);
        Assert.True(finding.WithinLimit);
        Assert.Equal("§ 107.51(b)", finding.Authority.Citation);
        Assert.Same(structure, finding.Structure);
    }

    public static TheoryData<decimal, decimal, decimal> WithinTheRadiusAboveTheAllowance => new()
    {
        { 100m, 500m, 900.01m },  // just above the allowance: 500 + 400.
        { 100m, 500m, 901m },
        { 0m, 0m, 400.01m },      // a structure at ground level allows exactly the 400 the ceiling does.
        { 400m, 1000m, 1400.01m },
    };

    [Theory]
    [MemberData(nameof(WithinTheRadiusAboveTheAllowance))]
    public void Within_the_radius_but_higher_than_400_feet_above_the_structures_immediate_uppermost_limit_is_beyond_the_limit(
        decimal distanceFeet,
        decimal uppermostLimitFeet,
        decimal altitudeFeet)
    {
        var structure = StructureStatement.Near(distanceFeet, uppermostLimitFeet, Caller);

        var finding = Finding(Resolve(altitudeFeet, structure, NoWaiver));

        Assert.False(finding.WithinGroundLevelCeiling);
        Assert.True(finding.WithinStructureRadius);
        Assert.False(finding.WithinStructureAllowance);
        Assert.False(finding.WithinLimit);
        Assert.Equal("§ 107.51(b)", finding.Authority.Citation);
    }

    public static TheoryData<decimal, decimal, decimal, bool> BeyondTheRadius => new()
    {
        // Just beyond the radius, and low enough for the allowance had it been met: still beyond the limit,
        // because (b)(1) and (b)(2) are joined by "and".
        { 400.01m, 500m, 900m, false },
        { 401m, 500m, 500m, false },
        { 5000m, 10000m, 401m, false },

        // Beyond the radius, at the ceiling: within, on paragraph (b)'s own terms.
        { 400.01m, 500m, 400m, true },
    };

    [Theory]
    [MemberData(nameof(BeyondTheRadius))]
    public void Beyond_a_400_foot_radius_the_structures_allowance_does_not_apply_and_only_the_400_foot_agl_ceiling_governs(
        decimal distanceFeet,
        decimal uppermostLimitFeet,
        decimal altitudeFeet,
        bool withinLimit)
    {
        var structure = StructureStatement.Near(distanceFeet, uppermostLimitFeet, Caller);

        var finding = Finding(Resolve(altitudeFeet, structure, NoWaiver));

        Assert.False(finding.WithinStructureRadius);
        Assert.True(finding.WithinStructureAllowance);
        Assert.Equal(withinLimit, finding.WithinLimit);
        Assert.Equal(withinLimit, finding.WithinGroundLevelCeiling);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(400)]
    [InlineData(2000)]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_whatever_the_altitude(int altitudeFeet)
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller, certificate: "107W-2026-00006");

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(Resolve(altitudeFeet, NoStructure, waiver)).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);

        // The gate is this entry's own: the decline names altitude-within-limit, not the altitude-limit it
        // depends on.
        Assert.Contains("'altitude-within-limit'", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("'altitude-limit'", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00006", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.51", "the remote pilot in command");
        var structure = StructureStatement.Near(100m, 500m, "the remote pilot in command");

        var within = Finding(Resolve(900m, structure, waiver));
        var beyond = Finding(Resolve(900.01m, structure, waiver));

        Assert.True(within.WithinLimit);
        Assert.False(beyond.WithinLimit);
        Assert.Same(waiver, within.Waiver);
        Assert.Same(waiver, beyond.Waiver);
        Assert.Equal("the remote pilot in command", beyond.Waiver.StatedBy);
        Assert.False(beyond.Waiver.InForce);
        Assert.Same(structure, beyond.Structure);
    }

    [Fact]
    public void Without_a_structure_statement_it_refuses_rather_than_assume_there_is_no_structure()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AltitudeWithinLimit.Resolve(
            new AltitudeWithinLimitRequest { AltitudeAboveGroundLevelFeet = 900m, Waiver = NoWaiver }));

        Assert.Equal(nameof(AltitudeWithinLimitRequest.Structure), error.ParamName);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AltitudeWithinLimit.Resolve(
            new AltitudeWithinLimitRequest { AltitudeAboveGroundLevelFeet = 100m, Structure = NoStructure }));

        Assert.Equal(nameof(AltitudeWithinLimitRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_an_altitude_it_refuses()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AltitudeWithinLimit.Resolve(
            new AltitudeWithinLimitRequest { Structure = NoStructure, Waiver = NoWaiver }));

        Assert.Equal(nameof(AltitudeWithinLimitRequest.AltitudeAboveGroundLevelFeet), error.ParamName);
    }
}
