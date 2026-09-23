using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>moving-vehicle-operation</c>, § 107.25(b), resolved through
/// <see cref="EntryPoints.MovingVehicleOperation"/> only: the two situations the evidence names that
/// do not turn on the entry's open question, the decline where the answer does turn on it, and the
/// § 107.205 gate.
/// </summary>
public class MovingVehicleOperationEntryPointTests
{
    private const string Caller = nameof(MovingVehicleOperationEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.25", Caller);

    private static MovingVehicleFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<MovingVehicleFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    private static Resolution<object> Resolve(bool fromVehicle, bool forHire, WaiverStatement waiver) =>
        EntryPoints.MovingVehicleOperation.Resolve(new MovingVehicleOperationRequest
        {
            FromMovingLandOrWaterBorneVehicle = fromVehicle,
            TransportingAnotherPersonsPropertyForCompensationOrHire = forHire,
            Waiver = waiver,
        });

    [Fact]
    public void Not_from_a_moving_land_or_water_borne_vehicle_it_resolves_not_prohibited_whether_or_not_property_is_carried_for_hire()
    {
        var finding = Finding(Resolve(fromVehicle: false, forHire: false, NoWaiver));

        Assert.False(finding.Prohibited);
        Assert.False(finding.FromMovingLandOrWaterBorneVehicle);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.25", finding.Authority.Citation);
        Assert.Equal(EntryPoints.MovingVehicleOperation.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);

        var carrying = Finding(Resolve(fromVehicle: false, forHire: true, NoWaiver));

        Assert.False(carrying.Prohibited);
        Assert.True(carrying.TransportingAnotherPersonsPropertyForCompensationOrHire);
    }

    [Fact]
    public void From_a_moving_vehicle_transporting_another_persons_property_for_compensation_or_hire_it_resolves_prohibited_citing_107_25()
    {
        var finding = Finding(Resolve(fromVehicle: true, forHire: true, NoWaiver));

        Assert.True(finding.Prohibited);
        Assert.True(finding.FromMovingLandOrWaterBorneVehicle);
        Assert.True(finding.TransportingAnotherPersonsPropertyForCompensationOrHire);
        Assert.Equal("§ 107.25", finding.Authority.Citation);
        Assert.Equal(EntryPoints.MovingVehicleOperation.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
        Assert.Contains("§ 107.25(b) prohibits it", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void From_a_moving_vehicle_not_transporting_another_persons_property_for_hire_it_declines_RequiresInterpretation_citing_107_25()
    {
        var unresolved = Decline(Resolve(fromVehicle: true, forHire: false, NoWaiver));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.25", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.MovingVehicleOperation.Registered.Locator, unresolved.Locator);
        Assert.Contains("moving-vehicle-operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("sparsely populated area", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("carries the whole exception", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void While_a_waiver_of_107_25_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement()
    {
        var waiver = WaiverStatement.Held("§ 107.25", Caller, certificate: "107W-2026-00013");

        var unresolved = Decline(Resolve(fromVehicle: true, forHire: false, waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("moving-vehicle-operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(Caller, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00013", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void A_stated_waiver_declines_rather_than_a_verdict_even_transporting_another_persons_property_for_compensation_or_hire()
    {
        var waiver = WaiverStatement.Held("§ 107.25", Caller);

        var unresolved = Decline(Resolve(fromVehicle: true, forHire: true, waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Contains("moving-vehicle-operation", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.MovingVehicleOperation.Resolve(
            new MovingVehicleOperationRequest
            {
                FromMovingLandOrWaterBorneVehicle = true,
                TransportingAnotherPersonsPropertyForCompensationOrHire = true,
            }));

        Assert.Equal(nameof(MovingVehicleOperationRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_the_facts_the_rule_turns_on_it_refuses_rather_than_infer_them()
    {
        var noVehicleStated = Assert.Throws<ArgumentException>(() => EntryPoints.MovingVehicleOperation.Resolve(
            new MovingVehicleOperationRequest
            {
                TransportingAnotherPersonsPropertyForCompensationOrHire = false,
                Waiver = NoWaiver,
            }));

        Assert.Equal(nameof(MovingVehicleOperationRequest.FromMovingLandOrWaterBorneVehicle), noVehicleStated.ParamName);

        var noCarriageStated = Assert.Throws<ArgumentException>(() => EntryPoints.MovingVehicleOperation.Resolve(
            new MovingVehicleOperationRequest
            {
                FromMovingLandOrWaterBorneVehicle = true,
                Waiver = NoWaiver,
            }));

        Assert.Equal(
            nameof(MovingVehicleOperationRequest.TransportingAnotherPersonsPropertyForCompensationOrHire),
            noCarriageStated.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            fromVehicle: true,
            forHire: false,
            WaiverStatement.Held("§ 107.51", Caller)));

        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
        Assert.Contains("moving-vehicle-operation", error.Message, StringComparison.Ordinal);
    }
}
