using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>single-aircraft</c>, § 107.35, resolved through <see cref="EntryPoints.SingleAircraft"/> only:
/// the three situations the entry's note names — one aircraft, two aircraft, and the same person
/// acting as visual observer for a second — either side of "more than one", each of the three roles
/// the evidence names, and the waiver gate in both directions (rules-factory decision 0021).
/// </summary>
public class SingleAircraftEntryPointTests
{
    private const string Caller = nameof(SingleAircraftEntryPointTests);

    private const string Person = "the person at the ground control station";

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.35", Caller);

    private static Resolution<object> Resolve(params AircraftEngagement[] engagements) =>
        Resolve(Person, NoWaiver, engagements);

    private static Resolution<object> Resolve(string person, WaiverStatement waiver, params AircraftEngagement[] engagements) =>
        EntryPoints.SingleAircraft.Resolve(new SingleAircraftRequest
        {
            Person = person,
            Engagements = engagements,
            Waiver = waiver,
        });

    private static MultipleAircraftFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<MultipleAircraftFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Not_more_than_one_unmanned_aircraft_at_the_same_time_resolves_permitted_citing_107_35()
    {
        var none = Finding(Resolve());
        var flightControls = Finding(Resolve(new AircraftEngagement("N1", AircraftRole.ManipulatingFlightControls)));
        var remotePilot = Finding(Resolve(new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand)));
        var visualObserver = Finding(Resolve(new AircraftEngagement("N1", AircraftRole.VisualObserver)));
        var allThreeRoles = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.ManipulatingFlightControls),
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N1", AircraftRole.VisualObserver)));

        Assert.All(
            new[] { none, flightControls, remotePilot, visualObserver, allThreeRoles },
            finding => Assert.True(finding.Permitted));
        Assert.Equal(0, none.AircraftCount);
        Assert.All(
            new[] { flightControls, remotePilot, visualObserver, allThreeRoles },
            finding => Assert.Equal(1, finding.AircraftCount));
        Assert.Equal(new[] { "N1" }, allThreeRoles.Aircraft);
        Assert.Equal(3, allThreeRoles.Engagements.Length);
        Assert.Equal(Person, flightControls.Person);
        Assert.Equal("cfr-14-107", flightControls.Authority.SourceId);
        Assert.Equal("§ 107.35", flightControls.Authority.Citation);
        Assert.Equal(EntryPoints.SingleAircraft.Registered.Locator, flightControls.Authority);
        Assert.Same(NoWaiver, flightControls.Waiver);
    }

    [Fact]
    public void More_than_one_unmanned_aircraft_at_the_same_time_resolves_prohibited_citing_107_35()
    {
        var twoAtTheControls = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.ManipulatingFlightControls),
            new AircraftEngagement("N2", AircraftRole.ManipulatingFlightControls)));
        var twoAsRemotePilot = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N2", AircraftRole.RemotePilotInCommand)));
        var twoAsVisualObserver = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.VisualObserver),
            new AircraftEngagement("N2", AircraftRole.VisualObserver)));
        var three = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.ManipulatingFlightControls),
            new AircraftEngagement("N2", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N3", AircraftRole.VisualObserver)));

        Assert.All(
            new[] { twoAtTheControls, twoAsRemotePilot, twoAsVisualObserver, three },
            finding => Assert.False(finding.Permitted));
        Assert.Equal(2, twoAtTheControls.AircraftCount);
        Assert.Equal(new[] { "N1", "N2" }, twoAtTheControls.Aircraft);
        Assert.Equal(3, three.AircraftCount);
        Assert.Equal(new[] { "N1", "N2", "N3" }, three.Aircraft);
        Assert.Equal("cfr-14-107", twoAtTheControls.Authority.SourceId);
        Assert.Equal("§ 107.35", twoAtTheControls.Authority.Citation);
        Assert.Equal(EntryPoints.SingleAircraft.Registered.Locator, twoAtTheControls.Authority);
        Assert.Same(NoWaiver, twoAtTheControls.Waiver);
    }

    [Fact]
    public void The_same_person_acting_as_visual_observer_for_a_second_aircraft_is_prohibited()
    {
        var remotePilotAndObserverOfASecond = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N2", AircraftRole.VisualObserver)));
        var atTheControlsAndObserverOfASecond = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.ManipulatingFlightControls),
            new AircraftEngagement("N2", AircraftRole.VisualObserver)));
        var observerOfTheSameAircraft = Finding(Resolve(
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N1", AircraftRole.VisualObserver)));

        Assert.False(remotePilotAndObserverOfASecond.Permitted);
        Assert.Equal(2, remotePilotAndObserverOfASecond.AircraftCount);
        Assert.False(atTheControlsAndObserverOfASecond.Permitted);
        Assert.Equal(new[] { "N1", "N2" }, atTheControlsAndObserverOfASecond.Aircraft);

        // The sentence counts unmanned aircraft, not roles: a second role for the same aircraft is
        // still one aircraft.
        Assert.True(observerOfTheSameAircraft.Permitted);
        Assert.Equal(1, observerOfTheSameAircraft.AircraftCount);
        Assert.Equal(2, observerOfTheSameAircraft.Engagements.Length);
    }

    [Fact]
    public void While_a_waiver_of_107_35_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_names_single_aircraft()
    {
        var waiver = WaiverStatement.Held("§ 107.35", Caller, "107W-0000-00000");

        var one = Declined(Resolve(Person, waiver, new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand)));
        var two = Declined(Resolve(
            Person,
            waiver,
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N2", AircraftRole.VisualObserver)));

        Assert.All(new[] { one, two }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
            Assert.Contains("single-aircraft", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.35", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.35", "the remote pilot in command");

        var permitted = Finding(Resolve(Person, waiver, new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand)));
        var prohibited = Finding(Resolve(
            Person,
            waiver,
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand),
            new AircraftEngagement("N2", AircraftRole.RemotePilotInCommand)));

        Assert.True(permitted.Permitted);
        Assert.False(prohibited.Permitted);
        Assert.Same(waiver, permitted.Waiver);
        Assert.Same(waiver, prohibited.Waiver);
        Assert.Equal("the remote pilot in command", prohibited.Waiver.StatedBy);
        Assert.False(prohibited.Waiver.InForce);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            Person,
            WaiverStatement.Held("§ 107.51", Caller),
            new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand)));

        Assert.Contains("single-aircraft", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SingleAircraft.Resolve(new SingleAircraftRequest
        {
            Person = Person,
            Engagements = new[] { new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand) },
        }));

        Assert.Equal(nameof(SingleAircraftRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_the_aircraft_the_person_is_engaged_with_it_refuses_rather_than_infer_them()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SingleAircraft.Resolve(new SingleAircraftRequest
        {
            Person = Person,
            Waiver = NoWaiver,
        }));

        Assert.Equal(nameof(SingleAircraftRequest.Engagements), error.ParamName);
    }

    [Fact]
    public void Without_the_person_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.SingleAircraft.Resolve(new SingleAircraftRequest
        {
            Engagements = new[] { new AircraftEngagement("N1", AircraftRole.RemotePilotInCommand) },
            Waiver = NoWaiver,
        }));

        Assert.Equal(nameof(SingleAircraftRequest.Person), error.ParamName);
    }
}
