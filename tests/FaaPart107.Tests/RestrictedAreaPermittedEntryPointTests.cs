using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>restricted-area-permitted</c>, § 107.45, resolved through
/// <see cref="EntryPoints.RestrictedAreaPermitted"/> only: both designations the section states,
/// each with and without the permission it names, and an area that is neither — the entry's note.
/// Both facts are the caller's, and neither is inferred.
/// </summary>
public class RestrictedAreaPermittedEntryPointTests
{
    private const string Caller = nameof(RestrictedAreaPermittedEntryPointTests);

    private static readonly PermissionStatement Granted = PermissionStatement.Granted(Caller);

    private static readonly PermissionStatement NotGranted = PermissionStatement.NotGranted(Caller);

    private static Resolution<object> Resolve(AreaDesignation area, PermissionStatement permission) =>
        EntryPoints.RestrictedAreaPermitted.Resolve(
            new RestrictedAreaPermittedRequest { Area = area, Permission = permission });

    private static AreaPermissionFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<AreaPermissionFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    /// <summary>The two designations § 107.45 states, in the order it states them.</summary>
    public static TheoryData<AreaDesignation> DesignatedAreas => new()
    {
        AreaDesignation.Prohibited,
        AreaDesignation.Restricted,
    };

    [Theory]
    [MemberData(nameof(DesignatedAreas))]
    public void In_a_prohibited_or_restricted_area_with_permission_the_operation_is_permitted_citing_107_45(AreaDesignation area)
    {
        var finding = Finding(Resolve(area, Granted));

        Assert.True(finding.Permitted);
        Assert.True(finding.PermissionRequired);
        Assert.Equal(area, finding.Designation);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.45", finding.Authority.Citation);
        Assert.Equal(EntryPoints.RestrictedAreaPermitted.Registered.Locator, finding.Authority);
    }

    [Theory]
    [MemberData(nameof(DesignatedAreas))]
    public void In_a_prohibited_or_restricted_area_without_permission_the_operation_is_not_permitted_citing_107_45(AreaDesignation area)
    {
        var finding = Finding(Resolve(area, NotGranted));

        Assert.False(finding.Permitted);
        Assert.True(finding.PermissionRequired);
        Assert.Equal(area, finding.Designation);
        Assert.Equal("§ 107.45", finding.Authority.Citation);
        Assert.Equal(EntryPoints.RestrictedAreaPermitted.Registered.Locator, finding.Authority);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Outside_a_prohibited_or_restricted_area_the_section_requires_no_permission_and_does_not_bar_the_operation(bool held)
    {
        // § 107.45 prohibits operating "in prohibited or restricted areas"; where the area is
        // neither, the prohibition does not reach the operation and the "unless" asks for nothing.
        var permission = held ? Granted : NotGranted;

        var finding = Finding(Resolve(AreaDesignation.NeitherProhibitedNorRestricted, permission));

        Assert.True(finding.Permitted);
        Assert.False(finding.PermissionRequired);
        Assert.Equal(AreaDesignation.NeitherProhibitedNorRestricted, finding.Designation);
        Assert.Same(permission, finding.Permission);
    }

    [Fact]
    public void The_permission_recorded_is_the_callers_own_statement_and_the_agency_it_names_is_not_judged()
    {
        // "as appropriate" is § 107.45's own, and the corpus states no test for which of the two
        // agencies is the appropriate one; the engine records what the caller named and judges none
        // of it. Both statements below are honoured exactly as stated.
        var named = PermissionStatement.Granted("the remote pilot in command", "the using agency");
        var unnamed = PermissionStatement.Granted("the remote pilot in command");

        var withAgency = Finding(Resolve(AreaDesignation.Restricted, named));
        var withoutAgency = Finding(Resolve(AreaDesignation.Restricted, unnamed));

        Assert.Same(named, withAgency.Permission);
        Assert.Same(unnamed, withoutAgency.Permission);
        Assert.Equal("the using agency", withAgency.Permission.Agency);
        Assert.Null(withoutAgency.Permission.Agency);
        Assert.Equal("the remote pilot in command", withAgency.Permission.StatedBy);
        Assert.True(withAgency.Permitted);
        Assert.True(withoutAgency.Permitted);
    }

    [Fact]
    public void Without_a_permission_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.RestrictedAreaPermitted.Resolve(
            new RestrictedAreaPermittedRequest { Area = AreaDesignation.Prohibited }));

        Assert.Equal(nameof(RestrictedAreaPermittedRequest.Permission), error.ParamName);
    }

    [Fact]
    public void Without_an_area_designation_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.RestrictedAreaPermitted.Resolve(
            new RestrictedAreaPermittedRequest { Permission = NotGranted }));

        Assert.Equal(nameof(RestrictedAreaPermittedRequest.Area), error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void An_area_designation_that_is_none_the_section_states_is_refused(int value)
    {
        // Nothing in the enum is zero, so a default-constructed designation is refused too rather
        // than read as the one that excuses the operation.
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => EntryPoints.RestrictedAreaPermitted.Resolve(
            new RestrictedAreaPermittedRequest { Area = (AreaDesignation)value, Permission = NotGranted }));

        Assert.Equal("designation", error.ParamName);
    }

    [Fact]
    public void The_entry_has_no_waiver_gate_so_it_asks_for_no_waiver_statement()
    {
        // The entry's suspendedBy is empty: § 107.205's list reaches § 107.41 and § 107.51 and not
        // § 107.45. An entry gets the gate its own map row gives it, so this request carries no
        // waiver statement and the rule resolves without one.
        Assert.Empty(Array.FindAll(
            typeof(RestrictedAreaPermittedRequest).GetProperties(),
            p => p.PropertyType == typeof(WaiverStatement)));

        Assert.True(Finding(Resolve(AreaDesignation.Prohibited, Granted)).Permitted);
        Assert.False(Finding(Resolve(AreaDesignation.Prohibited, NotGranted)).Permitted);
    }
}
