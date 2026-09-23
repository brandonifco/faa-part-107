using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>attached-object-no-adverse-effect</c>, § 107.49(e), resolved through
/// <see cref="EntryPoints.AttachedObjectNoAdverseEffect"/> only. The map records the entry as
/// <c>kind: assertion</c> with <c>clarity: clear</c>, asserted by the remote pilot in command, so
/// these pin the two halves of correspondence row 8: what the caller asserts is what the engine
/// answers, unchanged and in either direction; and asserting nothing is a demand on the caller,
/// not an unresolved result.
/// </summary>
/// <remarks>
/// One sentence of § 107.49(e) carries two map entries — this one and
/// <see cref="MapEntries.AttachedObjectSecure"/> — and their locators are identical, so a test
/// that checked only the citation would pass just as well against an answer about the other
/// constituent. These therefore pin the entry <em>by id and by name</em> everywhere the citation
/// alone would not be enough, and check that an assertion made about the other constituent of the
/// same sentence is refused rather than silently answered under this one.
/// </remarks>
public class AttachedObjectNoAdverseEffectEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    private static readonly MapEntry Entry = MapEntries.AttachedObjectNoAdverseEffect;

    private static Resolution<object> Resolve(object asserted) =>
        EntryPoints.AttachedObjectNoAdverseEffect.Resolve(AttachedObjectNoAdverseEffectRequest.Asserting(asserted));

    [Fact]
    public void An_assertion_that_an_attached_object_does_not_adversely_affect_the_aircraft_resolves_to_what_was_asserted_citing_107_49_e()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var resolved = Assert.IsType<Resolution<object>.Resolved>(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Equal("cfr-14-107", assertion.Authority.SourceId);
        Assert.Equal("§ 107.49(e)", assertion.Authority.Citation);
        Assert.Equal(EntryPoints.AttachedObjectNoAdverseEffect.Registered.Locator, assertion.Authority);

        // The citation cannot identify the constituent: the "is secure" conjunct of the same
        // sentence carries the very same locator. The answer names this one by id and by name.
        Assert.Equal(MapEntries.AttachedObjectSecure.Locator, assertion.Authority);
        Assert.Equal("attached-object-no-adverse-effect", assertion.Entry.Id);
        Assert.Equal(
            "An attached or carried object does not adversely affect flight characteristics or controllability",
            assertion.Entry.Name);
    }

    [Fact]
    public void An_assertion_that_an_attached_object_does_adversely_affect_the_aircraft_resolves_to_that_and_is_not_turned_into_a_decline()
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
            () => EntryPoints.AttachedObjectNoAdverseEffect.Resolve(AttachedObjectNoAdverseEffectRequest.Empty));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. Nothing about this entry ever resolves to an UnresolvedResult — and in
        // particular the clarity the map records for it, `clear`, is not qualified by the
        // neighbouring constituent's.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.AttachedObjectNoAdverseEffect.Resolve(new AttachedObjectNoAdverseEffectRequest()));
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
        Assert.Equal(Entry.Id, assertion.Entry.Id);
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
        Assert.Equal(
            RemotePilotInCommand,
            Assert.Single(EntryPoints.AttachedObjectNoAdverseEffect.Registered.AssertedBy));
    }

    [Fact]
    public void An_assertion_about_the_other_constituent_of_107_49_e_is_refused_though_its_locator_is_identical()
    {
        // The map splits one sentence of § 107.49(e) into two entries, and this is the half about
        // flight characteristics and controllability. The other half is a different entry with the
        // same locator, so the refusal cannot be reached by comparing citations: it is the id that
        // separates them.
        Assert.Equal(Entry.Locator, MapEntries.AttachedObjectSecure.Locator);
        Assert.NotEqual(Entry.Id, MapEntries.AttachedObjectSecure.Id);

        var error = Assert.Throws<ArgumentException>(
            () => Resolve(new Assertion(MapEntries.AttachedObjectSecure, Holds: true, RemotePilotInCommand)));

        Assert.Contains("attached-object-secure", error.Message, StringComparison.Ordinal);
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
        // answered under, and the name it is answered as, are the map's.
        var forged = new MapEntry(
            Entry.Id,
            "It flies just the same with the camera on, trust me",
            new SourceLocator("cfr-14-107", "§ 107.51(b)"));

        var resolved = Assert.IsType<Resolution<object>.Resolved>(
            Resolve(new Assertion(forged, Holds: true, RemotePilotInCommand)));

        var assertion = Assert.IsType<Assertion>(resolved.Value);
        Assert.Equal(Entry.Locator, assertion.Authority);
        Assert.Equal("§ 107.49(e)", assertion.Authority.Citation);
        Assert.Equal(Entry.Name, assertion.Entry.Name);
        Assert.DoesNotContain("trust me", assertion.ToString(), StringComparison.Ordinal);

        // The fact itself still came from the caller, untouched.
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
    }
}
