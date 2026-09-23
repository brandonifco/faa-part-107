using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>preflight-actions</c>, § 107.49, resolved through <see cref="EntryPoints.PreflightActions"/>
/// only.
/// </summary>
/// <remarks>
/// <para>
/// The entry states the section's lead-in — the conjunction, and who owes it when — plus
/// § 107.49(f)'s condition; every obligation is its own map entry. So these pin, first, the two
/// things no constituent carries. The lead-in adds <b>no input</b>: it names one person, whom the
/// map has already read into each assertion entry's <c>assertedBy</c>, and one time. § 107.49(f)'s
/// "If the operation will be conducted over human beings under subpart D of this part" is a
/// <b>condition</b>, and an operation the caller states is not one is an operation the paragraph
/// states no obligation about — the entry it defers to is then not asked at all.
/// </para>
/// <para>
/// Then the composite's shape (#78): every obligation is asked and the answer follows what each one
/// returned, and where an unanswered obligation decides the outcome the decline emitted is this
/// entry's own, naming both entries and citing the blocking entry's locator. That citation is
/// <c>control-links-working</c>'s § 107.49(c), and the reason is its <c>RequiresInterpretation</c> —
/// so this entry's decline and that entry's agree on both, and only what was attempted tells them
/// apart. Both halves are asserted below. And the reasons stay apart: <c>subpart-d-categories</c>'
/// <c>OutsideCurrentScope</c> is recorded on § 107.49(f)'s own outcome and named in the account, and
/// never becomes this entry's reason.
/// </para>
/// <para>
/// One case cannot be reached and it is not reachable by writing a different test. "All of them
/// done" needs § 107.49(c) and the first conjunct of § 107.49(e) to answer, and
/// <c>control-links-working</c> and <c>attached-object-secure</c> resolve nothing at all: part 107
/// defines neither "working properly" nor "secure". The nearest situation is every obligation this
/// engine can answer reported done, and the answer there is a decline, pinned below. What this entry
/// must not do is invent the missing halves, and what it must not do either is assert that they are
/// missing: <see cref="PreflightActionsFinding.AllDone"/> is computed from what the entries answered
/// at runtime, so if either is ever settled this entry follows it.
/// </para>
/// </remarks>
public class PreflightActionsEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    /// <summary>
    /// What the caller asserts for the four obligations § 107.49 leaves to the remote pilot in
    /// command to report, each under its own entry's id. A null leaves that entry unasserted, so an
    /// entry that is asked for it throws instead of answering.
    /// </summary>
    private static RuleRequest Asserted(
        bool? assessed = true,
        bool? briefed = true,
        bool? powered = true,
        bool? noAdverseEffect = true,
        string assertedBy = RemotePilotInCommand)
    {
        var assertions = RuleRequest.Empty;
        assertions = Add(assertions, MapEntries.PreflightRiskAssessment, assessed, assertedBy);
        assertions = Add(assertions, MapEntries.ParticipantBriefing, briefed, assertedBy);
        assertions = Add(assertions, MapEntries.SufficientAvailablePower, powered, assertedBy);
        assertions = Add(assertions, MapEntries.AttachedObjectNoAdverseEffect, noAdverseEffect, assertedBy);
        return assertions;
    }

    private static RuleRequest Add(RuleRequest assertions, MapEntry entry, bool? holds, string assertedBy) =>
        holds is { } fact ? assertions.Assert(entry.Id, new Assertion(entry, fact, assertedBy)) : assertions;

    /// <summary>
    /// An operation outside subpart D with all four obligations reported done — the situation every
    /// obligation this engine can answer is answered done on.
    /// </summary>
    private static Resolution<object> Resolve(
        SubpartDOperation? operation = null,
        bool? assessed = true,
        bool? briefed = true,
        bool? powered = true,
        bool? noAdverseEffect = true,
        string assertedBy = RemotePilotInCommand) =>
        EntryPoints.PreflightActions.Resolve(
            new PreflightActionsRequest(Asserted(assessed, briefed, powered, noAdverseEffect, assertedBy))
            {
                Operation = operation ?? SubpartDOperation.NotOverHumanBeings,
            });

    private static PreflightActionsFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<PreflightActionsFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    private static ObligationOutcome Obligation(PreflightActionsFinding finding, string entryId) =>
        finding.Obligations.Single(obligation => obligation.Entry.Id == entryId);

    /// <summary><c>control-links-working</c>'s own answer, § 107.49(c).</summary>
    private static UnresolvedResult ParagraphCOnItsOwn() =>
        Declined(EntryPoints.ControlLinksWorking.Resolve(ControlLinksWorkingRequest.Empty));

    /// <summary><c>attached-object-secure</c>'s own answer, the first conjunct of § 107.49(e).</summary>
    private static UnresolvedResult ParagraphEOnItsOwn() =>
        Declined(EntryPoints.AttachedObjectSecure.Resolve(AttachedObjectSecureRequest.Empty));

    /// <summary><c>subpart-d-categories</c>' own answer, what § 107.49(f) defers to.</summary>
    private static UnresolvedResult ParagraphFOnItsOwn() =>
        Declined(EntryPoints.SubpartDCategories.Resolve(SubpartDCategoriesRequest.Empty));

    [Fact]
    public void The_chapeau_adds_the_conjunction_and_no_input_of_its_own_and_107_49_fs_condition_is_the_entrys_one_input()
    {
        // "Prior to flight, the remote pilot in command must:" is this entry's evidence and no
        // constituent's, and it is carried quoted rather than interpreted.
        Assert.Equal("Prior to flight, the remote pilot in command must:", Preflight.LeadIn);

        // It adds nothing for a caller to state. The one person it names is already each
        // constituent's, through the assertedBy the map read out of this very sentence, so the
        // refusal of an assertion attributed to anybody else happens on the constituent's own row;
        // and "prior to flight" is when the obligations it conjoins are owed, not a second verdict
        // about them. So the request declares exactly one input, and it is § 107.49(f)'s condition.
        Assert.Equal(
            new[] { RemotePilotInCommand },
            EntryPoints.PreflightRiskAssessment.Registered.AssertedBy,
            StringComparer.Ordinal);
        var declared = typeof(PreflightActionsRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Assertions", "EntryId", "Operation"], declared);

        // And the condition's two sides are the only two, in § 107.49(f)'s own terms.
        Assert.Equal(
            new[]
            {
                "the operation will be conducted over human beings under subpart D of this part",
                "the operation will not be conducted over human beings under subpart D of this part",
            },
            SubpartDOperation.All.Select(operation => operation.Designation),
            StringComparer.Ordinal);
    }

    [Fact]
    public void Every_obligation_this_engine_can_answer_stated_done_it_declines_RequiresInterpretation_citing_control_links_workings_107_49_c()
    {
        var unresolved = Declined(Resolve());

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal(EntryPoints.ControlLinksWorking.Registered.Locator, unresolved.Locator);
        Assert.Equal("§ 107.49(c)", unresolved.Locator.Citation);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);

        // The entry the caller asked about, and both obligations that went unanswered, are named.
        Assert.Contains("preflight-actions", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("control-links-working", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("attached-object-secure", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.49(e)", unresolved.Attempted, StringComparison.Ordinal);

        // The obligations this engine did answer are not named: they did not block anything.
        Assert.DoesNotContain("preflight-risk-assessment", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("sufficient-available-power", unresolved.Attempted, StringComparison.Ordinal);
    }

    /// <summary>
    /// One situation per obligation this engine can resolve not done: the four § 107.49 leaves to
    /// the remote pilot in command to report. § 107.49(c) and the first conjunct of § 107.49(e) have
    /// no such situation — neither resolves anything.
    /// </summary>
    public static TheoryData<string, bool, bool, bool, bool> EachObligationNotDone => new()
    {
        { "preflight-risk-assessment", false, true, true, true },
        { "participant-briefing", true, false, true, true },
        { "sufficient-available-power", true, true, false, true },
        { "attached-object-no-adverse-effect", true, true, true, false },
    };

    [Theory]
    [MemberData(nameof(EachObligationNotDone))]
    public void An_obligation_stated_not_done_settles_the_conjunction_whatever_the_undetermined_ones_would_have_said(
        string notDone,
        bool assessed,
        bool briefed,
        bool powered,
        bool noAdverseEffect)
    {
        var finding = Finding(Resolve(
            assessed: assessed, briefed: briefed, powered: powered, noAdverseEffect: noAdverseEffect));

        Assert.False(finding.AllDone);
        Assert.False(Obligation(finding, notDone).Done);
        Assert.Equal("not done", Obligation(finding, notDone).Verdict);

        // Exactly that one is not done: the others are done or, where the map holds their question
        // open, undetermined — and an undetermined one does not stop the conjunction being settled
        // by one that is not done. § 107.49(c) and the first conjunct of § 107.49(e) are
        // undetermined in every row, and the answer is still reached.
        Assert.All(
            finding.Obligations.Where(obligation => obligation.Entry.Id != notDone),
            obligation => Assert.NotEqual(false, obligation.Done));
        Assert.Null(Obligation(finding, "control-links-working").Done);
        Assert.Null(Obligation(finding, "attached-object-secure").Done);

        Assert.Equal("§ 107.49", finding.Authority.Citation);
        Assert.Equal(EntryPoints.PreflightActions.Registered.Locator, finding.Authority);
    }

    public static TheoryData<bool, bool, bool, bool, bool> EveryCombination
    {
        get
        {
            var rows = new TheoryData<bool, bool, bool, bool, bool>();
            foreach (var subpartD in new[] { false, true })
            {
                foreach (var assessed in new[] { false, true })
                {
                    foreach (var briefed in new[] { false, true })
                    {
                        foreach (var powered in new[] { false, true })
                        {
                            foreach (var noAdverseEffect in new[] { false, true })
                            {
                                rows.Add(subpartD, assessed, briefed, powered, noAdverseEffect);
                            }
                        }
                    }
                }
            }

            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(EveryCombination))]
    public void Across_every_combination_of_the_four_stated_obligations_the_engine_never_resolves_107_49_complete(
        bool subpartD,
        bool assessed,
        bool briefed,
        bool powered,
        bool noAdverseEffect)
    {
        var resolution = Resolve(
            subpartD ? SubpartDOperation.OverHumanBeings : SubpartDOperation.NotOverHumanBeings,
            assessed,
            briefed,
            powered,
            noAdverseEffect);

        // Either the conjunction is settled false by an obligation reported not done, or it is
        // undetermined. It is never settled true, and not because this entry says so: § 107.49(c)
        // and the first conjunct of § 107.49(e) are questions the published map holds open, and
        // nothing this caller can state answers them.
        if (assessed && briefed && powered && noAdverseEffect)
        {
            Assert.Equal(UnresolvedReason.RequiresInterpretation, Declined(resolution).Reason);
            return;
        }

        Assert.False(Finding(resolution).AllDone);
    }

    [Fact]
    public void An_operation_stated_not_to_be_under_subpart_D_does_not_reach_107_49_f_and_that_entry_is_not_asked()
    {
        var finding = Finding(Resolve(SubpartDOperation.NotOverHumanBeings, assessed: false));

        // Six obligations, and § 107.49(f) is not one of them: the paragraph states its obligation
        // under a condition this operation does not satisfy, so there is nothing of it to conjoin.
        Assert.Equal(6, finding.Obligations.Count);
        Assert.DoesNotContain(finding.Obligations, obligation => obligation.Paragraph == "(f)");
        Assert.DoesNotContain(
            finding.Obligations,
            obligation => obligation.Entry.Id == MapEntries.SubpartDCategories.Id);
        Assert.Same(SubpartDOperation.NotOverHumanBeings, finding.Operation);

        // Nor does a decline mention it, for the same reason.
        Assert.DoesNotContain("subpart-d-categories", Declined(Resolve()).Attempted, StringComparison.Ordinal);

        // And the same operation stated to be under subpart D does reach it, so the difference is
        // § 107.49(f)'s condition and not an engine that never asks.
        Assert.Contains(
            Finding(Resolve(SubpartDOperation.OverHumanBeings, assessed: false)).Obligations,
            obligation => obligation.Entry.Id == MapEntries.SubpartDCategories.Id);
    }

    [Fact]
    public void Where_107_49_f_is_reached_its_OutsideCurrentScope_stays_that_entrys_own_and_never_becomes_this_entrys_reason()
    {
        // The entry's note: (f) "is reachable only through subpart D and is a dependency on
        // subpart-d-categories rather than a second reason on this entry". So that reason is
        // recorded where it belongs, on (f)'s outcome and in the account of a decline, and it never
        // becomes this entry's own: a caller asking about a section this engine maps is not told the
        // section is outside its scope.
        var constituent = ParagraphFOnItsOwn();

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, constituent.Reason);
        Assert.Equal("subpart D", constituent.Locator.Citation);

        var finding = Finding(Resolve(SubpartDOperation.OverHumanBeings, assessed: false));
        var paragraphF = Obligation(finding, MapEntries.SubpartDCategories.Id);

        Assert.Equal("(f)", paragraphF.Paragraph);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, paragraphF.Reason);
        Assert.Equal(constituent.Attempted, paragraphF.Account);

        var mine = Declined(Resolve(SubpartDOperation.OverHumanBeings));

        Assert.NotEqual(UnresolvedReason.OutsideCurrentScope, mine.Reason);
        Assert.Equal(UnresolvedReason.RequiresInterpretation, mine.Reason);
        Assert.NotEqual(constituent.Locator, mine.Locator);

        // The distinction the two reasons carry survives into the account, rather than being
        // flattened into the single reason an UnresolvedResult can hold.
        Assert.Contains("subpart-d-categories", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains("subpart D", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains(nameof(UnresolvedReason.OutsideCurrentScope), mine.Attempted, StringComparison.Ordinal);
        Assert.Contains(nameof(UnresolvedReason.RequiresInterpretation), mine.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_obligations_are_in_107_49s_own_paragraph_order_which_fixes_which_constituent_a_decline_cites()
    {
        var finding = Finding(Resolve(SubpartDOperation.OverHumanBeings, assessed: false));

        Assert.Equal(
            new[] { "(a)", "(b)", "(c)", "(d)", "(e)", "(e)", "(f)" },
            Preflight.Paragraphs,
            StringComparer.Ordinal);
        Assert.Equal(
            new[] { "(a)", "(b)", "(c)", "(d)", "(e)", "(e)", "(f)" },
            finding.Obligations.Select(obligation => obligation.Paragraph),
            StringComparer.Ordinal);
        Assert.Equal(
            new[]
            {
                "preflight-risk-assessment",
                "participant-briefing",
                "control-links-working",
                "sufficient-available-power",
                "attached-object-secure",
                "attached-object-no-adverse-effect",
                "subpart-d-categories",
            },
            finding.Obligations.Select(obligation => obligation.Entry.Id),
            StringComparer.Ordinal);

        // The order decides which unanswered obligation a decline cites: § 107.49(c) comes before
        // the first conjunct of § 107.49(e), so the citation is § 107.49(c) and not § 107.49(e),
        // even though both are open on every request and both are named in the account.
        var unresolved = Declined(Resolve(SubpartDOperation.OverHumanBeings));

        Assert.Equal(EntryPoints.ControlLinksWorking.Registered.Locator, unresolved.Locator);
        Assert.NotEqual(EntryPoints.AttachedObjectSecure.Registered.Locator, unresolved.Locator);
    }

    [Fact]
    public void The_verdicts_and_the_accounts_are_the_constituent_entries_own_and_are_not_restated_here()
    {
        var finding = Finding(Resolve(SubpartDOperation.OverHumanBeings, powered: false));

        // § 107.49(d) states its obligation under a condition of its own, "If the small unmanned
        // aircraft is powered" (#95), so that entry is asked about a powered aircraft — the case in
        // which the paragraph states the obligation this section conjoins — and what it recorded is
        // the assertion this entry read.
        var power = Assert.IsType<Assertion>(Assert.IsType<SufficientAvailablePowerFinding>(
            Assert.IsType<Resolution<object>.Resolved>(
                EntryPoints.SufficientAvailablePower.Resolve(
                    new SufficientAvailablePowerRequest(Asserted(powered: false))
                    {
                        Power = AircraftPower.Powered,
                    })).Value).Availability);

        // The verdict is what that entry answered, and the account is what it printed.
        Assert.Equal(power.Holds, Obligation(finding, "sufficient-available-power").Done);
        Assert.Equal(power.ToString(), Obligation(finding, "sufficient-available-power").Account);
        Assert.Equal("§ 107.49(d)", Obligation(finding, "sufficient-available-power").Entry.Locator.Citation);

        // § 107.49(c), the first conjunct of § 107.49(e) and § 107.49(f) resolve nothing, so what is
        // recorded for each is what that entry said it could not do, with that entry's own reason.
        foreach (var (entryId, constituent) in new (string, UnresolvedResult)[]
        {
            ("control-links-working", ParagraphCOnItsOwn()),
            ("attached-object-secure", ParagraphEOnItsOwn()),
            (MapEntries.SubpartDCategories.Id, ParagraphFOnItsOwn()),
        })
        {
            Assert.Null(Obligation(finding, entryId).Done);
            Assert.Equal("undetermined", Obligation(finding, entryId).Verdict);
            Assert.Equal(constituent.Attempted, Obligation(finding, entryId).Account);
            Assert.Equal(constituent.Reason, Obligation(finding, entryId).Reason);
        }
    }

    [Fact]
    public void The_decline_is_this_entrys_own_and_not_control_links_workings_handed_back()
    {
        var mine = Declined(Resolve());
        var constituent = ParagraphCOnItsOwn();

        // Not the same result, and not the same subject: the caller asked about § 107.49 and is
        // told so, by name.
        Assert.NotEqual(constituent, mine);
        Assert.NotEqual(constituent.Attempted, mine.Attempted);
        Assert.Contains("preflight-actions", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("preflight-actions", constituent.Attempted, StringComparison.Ordinal);

        // And the blind spot that makes Attempted the thing that carries it: the reason and the
        // citation are deliberately the constituent's, because the question that blocks the answer
        // is the constituent's question, so neither can tell the two declines apart
        // (docs/decisions/0001).
        Assert.Equal(constituent.Reason, mine.Reason);
        Assert.Equal(constituent.Locator, mine.Locator);
    }

    [Fact]
    public void An_obligation_the_caller_did_not_assert_is_demanded_rather_than_inferred() =>
        Assert.All(
            new Func<Resolution<object>>[]
            {
                () => Resolve(assessed: null),
                () => Resolve(briefed: null),
                () => Resolve(powered: null),
                () => Resolve(noAdverseEffect: null),
            },
            unasserted => Assert.Throws<AssertionRequiredException>(() => unasserted()));

    [Fact]
    public void Without_a_statement_about_107_49_fs_condition_it_refuses_rather_than_infer_one()
    {
        // Whether a paragraph of the corpus reaches the operation is not something this engine
        // decides from an absence.
        Assert.Equal(
            nameof(PreflightActionsRequest.Operation),
            Assert.Throws<ArgumentException>(() =>
                EntryPoints.PreflightActions.Resolve(new PreflightActionsRequest(Asserted()))).ParamName);

        // The dictionary dispatch builds a request with every input at its default, and refuses the
        // same way rather than answering from one.
        Assert.Equal(
            nameof(PreflightActionsRequest.Operation),
            Assert.Throws<ArgumentException>(() =>
                Registry.Resolve("preflight-actions", Asserted())).ParamName);
    }

    [Fact]
    public void Each_obligation_is_read_under_its_own_entry_and_an_assertion_about_another_or_by_another_asserter_is_refused()
    {
        // An assertion attributed to somebody § 107.49's lead-in does not name is refused, on the
        // constituent's own row — which is where the lead-in's one person lives.
        Assert.Contains(
            "visual observer",
            Assert.Throws<ArgumentException>(() => Resolve(assertedBy: "visual observer")).Message,
            StringComparison.Ordinal);

        // And an assertion about another entry does not answer this one's obligation: each of the
        // four is read under its own id, so a caller who reports the power under § 107.49(a)'s id
        // has not reported the assessment.
        var crossed = RuleRequest.Empty
            .Assert(
                MapEntries.PreflightRiskAssessment.Id,
                new Assertion(MapEntries.SufficientAvailablePower, true, RemotePilotInCommand));

        Assert.Contains(
            "preflight-risk-assessment",
            Assert.Throws<ArgumentException>(() => EntryPoints.PreflightActions.Resolve(
                new PreflightActionsRequest(crossed) { Operation = SubpartDOperation.NotOverHumanBeings })).Message,
            StringComparison.Ordinal);
    }
}
