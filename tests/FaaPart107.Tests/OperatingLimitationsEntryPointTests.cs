using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>operating-limitations</c>, § 107.51's introductory text, resolved through
/// <see cref="EntryPoints.OperatingLimitations"/> only: each of the limitations it conjoins
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

        // Nor is it the question three levels down. weather-minimums-met declines on
        // prominent-objects' § 107.51(c); this entry declines on weather-minimums-met's own
        // § 107.51(c)-(d), which is the entry it asked.
        Assert.Contains("prominent-objects", constituent.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("prominent-objects", mine.Attempted, StringComparison.Ordinal);
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

        // And the obligation does not turn on which of them asks: same facts, same answer.
        Assert.False(finding.CompliedWith);
        Assert.Equal(
            Finding(Resolve(Groundspeed.InKnots(120m), person: BoundPerson.RemotePilotInCommand)).CompliedWith,
            finding.CompliedWith);
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

        Assert.Equal(nameof(OperatingLimitationsRequest.Person), error.ParamName);
    }
}
