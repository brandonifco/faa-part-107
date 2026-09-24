using System.Globalization;
using System.Text.RegularExpressions;
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

    public static TheoryData<decimal, decimal, decimal, string> ExceptionPathRenderings => new()
    {
        // distance from the structure, its immediate uppermost limit, the altitude, the whole rendering.

        // The separating input #122 reports: within the limit on the exception, and rendered against the
        // 400-foot above ground level ceiling it is six hundred feet above.
        {
            200m, 600m, 1000m,
            "1000 feet above ground level is within 1000 feet above ground level, 400 feet above the "
            + "structure's immediate uppermost limit [cfr-14-107 / § 107.51(b)]; the small unmanned "
            + "aircraft is flown 200 feet from a structure whose immediate uppermost limit is 600 feet "
            + "above ground level, as stated by AltitudeWithinLimitEntryPointTests; no certificate of "
            + "waiver authorizing deviation from § 107.51 is in force, as stated by "
            + "AltitudeWithinLimitEntryPointTests"
        },

        // At paragraph (b)(2)'s figure exactly: 500 + 400, which "does not fly higher than" is met at.
        {
            0m, 500m, 900m,
            "900 feet above ground level is within 900 feet above ground level, 400 feet above the "
            + "structure's immediate uppermost limit [cfr-14-107 / § 107.51(b)]; the small unmanned "
            + "aircraft is flown 0 feet from a structure whose immediate uppermost limit is 500 feet "
            + "above ground level, as stated by AltitudeWithinLimitEntryPointTests; no certificate of "
            + "waiver authorizing deviation from § 107.51 is in force, as stated by "
            + "AltitudeWithinLimitEntryPointTests"
        },

        // A hundredth of a foot above it: beyond, and named against the same figure, not the ceiling.
        {
            0m, 500m, 900.01m,
            "900.01 feet above ground level is beyond 900 feet above ground level, 400 feet above the "
            + "structure's immediate uppermost limit [cfr-14-107 / § 107.51(b)]; the small unmanned "
            + "aircraft is flown 0 feet from a structure whose immediate uppermost limit is 500 feet "
            + "above ground level, as stated by AltitudeWithinLimitEntryPointTests; no certificate of "
            + "waiver authorizing deviation from § 107.51 is in force, as stated by "
            + "AltitudeWithinLimitEntryPointTests"
        },

        // Beyond the 400-foot radius, paragraph (b)(2) has nothing to measure above, so the ceiling
        // governs alone and is what the sentence names -- the wording unchanged from before #122.
        {
            400.01m, 500m, 900m,
            "900 feet above ground level is beyond 400 feet above ground level [cfr-14-107 / "
            + "§ 107.51(b)]; the small unmanned aircraft is flown 400.01 feet from a structure whose "
            + "immediate uppermost limit is 500 feet above ground level, as stated by "
            + "AltitudeWithinLimitEntryPointTests; no certificate of waiver authorizing deviation from "
            + "§ 107.51 is in force, as stated by AltitudeWithinLimitEntryPointTests"
        },

        // Within the radius but not higher than 400 feet above ground level: the ceiling is met and
        // paragraph (b)'s "unless" is never reached, so the ceiling is what the sentence names.
        {
            200m, 600m, 400m,
            "400 feet above ground level is within 400 feet above ground level [cfr-14-107 / "
            + "§ 107.51(b)]; the small unmanned aircraft is flown 200 feet from a structure whose "
            + "immediate uppermost limit is 600 feet above ground level, as stated by "
            + "AltitudeWithinLimitEntryPointTests; no certificate of waiver authorizing deviation from "
            + "§ 107.51 is in force, as stated by AltitudeWithinLimitEntryPointTests"
        },
    };

    /// <summary>
    /// § 107.51(b) states a ceiling and then an exception reached by "unless", and the figure the
    /// explanation names follows that order: the ceiling while it is met, and above it the figure
    /// paragraph (b)(2) names — "400 feet above the structure's immediate uppermost limit" — wherever
    /// paragraph (b)(1) opens the exception. Before <c>#122</c> the sentence named
    /// <see cref="AltitudeLimit.AboveGroundLevelFeet"/> whichever branch produced the verdict, so an
    /// aircraft correctly found within the limit a thousand feet up beside a six-hundred-foot
    /// structure was rendered "within 400 feet above ground level".
    /// </summary>
    [Theory]
    [MemberData(nameof(ExceptionPathRenderings))]
    public void The_explanation_names_the_figure_the_verdict_turned_on_and_not_the_400_foot_ceiling_whichever_branch_answered(
        decimal distanceFeet,
        decimal uppermostLimitFeet,
        decimal altitudeFeet,
        string expected)
    {
        var structure = StructureStatement.Near(distanceFeet, uppermostLimitFeet, Caller);

        var finding = Finding(Resolve(altitudeFeet, structure, NoWaiver));

        Assert.Equal(expected, finding.ToString());
    }

    public static TheoryData<decimal> SweptAltitudes =>
        [0m, 100m, 399.99m, 400m, 400.01m, 401m, 500m, 899.99m, 900m, 900.01m, 1000m, 1000.01m, 5000m];

    /// <summary>
    /// The property the rendering is for, swept rather than sampled: over every altitude above, and
    /// over the statement naming no structure and thirty-five naming one, the figure the sentence
    /// names is the one the verdict turned on — the altitude is within the named figure exactly while
    /// <see cref="AltitudeFinding.WithinLimit"/> is true, so the sentence is arithmetically true on
    /// every one of them rather than on the rows somebody thought to write down. Every row where
    /// § 107.51(b)'s own ceiling is met is checked to still name that ceiling, in the words it named
    /// it in before <c>#122</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(SweptAltitudes))]
    public void Whatever_figure_the_explanation_names_the_altitude_is_within_it_exactly_while_it_is_within_the_limit(
        decimal altitudeFeet)
    {
        var structures = new List<StructureStatement> { NoStructure };
        foreach (var distanceFeet in new[] { 0m, 200m, 399.99m, 400m, 400.01m, 401m, 1000m })
        {
            foreach (var uppermostLimitFeet in new[] { 0m, 100m, 500m, 600m, 1000m })
            {
                structures.Add(StructureStatement.Near(distanceFeet, uppermostLimitFeet, Caller));
            }
        }

        Assert.Equal(36, structures.Count);

        foreach (var structure in structures)
        {
            var finding = Finding(Resolve(altitudeFeet, structure, NoWaiver));
            var sentence = finding.ToString();

            var match = Regex.Match(
                sentence,
                @"^(?<altitude>[\d.]+) feet above ground level is (?<verdict>within|beyond) (?<figure>[\d.]+) feet above ground level[,\ ]");
            Assert.True(match.Success, sentence);

            var named = decimal.Parse(match.Groups["figure"].Value, CultureInfo.InvariantCulture);

            Assert.Equal(altitudeFeet, decimal.Parse(match.Groups["altitude"].Value, CultureInfo.InvariantCulture));
            Assert.Equal(finding.WithinLimit ? "within" : "beyond", match.Groups["verdict"].Value);
            Assert.Equal(finding.WithinLimit, altitudeFeet <= named);

            if (finding.WithinGroundLevelCeiling)
            {
                // Paragraph (b)'s own ceiling settled it, and the sentence is the one it always was.
                Assert.Equal(finding.Limit.AboveGroundLevelFeet, named);
                Assert.Contains(
                    $"is within {finding.Limit.AboveGroundLevelFeet} feet above ground level [",
                    sentence,
                    StringComparison.Ordinal);
            }
        }
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

    /// <summary>
    /// And the finding will not report either part of § 107.51(b)'s exception met where the
    /// statement names no structure, so that is a fact about the type and not only about the one
    /// rule that builds it (<c>#101</c>). Both parts are claimed against a structure — (b)(1) a
    /// radius of one, (b)(2) an allowance above one's immediate uppermost limit — so a statement
    /// naming none leaves nothing for either to be met by, and either true beside it would lift the
    /// ceiling on a structure nobody stated. False stands: that is the exception not satisfied,
    /// which is what <see cref="StructureStatement.NoneWithinRadius"/> says, so the finding is
    /// refused only where a part is reported met.
    /// </summary>
    [Fact]
    public void With_no_structure_stated_neither_part_of_the_exception_may_be_reported_met()
    {
        var limit = Assert.IsType<AltitudeLimit>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.AltitudeLimit.Resolve(new AltitudeLimitRequest { Waiver = NoWaiver })).Value);

        var radius = Assert.Throws<ArgumentException>(() => new AltitudeFinding(
            900m,
            WithinGroundLevelCeiling: false,
            WithinStructureRadius: true,
            WithinStructureAllowance: false,
            NoStructure,
            limit));
        var allowance = Assert.Throws<ArgumentException>(() => new AltitudeFinding(
            900m,
            WithinGroundLevelCeiling: false,
            WithinStructureRadius: false,
            WithinStructureAllowance: true,
            NoStructure,
            limit));
        var both = Assert.Throws<ArgumentException>(() => new AltitudeFinding(
            900m,
            WithinGroundLevelCeiling: false,
            WithinStructureRadius: true,
            WithinStructureAllowance: true,
            NoStructure,
            limit));

        foreach (var refused in new[] { radius, allowance, both })
        {
            Assert.Equal("Structure", refused.ParamName);
            Assert.Contains("claimed against a structure", refused.Message, StringComparison.Ordinal);
        }

        // Both false is the exception not satisfied, which is an answer, so the finding stands --
        // and it is the finding the rule already returns for an altitude above the ceiling with no
        // structure stated.
        var unmet = new AltitudeFinding(
            900m,
            WithinGroundLevelCeiling: false,
            WithinStructureRadius: false,
            WithinStructureAllowance: false,
            NoStructure,
            limit);

        Assert.False(unmet.WithinLimit);
        Assert.Equal(Finding(Resolve(900m, NoStructure, NoWaiver)), unmet);
    }
}
