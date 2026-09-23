using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>airspace-authorized</c>, § 107.41, resolved through <see cref="EntryPoints.AirspaceAuthorized"/>
/// only: each of Class B, Class C, Class D and the lateral surface area of Class E, with and without
/// prior ATC authorization; Class G, which needs none; and the waiver gate in both directions (the
/// entry's note, and rules-factory decision 0021).
/// </summary>
/// <remarks>
/// The two facts the rule turns on are the caller's: which airspace, and whether prior authorization
/// from ATC was obtained. Several of these pin that the engine states neither for the caller — it
/// refuses rather than infer an airspace, and refuses rather than read an unstated authorization as
/// a denial. Two more pin the other half of that: the airspaces a caller may state are a closed set,
/// so the engine never answers about an airspace it has not identified.
/// </remarks>
public class AirspaceAuthorizedEntryPointTests
{
    private const string Caller = nameof(AirspaceAuthorizedEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.41", Caller);

    /// <summary>The four airspaces § 107.41 names, checked exhaustively rather than sampled.</summary>
    private static readonly AirspaceClass[] TheSectionNames =
    [
        AirspaceClass.ClassB,
        AirspaceClass.ClassC,
        AirspaceClass.ClassD,
        AirspaceClass.ClassESurfaceAreaDesignatedForAnAirport,
    ];

    /// <summary>Airspace § 107.41 does not name: the note's Class G, and the Class E its own restriction leaves out.</summary>
    private static readonly AirspaceClass[] TheSectionDoesNotName =
    [
        AirspaceClass.ClassG,
        AirspaceClass.ClassEOutsideAnAirportSurfaceArea,
    ];

    /// <summary>The four airspaces § 107.41 names.</summary>
    public static TheoryData<AirspaceClass> AirspaceTheSectionNames => Rows(TheSectionNames);

    /// <summary>The airspaces this engine names that § 107.41 does not.</summary>
    public static TheoryData<AirspaceClass> AirspaceTheSectionDoesNotName => Rows(TheSectionDoesNotName);

    private static TheoryData<AirspaceClass> Rows(IEnumerable<AirspaceClass> airspaces)
    {
        var rows = new TheoryData<AirspaceClass>();
        foreach (var airspace in airspaces)
        {
            rows.Add(airspace);
        }

        return rows;
    }

    private static Resolution<object> Resolve(AirspaceClass airspace, AtcAuthorization authorization, WaiverStatement waiver) =>
        EntryPoints.AirspaceAuthorized.Resolve(new AirspaceAuthorizedRequest
        {
            Airspace = airspace,
            Authorization = authorization,
            Waiver = waiver,
        });

    private static AirspaceFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<AirspaceFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    [Theory]
    [MemberData(nameof(AirspaceTheSectionNames))]
    public void Each_airspace_107_41_names_may_be_operated_in_with_prior_ATC_authorization_citing_107_41(AirspaceClass airspace)
    {
        var authorization = AtcAuthorization.Prior(Caller, "ATC authorization 2026-0001");

        var finding = Finding(Resolve(airspace, authorization, NoWaiver));

        Assert.True(finding.AuthorizationRequired);
        Assert.True(finding.MayOperate);
        Assert.Equal(airspace, finding.Airspace);
        Assert.Same(authorization, finding.Authorization);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.41", finding.Authority.Citation);
        Assert.Equal(EntryPoints.AirspaceAuthorized.Registered.Locator, finding.Authority);
    }

    [Theory]
    [MemberData(nameof(AirspaceTheSectionNames))]
    public void Each_airspace_107_41_names_may_not_be_operated_in_without_ATC_authorization(AirspaceClass airspace)
    {
        var authorization = AtcAuthorization.None(Caller);

        var finding = Finding(Resolve(airspace, authorization, NoWaiver));

        Assert.True(finding.AuthorizationRequired);
        Assert.False(finding.MayOperate);
        Assert.Equal(airspace, finding.Airspace);
        Assert.Same(authorization, finding.Authorization);
        Assert.Equal("§ 107.41", finding.Authority.Citation);
    }

    [Theory]
    [MemberData(nameof(AirspaceTheSectionNames))]
    public void An_ATC_authorization_that_was_not_prior_does_not_satisfy_107_41(AirspaceClass airspace)
    {
        var authorization = AtcAuthorization.NotPrior(Caller, "ATC authorization 2026-0002");

        var finding = Finding(Resolve(airspace, authorization, NoWaiver));

        Assert.True(finding.AuthorizationRequired);
        Assert.False(finding.MayOperate);
        Assert.True(finding.Authorization.Held);
        Assert.False(finding.Authorization.ObtainedBeforeTheOperation);
    }

    [Theory]
    [MemberData(nameof(AirspaceTheSectionDoesNotName))]
    public void Airspace_107_41_does_not_name_needs_no_authorization_and_the_statement_is_still_recorded(AirspaceClass airspace)
    {
        var none = AtcAuthorization.None(Caller);
        var prior = AtcAuthorization.Prior(Caller);

        var without = Finding(Resolve(airspace, none, NoWaiver));
        var with = Finding(Resolve(airspace, prior, NoWaiver));

        Assert.False(without.AuthorizationRequired);
        Assert.True(without.MayOperate);
        Assert.Same(none, without.Authorization);

        Assert.False(with.AuthorizationRequired);
        Assert.True(with.MayOperate);
        Assert.Same(prior, with.Authorization);
    }

    [Theory]
    [MemberData(nameof(AirspaceTheSectionNames))]
    public void While_a_waiver_of_107_41_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement(AirspaceClass airspace)
    {
        var waiver = WaiverStatement.Held("§ 107.41", Caller);

        var unresolved = Assert.IsType<Resolution<object>.Unresolved>(
            Resolve(airspace, AtcAuthorization.None(Caller), waiver)).Result;

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("airspace-authorized", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.41", "the remote pilot in command");

        var authorized = Finding(Resolve(AirspaceClass.ClassB, AtcAuthorization.Prior(Caller), waiver));
        var unauthorized = Finding(Resolve(AirspaceClass.ClassB, AtcAuthorization.None(Caller), waiver));

        Assert.True(authorized.MayOperate);
        Assert.False(unauthorized.MayOperate);
        Assert.Same(waiver, authorized.Waiver);
        Assert.Same(waiver, unauthorized.Waiver);
        Assert.Equal("the remote pilot in command", unauthorized.Waiver.StatedBy);
        Assert.False(unauthorized.Waiver.InForce);
    }

    [Fact]
    public void Without_an_airspace_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AirspaceAuthorized.Resolve(
            new AirspaceAuthorizedRequest { Authorization = AtcAuthorization.None(Caller), Waiver = NoWaiver }));

        Assert.Equal(nameof(AirspaceAuthorizedRequest.Airspace), error.ParamName);
    }

    [Fact]
    public void Without_an_ATC_authorization_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AirspaceAuthorized.Resolve(
            new AirspaceAuthorizedRequest { Airspace = AirspaceClass.ClassB, Waiver = NoWaiver }));

        Assert.Equal(nameof(AirspaceAuthorizedRequest.Authorization), error.ParamName);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.AirspaceAuthorized.Resolve(
            new AirspaceAuthorizedRequest { Airspace = AirspaceClass.ClassB, Authorization = AtcAuthorization.None(Caller) }));

        Assert.Equal(nameof(AirspaceAuthorizedRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            AirspaceClass.ClassB,
            AtcAuthorization.None(Caller),
            WaiverStatement.Held("§ 107.51", Caller)));

        Assert.Contains("§ 107.41", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// § 107.41's table is finite, so it is checked exhaustively: every airspace a caller can state
    /// is on exactly one side of it. A seventh airspace added to <see cref="AirspaceClass.All"/>
    /// without a decision about which side it falls on turns this red rather than going unnoticed.
    /// </summary>
    [Fact]
    public void Every_airspace_this_engine_names_is_on_exactly_one_side_of_107_41s_table()
    {
        var covered = TheSectionNames.Concat(TheSectionDoesNotName).ToArray();

        Assert.Empty(TheSectionNames.Intersect(TheSectionDoesNotName));
        Assert.Equal(AirspaceClass.All.Count, covered.Length);
        Assert.Equal(
            AirspaceClass.All.Select(a => a.Designation).OrderBy(d => d, StringComparer.Ordinal),
            covered.Select(a => a.Designation).OrderBy(d => d, StringComparer.Ordinal));
    }

    /// <summary>
    /// The safety property of <see cref="AirspaceClass"/>: the set is closed, so the rule never
    /// answers about an airspace it has not identified. Were a factory taking a designation to
    /// exist, a caller writing "Class B" rather than "Class B airspace" would be told § 107.41 does
    /// not reach them — a permission the section does not give. That path must stay shut, and a
    /// reopened one is caught here rather than by a reader noticing.
    /// </summary>
    [Fact]
    public void There_is_no_public_way_to_state_an_airspace_this_engine_does_not_name()
    {
        Assert.Empty(typeof(AirspaceClass).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        var minting = typeof(AirspaceClass)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType == typeof(AirspaceClass) && method.GetParameters().Length > 0)
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(minting);
        Assert.Equal(6, AirspaceClass.All.Count);
    }
}
