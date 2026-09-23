using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>civil-twilight-operation</c>, § 107.29(b)-(c), resolved through
/// <see cref="EntryPoints.CivilTwilightOperation"/> only: the situations the entry's note asks for
/// — "Inside and outside each twilight window, with and without lighting" — and the place
/// § 107.29(c) makes its definition turn on.
/// </summary>
/// <remarks>
/// <para>
/// The entry is a composite over three, and what it states of its own is § 107.29(c)'s chapeau:
/// which of the three definitions of civil twilight applies, and therefore which operations
/// § 107.29(b) reaches. So the periods are never restated here and neither is the lighting
/// requirement: the tests below assert that this entry's answer <em>is</em> what
/// <c>civil-twilight-window</c> and <c>anti-collision-lighting</c> returned, asked at runtime.
/// </para>
/// <para>
/// <b>Three unresolved reasons, kept apart.</b> <c>civil-twilight-alaska</c> declines
/// <see cref="UnresolvedReason.MissingRulesData"/> — the corpus is clear and the text that would
/// settle it is in the Air Almanac, which this map has not admitted, which is a different thing
/// from an open question. The § 107.205 gate declines
/// <see cref="UnresolvedReason.OutsideCurrentScope"/>. And
/// <see cref="UnresolvedReason.RequiresInterpretation"/>, the reason a composite that flattened its
/// constituents' answers into "open" would give, is one this entry never gives at all while a
/// constituent gave it a reason. All three are pinned below.
/// </para>
/// <para>
/// One case is not reachable and cannot be made so by writing a different test, for the reason
/// <see cref="Lights"/> records: this entry's waiver gate and <c>anti-collision-lighting</c>'s are
/// the same gate on the same statement, § 107.205(b)'s single row, read here first — so once it has
/// passed, that constituent cannot decline. The decline this entry would emit for it is written and
/// documented in <see cref="Twilight"/>, carrying that entry's own reason; what is pinned below
/// instead is that the reason and the locator of every decline this entry does emit are the
/// blocking entry's, read from what it returned.
/// </para>
/// </remarks>
public class CivilTwilightOperationEntryPointTests
{
    private const string Caller = nameof(CivilTwilightOperationEntryPointTests);

    /// <summary>§ 107.205(b): the one statement this entry and the lighting entries are all asked under.</summary>
    private const string Regulation = "§ 107.29(a)(2) and (b)";

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld(Regulation, Caller);

    /// <summary>Lighting that meets § 107.29(b)'s unless-clause, on the caller's statement and the asserted flash rate.</summary>
    private static readonly LightingStatement Lit = LightingStatement.LightedAndVisibleFor(5m, Caller);

    /// <summary>An aircraft with no anti-collision lighting: the unless-clause is not met, and no flash rate is asked for.</summary>
    private static readonly LightingStatement Unlit = LightingStatement.NoneFitted(Caller);

    /// <summary>
    /// What the caller asserts for <c>flash-rate-sufficient</c>, which <c>anti-collision-lighting</c>
    /// asks for lighted lighting. A null asserts nothing, so an entry that is asked for it throws
    /// instead of answering.
    /// </summary>
    private static RuleRequest Asserting(bool? rate) =>
        rate is { } holds
            ? RuleRequest.Empty.Assert(
                MapEntries.FlashRateSufficient.Id,
                new Assertion(MapEntries.FlashRateSufficient, holds, "the operator"))
            : RuleRequest.Empty;

    private static Resolution<object> Resolve(
        OperationPeriod? period = null,
        LightingStatement? lighting = null,
        OperationPlace? place = null,
        WaiverStatement? waiver = null,
        bool? rate = true) =>
        EntryPoints.CivilTwilightOperation.Resolve(new CivilTwilightOperationRequest(Asserting(rate))
        {
            Place = place ?? OperationPlace.OutsideAlaska,
            Period = period ?? OperationPeriod.BeforeOfficialSunrise,
            Lighting = lighting ?? Lit,
            Waiver = waiver ?? NoWaiver,
        });

    /// <summary>A request with every input set but the ones passed false.</summary>
    private static CivilTwilightOperationRequest Incomplete(
        bool place = true,
        bool period = true,
        bool lighting = true,
        bool waiver = true) =>
        new(Asserting(true))
        {
            Place = place ? OperationPlace.OutsideAlaska : null,
            Period = period ? OperationPeriod.BeforeOfficialSunrise : null,
            Lighting = lighting ? Lit : null,
            Waiver = waiver ? NoWaiver : null,
        };

    private static CivilTwilightOperationFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<CivilTwilightOperationFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    /// <summary>The period § 107.29(c) states as item <paramref name="item"/>, as the caller states it.</summary>
    private static OperationPeriod Period(string item) =>
        OperationPeriod.All.Single(period => period.Item == item);

    /// <summary>What <c>civil-twilight-window</c> answers, asked as this entry asks it.</summary>
    private static CivilTwilightWindows Windows() =>
        Assert.IsType<CivilTwilightWindows>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.CivilTwilightWindow.Resolve(new CivilTwilightWindowRequest())).Value);

    /// <summary>What <c>civil-twilight-alaska</c> answers, asked as this entry asks it.</summary>
    private static UnresolvedResult Alaska() =>
        Declined(EntryPoints.CivilTwilightAlaska.Resolve(CivilTwilightAlaskaRequest.Empty));

    /// <summary>
    /// Inside each of § 107.29(c)'s two windows, with and without the lighting — the four
    /// situations the entry's note asks for on the inside.
    /// </summary>
    public static TheoryData<string, bool, bool> InsideEachWindow => new()
    {
        { "(c)(1)", true, true },
        { "(c)(1)", false, false },
        { "(c)(2)", true, true },
        { "(c)(2)", false, false },
    };

    [Theory]
    [MemberData(nameof(InsideEachWindow))]
    public void Inside_each_twilight_window_107_29_b_permits_the_operation_only_with_the_anti_collision_lighting_it_requires(
        string item,
        bool lighted,
        bool permitted)
    {
        var finding = Finding(Resolve(Period(item), lighted ? Lit : Unlit, rate: lighted ? true : null));

        // "No person may operate … during periods of civil twilight unless the small unmanned
        // aircraft has lighted anti-collision lighting …": the paragraph reaches the operation, and
        // the unless-clause decides it.
        Assert.True(finding.DuringCivilTwilight);
        Assert.Equal(permitted, finding.Permitted);
        Assert.Equal(permitted, Assert.IsType<AntiCollisionLightingFinding>(finding.Lighting).Met);

        // The period is the one § 107.29(c) states as this item, as civil-twilight-window prints it.
        Assert.Equal(item == "(c)(1)" ? Windows().BeforeSunrise : Windows().AfterSunset, finding.During);

        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.29(b)-(c)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.CivilTwilightOperation.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
        Assert.Contains(
            permitted ? "§ 107.29(b) permits the operation" : "§ 107.29(b) prohibits the operation",
            finding.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Outside_both_windows_107_29_b_states_no_prohibition_and_the_lighting_entry_is_not_asked()
    {
        // Nothing is asserted at all. If anti-collision-lighting were asked, flash-rate-sufficient
        // would demand its value and this would throw rather than answer — so resolving is itself
        // the evidence that the stated period stopped the paragraph before it.
        var finding = Finding(Resolve(OperationPeriod.NeitherPeriod, rate: null));

        Assert.False(finding.DuringCivilTwilight);
        Assert.Null(finding.During);
        Assert.Null(finding.Lighting);

        // The third answer, and it is neither of the other two: not permitted, and not prohibited.
        Assert.Null(finding.Permitted);
        Assert.Contains(
            "during neither period § 107.29(c) states, so § 107.29(b) states no prohibition about this operation",
            finding.ToString(),
            StringComparison.Ordinal);

        // Outside the windows, with and without lighting: the same answer, because the paragraph
        // says nothing about the operation either way.
        Assert.Equal(finding, Finding(Resolve(OperationPeriod.NeitherPeriod, Unlit, rate: null)));

        // And the same request inside a window does reach that entry, so the difference above is
        // the period § 107.29(b) states its prohibition of, not an engine that never asks.
        Assert.Throws<AssertionRequiredException>(() => Resolve(Period("(c)(2)"), rate: null));
    }

    [Fact]
    public void The_periods_are_civil_twilight_windows_own_and_no_figure_of_them_is_restated_here()
    {
        var windows = Windows();

        // The very periods that entry resolved, not periods this entry states: both 30-minute
        // figures and both official events stay where § 107.29(c)(1)-(2) prints them.
        Assert.Equal(windows.BeforeSunrise, Finding(Resolve(Period("(c)(1)"))).During);
        Assert.Equal(windows.AfterSunset, Finding(Resolve(Period("(c)(2)"))).During);

        Assert.Contains(
            "a period of time that begins 30 minutes before official sunrise and ends at official sunrise",
            Finding(Resolve(Period("(c)(1)"))).ToString(),
            StringComparison.Ordinal);
        Assert.Contains(
            "a period of time that begins at official sunset and ends 30 minutes after official sunset",
            Finding(Resolve(Period("(c)(2)"))).ToString(),
            StringComparison.Ordinal);

        // What the caller states is which item's period the operation was during, and it carries
        // none of the figures: the engine holds no clock and computes no official sunrise.
        Assert.Equal("during the period § 107.29(c)(1) states", Period("(c)(1)").Designation);
        Assert.Equal("during the period § 107.29(c)(2) states", Period("(c)(2)").Designation);
    }

    [Fact]
    public void The_places_and_the_periods_107_29_c_names_are_closed_sets_positively_stated_and_neither_is_a_default()
    {
        // "Except for Alaska" twice and "In Alaska" once: a condition with two sides and no middle,
        // in § 107.29(c)'s own terms.
        Assert.Equal(
            new[] { "the operation is outside Alaska", "the operation is in Alaska" },
            OperationPlace.All.Select(place => place.Designation),
            StringComparer.Ordinal);

        // The place this entry divides on is the place the definition entry excepts, in the
        // corpus's own word.
        Assert.Equal("Alaska", Windows().ExceptFor);
        Assert.Contains(Windows().ExceptFor, OperationPlace.InAlaska.Designation, StringComparison.Ordinal);

        // The two periods § 107.29(c) states, then the case neither reaches — stated, not inferred
        // from silence.
        Assert.Equal(
            new[]
            {
                "during the period § 107.29(c)(1) states",
                "during the period § 107.29(c)(2) states",
                "during neither period § 107.29(c) states",
            },
            OperationPeriod.All.Select(period => period.Designation),
            StringComparer.Ordinal);
        Assert.Equal(
            new[] { "(c)(1)", "(c)(2)", null },
            OperationPeriod.All.Select(period => period.Item),
            StringComparer.Ordinal);

        // And neither is what an unstated operation means. Whether a paragraph of the corpus
        // reaches an operation is not something this engine decides from an absence.
        Assert.Equal(
            nameof(CivilTwilightOperationRequest.Place),
            Assert.Throws<ArgumentException>(() =>
                EntryPoints.CivilTwilightOperation.Resolve(Incomplete(place: false))).ParamName);
        Assert.Equal(
            nameof(CivilTwilightOperationRequest.Period),
            Assert.Throws<ArgumentException>(() =>
                EntryPoints.CivilTwilightOperation.Resolve(Incomplete(period: false))).ParamName);
    }

    [Fact]
    public void In_Alaska_the_definition_is_civil_twilight_alaskas_and_this_entry_declines_MissingRulesData_citing_107_29_c_3()
    {
        var constituent = Alaska();
        var mine = Declined(Resolve(place: OperationPlace.InAlaska, rate: null));

        // The reason and the locator are that entry's, read from what it returned rather than
        // fixed here: the Air Almanac is not an admitted source, so the corpus does not supply the
        // fact — which is not an open question.
        Assert.Equal(UnresolvedReason.MissingRulesData, mine.Reason);
        Assert.Equal(constituent.Reason, mine.Reason);
        Assert.Equal(constituent.Locator, mine.Locator);
        Assert.Equal("§ 107.29(c)(3)", mine.Locator.Citation);
        Assert.Equal(EntryPoints.CivilTwilightAlaska.Registered.Locator, mine.Locator);

        // This entry's own decline, not that entry's handed back: a caller who asked whether
        // § 107.29(b) permits an operation is told so by name.
        Assert.NotEqual(constituent, mine);
        Assert.NotEqual(constituent.Attempted, mine.Attempted);
        Assert.Contains("civil-twilight-operation", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains("civil-twilight-alaska", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains("the operation is in Alaska", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("civil-twilight-operation", constituent.Attempted, StringComparison.Ordinal);

        // It is the place that decides it, and nothing else: § 107.29(c)(1) and (c)(2) say "Except
        // for Alaska", so no stated period of theirs answers an Alaskan operation, and the lighting
        // entry is never reached — nothing is asserted in any of these.
        Assert.All(
            new[]
            {
                Resolve(Period("(c)(1)"), Lit, OperationPlace.InAlaska, rate: null),
                Resolve(Period("(c)(2)"), Unlit, OperationPlace.InAlaska, rate: null),
                Resolve(OperationPeriod.NeitherPeriod, Lit, OperationPlace.InAlaska, rate: null),
            },
            resolution => Assert.Equal(UnresolvedReason.MissingRulesData, Declined(resolution).Reason));
    }

    [Fact]
    public void The_reasons_the_constituents_give_are_kept_apart_and_none_is_substituted_for_another()
    {
        var alaska = Declined(Resolve(place: OperationPlace.InAlaska, rate: null));
        var waived = Declined(Resolve(waiver: WaiverStatement.Held(Regulation, Caller), rate: null));

        // Two declines, two different reasons, two different citations — and a caller does
        // different things about them: one says the corpus does not supply the fact, the other says
        // the rule is out of reach while a waiver is in force.
        Assert.Equal(UnresolvedReason.MissingRulesData, alaska.Reason);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, waived.Reason);
        Assert.NotEqual(alaska.Reason, waived.Reason);
        Assert.NotEqual(alaska.Locator, waived.Locator);

        // Neither is the reason a composite that flattened its constituents' answers into "open"
        // would give, and each is the reason the thing that actually blocked gave.
        Assert.NotEqual(UnresolvedReason.RequiresInterpretation, alaska.Reason);
        Assert.NotEqual(UnresolvedReason.RequiresInterpretation, waived.Reason);
        Assert.Equal(Alaska().Reason, alaska.Reason);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, waived.Locator);
        Assert.Equal(EntryPoints.CivilTwilightAlaska.Registered.Locator, alaska.Locator);
    }

    [Fact]
    public void While_a_waiver_of_107_29_a_2_and_b_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_naming_this_entry()
    {
        var waiver = WaiverStatement.Held(Regulation, Caller);

        // Nothing is asserted: the gate is read before any constituent is asked, so no assertion is
        // demanded for an operation the caller has said is out of reach.
        var unresolved = Declined(Resolve(waiver: waiver, rate: null));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);

        // This entry's own gate. § 107.205(b) lists "Section 107.29(a)(2) and (b)", the one row of
        // § 107.205 that reaches § 107.29, so the same statement would suspend the lighting entry a
        // moment later, and only the entry named tells the two declines apart.
        Assert.Contains("civil-twilight-operation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.29(b)-(c)]", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("anti-collision-lighting", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);

        // The gate runs before § 107.29(c)'s definition: a waived Alaskan operation is answered by
        // the gate and not by the Air Almanac's absence.
        var alaska = Declined(Resolve(place: OperationPlace.InAlaska, waiver: waiver, rate: null));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, alaska.Reason);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, alaska.Locator);

        // And before the stated period: an operation outside both windows is not answered as one
        // the paragraph says nothing about. Nothing else would decline it — civil-twilight-window
        // has no gate at all, and the lighting entry is not asked for such an operation — so this
        // entry's own gate is the only thing between a waived operation and a resolved finding.
        var outside = Declined(Resolve(OperationPeriod.NeitherPeriod, waiver: waiver, rate: null));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, outside.Reason);
        Assert.Contains("civil-twilight-operation", outside.Attempted, StringComparison.Ordinal);

        // What the gate does not precede is the caller's typed inputs. They are demanded by the
        // handler as the arguments of the call, so a waived operation the caller has not fully
        // described is refused for the missing input rather than declined for the waiver — the
        // ordering operating-limitations, visual-observer-conditions and anti-collision-lighting
        // take, and not the one reasonable-protection takes, where the rule demands its input after
        // the gate. Nothing in this repository records which is right; this pins which one is here.
        Assert.Equal(
            nameof(CivilTwilightOperationRequest.Place),
            Assert.Throws<ArgumentException>(() =>
                EntryPoints.CivilTwilightOperation.Resolve(new CivilTwilightOperationRequest(Asserting(null))
                {
                    Period = OperationPeriod.NeitherPeriod,
                    Lighting = Lit,
                    Waiver = waiver,
                })).ParamName);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        // The statement must be about the regulation § 107.205 lists, and the refusal names this
        // entry rather than a constituent.
        Assert.Contains(
            "civil-twilight-operation",
            Assert.Throws<ArgumentException>(() =>
                Resolve(waiver: WaiverStatement.NoneHeld("§ 107.51", Caller))).Message,
            StringComparison.Ordinal);

        // § 107.205(b) designates two paragraphs, not the section: a statement about § 107.29 whole
        // would cover (c) and (d), which the list does not carry.
        Assert.Throws<ArgumentException>(() => Resolve(waiver: WaiverStatement.NoneHeld("§ 107.29", Caller)));

        // And it is the same designation, from the same row, that the lighting entries are asked
        // under: one statement reaches all four entries.
        Assert.Equal(Regulation, Twilight.Regulation);
        Assert.Equal(Lights.Regulation, Twilight.Regulation);
    }

    [Fact]
    public void The_lighting_requirement_is_anti_collision_lightings_answer_and_is_not_recomputed_here()
    {
        var constituent = Assert.IsType<AntiCollisionLightingFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.AntiCollisionLighting.Resolve(new AntiCollisionLightingRequest(Asserting(true))
            {
                Lighting = Lit,
                Waiver = NoWaiver,
            })).Value);
        var mine = Finding(Resolve(Period("(c)(1)"), Lit));

        // The finding this entry reports is that entry's own, and the verdict is read off it.
        Assert.Equal(constituent, mine.Lighting);
        Assert.Equal(constituent.Met, mine.Permitted);

        // Its own constituents decide it too: a flash rate the caller asserts insufficient makes
        // the unless-clause unmet, and this entry follows without knowing what a flash rate is.
        var insufficient = Finding(Resolve(Period("(c)(1)"), Lit, rate: false));

        Assert.False(insufficient.Permitted);
        Assert.False(Assert.IsType<AntiCollisionLightingFinding>(insufficient.Lighting).FlashRateSufficient);

        // And the 3 statute miles are that entry's printed figure, applied there and not here.
        var tooDim = Finding(Resolve(Period("(c)(1)"), LightingStatement.LightedAndVisibleFor(2m, Caller)));

        Assert.False(tooDim.Permitted);
        Assert.Equal(false, Assert.IsType<AntiCollisionLightingFinding>(tooDim.Lighting).VisibleFarEnough);
    }

    [Fact]
    public void Paragraph_c_selects_among_three_definitions_and_that_selection_is_this_entrys_own()
    {
        // The sentence that makes the three items a definition of the term paragraph (b) turns on
        // is in this entry's evidence and in no other's: civil-twilight-window quotes items (1) and
        // (2), civil-twilight-alaska quotes item (3), and no entry in the map carries § 107.29(c)
        // whole.
        Assert.Equal(
            "(c) For purposes of paragraph (b) of this section, civil twilight refers to the following:",
            Twilight.ParagraphC);
        Assert.DoesNotContain(
            Registry.Entries,
            entry => entry.Locators.Any(locator => locator.Citation == "§ 107.29(c)"));
        Assert.Equal("§ 107.29(c)(1)-(2)", EntryPoints.CivilTwilightWindow.Registered.Locator.Citation);
        Assert.Equal("§ 107.29(c)(3)", EntryPoints.CivilTwilightAlaska.Registered.Locator.Citation);
        Assert.Equal("§ 107.29(b)-(c)", EntryPoints.CivilTwilightOperation.Registered.Locator.Citation);

        // And the selection is the behaviour: the same operation, stated in the two places, is
        // answered by two different entries — one resolves from (c)(1)-(2)'s periods, the other
        // declines on (c)(3), and neither answers for the other's place.
        var outside = Finding(Resolve(Period("(c)(1)"), Lit, OperationPlace.OutsideAlaska));
        var inside = Declined(Resolve(Period("(c)(1)"), Lit, OperationPlace.InAlaska, rate: null));

        Assert.True(outside.DuringCivilTwilight);
        Assert.Equal(Windows().BeforeSunrise, outside.During);
        Assert.Equal(EntryPoints.CivilTwilightAlaska.Registered.Locator, inside.Locator);
    }

    [Fact]
    public void Without_the_facts_the_paragraph_turns_on_it_refuses_rather_than_assume_them_including_through_the_dictionary_dispatch()
    {
        Assert.All(
            new (string Name, CivilTwilightOperationRequest Request)[]
            {
                (nameof(CivilTwilightOperationRequest.Place), Incomplete(place: false)),
                (nameof(CivilTwilightOperationRequest.Period), Incomplete(period: false)),
                (nameof(CivilTwilightOperationRequest.Lighting), Incomplete(lighting: false)),
                (nameof(CivilTwilightOperationRequest.Waiver), Incomplete(waiver: false)),
            },
            missing => Assert.Equal(
                missing.Name,
                Assert.Throws<ArgumentException>(
                    () => EntryPoints.CivilTwilightOperation.Resolve(missing.Request)).ParamName));

        // The dictionary dispatch carries no typed input at all, so nothing but those refusals
        // stands between it and an answer about an operation nobody described.
        Assert.Equal(
            nameof(CivilTwilightOperationRequest.Place),
            Assert.Throws<ArgumentException>(() =>
                Registry.Resolve("civil-twilight-operation", RuleRequest.Empty)).ParamName);
    }
}
