using FaaPart107.Evaluation;
using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>operating-limitations</c>, § 107.51's introductory text, resolved through
/// <see cref="EntryPoints.OperatingLimitations"/>, and for the last through the evaluator: each
/// of the limitations it conjoins
/// failing on its own, every limitation the engine resolves met, each of the two people the
/// introductory text names, and the waiver gate in both directions (the entry's note, and
/// rules-factory decision 0021).
/// </summary>
/// <remarks>
/// <para>
/// The entry states no limitation of its own. It conjoins <c>speed-within-limit</c> (§ 107.51(a)),
/// <c>altitude-within-limit</c> (§ 107.51(b)) and <c>weather-minimums-met</c> (§ 107.51(c)-(d)),
/// and binds two people to all of them. So no figure of § 107.51 is written here as one expected
/// to come from this entry: where a constituent's account is checked it is resolved through that
/// constituent's own entry point and compared, and the figures in the situations below are the
/// facts a caller states about one operation.
/// </para>
/// <para>
/// One of the note's cases cannot be reached, and it is not reachable by writing a different test.
/// "All limitations met" needs <c>weather-minimums-met</c> to resolve a limitation met, and that
/// entry resolves exactly one outcome — the minimums <em>not</em> met, where neither cloud minimum
/// is met — declining everywhere else because § 107.51(c) turns on which objects are "prominent",
/// which the map holds open. So the nearest situation this engine can be put in is every
/// limitation it resolves met with the weather half undetermined, and the answer there is a
/// decline, pinned below. What this entry must not do is invent the missing half, and what it must
/// not do either is assert that the half is missing: the verdict below is computed from what the
/// constituents answered at runtime, so if that entry ever resolves a limitation met, this one
/// follows it.
/// </para>
/// </remarks>
public class OperatingLimitationsEntryPointTests
{
    private const string Caller = nameof(OperatingLimitationsEntryPointTests);

    private static readonly WaiverStatement NoWaiver = WaiverStatement.NoneHeld("§ 107.51", Caller);

    private static readonly StructureStatement NoStructure = StructureStatement.NoneWithinRadius(Caller);

    /// <summary>
    /// The operation the caller states, with every fact defaulting to one the engine can resolve a
    /// limitation met on: a groundspeed below either printed figure, an altitude below the ceiling
    /// with no structure claimed, and a cloud far enough away that both of § 107.51(d)'s minimums
    /// are met — which is where <c>weather-minimums-met</c> declines.
    /// </summary>
    private static Resolution<object> Resolve(
        Groundspeed? groundspeed = null,
        decimal altitudeFeet = 200m,
        decimal visibility = 10m,
        decimal below = 1000m,
        decimal horizontal = 5000m,
        BoundPerson? person = null,
        StructureStatement? structure = null,
        CloudStatement? cloud = null,
        WaiverStatement? waiver = null) =>
        EntryPoints.OperatingLimitations.Resolve(new OperatingLimitationsRequest
        {
            Person = person ?? BoundPerson.RemotePilotInCommand,
            Groundspeed = groundspeed ?? Groundspeed.InKnots(50m),
            AltitudeAboveGroundLevelFeet = altitudeFeet,
            Structure = structure ?? NoStructure,
            FlightVisibilityStatuteMiles = visibility,
            Cloud = cloud ?? CloudStatement.Measured(below, horizontal, Caller),
            Waiver = waiver ?? NoWaiver,
        });

    /// <summary>
    /// A request with every input set but the ones passed false, so that what is missing is the
    /// only thing between it and an answer.
    /// </summary>
    private static OperatingLimitationsRequest Incomplete(
        bool person = true,
        bool groundspeed = true,
        bool altitude = true,
        bool structure = true,
        bool visibility = true,
        bool cloud = true,
        bool waiver = true) =>
        new()
        {
            Person = person ? BoundPerson.RemotePilotInCommand : null,
            Groundspeed = groundspeed ? Groundspeed.InKnots(50m) : null,
            AltitudeAboveGroundLevelFeet = altitude ? 200m : null,
            Structure = structure ? NoStructure : null,
            FlightVisibilityStatuteMiles = visibility ? 10m : null,
            Cloud = cloud ? CloudStatement.Measured(1000m, 5000m, Caller) : null,
            Waiver = waiver ? NoWaiver : null,
        };

    private static OperatingLimitationsFinding Finding(Resolution<object> resolution) =>
        Assert.IsType<OperatingLimitationsFinding>(Assert.IsType<Resolution<object>.Resolved>(resolution).Value);

    private static UnresolvedResult Declined(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    private static LimitationOutcome Limitation(OperatingLimitationsFinding finding, string entryId) =>
        finding.Limitations.Single(limitation => limitation.Entry.Id == entryId);

    /// <summary>
    /// Every limitation as the entry that states it answered it — that entry's id, its citation, its
    /// verdict and its account — and the conjunction read off them. § 107.51's introductory text
    /// joins its two people by "and", so this is the one obligation both of them are under, and no
    /// part of it may turn on which of them asked.
    /// </summary>
    /// <remarks>
    /// It is not the whole finding, and the omissions are deliberate rather than an oversight:
    /// <see cref="OperatingLimitationsFinding.Person"/> is the thing being varied and so cannot be
    /// compared, and <c>Waiver</c> and <c>Authority</c> are the same on both sides by construction —
    /// one statement about one regulation, and this entry's own locator.
    /// <see cref="LimitationOutcome.Reason"/> is omitted too, and that omission is a real gap rather
    /// than a safe one: where a constituent declines, <c>Reason</c> is populated and is not derivable
    /// from the verdict or the account, so a defect that made a constituent's reason turn on which
    /// person asked while leaving its account alone would pass this comparison unreddened.
    /// </remarks>
    private static IEnumerable<string> Answer(OperatingLimitationsFinding finding) =>
        finding.Limitations
            .Select(limitation =>
                $"'{limitation.Entry.Id}' [{limitation.Entry.Locator.Citation}] {limitation.Verdict}: {limitation.Account}")
            .Append($"complied with all of them: {finding.CompliedWith}");

    /// <summary>
    /// One situation per limitation the introductory text conjoins, each with exactly that
    /// limitation resolved broken: a groundspeed beyond both printed figures; an altitude above the
    /// ceiling with no structure claimed; and a cloud close enough that neither of § 107.51(d)'s
    /// minimums is met, which is the one situation <c>weather-minimums-met</c> resolves.
    /// </summary>
    public static TheoryData<string, decimal, decimal, decimal, decimal> EachLimitationBroken => new()
    {
        { "speed-within-limit", 120m, 200m, 1000m, 5000m },
        { "altitude-within-limit", 50m, 1000m, 1000m, 5000m },
        { "weather-minimums-met", 50m, 200m, 100m, 1000m },
    };

    [Theory]
    [MemberData(nameof(EachLimitationBroken))]
    public void Each_limitation_the_engine_resolves_broken_makes_the_whole_not_complied_with_citing_107_51_introductory_text(
        string brokenEntryId,
        decimal knots,
        decimal altitudeFeet,
        decimal below,
        decimal horizontal)
    {
        var finding = Finding(Resolve(Groundspeed.InKnots(knots), altitudeFeet, below: below, horizontal: horizontal));

        Assert.False(finding.CompliedWith);
        Assert.False(Limitation(finding, brokenEntryId).Met);
        Assert.Equal("not met", Limitation(finding, brokenEntryId).Verdict);

        // Exactly that one is broken: the others are met or, where the map holds their question
        // open, undetermined — and an undetermined one does not stop "all of the following" being
        // settled by one that is broken.
        Assert.All(
            finding.Limitations.Where(limitation => limitation.Entry.Id != brokenEntryId),
            limitation => Assert.NotEqual(false, limitation.Met));

        // The three limitations are the entry's dependsOn, in its order, and no other.
        Assert.Equal(
            new[] { "speed-within-limit", "altitude-within-limit", "weather-minimums-met" },
            finding.Limitations.Select(limitation => limitation.Entry.Id));

        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.51 introductory text", finding.Authority.Citation);
        Assert.Equal(EntryPoints.OperatingLimitations.Registered.Locator, finding.Authority);
        Assert.Same(NoWaiver, finding.Waiver);
    }

    [Fact]
    public void The_verdicts_and_the_accounts_are_the_constituent_entries_own_and_are_not_restated_here()
    {
        var finding = Finding(Resolve(Groundspeed.InKnots(120m), 1000m));

        var speed = Assert.IsType<GroundspeedFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.SpeedWithinLimit.Resolve(
                new SpeedWithinLimitRequest { Groundspeed = Groundspeed.InKnots(120m), Waiver = NoWaiver })).Value);
        var altitude = Assert.IsType<AltitudeFinding>(Assert.IsType<Resolution<object>.Resolved>(
            EntryPoints.AltitudeWithinLimit.Resolve(
                new AltitudeWithinLimitRequest
                {
                    AltitudeAboveGroundLevelFeet = 1000m,
                    Structure = NoStructure,
                    Waiver = NoWaiver,
                })).Value);

        Assert.Equal(speed.WithinLimit, Limitation(finding, "speed-within-limit").Met);
        Assert.Equal(speed.ToString(), Limitation(finding, "speed-within-limit").Account);
        Assert.Equal(altitude.WithinLimit, Limitation(finding, "altitude-within-limit").Met);
        Assert.Equal(altitude.ToString(), Limitation(finding, "altitude-within-limit").Account);

        // And each limitation cites the entry that states it, not this entry.
        Assert.Equal(EntryPoints.SpeedWithinLimit.Registered.Locator, Limitation(finding, "speed-within-limit").Entry.Locator);
        Assert.Equal(EntryPoints.AltitudeWithinLimit.Registered.Locator, Limitation(finding, "altitude-within-limit").Entry.Locator);
        Assert.Equal(EntryPoints.WeatherMinimumsMet.Registered.Locator, Limitation(finding, "weather-minimums-met").Entry.Locator);
    }

    [Fact]
    public void Every_limitation_the_engine_resolves_met_with_one_undetermined_declines_naming_this_entry_and_the_constituent()
    {
        // Speed and altitude within their limits; the cloud far enough away that weather-minimums-met
        // reaches § 107.51(c) and declines. Nothing is broken, so nothing settles the conjunction.
        var one = Declined(Resolve());

        Assert.Equal(UnresolvedReason.RequiresInterpretation, one.Reason);
        Assert.Contains("operating-limitations", one.Attempted, StringComparison.Ordinal);
        Assert.Contains("weather-minimums-met", one.Attempted, StringComparison.Ordinal);
        Assert.Contains("a remote pilot in command", one.Attempted, StringComparison.Ordinal);
        Assert.Equal(EntryPoints.WeatherMinimumsMet.Registered.Locator, one.Locator);
        Assert.Equal("§ 107.51(c)-(d)", one.Locator.Citation);
        Assert.DoesNotContain("speed-within-limit", one.Attempted, StringComparison.Ordinal);

        // A groundspeed above 100 miles per hour and at or below 87 knots leaves speed-within-limit
        // undetermined too (decision 0001). Both are then named, and the decline cites the first in
        // this entry's dependsOn order.
        var two = Declined(Resolve(Groundspeed.InMilesPerHour(100.05m)));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, two.Reason);
        Assert.Contains("operating-limitations", two.Attempted, StringComparison.Ordinal);
        Assert.Contains("speed-within-limit", two.Attempted, StringComparison.Ordinal);
        Assert.Contains("weather-minimums-met", two.Attempted, StringComparison.Ordinal);
        Assert.Equal(EntryPoints.SpeedWithinLimit.Registered.Locator, two.Locator);
        Assert.Equal("§ 107.51(a)", two.Locator.Citation);

        // And each of the two is quoted with the account it recorded itself, not the blocking one's
        // twice over: a composite that attributed one constituent's words to another would be
        // telling a caller something no entry said (#103).
        var speed = Declined(EntryPoints.SpeedWithinLimit.Resolve(new SpeedWithinLimitRequest
        {
            Groundspeed = Groundspeed.InMilesPerHour(100.05m),
            Waiver = NoWaiver,
        }));
        var weather = Declined(EntryPoints.WeatherMinimumsMet.Resolve(new WeatherMinimumsMetRequest
        {
            FlightVisibilityStatuteMiles = 10m,
            Cloud = CloudStatement.Measured(1000m, 5000m, Caller),
            Waiver = NoWaiver,
        }));

        Assert.Contains(speed.Attempted, two.Attempted, StringComparison.Ordinal);
        Assert.Contains(weather.Attempted, two.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_this_entrys_own_and_not_the_constituents_handed_back()
    {
        var mine = Declined(Resolve());
        var constituent = Declined(EntryPoints.WeatherMinimumsMet.Resolve(new WeatherMinimumsMetRequest
        {
            FlightVisibilityStatuteMiles = 10m,
            Cloud = CloudStatement.Measured(1000m, 5000m, Caller),
            Waiver = NoWaiver,
        }));

        // Not the same result, and not the same subject: the caller asked about the operating
        // limitations and is told so, by name.
        Assert.NotEqual(constituent, mine);
        Assert.NotEqual(constituent.Attempted, mine.Attempted);
        Assert.Contains("operating-limitations", mine.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("operating-limitations", constituent.Attempted, StringComparison.Ordinal);

        // And what the constituent recorded is carried, whole and word for word, inside this
        // entry's own account of what it attempted: the openness originates in prominent-objects,
        // three levels down, and a caller who asked about § 107.51's introductory text can see that
        // without holding the map (docs/decisions/0006, #103). Containing the constituent's account
        // is not being the constituent's result — the two assertions above still hold.
        Assert.Contains("prominent-objects", constituent.Attempted, StringComparison.Ordinal);
        Assert.Contains("prominent-objects", mine.Attempted, StringComparison.Ordinal);
        Assert.Contains(constituent.Attempted, mine.Attempted, StringComparison.Ordinal);
        Assert.Contains("what 'weather-minimums-met' recorded: ", mine.Attempted, StringComparison.Ordinal);

        // The quotation is one hop and the citation does not follow it down. weather-minimums-met
        // declines on prominent-objects' § 107.51(c); this entry declines on weather-minimums-met's
        // own § 107.51(c)-(d), which is the entry its dependsOn names and the entry it asked.
        Assert.NotEqual(constituent.Locator, mine.Locator);
        Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, constituent.Locator);
        Assert.Equal(EntryPoints.WeatherMinimumsMet.Registered.Locator, mine.Locator);
    }

    /// <summary>
    /// The operation issue #79 names: a remote pilot in command, 50 knots, 200 feet above ground
    /// level, no structure claimed, 10 statute miles of stated visibility, no waiver — and clear
    /// air. Put to the constituent and to this entry, and answered the same way by both.
    /// </summary>
    /// <remarks>
    /// This is where the defect surfaced. With two bare distances the only thing such a caller
    /// could state was zero feet below and zero feet horizontally from a cloud, which is the
    /// aircraft at the cloud: <c>weather-minimums-met</c> resolved the minimums not met, and this
    /// entry resolved that § 107.51 was not complied with, on the most ordinary weather there is.
    /// Both now decline, and the decline is the honest answer — § 107.51(d) has no measured
    /// distance and § 107.51(c)'s quantity is <c>prominent-objects</c>', which the map holds open.
    /// </remarks>
    [Fact]
    public void An_ordinary_clear_air_operation_is_not_reported_as_breaking_the_operating_limitations()
    {
        var clearAir = CloudStatement.NoCloud(Caller);

        // The constituent, asked directly.
        var weather = Declined(EntryPoints.WeatherMinimumsMet.Resolve(new WeatherMinimumsMetRequest
        {
            FlightVisibilityStatuteMiles = 10m,
            Cloud = clearAir,
            Waiver = NoWaiver,
        }));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, weather.Reason);
        Assert.Equal(EntryPoints.ProminentObjects.Registered.Locator, weather.Locator);
        Assert.Contains("not operated near a cloud", weather.Attempted, StringComparison.Ordinal);

        // And this entry, on the same operation: undetermined, not "not complied with".
        var limitations = Declined(Resolve(cloud: clearAir));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, limitations.Reason);
        Assert.Equal(EntryPoints.WeatherMinimumsMet.Registered.Locator, limitations.Locator);
        Assert.Contains("operating-limitations", limitations.Attempted, StringComparison.Ordinal);
        Assert.Contains("weather-minimums-met", limitations.Attempted, StringComparison.Ordinal);
        Assert.Contains(
            "no limitation this engine resolved is broken",
            limitations.Attempted,
            StringComparison.Ordinal);

        // § 107.51(a) and (b) are answered on this operation and neither is broken, so neither is
        // named among the entries that did not resolve.
        Assert.DoesNotContain("speed-within-limit", limitations.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("altitude-within-limit", limitations.Attempted, StringComparison.Ordinal);

        // The same operation stated as a measured zero is a different operation — the aircraft at
        // the cloud — and that one still resolves, not complied with. Clear air is no longer it.
        var atTheCloud = Finding(Resolve(cloud: CloudStatement.Measured(0m, 0m, Caller)));

        Assert.False(atTheCloud.CompliedWith);
        Assert.False(Limitation(atTheCloud, "weather-minimums-met").Met);
    }

    [Theory]
    [MemberData(nameof(BothPersons))]
    public void Both_persons_the_introductory_text_names_are_bound_and_are_answered_alike(BoundPerson person)
    {
        var finding = Finding(Resolve(Groundspeed.InKnots(120m), person: person));
        var unresolved = Declined(Resolve(person: person));

        // The person is recorded, and it is the one the caller stated.
        Assert.Same(person, finding.Person);
        Assert.Contains(person.Designation, finding.ToString(), StringComparison.Ordinal);
        Assert.Contains(person.Designation, unresolved.Attempted, StringComparison.Ordinal);

        // And the obligation does not turn on which of them asks: same facts, same answer. What is
        // compared is the whole of that answer — every limitation as the entry that states it
        // answered it, and the conjunction read off them — and not CompliedWith on its own. That
        // property is false for every operation this engine can resolve, because
        // weather-minimums-met resolves one outcome only and it is the minimums not met; comparing
        // it across the two people compares one constant with another, which any implementation
        // answering false for everything satisfies. The accounts below are not constant: they are
        // what each constituent said about the operation the caller stated.
        Assert.False(finding.CompliedWith);
        Assert.Equal(
            Answer(Finding(Resolve(Groundspeed.InKnots(120m), person: BoundPerson.RemotePilotInCommand))),
            Answer(finding));
    }

    /// <summary>The two people § 107.51's introductory text names, and there is no third.</summary>
    public static TheoryData<BoundPerson> BothPersons
    {
        get
        {
            var data = new TheoryData<BoundPerson>();
            foreach (var person in BoundPerson.All)
            {
                data.Add(person);
            }

            return data;
        }
    }

    [Fact]
    public void Both_persons_are_every_person_the_introductory_text_names() =>
        Assert.Equal(
            new[] { "a remote pilot in command", "the person manipulating the flight controls of the small unmanned aircraft system" },
            BoundPerson.All.Select(person => person.Designation));

    [Fact]
    public void While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_naming_this_entry()
    {
        var waiver = WaiverStatement.Held("§ 107.51", Caller);

        var unresolved = Declined(Resolve(Groundspeed.InKnots(120m), 1000m, waiver: waiver));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.205", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.WaivableRegulations.Registered.Locator, unresolved.Locator);

        // This entry's own gate, run before any constituent is asked: the decline names
        // operating-limitations and not whichever constituent would have been asked first.
        Assert.Contains("operating-limitations", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("speed-within-limit", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains($"in force, as stated by {Caller}", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void Stated_that_no_waiver_is_in_force_the_rule_is_evaluated_and_the_statement_recorded()
    {
        var waiver = WaiverStatement.NoneHeld("§ 107.51", "the person manipulating the flight controls");

        var finding = Finding(Resolve(Groundspeed.InKnots(120m), waiver: waiver));

        Assert.False(finding.CompliedWith);
        Assert.Same(waiver, finding.Waiver);
        Assert.False(finding.Waiver.InForce);
        Assert.Equal("the person manipulating the flight controls", finding.Waiver.StatedBy);
    }

    [Fact]
    public void A_waiver_statement_about_another_regulation_is_refused()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Resolve(waiver: WaiverStatement.NoneHeld("§ 107.37(a)", Caller)));

        Assert.Contains("operating-limitations", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_the_facts_the_limitations_are_tested_against_it_refuses_rather_than_assume_them()
    {
        // Every input, one at a time: a complete request with exactly that one left unset.
        Assert.All(
            new (string Name, OperatingLimitationsRequest Request)[]
            {
                (nameof(OperatingLimitationsRequest.Person), Incomplete(person: false)),
                (nameof(OperatingLimitationsRequest.Groundspeed), Incomplete(groundspeed: false)),
                (nameof(OperatingLimitationsRequest.AltitudeAboveGroundLevelFeet), Incomplete(altitude: false)),
                (nameof(OperatingLimitationsRequest.Structure), Incomplete(structure: false)),
                (nameof(OperatingLimitationsRequest.FlightVisibilityStatuteMiles), Incomplete(visibility: false)),
                (nameof(OperatingLimitationsRequest.Cloud), Incomplete(cloud: false)),
                (nameof(OperatingLimitationsRequest.Waiver), Incomplete(waiver: false)),
            },
            missing => Assert.Equal(
                missing.Name,
                Assert.Throws<ArgumentException>(
                    () => EntryPoints.OperatingLimitations.Resolve(missing.Request)).ParamName));
    }

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_answering_from_defaults()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Registry.Resolve("operating-limitations", RuleRequest.Empty));

        // The waiver statement first, because the gate reads it and the gate comes first
        // (docs/decisions/0008); the six operational facts are owed next, once a statement that no
        // waiver is in force has put the entry back in reach.
        Assert.Equal(nameof(OperatingLimitationsRequest.Waiver), error.ParamName);
        Assert.Equal(
            nameof(OperatingLimitationsRequest.Person),
            Assert.Throws<ArgumentException>(() => EntryPoints.OperatingLimitations.Resolve(
                new OperatingLimitationsRequest(RuleRequest.Empty) { Waiver = NoWaiver })).ParamName);
    }

    /// <summary>
    /// Two resolutions of one request say the same thing, so they are the same finding: compared by
    /// what they say, and never by the identity of the list of limitations carrying it
    /// (<c>AGENTS.md</c> §8).
    /// </summary>
    /// <remarks>
    /// The two resolutions share no object equality could hold by identity on — each carries its own
    /// waiver statement, and the entry builds each finding its own list of limitations, which the
    /// <c>NotSame</c> assertions pin so that the comparison cannot pass by accident.
    /// </remarks>
    [Fact]
    public void Two_resolutions_of_one_request_are_equal_and_hash_alike_with_the_limitations_compared_by_element()
    {
        var first = Finding(Resolve(
            Groundspeed.InKnots(120m), 1000m, waiver: WaiverStatement.NoneHeld("§ 107.51", Caller)));
        var second = Finding(Resolve(
            Groundspeed.InKnots(120m), 1000m, waiver: WaiverStatement.NoneHeld("§ 107.51", Caller)));

        Assert.NotSame(first, second);
        Assert.NotSame(first.Limitations, second.Limitations);
        Assert.NotSame(first.Waiver, second.Waiver);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        // Equality still says what it is for: a finding about the other person, and one whose
        // limitations were answered differently, are each unequal to the first.
        Assert.NotEqual(
            first,
            Finding(Resolve(
                Groundspeed.InKnots(120m), 1000m, person: BoundPerson.PersonManipulatingTheFlightControls)));
        Assert.NotEqual(first, Finding(Resolve(Groundspeed.InKnots(120m), 200m)));
    }

    /// <summary>
    /// An operation the evaluator can reach this entry on, and nothing more than that: a
    /// groundspeed beyond both printed figures so § 107.51(a) resolves not met, an altitude and a
    /// structure statement § 107.51(b) resolves on, a cloud meeting one of § 107.51(d)'s two
    /// minimums and not the other so <c>weather-minimums-met</c> declines, the person the
    /// introductory text binds, the stated flight visibility, and a statement that no waiver of
    /// § 107.51 is in force.
    /// </summary>
    private static OperationFacts Operation(decimal flightVisibilityStatuteMiles) => new OperationFacts
    {
        BoundPerson = BoundPerson.RemotePilotInCommand,
        Groundspeed = Groundspeed.InKnots(200m),
        AltitudeAboveGroundLevelFeet = 300m,
        Structure = NoStructure,
        FlightVisibilityStatuteMiles = flightVisibilityStatuteMiles,
        Cloud = CloudStatement.Measured(500m, 100m, Caller),
    }
        .Stating(WaiverStatement.NoneHeld(Compliance.Regulation, Caller));

    /// <summary>This entry's outcome, as a product layer receives it.</summary>
    private static EvaluatedRequirement Outcome(decimal flightVisibilityStatuteMiles) =>
        OperationEvaluator.Evaluate(Operation(flightVisibilityStatuteMiles))
            .Requirement(MapEntries.OperatingLimitations.Id);

    /// <summary>
    /// The same thing <c>weather-minimums-met</c>'s last test pins, one level up and through a
    /// collection. A constituent's <see cref="LimitationOutcome.Account"/> records what that
    /// constituent said; <see cref="LimitationOutcome.ToString"/> prints only its id, its locator
    /// and its verdict, and this finding's own <c>ToString</c> prints those. So two operations
    /// differing only in the stated flight visibility — which reaches
    /// <c>weather-minimums-met</c>'s decline and nothing else here — are one set of words and two
    /// findings, and they are two outcomes.
    /// </summary>
    /// <remarks>
    /// This is the case an enumeration of "the one finding that does this" missed, and it is why
    /// the claim is now stated as a shape rather than as a list: the difference is not on the
    /// finding's own fields at all, it is on the element type of
    /// <see cref="OperatingLimitationsFinding.Limitations"/>. Comparing the rendering could not
    /// reach it, and comparing the finding does — through
    /// <see cref="OperatingLimitationsFinding.Equals(OperatingLimitationsFinding)"/>'s
    /// element-by-element comparison, which is what makes the two halves of this fit together.
    /// </remarks>
    [Fact]
    public void Two_operations_differing_only_in_a_constituents_recorded_account_are_different_outcomes()
    {
        var atFive = Outcome(5m);
        var atFour = Outcome(4m);

        var findingAtFive = Assert.IsType<OperatingLimitationsFinding>(atFive.Finding);
        var findingAtFour = Assert.IsType<OperatingLimitationsFinding>(atFour.Finding);

        // The weather limitation is the one that carries the stated figure, and it is undetermined
        // here: found by entry id rather than by index, so a reordered conjunction still finds it.
        var weatherAtFive = Limitation(findingAtFive, MapEntries.WeatherMinimumsMet.Id);
        var weatherAtFour = Limitation(findingAtFour, MapEntries.WeatherMinimumsMet.Id);

        Assert.Null(weatherAtFive.Met);
        Assert.Contains("5 statute miles", weatherAtFive.Account, StringComparison.Ordinal);
        Assert.Contains("4 statute miles", weatherAtFour.Account, StringComparison.Ordinal);
        Assert.Equal(weatherAtFive with { Account = weatherAtFour.Account }, weatherAtFour);

        // And nothing the engine prints about either operation differs: not the constituent's own
        // rendering, not the finding's, not the outcome's explanation.
        Assert.Equal(weatherAtFive.ToString(), weatherAtFour.ToString());
        Assert.Equal(findingAtFive.ToString(), findingAtFour.ToString());
        Assert.Equal(atFive.Explanation, atFour.Explanation);

        Assert.NotEqual(findingAtFive, findingAtFour);
        Assert.NotEqual(atFive, atFour);
    }
}
