using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>moving-aircraft-operation</c>, § 107.25(a), resolved through
/// <see cref="EntryPoints.MovingAircraftOperation"/> only: the prohibition in both directions, that
/// it reads nothing that qualifies only § 107.25(b), and the waiver gate in both directions naming
/// this entry rather than the other limb of the same section (the entry's note, and rules-factory
/// decision 0021).
/// </summary>
public class MovingAircraftOperationEntryPointTests
{
    private const string Caller = nameof(MovingAircraftOperationEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.25", Caller);

    private static Resolution<object> Resolve(bool fromAMovingAircraft, WaiverStatement waiver) =>
        EntryPoints.MovingAircraftOperation.Resolve(new MovingAircraftOperationRequest
        {
            FromAMovingAircraft = fromAMovingAircraft,
            Waiver = waiver,
        });

    private static MovingAircraftFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<MovingAircraftFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Operated_from_a_moving_aircraft_it_resolves_prohibited_citing_107_25()
    {
        var finding = Finding(Resolve(fromAMovingAircraft: true, NoWaiver));

        Assert.True(finding.FromAMovingAircraft);
        Assert.True(finding.Prohibited);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.25", finding.Authority.Citation);
        Assert.Equal(EntryPoints.MovingAircraftOperation.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Fact]
    public void Operated_from_an_aircraft_that_is_not_moving_107_25_a_does_not_prohibit_it()
    {
        var finding = Finding(Resolve(fromAMovingAircraft: false, NoWaiver));

        Assert.False(finding.FromAMovingAircraft);
        Assert.False(finding.Prohibited);
        Assert.Equal("§ 107.25", finding.Authority.Citation);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Fact]
    public void The_entry_reads_only_whether_the_aircraft_is_moving_and_no_condition_that_qualifies_only_the_vehicle_limb()
    {
        var inputs = typeof(MovingAircraftOperationRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { nameof(MovingAircraftOperationRequest.FromAMovingAircraft), nameof(MovingAircraftOperationRequest.Waiver) },
            inputs);

        var prohibited = Finding(Resolve(fromAMovingAircraft: true, NoWaiver)).ToString();
        var notProhibited = Finding(Resolve(fromAMovingAircraft: false, NoWaiver)).ToString();

        Assert.DoesNotContain("sparsely populated", prohibited, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compensation or hire", prohibited, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sparsely populated", notProhibited, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compensation or hire", notProhibited, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void While_a_waiver_of_107_25_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_names_this_entry(bool fromAMovingAircraft)
    {
        var waiver = WaiverStatement.Held("§ 107.25", Caller);

        var unresolved = Decline(Resolve(fromAMovingAircraft, waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("moving-aircraft-operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("moving-vehicle-operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.25", "the remote pilot in command");

        var moving = Finding(Resolve(fromAMovingAircraft: true, waiver));
        var still = Finding(Resolve(fromAMovingAircraft: false, waiver));

        Assert.True(moving.Prohibited);
        Assert.False(still.Prohibited);
        Assert.Same(waiver, moving.Waiver);
        Assert.Same(waiver, still.Waiver);
        Assert.Equal("the remote pilot in command", moving.Waiver.StatedBy);
        Assert.False(moving.Waiver.InForce);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.MovingAircraftOperation.Resolve(
            new MovingAircraftOperationRequest { FromAMovingAircraft = true }));

        Assert.Equal(nameof(MovingAircraftOperationRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_a_statement_whether_the_aircraft_is_moving_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.MovingAircraftOperation.Resolve(
            new MovingAircraftOperationRequest { Waiver = NoWaiver }));

        Assert.Equal(nameof(MovingAircraftOperationRequest.FromAMovingAircraft), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            fromAMovingAircraft: true, WaiverStatement.Held("§ 107.51", Caller)));

        Assert.Contains("moving-aircraft-operation", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.25", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_infer_the_facts()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve("moving-aircraft-operation", RuleRequest.Empty));

        // The waiver statement first, because the gate reads it and the gate comes first
        // (docs/decisions/0008). The paragraph's own fact is owed next, once a statement that no
        // waiver is in force has put the entry back in reach.
        Assert.Equal(nameof(MovingAircraftOperationRequest.Waiver), error.ParamName);
        Assert.Contains("moving-aircraft-operation", error.Message, StringComparison.Ordinal);

        Assert.Equal(
            nameof(MovingAircraftOperationRequest.FromAMovingAircraft),
            Assert.Throws<ArgumentException>(() => EntryPoints.MovingAircraftOperation.Resolve(
                new MovingAircraftOperationRequest(RuleRequest.Empty) { Waiver = NoWaiver })).ParamName);
    }
}
