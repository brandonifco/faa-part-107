using FaaPart107.Requests;
using RulesKernel.Provenance;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>sufficient-available-power</c>, § 107.49(d), resolved through
/// <see cref="EntryPoints.SufficientAvailablePower"/> only.
/// </summary>
/// <remarks>
/// <para>
/// The paragraph states one obligation under one condition. The map records the obligation as
/// <c>kind: assertion</c>, asserted by the remote pilot in command, so these pin the two halves of
/// correspondence row 8: what the caller asserts is what the engine answers, unchanged and in either
/// direction; and asserting nothing is a demand on the caller, not an unresolved result. They pin
/// the division of ownership too — the fact is the caller's, the citation is the map's — by value,
/// never by object identity, which is not something this engine's behaviour may turn on.
/// </para>
/// <para>
/// And they pin the condition, which is in this entry's own evidence: "If the small unmanned
/// aircraft is powered". An aircraft the caller states is not powered is an aircraft § 107.49(d)
/// states no obligation about, so nothing is asserted and nothing is demanded — a <b>third</b>
/// answer, which is neither the obligation met nor the obligation unmet, and which is never reached
/// from an absence (<c>#95</c>).
/// </para>
/// </remarks>
public class SufficientAvailablePowerEntryPointTests
{
    private const string RemotePilotInCommand = "remote pilot in command";

    private static readonly MapEntry Entry = MapEntries.SufficientAvailablePower;

    private static Resolution<object> Resolve(object asserted) => Resolve(AircraftPower.Powered, asserted);

    private static Resolution<object> Resolve(AircraftPower power, object? asserted = null) =>
        EntryPoints.SufficientAvailablePower.Resolve(new SufficientAvailablePowerRequest(
            asserted is null ? RuleRequest.Empty : RuleRequest.Empty.Assert(Entry.Id, asserted))
        {
            Power = power,
        });

    private static SufficientAvailablePowerFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<SufficientAvailablePowerFinding>(
            Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    [Fact]
    public void An_assertion_that_there_is_enough_power_resolves_to_what_was_asserted_citing_107_49_d()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Finding(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(finding.Availability);
        Assert.True(finding.Holds);
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(RemotePilotInCommand, finding.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Equal("cfr-14-107", assertion.Authority.SourceId);
        Assert.Equal("§ 107.49(d)", assertion.Authority.Citation);
        Assert.Equal(EntryPoints.SufficientAvailablePower.Registered.Locator, assertion.Authority);
        Assert.Equal(EntryPoints.SufficientAvailablePower.Registered.Locator, finding.Authority);
    }

    [Fact]
    public void An_assertion_that_there_is_not_enough_power_resolves_to_that_and_is_not_turned_into_a_decline()
    {
        var asserted = new Assertion(Entry, Holds: false, RemotePilotInCommand);

        var finding = Finding(Resolve(asserted));

        var assertion = Assert.IsType<Assertion>(finding.Availability);
        Assert.False(finding.Holds);
        Assert.False(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
        Assert.Equal(asserted, assertion);
        Assert.Contains(RemotePilotInCommand, finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_unpowered_aircraft_is_answered_that_107_49_d_states_no_obligation_about_the_operation()
    {
        // The condition is in this entry's own evidence — "If the small unmanned aircraft is
        // powered" — so an aircraft the caller states is not powered is an aircraft the paragraph
        // says nothing about. That is the third answer: not the obligation met, not the obligation
        // unmet, and not a decline.
        var finding = Finding(Resolve(AircraftPower.NotPowered));

        Assert.False(finding.ParagraphApplies);
        Assert.Null(finding.Holds);
        Assert.Null(finding.Availability);
        Assert.Null(finding.AssertedBy);
        Assert.Same(AircraftPower.NotPowered, finding.Power);
        Assert.Contains("states no obligation", finding.ToString(), StringComparison.Ordinal);

        // And nothing is demanded for a paragraph that asks nobody anything: the assertion the
        // powered case owes is not owed here, so this answers where that case throws.
        Assert.Throws<AssertionRequiredException>(() => Resolve(AircraftPower.Powered));

        // Asserting it anyway changes nothing: the condition decides whether the paragraph reaches
        // the operation, and a fact about a paragraph that states no obligation is not read.
        var alsoAsserted = Finding(
            Resolve(AircraftPower.NotPowered, new Assertion(Entry, Holds: false, RemotePilotInCommand)));

        Assert.Null(alsoAsserted.Holds);
        Assert.Null(alsoAsserted.Availability);
    }

    [Fact]
    public void An_unpowered_aircraft_is_not_a_powered_one_that_has_too_little_power()
    {
        var unpowered = Finding(Resolve(AircraftPower.NotPowered));
        var tooLittle = Finding(Resolve(new Assertion(Entry, Holds: false, RemotePilotInCommand)));
        var enough = Finding(Resolve(new Assertion(Entry, Holds: true, RemotePilotInCommand)));

        // Three answers, and no two of them are the same. Reading the null as either verdict would
        // be the engine saying something about an operation § 107.49(d) does not reach.
        Assert.Null(unpowered.Holds);
        Assert.False(tooLittle.Holds);
        Assert.True(enough.Holds);
        Assert.NotEqual(unpowered, tooLittle);
        Assert.NotEqual(unpowered, enough);
        Assert.NotEqual(tooLittle, enough);

        // ParagraphApplies is what says which case a null Holds is, so a null is never read as
        // "undetermined": this entry has no unresolved outcome of its own (correspondence row 8).
        Assert.True(tooLittle.ParagraphApplies);
        Assert.True(enough.ParagraphApplies);
        Assert.False(unpowered.ParagraphApplies);
        Assert.IsAssignableFrom<IConditionalAssertion>(unpowered);
    }

    [Fact]
    public void The_condition_is_stated_by_the_caller_and_is_never_defaulted_or_inferred()
    {
        // Saying nothing about the power is not saying the aircraft is unpowered, and it is not
        // saying it is powered either. The entry refuses, naming the input: the caller's omission,
        // not a gap in § 107.49.
        var error = Assert.Throws<ArgumentException>(
            () => EntryPoints.SufficientAvailablePower.Resolve(
                SufficientAvailablePowerRequest.Asserting(new Assertion(Entry, Holds: true, RemotePilotInCommand))));

        Assert.Equal(nameof(SufficientAvailablePowerRequest.Power), error.ParamName);
        Assert.Contains(Entry.Id, error.Message, StringComparison.Ordinal);

        // The set is closed and has exactly the condition's two sides. There is no third, so
        // "not powered" can never arrive as missing data or as an assertion that power suffices.
        Assert.Equal(
            new[] { AircraftPower.Powered, AircraftPower.NotPowered },
            AircraftPower.All);
        Assert.Empty(typeof(AircraftPower).GetConstructors());
    }

    [Fact]
    public void Asserting_nothing_demands_the_value_rather_than_declining_the_entry()
    {
        var error = Assert.Throws<AssertionRequiredException>(() => Resolve(AircraftPower.Powered));

        Assert.Equal(Entry.Id, error.EntryId);

        // Row 8 is not a decline: the corpus gave the engine the means to proceed, and the caller
        // owes the value. Nothing about this entry ever resolves to an UnresolvedResult.
        Assert.Throws<AssertionRequiredException>(
            () => EntryPoints.SufficientAvailablePower.Resolve(
                new SufficientAvailablePowerRequest { Power = AircraftPower.Powered }));
    }

    [Fact]
    public void The_dictionary_dispatch_answers_and_demands_the_same_way()
    {
        var asserted = new Assertion(Entry, Holds: true, RemotePilotInCommand);

        var finding = Assert.IsType<SufficientAvailablePowerFinding>(
            Assert.IsType<Resolution<object>.Resolved>(Registry.Resolve(new SufficientAvailablePowerRequest(
                RuleRequest.Empty.Assert(Entry.Id, asserted))
            {
                Power = AircraftPower.Powered,
            })).Value);

        Assert.True(finding.Holds);
        Assert.Equal(Entry.Locator, finding.Authority);
        Assert.Equal(asserted, finding.Availability);

        // Resolved by id, the request is built from the assertions alone, so it carries no
        // statement about the power and the entry refuses rather than inferring one.
        var error = Assert.Throws<ArgumentException>(
            () => Registry.Resolve(Entry.Id, RuleRequest.Empty.Assert(Entry.Id, asserted)));

        Assert.Equal(nameof(SufficientAvailablePowerRequest.Power), error.ParamName);
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

    [Fact]
    public void The_citation_is_the_maps_and_a_caller_supplied_entry_does_not_become_it()
    {
        // MapEntry and SourceLocator are publicly constructible, so a caller can hand in an
        // assertion carrying this entry's id under somebody else's paragraph and somebody else's
        // name. The fact it reports is the caller's and is taken as given; the citation it is
        // answered under is the map's, and is not on offer.
        var forged = new MapEntry(
            Entry.Id,
            "There is plenty of battery, trust me",
            new SourceLocator("cfr-14-107", "§ 107.51(b)"));

        var finding = Finding(Resolve(new Assertion(forged, Holds: true, RemotePilotInCommand)));

        var assertion = Assert.IsType<Assertion>(finding.Availability);
        Assert.Equal(MapEntries.SufficientAvailablePower.Locator, assertion.Authority);
        Assert.Equal("§ 107.49(d)", assertion.Authority.Citation);
        Assert.Equal(MapEntries.SufficientAvailablePower.Name, assertion.Entry.Name);
        Assert.DoesNotContain("trust me", finding.ToString(), StringComparison.Ordinal);

        // The fact itself still came from the caller, untouched.
        Assert.True(assertion.Holds);
        Assert.Equal(RemotePilotInCommand, assertion.AssertedBy);
    }

    [Fact]
    public void The_paragraph_is_carried_verbatim_condition_and_all()
    {
        // Quoted, not interpreted: the condition the engine tests is legible beside the answer, and
        // it is the map's evidence for this entry rather than a paraphrase of it.
        Assert.Equal(
            "(d) If the small unmanned aircraft is powered, ensure that there is enough available "
            + "power for the small unmanned aircraft system to operate for the intended operational time;",
            AvailablePower.Paragraph);
    }
}
