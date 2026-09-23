using System.Reflection;
using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>preflight-risk-assessment</c>, § 107.49(a), resolved through
/// <see cref="EntryPoints.PreflightRiskAssessment"/> only. The map records the entry as
/// <c>kind: assertion</c>, asserted by the remote pilot in command, so these pin the two halves of
/// correspondence row 8: what the caller asserts is what the engine answers, unchanged and in
/// either direction; and asserting nothing is a demand on the caller, not an unresolved result.
/// They pin the division of ownership too — the fact is the caller's, the citation is the map's —
/// by value, never by object identity, which is not something this engine's behaviour may turn on.
/// </summary>
/// <remarks>
/// One of them is this entry's own rather than the shape's: § 107.49(a) states a closed four-item
/// list of what the assessment "must include", and the map makes one entry of the whole paragraph
/// and no entry of any item. The engine therefore takes no input over those four items and does no
/// assessing, and that is checked rather than left to the handler's prose.
/// </remarks>
public class PreflightRiskAssessmentEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    private static readonly MapEntry Entry = MapEntries.PreflightRiskAssessment;

    private static Resolution<object> Resolve(object asserted) =>
        EntryPoints.PreflightRiskAssessment.Resolve(PreflightRiskAssessmentRequest.Asserting(asserted));

    [Fact]
    public void An_assertion_that_the_operating_environment_was_assessed_resolves_to_what_was_asserted_citing_107_49_a()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Equal("cfr-14-107", assertion.Authority.SourceId);
        Assert.Equal("§ 107.49(a)", assertion.Authority.Citation);
        Assert.Equal(EntryPoints.PreflightRiskAssessment.Registered.Locator, assertion.Authority);
    }

    [Fact]
    public void An_assertion_that_it_was_not_assessed_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        var asserted = new Assertion(Entry, Holds: false, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.False(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Contains(RemotePilotInCommand, assertion.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.PreflightRiskAssessment.Resolve(PreflightRiskAssessmentRequest.Empty));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. Nothing about this entry ever resolves to an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.PreflightRiskAssessment.Resolve(new PreflightRiskAssessmentRequest()));
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_demands_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(Entry.Locator, assertion.Authority);
        Assert.Equal(asserted, assertion);
        Assert.Throws<AssertionRequiredException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }

    [Fact]
    public void An_assertion_attributed_to_someone_107_49_does_not_name_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(Entry, Holds: true, "visual observer")));

        Assert.Contains(RemotePilotInCommand, error.Message, StringComparison.Ordinal);
        Assert.Contains("visual observer", error.Message, StringComparison.Ordinal);
        Assert.Equal(RemotePilotInCommand, Assert.Single(EntryPoints.PreflightRiskAssessment.Registered.AssertedBy));
    }

    [Fact]
    public void An_assertion_about_another_entry_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.SufficientAvailablePower, Holds: true, RemotePilotInCommand)));

        Assert.Contains("sufficient-available-power", error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_that_is_not_an_assertion_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(true));

        Assert.Contains(nameof(Assertion), error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible, so a caller can hand in an
        // assertion carrying this entry's id under somebody else's paragraph and somebody else's
        // name. The fact it reports is the caller's and is taken as given; the citation it is
        // answered under is the map's, and is not on offer.
        var forged = new MapEntry(
            Entry.Id,
            "I had a good look round, trust me",
            new SourceLocator("cfr-14-107", "§ 107.51(b)"));

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Resolve(new Assertion(forged, Holds: true, RemotePilotInCommand)));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Equal(MapEntries.PreflightRiskAssessment.Locator, assertion.Authority);
        Assert.Equal("§ 107.49(a)", assertion.Authority.Citation);
        Assert.Equal(MapEntries.PreflightRiskAssessment.Name, assertion.Entry.Name);
        Assert.DoesNotContain("trust me", assertion.ToString(), StringComparison.Ordinal);

        // The fact itself still came from the caller, untouched.
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
    }

    [Fact]
    public void The_four_item_enumeration_is_one_asserted_fact_and_the_engine_takes_no_input_of_its_own()
    {
        // § 107.49(a) fixes what the assessment "must include": "(1) Local weather conditions;
        // (2) Local airspace and any flight restrictions; (3) The location of persons and property
        // on the surface; and (4) Other ground hazards." The map makes one entry of the paragraph
        // and no entry of any of the four, so the closed list is the measure the assessment is
        // taken against and not four obligations to answer separately. One asserted fact therefore
        // answers the whole of it, under the name the map gives the whole.
        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Resolve(new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        Assert.Equal(
            "The operating environment has been assessed against the stated risks",
            Assert.IsType<Assertion>(resolved.Value).Entry.Name);

        // And the request carries nothing of its own over those four items — no weather, no
        // airspace or flight restrictions, no location of persons and property, no ground hazards.
        // An engine given any of them would be assessing, which § 107.49(a) gives to the remote
        // pilot in command; what it is given is the finished assessment's verdict and nothing else.
        // Ordered ordinally: reflection's own order is not something a check here may depend on.
        var carried = typeof(PreflightRiskAssessmentRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal<string>(["Assertions", "EntryId"], carried);
    }
}
