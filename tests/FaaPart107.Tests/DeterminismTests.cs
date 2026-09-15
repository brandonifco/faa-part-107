using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// The corpus declares <c>randomness: none</c> (rules-factory decision 0019). The gate refuses the
/// randomness package at restore; these check the built engine too.
/// </summary>
public class DeterminismTests
{
    [Fact]
    public void The_engine_assembly_references_the_kernel_and_never_the_randomness_package()
    {
        var referenced = typeof(EntryPoints).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.StartsWith("RulesKernel", StringComparison.Ordinal))
            .ToArray();

        Assert.Contains("RulesKernel", referenced);
        Assert.DoesNotContain("RulesKernel.Randomness", referenced);
    }

    [Fact]
    public void The_replay_identity_names_no_random_algorithm_and_pins_the_corpus()
    {
        Assert.True(Ruleset.Identity.IsDeterministicWithoutRandomness);
        Assert.Null(Ruleset.Identity.RandomAlgorithm);
        Assert.Equal(MapEntries.Baseline, Assert.Single(Ruleset.Identity.SourceBaselines));
    }

    [Fact]
    public void The_same_request_resolves_the_same_way_every_time()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.51", nameof(DeterminismTests));
        var request = new Requests.SpeedWithinLimitRequest { Groundspeed = Groundspeed.InMilesPerHour(99m), Waiver = waiver };

        var first = EntryPoints.SpeedWithinLimit.Resolve(request);
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(first, EntryPoints.SpeedWithinLimit.Resolve(request));
        }
    }
}
