using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>right-of-way</c>, § 107.37(a), resolved through <see cref="EntryPoints.RightOfWay"/> only.
/// The entry's two enumerations are finite and closed — three kinds the paragraph names and three
/// relative positions it prohibits, each with the case the caller states is none of them — so the
/// sixteen cells are checked exhaustively rather than sampled: which of them the engine decides,
/// which reach the exception "unless well clear" and so decline on <c>well-clear</c>'s question,
/// and that the waiver gate answers first and names this entry rather than that one.
/// </summary>
public class RightOfWayEntryPointTests
{
    private const string Caller = nameof(RightOfWayEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.37(a)", Caller);

    /// <summary>The three kinds § 107.37(a) names, written out here rather than asked of the rule.</summary>
    private static readonly EncounteredObject[] Named =
    [
        EncounteredObject.Aircraft,
        EncounteredObject.AirborneVehicle,
        EncounteredObject.LaunchOrReentryVehicle,
    ];

    /// <summary>The three relative positions § 107.37(a) prohibits, written out here rather than asked of the rule.</summary>
    private static readonly RelativePosition[] Prohibited =
    [
        RelativePosition.Over,
        RelativePosition.Under,
        RelativePosition.Ahead,
    ];

    public static TheoryData<EncounteredObject, RelativePosition> EveryPass =>
        Cells(EncounteredObject.All, RelativePosition.All);

    public static TheoryData<EncounteredObject, RelativePosition> NotNamed =>
        Cells([EncounteredObject.NoneOfThem], RelativePosition.All);

    public static TheoryData<EncounteredObject, RelativePosition> NamedButNotProhibited =>
        Cells(Named, [RelativePosition.NoneOfThem]);

    public static TheoryData<EncounteredObject, RelativePosition> NamedAndProhibited =>
        Cells(Named, Prohibited);

    private static TheoryData<EncounteredObject, RelativePosition> Cells(
        IReadOnlyList<EncounteredObject> objects,
        IReadOnlyList<RelativePosition> positions)
    {
        var data = new TheoryData<EncounteredObject, RelativePosition>();
        foreach (var encountered in objects)
        {
            foreach (var position in positions)
            {
                data.Add(encountered, position);
            }
        }

        return data;
    }

    private static Resolution<object> Resolve(
        EncounteredObject encountered,
        RelativePosition position,
        WaiverStatement waiver) =>
        EntryPoints.RightOfWay.Resolve(new RightOfWayRequest
        {
            Encountered = encountered,
            Position = position,
            Waiver = waiver,
        });

    private static RightOfWayFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<RightOfWayFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Theory]
    [MemberData(nameof(NotNamed))]
    public void An_object_107_37_a_does_not_name_resolves_whatever_the_pass_citing_107_37_a(
        EncounteredObject encountered,
        RelativePosition position)
    {
        var finding = Finding(Resolve(encountered, position, NoWaiver));

        Assert.False(finding.ObjectIsOneTheSectionNames);
        Assert.True(finding.MayPass);
        Assert.Equal(encountered, finding.Object);
        Assert.Equal(position, finding.Position);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.37(a)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.RightOfWay.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Theory]
    [MemberData(nameof(NamedButNotProhibited))]
    public void An_object_107_37_a_names_passed_neither_over_under_nor_ahead_resolves_citing_107_37_a(
        EncounteredObject encountered,
        RelativePosition position)
    {
        var finding = Finding(Resolve(encountered, position, NoWaiver));

        Assert.True(finding.ObjectIsOneTheSectionNames);
        Assert.False(finding.PositionIsOneTheSectionProhibits);
        Assert.True(finding.MayPass);
        Assert.Equal(encountered, finding.Object);
        Assert.Equal("§ 107.37(a)", finding.Authority.Citation);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    /// <summary>
    /// The nine cells that reach the exception. The decline is this entry's own and follows what
    /// <c>well-clear</c> answered on the same request: its reason, its citation, and its account
    /// quoted whole — but naming the entry the caller actually asked about, so that a
    /// <c>right-of-way</c> decline is not a <c>well-clear</c> decline wearing the same bytes
    /// (<c>docs/decisions/0006</c>).
    /// </summary>
    [Theory]
    [MemberData(nameof(NamedAndProhibited))]
    public void An_object_107_37_a_names_passed_over_under_or_ahead_declines_with_this_entrys_own_decline_on_well_clears_question(
        EncounteredObject encountered,
        RelativePosition position)
    {
        var resolution = Resolve(encountered, position, NoWaiver);
        var unresolved = Decline(resolution);

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.37(a)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WellClear.Registered.Locator, unresolved.Locator);

        // What the dependency actually answered here is what this decline is built from, and the
        // decline says so in those terms rather than asserting the openness from a constant.
        Assert.Contains("did not answer whether the pass is well clear", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("has no verdict to read", unresolved.Attempted, StringComparison.Ordinal);

        var wellClear = Decline(EntryPoints.WellClear.Resolve(new WellClearRequest { Waiver = NoWaiver }));

        // Not that entry's decline handed back. The reason and the locator are its, because the
        // question that blocks the answer is its question; both entries cite § 107.37(a), so what
        // was attempted is the only thing that can tell the two apart -- and it does, naming the
        // entry the caller asked about as well as the one it reached.
        Assert.NotEqual(wellClear, unresolved);
        Assert.NotEqual(wellClear.Attempted, unresolved.Attempted);
        Assert.Equal(wellClear.Reason, unresolved.Reason);
        Assert.Equal(wellClear.Locator, unresolved.Locator);
        Assert.Contains("right-of-way", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("well-clear", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("right-of-way", wellClear.Attempted, StringComparison.Ordinal);

        // And the constituent's account is carried forward whole rather than lost, so a caller
        // sees where the openness originates without being handed that entry's result instead.
        Assert.Contains(wellClear.Attempted, unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("\"well clear\"", unresolved.Attempted, StringComparison.Ordinal);

        // Nothing quantitative stands in for the term the corpus leaves undefined.
        Assert.DoesNotContain("feet", unresolved.Attempted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("seconds", unresolved.Attempted, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(EveryPass))]
    public void Every_combination_of_the_two_enumerations_is_decided_by_those_enumerations_alone(
        EncounteredObject encountered,
        RelativePosition position)
    {
        // Sixteen cells, exhaustive over both closed sets. The expectation is the map's own
        // reading, written independently of the rule: the case reaches the exception only when the
        // object is one of the three § 107.37(a) names and the pass is in one of the three it
        // prohibits, and is decided without the exception otherwise.
        Assert.Equal(4, EncounteredObject.All.Count);
        Assert.Equal(4, RelativePosition.All.Count);

        var reachesTheException = Named.Contains(encountered) && Prohibited.Contains(position);
        var resolution = Resolve(encountered, position, NoWaiver);

        if (reachesTheException)
        {
            Assert.Equal(UnresolvedReason.RequiresInterpretation, Decline(resolution).Reason);
        }
        else
        {
            var finding = Finding(resolution);
            Assert.True(finding.MayPass);
            Assert.Equal(Named.Contains(encountered), finding.ObjectIsOneTheSectionNames);
            Assert.Equal(Prohibited.Contains(position), finding.PositionIsOneTheSectionProhibits);
        }
    }

    [Theory]
    [MemberData(nameof(EveryPass))]
    public void While_a_waiver_of_107_37_a_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_names_this_entry_not_well_clear(
        EncounteredObject encountered,
        RelativePosition position)
    {
        var waiver = WaiverStatement.Held("§ 107.37(a)", Caller, certificate: "107W-2026-00018");

        var unresolved = Decline(Resolve(encountered, position, waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("107W-2026-00018", unresolved.Attempted, StringComparison.Ordinal);

        // The gate is this entry's own, run as its own check. well-clear is suspended by the same
        // § 107.205(f) and cites the same § 107.37(a), so neither the reason nor the locator tells
        // the two apart; the entry the gate names does.
        Assert.Contains("right-of-way", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("well-clear", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void It_resolves_the_same_way_whatever_the_caller_asserts_for_it()
    {
        // right-of-way is kind: operation, not assertion: the caller states the two facts the
        // paragraph's enumerations are tested against, and the engine reads nothing else it is
        // told about the entry -- including about well-clear, which is its own entry and declines.
        var assertions = RuleRequest.Empty.Assert("right-of-way", true).Assert("well-clear", true);

        var asserted = new RightOfWayRequest(assertions)
        {
            Encountered = EncounteredObject.Aircraft,
            Position = RelativePosition.Over,
            Waiver = NoWaiver,
        };

        Assert.Equal(
            Resolve(EncounteredObject.Aircraft, RelativePosition.Over, NoWaiver),
            EntryPoints.RightOfWay.Resolve(asserted));
        Assert.Equal(UnresolvedReason.RequiresInterpretation, Decline(EntryPoints.RightOfWay.Resolve(asserted)).Reason);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.RightOfWay.Resolve(new RightOfWayRequest
        {
            Encountered = EncounteredObject.Aircraft,
            Position = RelativePosition.Over,
        }));

        Assert.Equal(nameof(RightOfWayRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_the_object_or_the_pass_it_refuses()
    {
        var withoutObject = Assert.Throws<ArgumentException>(() => EntryPoints.RightOfWay.Resolve(
            new RightOfWayRequest { Position = RelativePosition.Over, Waiver = NoWaiver }));
        var withoutPosition = Assert.Throws<ArgumentException>(() => EntryPoints.RightOfWay.Resolve(
            new RightOfWayRequest { Encountered = EncounteredObject.Aircraft, Waiver = NoWaiver }));

        Assert.Equal(nameof(RightOfWayRequest.Encountered), withoutObject.ParamName);
        Assert.Equal(nameof(RightOfWayRequest.Position), withoutPosition.ParamName);

        // The dictionary dispatch builds a request with every input at its default, so the same
        // refusal is what stands between it and an invented answer.
        Assert.Throws<ArgumentException>(() => Registry.Resolve("right-of-way", RuleRequest.Empty));
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.RightOfWay.Resolve(new RightOfWayRequest
        {
            Encountered = EncounteredObject.Aircraft,
            Position = RelativePosition.Over,
            Waiver = WaiverStatement.Held("§ 107.51", Caller),
        }));

        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
        Assert.Contains("right-of-way", error.Message, StringComparison.Ordinal);
    }
}
