using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>participant-briefing</c>, § 107.49(b), resolved through
/// <see cref="EntryPoints.ParticipantBriefing"/> only. The map records the entry as
/// <c>kind: assertion</c>, asserted by the remote pilot in command, so these pin the two halves of
/// correspondence row 8: what the caller asserts is what the engine answers, unchanged and in
/// either direction; and asserting nothing is a demand on the caller, not an unresolved result.
/// They pin the division of ownership too — the fact is the caller's, the citation is the map's —
/// by value, never by object identity, which is not something this engine's behaviour may turn on.
/// The last one pins what this entry's <c>dependsOn</c> edge does and does not do: the term that
/// delimits who must be briefed is open, <c>direct-participation</c> declines over it, and this
/// entry still answers on its own row.
/// </summary>
public class ParticipantBriefingEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    private static readonly MapEntry Entry = MapEntries.ParticipantBriefing;

    private static Resolution<object> Resolve(object asserted) =>
        EntryPoints.ParticipantBriefing.Resolve(ParticipantBriefingRequest.Asserting(asserted));

    [Fact]
    public void An_assertion_that_the_directly_participating_persons_were_informed_resolves_to_what_was_asserted_citing_107_49_b()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Equal("cfr-14-107", assertion.Authority.SourceId);
        Assert.Equal("§ 107.49(b)", assertion.Authority.Citation);
        Assert.Equal(EntryPoints.ParticipantBriefing.Registered.Locator, assertion.Authority);
    }

    [Fact]
    public void An_assertion_that_they_were_not_informed_resolves_to_that_and_is_not_turned_into_a_decline()
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
            () => EntryPoints.ParticipantBriefing.Resolve(ParticipantBriefingRequest.Empty));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. Nothing about this entry ever resolves to an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.ParticipantBriefing.Resolve(new ParticipantBriefingRequest()));
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
        Assert.Equal(RemotePilotInCommand, Assert.Single(EntryPoints.ParticipantBriefing.Registered.AssertedBy));
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
            "Everyone who mattered was told, trust me",
            new SourceLocator("cfr-14-107", "§ 107.51(b)"));

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Resolve(new Assertion(forged, Holds: true, RemotePilotInCommand)));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Equal(MapEntries.ParticipantBriefing.Locator, assertion.Authority);
        Assert.Equal("§ 107.49(b)", assertion.Authority.Citation);
        Assert.Equal(MapEntries.ParticipantBriefing.Name, assertion.Entry.Name);
        Assert.DoesNotContain("trust me", assertion.ToString(), StringComparison.Ordinal);

        // The fact itself still came from the caller, untouched.
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
    }

    [Fact]
    public void The_entry_answers_on_row_8_although_the_dependency_that_holds_its_audience_term_open_declines()
    {
        // § 107.49(b) has two open terms and the map gives them two verdicts: "informed about" is
        // fixed by the five matters stated beside it and is this entry, kind: assertion; "all
        // persons directly participating" is the undefined term of § 107.39(a) and is
        // direct-participation, a gap. This entry names that gap in dependsOn — which orders
        // implementation and is not a runtime precondition — and does not borrow its decline.
        var dependency = Assert.IsType<Resolution<object>.Unresolved>(
            EntryPoints.DirectParticipation.Resolve(new DirectParticipationRequest
            {
                Waiver = WaiverStatement.NoneHeld("§ 107.39", nameof(ParticipantBriefingEntryPointTests)),
            })).Result;

        Assert.Equal(UnresolvedReason.RequiresInterpretation, dependency.Reason);
        Assert.Equal("§ 107.39(a)", dependency.Locator.Citation);

        // The correspondence table reaches a dependency in one row only, row 5, an operation whose
        // value dependency is unimplemented. This entry is neither, so its first row is row 8.
        Assert.Equal(CorrespondenceRow.Assertion, EntryPoints.ParticipantBriefing.Registered.Row);
        Assert.Equal(CorrespondenceRow.UnresolvedAmbiguity, EntryPoints.DirectParticipation.Registered.Row);

        // So the remote pilot in command's assertion is answered, citing § 107.49(b) and not
        // § 107.39(a), whatever the caller does or does not also say about the open term.
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal(Entry.Locator, assertion.Authority);
        Assert.NotEqual(dependency.Locator, assertion.Authority);

        // And the demand when nothing is asserted is for this entry, not for the one it depends on.
        var error = Assert.Throws<AssertionRequiredException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty));

        Assert.Equal(Entry.Id, error.EntryId);
        Assert.DoesNotContain("direct-participation", error.Message, StringComparison.Ordinal);
    }
}
