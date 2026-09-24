using FaaPart107.Evaluation;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>weather-minimums-met</c>, § 107.51(c)-(d), resolved through
/// <see cref="EntryPoints.WeatherMinimumsMet"/>: each cloud minimum at, just below and just
/// above its printed figure and each independently of the other, the operation the caller states
/// is not near a cloud at all, a stated flight visibility at, just below and just above 3 statute
/// miles, and the waiver gate in both directions (the entry's note, and rules-factory decision
/// 0021).
/// </summary>
/// <remarks>
/// <para>
/// The entry resolves exactly one outcome, the minimums not met, and only where neither cloud
/// minimum is met — § 107.51(d) is then broken on either reading of how its two figures combine,
/// so the conjunction is false whatever the visibility is. Everywhere else a cloud is named,
/// § 107.51(c) is reached and the answer turns on which objects are "prominent", which
/// <c>prominent-objects</c> holds open, so the engine declines rather than reading the caller's
/// stated figure as the section's defined flight visibility. The tests below pin both halves of
/// that, and the figures compared
/// against are never written here as literals expected to come from the entry: they are
/// <c>visibility-minimum</c>'s and <c>cloud-clearance</c>'s, resolved through their own entry
/// points.
/// </para>
/// <para>
/// The third case is the one <see cref="CloudStatement.NoCloud"/> exists for, and it is the
/// ordinary weather: no cloud to measure either minimum from. It is not the resolving outcome —
/// nothing was measured, so neither minimum was found unmet — and it is not § 107.51(d) met
/// either, which would be this engine answering a question the map does not settle. It declines,
/// and the decline says which of those two it is not.
/// </para>
/// <para>
/// The last two reach the entry through <see cref="OperationEvaluator"/> rather than through its
/// entry point, because what they pin is about the finding and can only be seen there: this
/// entry's finding records § 107.51(c)'s stated flight visibility and its
/// <see cref="WeatherMinimumsFinding.ToString"/> does not print it, so two operations differing
/// only in that figure are two findings the engine says the same words about. That makes this
/// entry <em>a</em> worked case for
/// <see cref="EvaluatedRequirement.Equals(EvaluatedRequirement)"/> comparing the finding rather
/// than the rendering, and the simplest one, because the unprinted field is on the finding itself.
/// </para>
/// <para>
/// It is <b>not the only one</b>. Every composite finding lists per-constituent outcomes —
/// <see cref="LimitationOutcome"/>, <see cref="ExceptedCaseOutcome"/>,
/// <see cref="RequirementOutcome"/>, <see cref="ObligationOutcome"/> — each recording an
/// <c>Account</c> of what that constituent said and printing only its id, locator and verdict,
/// which is the same shape one level down a collection. Three entries are pinned on it:
/// <c>OperatingLimitationsEntryPointTests.Two_operations_differing_only_in_a_constituents_recorded_account_are_different_outcomes</c>,
/// which is § 107.51's introductory text reading this very entry's decline;
/// <c>VisualObserverConditionsEntryPointTests.Two_operations_differing_only_in_who_asserted_the_coordination_are_different_outcomes</c>;
/// and
/// <c>OverHumanBeingsEntryPointTests.Two_operations_differing_only_in_who_asserted_the_reasonable_protection_are_different_outcomes</c>.
/// In the last two what is recorded and not printed is <em>who</em> the assertion is attributed
/// to, which is the thing an assertion exists to carry.
/// </para>
/// <para>
/// <c>preflight-actions</c> is the one composite with no test of that shape, and the reason is
/// enumerated rather than assumed: over every state its evaluator arm can be put in — every
/// <see cref="SubpartDOperation"/>, every <see cref="AircraftPower"/>, all sixteen polarities of
/// its four § 107.49 assertion entries, and every asserter each of those accepts, all four being
/// <c>assertedBy: ["remote pilot in command"]</c> and so closed sets — no two of the 58 states in
/// which it resolves have one rendering and two findings. That is a fact about the inputs that
/// entry accepts and not about this comparison, and it is the only claim of that form here that a
/// complete enumeration supports: where <c>assertedBy</c> is the marker <c>["caller"]</c> the
/// asserter is not narrowed at all (<c>docs/decisions/0003</c>), so a sweep over any entry
/// reaching one of those three samples that dimension rather than covering it.
/// </para>
/// <para>
/// Each of the two below is red on a mutation to this entry's own rule as well as on one to that
/// comparison, and both mutations were observed rather than assumed: the first loses its finding
/// when <see cref="Weather.MinimumsMet"/> resolves on the wrong side of § 107.51(d), and the
/// second loses the stated visibility when the finding stops recording it. The overlay holds
/// both.
/// </para>
/// </remarks>
public class WeatherMinimumsMetEntryPointTests
{
    private const string Caller = nameof(WeatherMinimumsMetEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    private static Resolution<object> Resolve(
        decimal flightVisibilityStatuteMiles,
        decimal feetBelowCloud,
        decimal feetHorizontallyFromCloud,
        WaiverStatement waiver) =>
        Resolve(flightVisibilityStatuteMiles, CloudStatement.Measured(feetBelowCloud, feetHorizontallyFromCloud, Caller), waiver);

    private static Resolution<object> Resolve(
        decimal flightVisibilityStatuteMiles,
        CloudStatement cloud,
        WaiverStatement waiver) =>
        EntryPoints.WeatherMinimumsMet.Resolve(new WeatherMinimumsMetRequest
        {
            FlightVisibilityStatuteMiles = flightVisibilityStatuteMiles,
            Cloud = cloud,
            Waiver = waiver,
        });

    private static WeatherMinimumsFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<WeatherMinimumsFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    /// <summary>§ 107.51(c)'s figure, from <c>visibility-minimum</c> and not restated here.</summary>
    private static VisibilityMinimum Minimum =>
        Assert.IsType<VisibilityMinimum>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.VisibilityMinimum.Resolve(new VisibilityMinimumRequest { Waiver = NoWaiver })).Value);

    /// <summary>§ 107.51(d)'s two figures, from <c>cloud-clearance</c> and not restated here.</summary>
    private static CloudClearance Clearance =>
        Assert.IsType<CloudClearance>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.CloudClearance.Resolve(new CloudClearanceRequest { Waiver = NoWaiver })).Value);

    /// <summary>
    /// Stated flight visibilities spanning § 107.51(c)'s figure: well below it, just below it, at
    /// it, just above it and well above it. None of them changes what the entry answers.
    /// </summary>
    public static TheoryData<decimal> StatedVisibilities => new() { 0m, 2.99m, 3m, 3.01m, 10m };

    public static TheoryData<decimal, decimal, decimal> NeitherCloudMinimumMet => new()
    {
        // Nowhere near either figure.
        { 10m, 0m, 0m },

        // Just below both figures, with the visibility just below its own figure.
        { 2.99m, 499.99m, 1999.99m },

        // Just below both figures, with the visibility well above its own figure.
        { 10m, 499.99m, 1999.99m },
    };

    [Theory]
    [MemberData(nameof(NeitherCloudMinimumMet))]
    public void Neither_cloud_minimum_met_resolves_the_minimums_not_met_whatever_the_stated_visibility_citing_107_51_c_d(
        decimal visibility,
        decimal below,
        decimal horizontal)
    {
        var finding = Finding(Resolve(visibility, below, horizontal, NoWaiver));

        Assert.False(finding.MinimumsMet);
        Assert.False(finding.BelowCloudMinimumMet);
        Assert.False(finding.HorizontallyFromCloudMinimumMet);
        Assert.Equal(visibility, finding.FlightVisibilityStatuteMiles);
        Assert.Equal(below, finding.Cloud.FeetBelowCloud);
        Assert.Equal(horizontal, finding.Cloud.FeetHorizontallyFromCloud);
        Assert.True(finding.Cloud.NamesACloud);

        // The figures it applied are its dependencies', reached through dependsOn.
        Assert.Equal(Clearance.BelowCloudFeet, finding.Clearance.BelowCloudFeet);
        Assert.Equal(Clearance.HorizontallyFromCloudFeet, finding.Clearance.HorizontallyFromCloudFeet);
        Assert.Equal(Minimum.StatuteMiles, finding.Minimum.StatuteMiles);

        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.51(c)-(d)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.WeatherMinimumsMet.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Theory]
    [MemberData(nameof(StatedVisibilities))]
    public void Both_cloud_minimums_met_declines_RequiresInterpretation_citing_prominent_objects_107_51_c_whatever_the_stated_visibility(
        decimal visibility)
    {
        // At both printed figures exactly: "no less than" is met at the figure.
        var atTheFigures = Declined(Resolve(visibility, Clearance.BelowCloudFeet, Clearance.HorizontallyFromCloudFeet, NoWaiver));

        // And well beyond them.
        var wellClear = Declined(Resolve(visibility, 1000m, 5000m, NoWaiver));

        foreach (var unresolved in new[] { atTheFigures, wellClear })
        {
            Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
            Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
            Assert.Equal("§ 107.51(c)", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, unresolved.Locator);
            Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("\"prominent\"", unresolved.Attempted, StringComparison.Ordinal);

            // The cloud half is settled, so cloud-clearance's question is not named.
            Assert.DoesNotContain("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
        }
    }

    public static TheoryData<decimal, decimal> ExactlyOneCloudMinimumMet => new()
    {
        // At 500 feet below the cloud, just short of 2,000 feet horizontally from it.
        { 500m, 1999.99m },

        // Just short of 500 feet below, at 2,000 feet horizontally.
        { 499.99m, 2000m },

        // Directly below the cloud at the vertical minimum, and nowhere near the horizontal one.
        { 500m, 0m },

        // Level with the cloud at the horizontal minimum, and nowhere near the vertical one.
        { 0m, 2000m },
    };

    [Theory]
    [MemberData(nameof(ExactlyOneCloudMinimumMet))]
    public void Exactly_one_cloud_minimum_met_declines_and_names_cloud_clearances_open_question_too(decimal below, decimal horizontal)
    {
        var unresolved = Declined(Resolve(10m, below, horizontal, NoWaiver));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("§ 107.51(c)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, unresolved.Locator);
        Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("one of § 107.51(d)'s two minimums and not the other", unresolved.Attempted, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every situation that reaches § 107.51(c), which is every situation this entry declines on
    /// the rule rather than on the waiver gate.
    /// </summary>
    public static TheoryData<decimal, CloudStatement> EverySituationThatReaches107_51_c => new()
    {
        // Both cloud minimums met, so § 107.51(d) is satisfied on either reading and the answer
        // turns on § 107.51(c) alone.
        { 10m, CloudStatement.Measured(1000m, 5000m, Caller) },

        // Exactly one of § 107.51(d)'s two minimums met, so the cloud half is open too.
        { 10m, CloudStatement.Measured(500m, 0m, Caller) },

        // No cloud to measure either minimum from.
        { 10m, CloudStatement.NoCloud(Caller) },
    };

    /// <summary>
    /// The decline follows <c>prominent-objects</c> rather than asserting its openness. The entry
    /// asks its dependency and reports what that dependency answered on this request — its reason,
    /// its citation and its account quoted whole — while naming the entry the caller actually asked
    /// about, so the decline is this entry's own and not the dependency's handed back
    /// (<c>docs/decisions/0006</c>).
    /// </summary>
    /// <remarks>
    /// The first assertion in the loop is the one that fails if <c>prominent-objects</c> ever
    /// answers: the wording it pins is chosen at runtime from what that entry returned, so an entry
    /// that resolved would put the other wording here and no amount of restating the openness in
    /// this file would keep it green.
    /// </remarks>
    [Theory]
    [MemberData(nameof(EverySituationThatReaches107_51_c))]
    public void The_decline_follows_prominent_objects_own_answer_and_is_not_that_answer_handed_back(
        decimal visibility,
        CloudStatement cloud)
    {
        var unresolved = Declined(Resolve(visibility, cloud, NoWaiver));

        // What the dependency actually answered here, in this decline's own words.
        Assert.Contains("did not answer which objects are \"prominent\"", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("has no verdict to read", unresolved.Attempted, StringComparison.Ordinal);

        var prominence = Declined(EntryPoints.ProminentObjects.Resolve(
            new ProminentObjectsRequest { Waiver = NoWaiver }));

        // It follows: the reason and the citation are the dependency's, as it gave them here.
        Assert.Equal(prominence.Reason, unresolved.Reason);
        Assert.Equal(prominence.Locator, unresolved.Locator);

        // It carries that entry's own account forward whole, so a caller sees where the openness
        // originates rather than being told about it in this entry's paraphrase.
        Assert.Contains(prominence.Attempted, unresolved.Attempted, StringComparison.Ordinal);

        // And it is this entry's decline, not that one's handed back: the two cite § 107.51(c)
        // alike, so only what was attempted tells them apart, and it names both entries.
        Assert.NotEqual(prominence, unresolved);
        Assert.NotEqual(prominence.Attempted, unresolved.Attempted);
        Assert.Contains("weather-minimums-met", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("weather-minimums-met", prominence.Attempted, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every situation the entry answers differently when no waiver is in force — each cloud
    /// statement it can be given, and the visibility at and away from the figure — so that the gate
    /// is shown to run before any of them. The last row is the one <see cref="CloudStatement.NoCloud"/>
    /// added: the gate has to be reached before the no-cloud branch as much as before the others.
    /// </summary>
    public static TheoryData<decimal, CloudStatement> EverySituation => new()
    {
        { 10m, CloudStatement.Measured(0m, 0m, Caller) },
        { 10m, CloudStatement.Measured(500m, 1999.99m, Caller) },
        { 10m, CloudStatement.Measured(1000m, 5000m, Caller) },
        { 0m, CloudStatement.Measured(1000m, 5000m, Caller) },
        { 10m, CloudStatement.NoCloud(Caller) },
    };

    [Theory]
    [MemberData(nameof(EverySituation))]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement(
        decimal visibility,
        CloudStatement cloud)
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller);

        var unresolved = Declined(Resolve(visibility, cloud, waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
        Assert.Contains("weather-minimums-met", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);

        // The gate is the whole of the answer and the rule was never applied: a statement naming no
        // cloud gets this decline too, not the no-cloud branch's, because the gate runs first.
        Assert.DoesNotContain("has no measured distance on this operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.51", "the remote pilot in command");

        var finding = Finding(Resolve(10m, 0m, 0m, waiver));
        var unresolved = Declined(Resolve(10m, 1000m, 5000m, waiver));

        Assert.False(finding.MinimumsMet);
        Assert.Same(waiver, finding.Waiver);
        Assert.Same(waiver, finding.Minimum.Waiver);
        Assert.Same(waiver, finding.Clearance.Waiver);
        Assert.Equal("the remote pilot in command", finding.Waiver.StatedBy);
        Assert.False(finding.Waiver.InForce);
        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
    }

    [Fact]
    public void Without_a_waiver_statement_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest
            {
                FlightVisibilityStatuteMiles = 10m,
                Cloud = CloudStatement.Measured(0m, 0m, Caller),
            }));

        Assert.Equal(nameof(WeatherMinimumsMetRequest.Waiver), error.ParamName);
    }

    [Fact]
    public void Without_a_stated_figure_it_refuses_rather_than_assume_one()
    {
        var withoutVisibility = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest { Cloud = CloudStatement.Measured(0m, 0m, Caller), Waiver = NoWaiver }));
        var withoutCloud = Assert.Throws<ArgumentException>(() => EntryPoints.WeatherMinimumsMet.Resolve(
            new WeatherMinimumsMetRequest { FlightVisibilityStatuteMiles = 10m, Waiver = NoWaiver }));

        Assert.Equal(nameof(WeatherMinimumsMetRequest.FlightVisibilityStatuteMiles), withoutVisibility.ParamName);

        // And the cloud in particular: an unsupplied statement is not an operation with no cloud.
        // The engine does not read the absence of the fact as either case of it.
        Assert.Equal(nameof(WeatherMinimumsMetRequest.Cloud), withoutCloud.ParamName);
    }

    /// <summary>
    /// The case two bare distances could not express, and the one an ordinary flight is in: the
    /// caller states that the aircraft is not operated near a cloud. § 107.51(d)'s minimums are
    /// distances from a cloud and there is none to measure from, so the entry declines — it does
    /// not read the statement as both minimums broken, which is what a stated zero is, and it does
    /// not read it as § 107.51(d) met either.
    /// </summary>
    [Theory]
    [MemberData(nameof(StatedVisibilities))]
    public void Stated_that_the_aircraft_is_not_operated_near_a_cloud_it_declines_and_says_107_51_d_has_no_measured_distance(
        decimal visibility)
    {
        var noCloud = CloudStatement.NoCloud(Caller);
        var unresolved = Declined(Resolve(visibility, noCloud, NoWaiver));

        Assert.False(noCloud.NamesACloud);
        Assert.Null(noCloud.FeetBelowCloud);
        Assert.Null(noCloud.FeetHorizontallyFromCloud);

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.51(c)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, unresolved.Locator);

        // What happened, in the decline's own words: the caller's statement, that § 107.51(d) has
        // no measured distance on this operation, that the engine does not decide what the
        // paragraph requires of one, and that what is left open is prominent-objects'.
        Assert.Contains("not operated near a cloud", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("has no measured distance on this operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            "this engine does not decide what that paragraph requires of one",
            unresolved.Attempted,
            StringComparison.Ordinal);
        Assert.Contains("prominent-objects", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("\"prominent\"", unresolved.Attempted, StringComparison.Ordinal);

        // And it claims no more than that. Undecided what § 107.51(d) requires here is not
        // § 107.51(d) satisfied, so the decline does not say the outcome turns on § 107.51(c):
        // it says § 107.51(d) settles nothing here, and that § 107.51(c) would be undetermined
        // whatever § 107.51(d) came to.
        Assert.Contains("§ 107.51(d) does not settle the outcome here", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.51(c) is undetermined in any event", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("the outcome turns on", unresolved.Attempted, StringComparison.Ordinal);

        // cloud-clearance's open question is how § 107.51(d)'s two figures combine. Nothing was
        // measured for them to combine over, so it is not what blocks this answer and is not named.
        Assert.DoesNotContain("cloud-clearance", unresolved.Attempted, StringComparison.Ordinal);
    }

    /// <summary>
    /// The two statements are different statements and get different answers: no cloud declines,
    /// and a measured zero — the aircraft at the cloud — resolves the minimums not met. This is the
    /// defect the entry had, stated as a test: before, an operation in clear air had only the
    /// second to say.
    /// </summary>
    [Fact]
    public void A_measured_zero_is_not_the_same_statement_as_no_cloud_and_is_not_answered_alike()
    {
        var clearAir = Declined(Resolve(10m, CloudStatement.NoCloud(Caller), NoWaiver));
        var atTheCloud = Finding(Resolve(10m, CloudStatement.Measured(0m, 0m, Caller), NoWaiver));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, clearAir.Reason);

        Assert.False(atTheCloud.MinimumsMet);
        Assert.False(atTheCloud.BelowCloudMinimumMet);
        Assert.False(atTheCloud.HorizontallyFromCloudMinimumMet);
        Assert.Equal(0m, atTheCloud.Cloud.FeetBelowCloud);
        Assert.Equal(0m, atTheCloud.Cloud.FeetHorizontallyFromCloud);
    }

    /// <summary>
    /// A negative figure is a malformed value and is refused, not answered. The flight visibility
    /// is refused by the rule; each cloud distance is refused by <see cref="CloudStatement"/>, one
    /// step earlier, so a statement carrying a negative distance cannot be made at all.
    /// </summary>
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(10, -1, 0)]
    [InlineData(10, 0, -1)]
    public void A_negative_stated_figure_is_refused(int visibility, int below, int horizontal) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Resolve(visibility, below, horizontal, NoWaiver));

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_answering_from_defaults()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Registry.Resolve("weather-minimums-met", RuleRequest.Empty));

        Assert.Equal(nameof(WeatherMinimumsMetRequest.FlightVisibilityStatuteMiles), error.ParamName);
    }

    /// <summary>
    /// An operation the evaluator can reach this entry on, and nothing more than that: the two
    /// distances § 107.51(d) measures, both well short of their figures so the entry resolves, the
    /// stated flight visibility, and a statement that no waiver of § 107.51 is in force. Every
    /// other entry wants a fact these do not supply and says so, which is no part of what the two
    /// tests below pin.
    /// </summary>
    private static OperationFacts Operation(decimal flightVisibilityStatuteMiles) => new OperationFacts
    {
        FlightVisibilityStatuteMiles = flightVisibilityStatuteMiles,
        Cloud = CloudStatement.Measured(100m, 100m, Caller),
    }
        .Stating(WaiverStatement.NoneHeld(Weather.Regulation, Caller));

    /// <summary>This entry's outcome, as a product layer receives it.</summary>
    private static EvaluatedRequirement Outcome(decimal flightVisibilityStatuteMiles) =>
        OperationEvaluator.Evaluate(Operation(flightVisibilityStatuteMiles))
            .Requirement(MapEntries.WeatherMinimumsMet.Id);

    /// <summary>
    /// Two evaluations of one operation are the same outcome, and the finding is what was compared
    /// to say so: each evaluation builds its own <see cref="WeatherMinimumsFinding"/>, so they are
    /// two objects, and they are equal because their values are equal and not because one was
    /// reached twice. That is the property <c>AGENTS.md</c> §8 asks for — same inputs, same output
    /// — held at the level of the value rather than of the words printed about it.
    /// </summary>
    [Fact]
    public void Two_evaluations_of_one_operation_are_the_same_outcome_with_the_finding_itself_compared()
    {
        var first = Outcome(10m);
        var second = Outcome(10m);

        var firstFinding = Assert.IsType<WeatherMinimumsFinding>(first.Finding);
        var secondFinding = Assert.IsType<WeatherMinimumsFinding>(second.Finding);

        // Two objects, not one reached twice: what follows is a comparison of values.
        Assert.NotSame(firstFinding, secondFinding);
        Assert.Equal(firstFinding, secondFinding);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// The consequence of comparing the rendering rather than the finding, pinned from the side it
    /// used to be wrong on. § 107.51(c)'s stated flight visibility is recorded on the finding and
    /// is not printed by it, so two operations that differ only in that figure are two different
    /// findings the engine says identical words about. They are different outcomes, and the
    /// identical words are asserted here too — without them the test would pass on a comparison
    /// that never looked at the finding at all.
    /// </summary>
    [Fact]
    public void Two_operations_differing_only_in_the_stated_visibility_the_finding_does_not_print_are_different_outcomes()
    {
        var atFive = Outcome(5m);
        var atFour = Outcome(4m);

        var findingAtFive = Assert.IsType<WeatherMinimumsFinding>(atFive.Finding);
        var findingAtFour = Assert.IsType<WeatherMinimumsFinding>(atFour.Finding);

        // The stated figure is the one thing that differs, and the finding records it.
        Assert.Equal(5m, findingAtFive.FlightVisibilityStatuteMiles);
        Assert.Equal(4m, findingAtFour.FlightVisibilityStatuteMiles);
        Assert.Equal(findingAtFive with { FlightVisibilityStatuteMiles = 4m }, findingAtFour);

        // And the engine says exactly the same words about both, so the rendering cannot tell them
        // apart: this is what the outcomes were compared by before, and why they compared equal.
        Assert.Equal(findingAtFive.ToString(), findingAtFour.ToString());
        Assert.Equal(atFive.Explanation, atFour.Explanation);

        Assert.NotEqual(findingAtFive, findingAtFour);
        Assert.NotEqual(atFive, atFour);
    }
}
