using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>sufficient-available-power</c>, § 107.49(d), resolved through
/// <see cref="EntryPoints.SufficientAvailablePower"/> only. The map records the entry as
/// <c>kind: assertion</c>, asserted by the remote pilot in command, so these pin the two halves of
/// correspondence row 8: what the caller asserts is what the engine answers, unchanged and in
/// either direction; and asserting nothing is a demand on the caller, not an unresolved result.
/// </summary>
public class SufficientAvailablePowerEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    private static readonly MapEntry Entry = MapEntries.SufficientAvailablePower;

    private static Resolution<object> Resolve(object asserted) =>
        EntryPoints.SufficientAvailablePower.Resolve(SufficientAvailablePowerRequest.Asserting(asserted));

    [Fact]
    public void An_assertion_that_there_is_enough_power_resolves_to_what_was_asserted_citing_107_49_d()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Same(asserted, assertion);
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal("cfr-14-107", assertion.Authority.SourceId);
        Assert.Equal("§ 107.49(d)", assertion.Authority.Citation);
        Assert.Equal(EntryPoints.SufficientAvailablePower.Registered.Locator, assertion.Authority);
    }

    [Fact]
    public void An_assertion_that_there_is_not_enough_power_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        var asserted = new Assertion(Entry, Holds: false, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Same(asserted, assertion);
        Assert.False(assertion.Holds);
        Assert.Contains(RemotePilotInCommand, assertion.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.SufficientAvailablePower.Resolve(SufficientAvailablePowerRequest.Empty));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. Nothing about this entry ever resolves to an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.SufficientAvailablePower.Resolve(new SufficientAvailablePowerRequest()));
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_demands_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));

        Assert.Same(asserted, resolved.Value);
        Assert.Throws<AssertionRequiredException>(() => Registry.Resolve(Entry.Id, RuleRequest.Empty));
    }

    [Fact]
    public void An_assertion_attributed_to_someone_107_49_does_not_name_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(Entry, Holds: true, "visual observer")));

        Assert.Contains(RemotePilotInCommand, error.Message, StringComparison.Ordinal);
        Assert.Contains("visual observer", error.Message, StringComparison.Ordinal);
        Assert.Equal(RemotePilotInCommand, Assert.Single(EntryPoints.SufficientAvailablePower.Registered.AssertedBy));
    }

    [Fact]
    public void An_assertion_about_another_entry_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.ParticipantBriefing, Holds: true, RemotePilotInCommand)));

        Assert.Contains("participant-briefing", error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_that_is_not_an_assertion_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() => Resolve(true));

        Assert.Contains(nameof(Assertion), error.Message, StringComparison.Ordinal);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);
    }
}
