using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The one finding § 107.51(c)-(d) lets this engine resolve:
/// <see cref="MapEntries.WeatherMinimumsMet"/> is not met, because neither of § 107.51(d)'s two
/// printed minimums is met.
/// </summary>
/// <remarks>
/// The entry is a conjunction — "(c) The minimum flight visibility … must be no less than 3 statute
/// miles" and "(d) The minimum distance … from clouds must be no less than: (1) 500 feet below the
/// cloud; and (2) 2,000 feet horizontally from the cloud" — so one conjunct that is false on every
/// reading settles it, whatever the other is. Neither cloud minimum met is that case: read
/// literally both must hold, read as a region around the cloud either suffices, and neither holds,
/// so <c>cloud-clearance</c>'s open question does not have to be answered to know § 107.51(d) is
/// not met. Every other situation leaves the conjunction undetermined — § 107.51(c)'s quantity is
/// held open by <c>prominent-objects</c>, and a statement naming no cloud leaves § 107.51(d)
/// unsettled besides — and <see cref="Weather.MinimumsMet"/> declines instead of constructing this.
/// </remarks>
/// <param name="FlightVisibilityStatuteMiles">The flight visibility the caller stated, in statute miles, recorded with the outcome.</param>
/// <param name="BelowCloudMinimumMet">
/// Paragraph (d)(1): true while the aircraft is no less than <c>cloud-clearance</c>'s printed
/// distance below the cloud. "No less than" is met at the figure and broken below it.
/// </param>
/// <param name="HorizontallyFromCloudMinimumMet">
/// Paragraph (d)(2): true while the aircraft is no less than <c>cloud-clearance</c>'s printed
/// distance horizontally from the cloud. "No less than" is met at the figure and broken below it.
/// </param>
/// <param name="Cloud">
/// What the caller stated about the cloud, recorded with the outcome. A finding is made only from a
/// statement that names one: where the caller states that the aircraft is not operated near a
/// cloud there is no measured distance for either minimum to be compared with, and
/// <see cref="Weather.MinimumsMet"/> declines rather than making a finding — so neither minimum is
/// ever reported unmet for want of a cloud to measure from. The <b>constructor</b> holds that, and
/// not the rule's discipline (<c>#101</c>).
/// </param>
/// <param name="Minimum">§ 107.51(c)'s figure, as <c>visibility-minimum</c> states it.</param>
/// <param name="Clearance">§ 107.51(d)'s two figures, as <c>cloud-clearance</c> states them, and the waiver statement this was resolved under.</param>
public sealed record WeatherMinimumsFinding(
    decimal FlightVisibilityStatuteMiles,
    bool BelowCloudMinimumMet,
    bool HorizontallyFromCloudMinimumMet,
    CloudStatement Cloud,
    VisibilityMinimum Minimum,
    CloudClearance Clearance)
{
    /// <summary>
    /// What the caller stated about the cloud, checked to name one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// § 107.51(d)'s two minimums are distances <em>from a cloud</em>, and
    /// <see cref="BelowCloudMinimumMet"/> and <see cref="HorizontallyFromCloudMinimumMet"/> report
    /// each of them as met or not met. A statement that the aircraft is not operated near a cloud
    /// measured neither distance, so there is no value of either that this finding could honestly
    /// carry: false says the minimum is broken, which is the stated zero this engine refuses to read
    /// an absence as, and true says § 107.51(d) is met with no cloud, which is the question the map
    /// does not settle and <see cref="Weather.MinimumsMet"/> declines rather than deciding. So the
    /// whole finding is refused, which is the same answer the rule gives, made unavoidable
    /// (<c>#101</c>).
    /// </para>
    /// <para>
    /// This is the check <see cref="AltitudeFinding.Structure"/> makes on the same shape one
    /// paragraph up, and the two differ only where § 107.51(b) and § 107.51(d) differ: there, a
    /// statement naming no structure leaves the exception unsatisfied, which is an answer, so the
    /// finding stands with both of its structure members false.
    /// </para>
    /// </remarks>
    public CloudStatement Cloud { get; } = CheckNamesACloud(Cloud, nameof(Cloud));

    /// <summary>
    /// Whether § 107.51(c) and § 107.51(d) are both met: false, in every finding this engine can
    /// resolve. A finding is constructed only where neither cloud minimum is met, which makes the
    /// conjunction false on every reading; the engine can never resolve it true, because that would
    /// need § 107.51(c) met, and the quantity § 107.51(c)'s figure is compared against is
    /// <c>prominent-objects</c>' open question.
    /// </summary>
    public bool MinimumsMet => false;

    /// <summary>Where the rule is stated: <c>§ 107.51(c)-(d)</c>.</summary>
    public SourceLocator Authority => MapEntries.WeatherMinimumsMet.Locator;

    /// <summary>The caller's waiver statement, recorded with the outcome.</summary>
    public WaiverStatement Waiver => Clearance.Waiver;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"the minimums are not met: {Cloud.FeetBelowCloud} feet below the cloud is less than {Clearance.BelowCloudFeet} and "
            + $"{Cloud.FeetHorizontallyFromCloud} feet horizontally from it is less than {Clearance.HorizontallyFromCloudFeet} "
            + $"[{Authority}]; {Cloud}; {Waiver}");

    private static CloudStatement CheckNamesACloud(CloudStatement cloud, string name)
    {
        ArgumentNullException.ThrowIfNull(cloud, name);
        return cloud.NamesACloud
            ? cloud
            : throw new ArgumentException(
                "§ 107.51(d)'s two minimums are distances from a cloud, so a finding is made only "
                + "from a statement that names one: a statement that the small unmanned aircraft is "
                + "not operated near a cloud measured neither distance, and this engine neither "
                + "reads that as both minimums broken nor decides that § 107.51(d) is met",
                name);
    }
}

/// <summary>
/// § 107.51(c)-(d): whether the visibility and cloud-clearance minimums are met,
/// <see cref="MapEntries.WeatherMinimumsMet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entry states no figure of its own. The 3 statute miles are <c>visibility-minimum</c>'s and
/// the 500 and 2,000 feet are <c>cloud-clearance</c>'s, reached through <c>dependsOn</c> and read
/// from those entries' values, so a figure that moved there moves here.
/// </para>
/// <para>
/// Its third dependency, <c>prominent-objects</c>, is asked wherever § 107.51(c) is reached, and
/// what it answers on the request in hand is what decides: nothing here records in advance that it
/// declines. That is not an accident of build order, and this entry is where it bites.
/// <c>visibility-minimum</c>'s note says the gap is "carried by the entry that applies the
/// definition rather than by the one that states the figure", and this entry is the one that
/// applies it: § 107.51(c)'s threshold is on "flight visibility", which the section defines as the
/// average slant distance at which <em>prominent</em> objects may be seen and identified, and the
/// corpus fixes no degree of prominence. So a distance the caller states is a flight visibility
/// only once that degree is fixed, and comparing the stated figure with 3 statute miles and
/// answering met or not met would be this engine fixing it.
/// </para>
/// <para>
/// This entry does not borrow <c>prominent-objects</c>' decline either, the way
/// <see cref="NightTraining"/> does not borrow <c>knowledge-recency</c>'s. It answers on its own
/// terms, and its own terms are a conjunction: one conjunct false on every reading settles the
/// whole of it. Neither cloud minimum met is exactly that, and the entry resolves it — which is why
/// the map split <c>prominent-objects</c> out instead of reclassifying this entry, "classifying the
/// whole entry by one of its clauses would have thrown away the 500-foot and 2,000-foot figures".
/// Everywhere else a cloud is named, § 107.51(c) is reached and the answer is undetermined, so the
/// decline is this entry's own: it names the entry the caller asked about and the entry whose
/// question blocks it, carries that entry's reason, cites that entry's § 107.51(c), and quotes what
/// that entry itself recorded (<c>docs/decisions/0006</c>).
/// </para>
/// <para>
/// <b>The cloud is a <see cref="CloudStatement"/>, and no cloud is a case the caller states.</b>
/// § 107.51(d)'s two minimums are distances from a cloud, so an operation with no cloud has no
/// distance to state — and two bare numbers gave such a caller only zero, which is a measurement:
/// the aircraft at the cloud, breaking both minimums. That is how this engine came to answer "the
/// weather minimums are not met" to an ordinary clear-air flight. The second case is now stated
/// rather than encoded, on the model of <see cref="EncounteredObject.NoneOfThem"/> and
/// <see cref="StructureStatement.NoneWithinRadius"/>, and it is a decline: what § 107.51(d)
/// requires of an operation with no cloud is a question the map does not settle, and this engine
/// does not answer it in either direction.
/// </para>
/// </remarks>
public static class Weather
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary>
    /// <see cref="MapEntries.WeatherMinimumsMet"/>: whether the operation the caller states meets
    /// "(c) The minimum flight visibility, as observed from the location of the control station must
    /// be no less than 3 statute miles" and "(d) The minimum distance of the small unmanned aircraft
    /// from clouds must be no less than: (1) 500 feet below the cloud; and (2) 2,000 feet
    /// horizontally from the cloud."
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolves only where the caller's statement names a cloud and neither cloud minimum is met:
    /// § 107.51(d) is then broken on either reading of how its two figures combine, so the
    /// conjunction is false whatever the flight visibility is, and § 107.51(c) is never reached.
    /// Every other statement that names a cloud reaches § 107.51(c) and declines, including one
    /// whose stated visibility is far above or far below the figure: the stated distance is not § 107.51(c)'s
    /// flight visibility until "prominent" is fixed, and this engine does not fix it. Where the
    /// cloud half is itself undetermined — one minimum met and not the other — the decline names
    /// <c>cloud-clearance</c>'s question as well.
    /// </para>
    /// <para>
    /// A statement that the aircraft is not operated near a cloud declines too, and for a reason of
    /// its own: § 107.51(d)'s minimums are distances from a cloud, and there is then no distance to
    /// compare either of them with. The engine does not read that as both minimums broken — which
    /// is what a stated zero is, and what an operation in clear air had no other way to say — and it
    /// does not read it as § 107.51(d) met either, because what that paragraph requires of an
    /// operation with no cloud is not something the map settles and not this engine's to decide
    /// (<c>AGENTS.md</c> §6). Declining is the whole of the answer: § 107.51(d) settles the
    /// conjunction neither way here, and § 107.51(c) is undetermined in any event, its quantity
    /// held open by <c>prominent-objects</c>. The outcome is not said to turn on § 107.51(c)
    /// alone, which would presume of § 107.51(d) the very thing this entry declines to decide.
    /// </para>
    /// </remarks>
    /// <param name="flightVisibilityStatuteMiles">The flight visibility the caller states, observed from the location of the control station, in statute miles, not negative.</param>
    /// <param name="cloud">What the caller states about the cloud § 107.51(d)'s minimums are distances from: the two distances, or that the aircraft is not operated near one.</param>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding, the minimums not met, where the statement names a cloud and neither cloud
    /// minimum is met; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver is in force; otherwise this entry's own decline, carrying the reason
    /// <c>prominent-objects</c> gave on this request and citing that entry's § 107.51(c).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="cloud"/> is null.</exception>
    public static Resolution<WeatherMinimumsFinding> MinimumsMet(
        decimal flightVisibilityStatuteMiles,
        CloudStatement cloud,
        WaiverStatement waiver)
    {
        ArgumentNullException.ThrowIfNull(cloud);
        ArgumentOutOfRangeException.ThrowIfNegative(flightVisibilityStatuteMiles);

        if (Waivers.Suspension(MapEntries.WeatherMinimumsMet, Regulation, waiver) is { } suspended)
        {
            return Resolution<WeatherMinimumsFinding>.FromUnresolved(suspended);
        }

        return Visibility.Minimum(waiver).Match(
            minimum => Clouds.Clearance(waiver).Match(
                clearance =>
                {
                    // The two minimums are compared with the distances the statement measured, and
                    // a statement that names no cloud measured none. Nothing here supplies one:
                    // there is no figure to compare, so there is no comparison, and the entry says
                    // so rather than answering from the absence.
                    if (cloud.FeetBelowCloud is not { } below || cloud.FeetHorizontallyFromCloud is not { } horizontal)
                    {
                        return Resolution<WeatherMinimumsFinding>.FromUnresolved(
                            NoCloudToMeasureFrom(flightVisibilityStatuteMiles, cloud, minimum));
                    }

                    var finding = new WeatherMinimumsFinding(
                        flightVisibilityStatuteMiles,
                        BelowCloudMinimumMet: below >= clearance.BelowCloudFeet,
                        HorizontallyFromCloudMinimumMet: horizontal >= clearance.HorizontallyFromCloudFeet,
                        cloud,
                        minimum,
                        clearance);

                    return !finding.BelowCloudMinimumMet && !finding.HorizontallyFromCloudMinimumMet
                        ? Resolution<WeatherMinimumsFinding>.FromValue(finding)
                        : Resolution<WeatherMinimumsFinding>.FromUnresolved(Undetermined(finding));
                },
                Resolution<WeatherMinimumsFinding>.FromUnresolved),
            Resolution<WeatherMinimumsFinding>.FromUnresolved);
    }

    /// <summary>
    /// The decline for an operation the caller states is not near a cloud: § 107.51(d) has no
    /// measured distance to reach and this engine does not decide what the paragraph then requires,
    /// so nothing there settles the outcome; and § 107.51(c) is undetermined in any event, its
    /// quantity still open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two halves are stated separately because neither carries the other. § 107.51(d) is not
    /// said to be met and is not said to be broken, so it settles nothing here — and that is
    /// exactly why § 107.51(c) has to be spoken to on its own: were the conjunction's cloud half
    /// merely left to one side, the outcome would look as though it turned on § 107.51(c) alone,
    /// which is more than this engine knows.
    /// </para>
    /// <para>
    /// <c>cloud-clearance</c>'s question is not named here, and deliberately: what that entry holds
    /// open is how § 107.51(d)'s two figures combine, and no measurement has been offered for
    /// either of them to combine over. The question this decline cites is § 107.51(c)'s, which is
    /// open on this operation as on any other.
    /// </para>
    /// </remarks>
    private static UnresolvedResult NoCloudToMeasureFrom(
        decimal flightVisibilityStatuteMiles,
        CloudStatement cloud,
        VisibilityMinimum minimum)
    {
        var prominence = AskProminentObjects(minimum.Waiver);

        return new UnresolvedResult(
            prominence.Reason ?? UnresolvedReason.RequiresInterpretation,
            string.Create(
                CultureInfo.InvariantCulture,
                $"decide whether the map entry '{MapEntries.WeatherMinimumsMet.Id}' is met on a stated flight "
                + $"visibility of {flightVisibilityStatuteMiles} statute miles: {cloud}, so § 107.51(d)'s \"minimum "
                + $"distance of the small unmanned aircraft from clouds\" has no measured distance on this operation "
                + $"and this engine does not decide what that paragraph requires of one, so § 107.51(d) does not "
                + $"settle the outcome here; and § 107.51(c) is undetermined in any event, because it requires no "
                + $"less than {minimum.StatuteMiles} statute miles of the flight "
                + $"visibility it defines, and {prominence}"),
            MapEntries.ProminentObjects.Locator);
    }

    /// <summary>
    /// The decline for a situation that reaches § 107.51(c): the visibility half is undetermined
    /// however the stated figure compares with the minimum, and the cloud half is named too when it
    /// is itself undetermined.
    /// </summary>
    private static UnresolvedResult Undetermined(WeatherMinimumsFinding finding)
    {
        var prominence = AskProminentObjects(finding.Waiver);

        var cloudHalf = finding.BelowCloudMinimumMet == finding.HorizontallyFromCloudMinimumMet
            ? string.Empty
            : string.Create(
                CultureInfo.InvariantCulture,
                $"; and {finding.Cloud.FeetBelowCloud} feet below the cloud with {finding.Cloud.FeetHorizontallyFromCloud} feet "
                + $"horizontally from it meets one of § 107.51(d)'s two minimums and not the other, which the map "
                + $"entry '{MapEntries.CloudClearance.Id}' holds open");

        return new UnresolvedResult(
            prominence.Reason ?? UnresolvedReason.RequiresInterpretation,
            string.Create(
                CultureInfo.InvariantCulture,
                $"decide whether the map entry '{MapEntries.WeatherMinimumsMet.Id}' is met on a stated flight "
                + $"visibility of {finding.FlightVisibilityStatuteMiles} statute miles: § 107.51(c) requires no less "
                + $"than {finding.Minimum.StatuteMiles} statute miles of the flight visibility it defines, "
                + $"and {prominence}{cloudHalf}"),
            MapEntries.ProminentObjects.Locator);
    }

    /// <summary>
    /// Asks <c>prominent-objects</c>, and records what it answered on this request.
    /// </summary>
    /// <remarks>
    /// It is asked only where § 107.51(c) is reached, which is everywhere this entry declines and
    /// nowhere it resolves: the one finding this entry makes is settled by § 107.51(d) alone, and
    /// putting the call here rather than in <see cref="MinimumsMet"/> keeps that so.
    /// </remarks>
    private static ProminenceOutcome AskProminentObjects(WaiverStatement waiver) =>
        Prominence.Objects(waiver).Match(
            value => new ProminenceOutcome(null, value.ToString() ?? string.Empty),
            unresolved => new ProminenceOutcome(unresolved.Reason, unresolved.Attempted));

    /// <summary>
    /// What <c>prominent-objects</c> answered on this request, as this entry records it: the reason
    /// it gave where it did not resolve, and its own account either way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The map gives <c>prominent-objects</c> no verdict type — it is <c>Resolution&lt;object&gt;</c>
    /// — so a resolved value is recorded as one this entry has no verdict to read rather than
    /// guessed at, which is the shape <c>over-human-beings</c> already gives a constituent with
    /// nothing to read. The map would have to give that entry a verdict before this one could read
    /// one, and nothing here assumes which way it answered.
    /// </para>
    /// <para>
    /// <see cref="Account"/> is that entry's own words, carried into this entry's decline rather
    /// than restated in this entry's: a caller who asked about the weather minimums is told where
    /// the openness originates, and is still told which entry it asked
    /// (<c>docs/decisions/0006</c>).
    /// </para>
    /// </remarks>
    /// <param name="Reason">The reason it gave, or null where it resolved.</param>
    /// <param name="Account">What it recorded: its outcome where it resolved, what it attempted where it did not.</param>
    private sealed record ProminenceOutcome(UnresolvedReason? Reason, string Account)
    {
        /// <summary>What that entry did with § 107.51(c)'s definition, in this decline's words.</summary>
        private string Answered => Reason is null
            ? "answered which objects are \"prominent\", the degree that fixes the distance the definition "
              + "reports, with a value this entry has no verdict to read"
            : "did not answer which objects are \"prominent\", the degree that fixes the distance the "
              + "definition reports";

        /// <inheritdoc/>
        public override string ToString() =>
            $"the map entry '{MapEntries.ProminentObjects.Id}' [{MapEntries.ProminentObjects.Locator.Citation}] "
            + $"{Answered}, so the stated figure is not yet that quantity; what "
            + $"'{MapEntries.ProminentObjects.Id}' recorded: {Account}";
    }
}
