using System.Reflection;
using FaaPart107.Evaluation;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>over-human-beings</c>, § 107.39, resolved through <see cref="EntryPoints.OverHumanBeings"/>
/// only: the section's opening, which is this entry's alone, and each of the three cases its
/// "unless—" excepts, answered by the entry the map gives it.
/// </summary>
/// <remarks>
/// <para>
/// The entry states no case of its own. It carries the prohibition and the disjunction, and asks
/// <c>direct-participation</c> (§ 107.39(a)), <c>reasonable-protection</c> (§ 107.39(b)) and
/// <c>subpart-d-categories</c> (subpart D). So no verdict of a constituent is written here as one
/// expected to come from this entry: where a constituent's account is checked it is resolved
/// through that constituent's own entry point and compared.
/// </para>
/// <para>
/// One half of § 107.39(b) <em>is</em> this entry's, and it is the crux these tests spend most of
/// their assertions on. The paragraph's relative clause binds where the human being is located to
/// the place that can provide reasonable protection; <c>reasonable-protection</c> records which
/// place the standard was asserted over, and this entry owns which place the human being is
/// actually located under. An assertion about the other place is not an answer here, and the tests
/// check that in both directions.
/// </para>
/// <para>
/// One of the note's cases cannot resolve, and it is not reachable by writing a different test.
/// "§ 107.39 prohibits the operation" needs every one of the three cases answered and none met, and
/// two of them answer nothing at all: <c>direct-participation</c> declines over an undefined term,
/// and <c>subpart-d-categories</c> is <c>scope: out</c>. So the nearest situation this engine can be
/// put in is a plain overflight with none of them, and the answer there is a decline, pinned below.
/// <see cref="OverHumanBeingsFinding.MayOperate"/> computes the missing case from what the
/// constituents answered at runtime rather than asserting it away, so if either entry ever answers,
/// this one follows it.
/// </para>
/// </remarks>
public class OverHumanBeingsEntryPointTests
{
    private const string Caller = nameof(OverHumanBeingsEntryPointTests);

    private static readonly MapEntry Entry = MapEntries.OverHumanBeings;

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.39", Caller);

    private static OverHumanBeingsRequest Request(
        HumanBeingLocation? location,
        Shelter? shelter,
        WaiverStatement? waiver,
        bool? protectionHolds) =>
        new(protectionHolds is { } holds
            ? RuleRequest.Empty.Assert(
                MapEntries.ReasonableProtection.Id,
                new Assertion(MapEntries.ReasonableProtection, holds, "the operator"))
            : RuleRequest.Empty)
        {
            Location = location,
            Shelter = shelter,
            Waiver = waiver,
        };

    private static Resolution<object> Resolve(
        HumanBeingLocation? location = null,
        Shelter? shelter = null,
        WaiverStatement? waiver = null,
        bool? protectionHolds = null) =>
        EntryPoints.OverHumanBeings.Resolve(
            Request(location ?? HumanBeingLocation.NeitherOfThem, shelter, waiver ?? NoWaiver, protectionHolds));

    private static OverHumanBeingsFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<OverHumanBeingsFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    private static ExceptedCaseOutcome Case(OverHumanBeingsFinding finding, string paragraph) =>
        Assert.Single(finding.ExceptedCases, excepted => excepted.Paragraph == paragraph);

    /// <summary><c>direct-participation</c>'s own answer, § 107.39(a), with no waiver in force.</summary>
    private static UnresolvedResult ParagraphAOnItsOwn() =>
        Declined(EntryPoints.DirectParticipation.Resolve(new DirectParticipationRequest { Waiver = NoWaiver }));

    /// <summary><c>subpart-d-categories</c>' own answer, the case § 107.39(c) defers to.</summary>
    private static UnresolvedResult ParagraphCOnItsOwn() =>
        Declined(EntryPoints.SubpartDCategories.Resolve(SubpartDCategoriesRequest.Empty));

    /// <summary><c>reasonable-protection</c>'s own answer about one place, with no waiver in force.</summary>
    private static ReasonableProtectionFinding ParagraphBOnItsOwn(Shelter shelter, bool holds) =>
        Assert.IsType<ReasonableProtectionFinding>(
            Assert.IsType<Resolution<object>.Resolved>(
                EntryPoints.ReasonableProtection.Resolve(
                    new ReasonableProtectionRequest(
                        RuleRequest.Empty.Assert(
                            MapEntries.ReasonableProtection.Id,
                            new Assertion(MapEntries.ReasonableProtection, holds, "the operator")))
                    {
                        Shelter = shelter,
                        Waiver = NoWaiver,
                    })).Value);

    [Fact]
    public void The_covered_structure_limb_resolves_that_107_39_does_not_prohibit_it_when_the_standard_is_asserted_for_the_place_the_human_being_is_located()
    {
        // The human being is under the carport, and the standard is asserted for that carport. Both
        // halves of § 107.39(b) are answered about the same place, so the case the paragraph excepts
        // is met — and "unless" makes one case enough, whatever (a) and (c) would have said.
        var finding = Finding(Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure, protectionHolds: true));

        Assert.True(finding.MayOperate);
        Assert.Same(HumanBeingLocation.UnderACoveredStructure, finding.Location);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.39", finding.Authority.Citation);
        Assert.Equal(EntryPoints.OverHumanBeings.Registered.Locator, finding.Authority);

        Assert.True(Case(finding, "(b)").Met);
        Assert.Equal(MapEntries.ReasonableProtection.Id, Case(finding, "(b)").Entry.Id);

        // The other two did not answer, and the disjunction was settled without them.
        Assert.Null(Case(finding, "(a)").Met);
        Assert.Null(Case(finding, "(c)").Met);
        Assert.Contains("does not prohibit", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_stationary_vehicle_limb_answers_on_its_own_account_and_neither_place_is_privileged()
    {
        // § 107.39(b) prints two places and the map distributes the standard over both, so each
        // answers on its own: neither is the one the engine reaches for, and neither is a default.
        foreach (var location in new[] { HumanBeingLocation.UnderACoveredStructure, HumanBeingLocation.InsideAStationaryVehicle })
        {
            var place = Assert.IsType<Shelter>(location.Place);

            var met = Finding(Resolve(location, place, protectionHolds: true));

            Assert.True(met.MayOperate);
            Assert.True(Case(met, "(b)").Met);
            Assert.Same(location, met.Location);
            Assert.Contains(location.Designation, met.ToString(), StringComparison.Ordinal);

            // Withheld for this place: the engine demands the value rather than reaching for the
            // other place's, and rather than declining.
            Assert.Equal(
                MapEntries.ReasonableProtection.Id,
                Assert.Throws<AssertionRequiredException>(() => Resolve(location, place)).EntryId);
        }
    }

    [Fact]
    public void A_standard_asserted_about_the_other_place_is_not_an_answer_about_the_place_the_human_being_is_located()
    {
        // The crux. § 107.39(b) requires the human being to be located under or inside *the very
        // thing* that can provide reasonable protection. A caller with a stationary vehicle on site
        // asserts the standard for it, while the human being is under a carport nobody has said
        // anything about. That assertion is a good answer to reasonable-protection's question and no
        // answer at all to this entry's, so the engine refuses rather than reading one as the other.
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.StationaryVehicle, protectionHolds: true));

        Assert.Equal(nameof(OverHumanBeingsRequest.Shelter), error.ParamName);
        Assert.Contains("over-human-beings", error.Message, StringComparison.Ordinal);
        Assert.Contains("reasonable-protection", error.Message, StringComparison.Ordinal);
        Assert.Contains(HumanBeingLocation.UnderACoveredStructure.Designation, error.Message, StringComparison.Ordinal);
        Assert.Contains(Shelter.StationaryVehicle.Designation, error.Message, StringComparison.Ordinal);

        // And the same the other way round, so neither place is privileged.
        Assert.Contains(
            HumanBeingLocation.InsideAStationaryVehicle.Designation,
            Assert.Throws<ArgumentException>(() => Resolve(
                HumanBeingLocation.InsideAStationaryVehicle, Shelter.CoveredStructure, protectionHolds: true))
                .Message,
            StringComparison.Ordinal);

        // The refusal is this entry's own check on the two places, not a defect in the constituent:
        // asked its own question, reasonable-protection answers that same assertion happily, about
        // the place it was made about.
        var constituent = ParagraphBOnItsOwn(Shelter.StationaryVehicle, holds: true);

        Assert.True(constituent.Holds);
        Assert.Same(Shelter.StationaryVehicle, constituent.Shelter);

        // Nor is it a refusal of a "no": a negative assertion about the other place is refused too,
        // because what is missing is an answer about the place the human being is.
        Assert.Equal(
            nameof(OverHumanBeingsRequest.Shelter),
            Assert.Throws<ArgumentException>(() => Resolve(
                HumanBeingLocation.UnderACoveredStructure, Shelter.StationaryVehicle, protectionHolds: false))
                .ParamName);
    }

    [Fact]
    public void The_standard_asserted_not_to_be_met_for_that_place_leaves_paragraph_b_not_met_and_the_entry_declines()
    {
        // The asserter reports that the carport the human being is under cannot provide reasonable
        // protection. That is an answer, not a gap: § 107.39(b)'s case is *not met*. It does not
        // settle the section, because (a) and (c) are still unanswered and any one of the three is
        // enough — so the entry declines, and (b) is not among the entries it names.
        var unresolved = Declined(Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure, protectionHolds: false));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Contains("over-human-beings", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("direct-participation", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("subpart-d-categories", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("reasonable-protection", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void A_plain_overflight_with_none_of_them_is_a_case_107_39_b_does_not_reach_and_the_standard_is_not_asked()
    {
        // The entry's note keeps this case: "a plain overflight with none of them". The human being
        // is under neither of § 107.39(b)'s two places, so the paragraph's first conjunct is false
        // and the case is not met whatever anybody asserts about a structure or a vehicle elsewhere.
        // The standard is therefore not asked at all, and a caller who has no assertion to make gets
        // an answer rather than a demand — which is the one input that tells this reading from one
        // that asks (b) regardless.
        var unresolved = Declined(EntryPoints.OverHumanBeings.Resolve(
            new OverHumanBeingsRequest { Location = HumanBeingLocation.NeitherOfThem, Waiver = NoWaiver }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Contains(HumanBeingLocation.NeitherOfThem.Designation, unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("reasonable-protection", unresolved.Attempted, StringComparison.Ordinal);

        // No place the assertion is about need be named either: there is no assertion to place.
        Assert.Equal(
            unresolved.Attempted,
            Declined(Resolve(HumanBeingLocation.NeitherOfThem, Shelter.CoveredStructure)).Attempted);

        // Contrast: with the human being under one of the two, the standard *is* asked, and nothing
        // asserted is a demand on the caller rather than a decline (correspondence row 8).
        Assert.Throws<AssertionRequiredException>(
            () => Resolve(HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure));
    }

    [Fact]
    public void Paragraph_a_is_asked_at_runtime_and_its_undefined_term_is_what_blocks_the_disjunction()
    {
        // (a) is asked, not assumed. Its answer today is direct-participation's own decline over an
        // undefined term, and because § 107.39's paragraphs are held in the section's own order it
        // is the first unanswered case — so it is the one whose reason and locator this entry's
        // decline carries.
        var constituent = ParagraphAOnItsOwn();
        var mine = Declined(Resolve(HumanBeingLocation.NeitherOfThem));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, constituent.Reason);
        Assert.Equal(constituent.Reason, mine.Reason);
        Assert.Equal("§ 107.39(a)", mine.Locator.Citation);
        Assert.Equal(EntryPoints.DirectParticipation.Registered.Locator, mine.Locator);
        Assert.NotEqual(Entry.Locator, mine.Locator);

        // And where the section is settled anyway, (a)'s account on the finding is that entry's own
        // words, carried rather than restated.
        var finding = Finding(Resolve(
            HumanBeingLocation.InsideAStationaryVehicle, Shelter.StationaryVehicle, protectionHolds: true));

        Assert.Equal(constituent.Attempted, Case(finding, "(a)").Account);
        Assert.Equal(constituent.Reason, Case(finding, "(a)").Reason);
        Assert.Null(Case(finding, "(a)").Met);
    }

    [Fact]
    public void Paragraph_c_is_carried_as_subpart_d_categories_own_OutsideCurrentScope_and_never_becomes_this_entrys_reason()
    {
        // The entry's note: the deferral in (c) "is subpart-d-categories, which is scope: out and
        // carries OutsideCurrentScope itself — a dependency, not a second reason on this entry". So
        // that reason is recorded where it belongs, on (c)'s outcome and in the account of a
        // decline, and it never becomes this entry's own: a caller asking about a section this
        // engine maps is not told the section is outside its scope.
        var constituent = ParagraphCOnItsOwn();

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, constituent.Reason);
        Assert.Equal("subpart D", constituent.Locator.Citation);

        var finding = Finding(Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure, protectionHolds: true));

        Assert.Equal(MapEntries.SubpartDCategories.Id, Case(finding, "(c)").Entry.Id);
        Assert.Equal(UnresolvedReason.OutsideCurrentScope, Case(finding, "(c)").Reason);
        Assert.Equal(constituent.Attempted, Case(finding, "(c)").Account);

        var mine = Declined(Resolve(HumanBeingLocation.NeitherOfThem));

        Assert.NotEqual(UnresolvedReason.OutsideCurrentScope, mine.Reason);
        Assert.NotEqual(constituent.Locator, mine.Locator);

        // The distinction the two reasons carry survives into the account, rather than being
        // flattened into the single reason an UnresolvedResult can hold.
        Assert.Contains("subpart-d-categories", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains("subpart D", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            nameof(UnresolvedReason.OutsideCurrentScope), mine.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            nameof(UnresolvedReason.RequiresInterpretation), mine.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_this_entrys_own_and_not_direct_participations_handed_back()
    {
        // #78's shape. The reason and the citation are deliberately the blocking entry's — the
        // question that blocks the answer is that entry's question — so what tells a caller which
        // entry it asked is what was attempted, and the two must not be byte-identical.
        var constituent = ParagraphAOnItsOwn();
        var mine = Declined(Resolve(HumanBeingLocation.NeitherOfThem));

        Assert.NotEqual(constituent, mine);
        Assert.NotEqual(constituent.Attempted, mine.Attempted);
        Assert.Contains("over-human-beings", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("over-human-beings", constituent.Attempted, StringComparison.Ordinal);

        // The blind spot, pinned rather than papered over: on these two the reason and the locator
        // agree, and only Attempted differs (docs/decisions/0001 records the same for speed).
        Assert.Equal(constituent.Reason, mine.Reason);
        Assert.Equal(constituent.Locator, mine.Locator);
    }

    [Fact]
    public void The_three_excepted_cases_are_the_sections_own_paragraphs_in_its_own_order()
    {
        // § 107.39 letters its three cases (a), (b), (c), and they are held in that order. It is
        // deterministic either way, and the choice is visible in one array literal, because it fixes
        // which entry a decline cites when more than one case is unanswered.
        var finding = Finding(Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure, protectionHolds: true));

        Assert.Equal(
            new[] { "(a)", "(b)", "(c)" },
            finding.ExceptedCases.Select(excepted => excepted.Paragraph));
        Assert.Equal(
            new[] { "direct-participation", "reasonable-protection", "subpart-d-categories" },
            finding.ExceptedCases.Select(excepted => excepted.Entry.Id));

        // Two cases are unanswered on a plain overflight, and the one cited is the first of them in
        // that order, not the last and not the one that would have been cheapest to reach.
        Assert.Equal("§ 107.39(a)", Declined(Resolve(HumanBeingLocation.NeitherOfThem)).Locator.Citation);
    }

    [Fact]
    public void While_a_waiver_of_107_39_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_naming_this_entry()
    {
        // § 107.205(g) lists § 107.39 whole, so one caller statement reaches this entry,
        // direct-participation and reasonable-protection. The gate runs here as this entry's own,
        // before any case is asked and before any fact is demanded — the first case below states
        // nothing at all, which is the only input that tells the orders apart.
        var waiver = WaiverStatement.Held("§ 107.39", Caller, "107W-0000-00000");

        var withNothingAtAll = Declined(
            EntryPoints.OverHumanBeings.Resolve(new OverHumanBeingsRequest { Waiver = waiver }));
        var withEverything = Declined(Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure, waiver, protectionHolds: true));

        Assert.All(new[] { withNothingAtAll, withEverything }, unresolved =>
        {
            Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
            Assert.Equal("§ 107.205", unresolved.Locator.Citation);
            Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);
            Assert.Contains("over-human-beings", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains("§ 107.39", unresolved.Attempted, StringComparison.Ordinal);
            Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);

            // This entry's decline, not the constituent that would otherwise have been asked first:
            // all three share the § 107.205 citation, so only what was attempted tells them apart.
            Assert.DoesNotContain("direct-participation", unresolved.Attempted, StringComparison.Ordinal);
            Assert.DoesNotContain("reasonable-protection", unresolved.Attempted, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        // The statement is about the regulation § 107.205 lists for this entry, and no other. A
        // statement about § 107.51 is refused here, naming this entry, rather than being carried
        // down and refused by whichever constituent happened to be asked first.
        var error = Assert.Throws<ArgumentException>(() => Resolve(
            HumanBeingLocation.UnderACoveredStructure,
            Shelter.CoveredStructure,
            WaiverStatement.NoneHeld("§ 107.51", Caller),
            protectionHolds: true));

        Assert.Contains("over-human-beings", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.39", error.Message, StringComparison.Ordinal);
        Assert.Contains("§ 107.51", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("direct-participation", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        // The other direction of the gate: the entry is reachable, so the other facts are owed, and
        // the statement the finding was resolved under is carried on it as a typed field rather than
        // dropped — an answer that lost it would say § 107.39 was read without saying on whose word
        // (docs/decisions/0001).
        var waiver = WaiverStatement.NoneHeld("§ 107.39", "the remote pilot in command");

        var finding = Finding(Resolve(
            HumanBeingLocation.InsideAStationaryVehicle, Shelter.StationaryVehicle, waiver, protectionHolds: true));

        Assert.Same(waiver, finding.Waiver);
        Assert.Equal("§ 107.39", finding.Waiver.Regulation);
        Assert.Equal("the remote pilot in command", finding.Waiver.StatedBy);
        Assert.False(finding.Waiver.InForce);
        Assert.Contains("no certificate of waiver", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_three_places_a_human_being_may_be_are_107_39_b_s_own_two_and_the_case_it_does_not_reach_and_none_is_a_default()
    {
        // § 107.39(b)'s two places, in the paragraph's own order, then the case it does not reach —
        // which the entry's note keeps and which is stated by the caller, never inferred from an
        // absence. The set is closed: no caller can make a fourth.
        Assert.Equal(
            new[]
            {
                "located under a covered structure",
                "located inside a stationary vehicle",
                "located neither under a covered structure nor inside a stationary vehicle",
            },
            HumanBeingLocation.All.Select(location => location.Designation));
        Assert.Empty(typeof(HumanBeingLocation).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        // Each of the two carries the Shelter the standard must have been asserted over, which is
        // what makes the comparison possible at all; the third carries none, because it is neither.
        Assert.Same(Shelter.CoveredStructure, HumanBeingLocation.UnderACoveredStructure.Place);
        Assert.Same(Shelter.StationaryVehicle, HumanBeingLocation.InsideAStationaryVehicle.Place);
        Assert.Null(HumanBeingLocation.NeitherOfThem.Place);

        // And the asymmetry is deliberate: reasonable-protection's Shelter has exactly the
        // paragraph's two and no "neither", because the standard is asserted over a place, while
        // where a human being is includes the place the paragraph does not reach.
        Assert.Equal(2, Shelter.All.Count);

        // Stating no place refuses, naming the request property, and is an ArgumentException rather
        // than an unresolved result: a missing input is the caller's error, not a gap in the corpus.
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.OverHumanBeings.Resolve(
            new OverHumanBeingsRequest { Shelter = Shelter.CoveredStructure, Waiver = NoWaiver }));

        Assert.Equal(nameof(OverHumanBeingsRequest.Location), error.ParamName);
        Assert.Contains("over-human-beings", error.Message, StringComparison.Ordinal);
        Assert.Contains("the engine locates nobody", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_verdicts_and_the_accounts_are_the_answering_entries_own_and_are_not_restated_here()
    {
        // Nothing this entry reports about a case is re-derived here. Each verdict is what the
        // answering entry's answer came to, and each account is that entry's own rendering or its
        // own Attempted, resolved below through that entry's own entry point and compared.
        var finding = Finding(Resolve(
            HumanBeingLocation.UnderACoveredStructure, Shelter.CoveredStructure, protectionHolds: true));

        var protection = ParagraphBOnItsOwn(Shelter.CoveredStructure, holds: true);

        Assert.Equal(protection.Holds, Case(finding, "(b)").Met);
        Assert.Equal(protection.ToString(), Case(finding, "(b)").Account);
        Assert.Equal(ParagraphAOnItsOwn().Attempted, Case(finding, "(a)").Account);
        Assert.Equal(ParagraphCOnItsOwn().Attempted, Case(finding, "(c)").Account);

        // A negative assertion is carried the same way round, and is not turned into a gap.
        var notMet = ParagraphBOnItsOwn(Shelter.StationaryVehicle, holds: false);
        var declined = Declined(Resolve(
            HumanBeingLocation.InsideAStationaryVehicle, Shelter.StationaryVehicle, protectionHolds: false));

        Assert.False(notMet.Holds);
        Assert.DoesNotContain("reasonable-protection", declined.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_the_facts_the_section_is_tested_against_it_refuses_rather_than_assume_them()
    {
        // Each fact is the caller's and none is inferred. A missing waiver statement is refused
        // rather than assumed in either direction — assuming none held convicts a holder, and
        // assuming one held excuses everyone — and a missing place is refused rather than picked.
        Assert.Equal(
            nameof(OverHumanBeingsRequest.Waiver),
            Assert.Throws<ArgumentException>(() => EntryPoints.OverHumanBeings.Resolve(
                new OverHumanBeingsRequest
                {
                    Location = HumanBeingLocation.UnderACoveredStructure,
                    Shelter = Shelter.CoveredStructure,
                })).ParamName);

        Assert.Equal(
            nameof(OverHumanBeingsRequest.Location),
            Assert.Throws<ArgumentException>(
                () => EntryPoints.OverHumanBeings.Resolve(new OverHumanBeingsRequest { Waiver = NoWaiver }))
                .ParamName);

        // And the place the standard was asserted over is demanded by the entry that owns it, in its
        // own words, rather than picked from where the human being is.
        Assert.Equal(
            nameof(ReasonableProtectionRequest.Shelter),
            Assert.Throws<ArgumentException>(() => Resolve(
                HumanBeingLocation.UnderACoveredStructure, shelter: null, protectionHolds: true)).ParamName);
    }

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_answering_from_defaults()
    {
        // A request built by the registry's dictionary dispatch has every input at its default, so
        // this entry has no waiver statement, no place and no location. It refuses, and in
        // particular it does not answer about a human being nobody has located.
        Assert.Throws<ArgumentException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
        Assert.Equal(EntryStatus.Implemented, Registry.Entry(Entry.Id).Status);
        Assert.True(Registry.HasImplementation(Entry.Id));
    }

    /// <summary>
    /// Two resolutions of one request say the same thing, so they are the same finding: compared by
    /// what they say, and never by the identity of the list of excepted cases carrying it
    /// (<c>AGENTS.md</c> §8).
    /// </summary>
    /// <remarks>
    /// The two resolutions share no object equality could hold by identity on — each carries its own
    /// waiver statement, and the entry builds each finding its own list of excepted cases, which the
    /// <c>NotSame</c> assertions pin so that the comparison cannot pass by accident.
    /// </remarks>
    [Fact]
    public void Two_resolutions_of_one_request_are_equal_and_hash_alike_with_the_excepted_cases_compared_by_element()
    {
        var first = Finding(Resolve(
            HumanBeingLocation.UnderACoveredStructure,
            Shelter.CoveredStructure,
            WaiverStatement.NoneHeld("§ 107.39", Caller),
            protectionHolds: true));
        var second = Finding(Resolve(
            HumanBeingLocation.UnderACoveredStructure,
            Shelter.CoveredStructure,
            WaiverStatement.NoneHeld("§ 107.39", Caller),
            protectionHolds: true));

        Assert.NotSame(first, second);
        Assert.NotSame(first.ExceptedCases, second.ExceptedCases);
        Assert.NotSame(first.Waiver, second.Waiver);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        // Equality still says what it is for: a finding about a human being in the other of
        // § 107.39(b)'s two places is unequal to the first.
        Assert.NotEqual(
            first,
            Finding(Resolve(
                HumanBeingLocation.InsideAStationaryVehicle,
                Shelter.StationaryVehicle,
                protectionHolds: true)));
    }

    /// <summary>
    /// An operation the evaluator resolves this entry on: a human being under a covered structure
    /// and a covered structure to be under, so § 107.39(b) is the excepted case that settles it,
    /// with no waiver of § 107.39 in force. <paramref name="protectionAssertedBy"/> is who made
    /// § 107.39(b)'s assertion.
    /// </summary>
    private static OperationFacts Operation(string protectionAssertedBy) => new OperationFacts
    {
        HumanBeingLocation = HumanBeingLocation.UnderACoveredStructure,
        Shelter = Shelter.CoveredStructure,
    }
        .Stating(WaiverStatement.NoneHeld(Overflight.Regulation, Caller))
        .Asserting(new Assertion(MapEntries.ReasonableProtection, true, protectionAssertedBy));

    /// <summary>This entry's outcome, as a product layer receives it.</summary>
    private static EvaluatedRequirement Outcome(string protectionAssertedBy) =>
        OperationEvaluator.Evaluate(Operation(protectionAssertedBy)).Requirement(Entry.Id);

    /// <summary>
    /// Who is answerable for § 107.39(b)'s assertion is recorded and is not printed, so two
    /// operations differing only in that name are one set of words and two findings — and they are
    /// two outcomes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two names are both accepted because <c>reasonable-protection</c>'s <c>assertedBy</c> is
    /// exactly <c>["caller"]</c>, which <c>docs/decisions/0003</c> settles: the marker is not a
    /// name to match, it records that the corpus narrows nobody, and
    /// <c>Assertions.Stated</c>'s <c>NamesNobody</c> skips the attribution check so that the
    /// engine attributes the assertion to whoever the caller says made it. <b>The set of asserters
    /// this entry accepts is therefore unbounded</b>, and two of them are all this test needs.
    /// </para>
    /// <para>
    /// <see cref="ExceptedCaseOutcome.Account"/> holds the answering entry's own
    /// <c>ToString()</c>, which for an assertion entry carries "as asserted by …";
    /// <see cref="ExceptedCaseOutcome.ToString"/> prints only the paragraph, the entry and the
    /// verdict, and this finding's own <c>ToString</c> prints those. So the difference is real and
    /// invisible — and it is the difference decision 0003 exists to preserve, an assertion
    /// travelling with whoever is answerable for it.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_operations_differing_only_in_who_asserted_the_reasonable_protection_are_different_outcomes()
    {
        // Compared element by element and never as one ImmutableArray against another, which is
        // the very identity comparison issue #97 is about.
        Assert.Equal(["caller"], MapEntries.ReasonableProtection.AssertedBy.ToArray());

        var byOne = Outcome("caller");
        var byAnother = Outcome("Alice, a named person the corpus does not narrow to");

        var findingByOne = Assert.IsType<OverHumanBeingsFinding>(byOne.Finding);
        var findingByAnother = Assert.IsType<OverHumanBeingsFinding>(byAnother.Finding);

        var protectionByOne = Case(findingByOne, "(b)");
        var protectionByAnother = Case(findingByAnother, "(b)");

        // The attribution is the one thing that differs, and the finding records it.
        Assert.True(protectionByOne.Met);
        Assert.Contains("as asserted by caller", protectionByOne.Account, StringComparison.Ordinal);
        Assert.Contains("as asserted by Alice", protectionByAnother.Account, StringComparison.Ordinal);
        Assert.Equal(protectionByOne with { Account = protectionByAnother.Account }, protectionByAnother);

        // And nothing the engine prints about either operation differs.
        Assert.Equal(protectionByOne.ToString(), protectionByAnother.ToString());
        Assert.Equal(findingByOne.ToString(), findingByAnother.ToString());
        Assert.Equal(byOne.Explanation, byAnother.Explanation);

        Assert.NotEqual(findingByOne, findingByAnother);
        Assert.NotEqual(byOne, byAnother);
    }
}
