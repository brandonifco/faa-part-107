using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// One of the operating limitations § 107.51's introductory text conjoins, as the map entry that
/// states it answered it on this operation: met, not met, or undetermined.
/// </summary>
/// <remarks>
/// Every field here is the constituent entry's own. <see cref="Met"/> is the verdict that entry
/// resolved, never a verdict re-derived here, and <see cref="Account"/> is what that entry said —
/// its resolved outcome, or, where it did not resolve, what it recorded as attempted. So the
/// figures the limitations are tested against stay where the map puts them: none of them is
/// restated by <see cref="Compliance"/>, and one that moved in its own entry moves here.
/// </remarks>
/// <param name="Entry">The map entry that states this limitation, as the map has it.</param>
/// <param name="Met">
/// What that entry resolved: true where the limitation is met, false where it is not, and null
/// where that entry did not resolve it.
/// </param>
/// <param name="Account">
/// What that entry said: its outcome where it resolved, and what it recorded as attempted where it
/// did not.
/// </param>
/// <param name="Reason">
/// Why that entry did not resolve, as it gave it; null where it resolved.
/// </param>
public sealed record LimitationOutcome(MapEntry Entry, bool? Met, string Account, UnresolvedReason? Reason)
{
    /// <summary>This limitation's verdict in words: met, not met, or undetermined.</summary>
    public string Verdict => Met switch
    {
        true => "met",
        false => "not met",
        null => "undetermined",
    };

    /// <inheritdoc/>
    public override string ToString() => $"'{Entry.Id}' [{Entry.Locator.Citation}] {Verdict}";
}

/// <summary>
/// Whether § 107.51's introductory text is complied with:
/// <see cref="MapEntries.OperatingLimitations"/>, "A remote pilot in command and the person
/// manipulating the flight controls of the small unmanned aircraft system must comply with all of
/// the following operating limitations when operating a small unmanned aircraft system:".
/// </summary>
/// <remarks>
/// The introductory text states no limitation of its own. It conjoins the limitations its entry
/// lists in <c>dependsOn</c> and names who must comply with them, and this finding is exactly
/// that: the person the caller asked about, and each constituent entry's own verdict.
/// </remarks>
/// <param name="Person">Which of the two people the introductory text names the caller asked about.</param>
/// <param name="Limitations">
/// The limitations it conjoins, in the order this entry's <c>dependsOn</c> lists them, each as the
/// entry that states it answered it.
/// </param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record OperatingLimitationsFinding(
    BoundPerson Person,
    IReadOnlyList<LimitationOutcome> Limitations,
    WaiverStatement Waiver)
{
    /// <summary>
    /// Whether the person must comply with all of them and they are all complied with: true only
    /// where every constituent entry resolved its limitation met.
    /// </summary>
    /// <remarks>
    /// This is computed from what the constituents answered on this operation, and is not a
    /// constant of this entry. No situation this engine can resolve makes it true today, because
    /// <see cref="MapEntries.WeatherMinimumsMet"/> resolves one outcome only and that outcome is
    /// "not met"; that is a fact about that entry rather than about this one, and if it ever
    /// resolves a limitation met this property follows it with nothing changed here.
    /// </remarks>
    public bool CompliedWith => Limitations.All(limitation => limitation.Met == true);

    /// <summary>Where the rule is stated: <c>§ 107.51 introductory text</c>.</summary>
    public SourceLocator Authority => MapEntries.OperatingLimitations.Locator;

    /// <summary>Whether this finding is the same as <paramref name="other"/>, comparing the limitations by element.</summary>
    /// <param name="other">The other finding.</param>
    /// <returns>True when both are about the same person under the same waiver statement, and carry the same limitations answered the same way, in the same order.</returns>
    /// <remarks>
    /// A record's generated equality would compare <see cref="Limitations"/> with
    /// <c>EqualityComparer&lt;IReadOnlyList&lt;LimitationOutcome&gt;&gt;.Default</c>, which is the
    /// identity of the list object, so two resolutions of the same request would be unequal.
    /// Determinism is about what the engine says, so equality is by element (<c>AGENTS.md</c> §8) —
    /// the same reason <see cref="MultipleAircraftFinding.Equals(MultipleAircraftFinding)"/> gives
    /// for overriding its own.
    /// </remarks>
    public bool Equals(OperatingLimitationsFinding? other) =>
        other is not null
        && Person == other.Person
        && Waiver == other.Waiver
        && Limitations.SequenceEqual(other.Limitations);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Person);
        hash.Add(Waiver);
        foreach (var limitation in Limitations)
        {
            hash.Add(limitation);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Person} must comply with all of them, and they are {(CompliedWith ? "complied with" : "not complied with")} "
        + $"[{Authority}]: {string.Join(", ", Limitations)}; {Waiver}";
}

/// <summary>
/// § 107.51's introductory text: the one obligation its four limitations make, and the two people
/// it binds.
/// </summary>
/// <remarks>
/// <para>
/// The class is named for what the introductory text is about rather than for the entry, because a
/// type named <c>OperatingLimitations</c> collides with the <c>OperatingLimitations</c> the
/// generated <see cref="Handlers"/> partial declares — the reason <see cref="Yielding"/> is named
/// as it is.
/// </para>
/// <para>
/// <b>This entry states nothing of its own but the conjunction and the two people.</b> The
/// groundspeed limit, the altitude limit, the flight visibility minimum and the cloud clearances
/// are the constituent entries', reached through <c>dependsOn</c>: <c>speed-within-limit</c>
/// (§ 107.51(a)), <c>altitude-within-limit</c> (§ 107.51(b)) and <c>weather-minimums-met</c>
/// (§ 107.51(c)-(d)), the last itself a conjunction of paragraphs (c) and (d). Not one of their
/// figures is written here, and each is asked at runtime and answered by that entry.
/// </para>
/// <para>
/// <b>A constituent that does not resolve is reported as this entry's own decline.</b> Two things
/// are separate and both matter. The mechanism is that every constituent is called and the answer
/// follows what it actually returned, so that a constituent whose question the map later settles
/// changes this entry's answer with nothing changed here; nothing about a constituent's openness
/// is asserted from a constant. The shape is that the decline emitted is this entry's, naming the
/// entry the caller asked about and the constituent whose question blocks it, and citing that
/// constituent's locator — the shape <c>docs/decisions/0001</c> records for
/// <c>speed-within-limit</c> → <c>speed-limit</c>. A caller who asks about the operating
/// limitations is not handed a decline whose only subject is an entry two levels below, and this
/// entry is the first where that distance is two.
/// </para>
/// <para>
/// <b>The conjunction is "all of the following", so one limitation resolved broken settles it.</b>
/// Where a constituent resolved its limitation not met, the person has not complied with all of
/// them whatever the others are, and the answer is that they are not complied with even though
/// another constituent did not resolve. That is the reading <see cref="Weather"/> already applies
/// one paragraph down — "one conjunct that is false on every reading settles it, whatever the
/// other is" — and it is the introductory text's own word "all", not a reading of a question the
/// map holds open. An undetermined constituent blocks the answer only where nothing else has
/// already settled it.
/// </para>
/// <para>
/// § 107.51 is waivable — § 107.205(i) lists it — so this entry is suspended by
/// <see cref="MapEntries.WaivableRegulations"/>, exactly as its three constituents are. The gate
/// runs here as this entry's own, before any constituent is asked, so that the decline names
/// <c>operating-limitations</c> and not whichever constituent would have been asked first.
/// </para>
/// </remarks>
public static class Compliance
{
    /// <summary>The regulation § 107.205(i) lists that states the entry: the whole of § 107.51.</summary>
    public const string Regulation = "§ 107.51";

    /// <summary>
    /// <see cref="MapEntries.OperatingLimitations"/>: whether <paramref name="person"/> has complied
    /// with all of the operating limitations § 107.51 lists, on the operation the caller states.
    /// </summary>
    /// <remarks>
    /// Each constituent entry is asked, in the order this entry's <c>dependsOn</c> lists them, and
    /// each answers on its own terms with the facts the caller stated for it. The conjunction is
    /// then read off those answers: any limitation resolved not met makes the whole not complied
    /// with; all of them resolved met makes it complied with; otherwise the first constituent that
    /// did not resolve blocks the answer and this entry declines, naming itself and that
    /// constituent.
    /// </remarks>
    /// <param name="person">Which of the two people the introductory text names the caller asks about. Required, and never inferred.</param>
    /// <param name="groundspeed">The small unmanned aircraft's groundspeed, for § 107.51(a).</param>
    /// <param name="altitudeAboveGroundLevelFeet">The small unmanned aircraft's altitude, in feet above ground level, for § 107.51(b).</param>
    /// <param name="structure">What the caller states about the structure § 107.51(b)'s exception is claimed under.</param>
    /// <param name="flightVisibilityStatuteMiles">The flight visibility the caller states, observed from the location of the control station, in statute miles, for § 107.51(c).</param>
    /// <param name="cloud">What the caller states about the cloud § 107.51(d)'s two minimums are distances from, or that the aircraft is not operated near one.</param>
    /// <param name="waiver">Whether a waiver of § 107.51 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a
    /// waiver of § 107.51 is in force; otherwise, where no limitation is resolved broken and a
    /// constituent did not resolve, this entry's own decline, carrying that constituent's reason
    /// and citing its locator.
    /// </returns>
    /// <exception cref="ArgumentException">The waiver statement is about another regulation.</exception>
    public static Resolution<OperatingLimitationsFinding> CompliedWith(
        BoundPerson person,
        Groundspeed groundspeed,
        decimal altitudeAboveGroundLevelFeet,
        StructureStatement structure,
        decimal flightVisibilityStatuteMiles,
        CloudStatement cloud,
        WaiverStatement waiver)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(cloud);

        if (Waivers.Suspension(MapEntries.OperatingLimitations, Regulation, waiver) is { } suspended)
        {
            return Resolution<OperatingLimitationsFinding>.FromUnresolved(suspended);
        }

        // Every constituent is asked, and the answer below is read off what each one actually
        // returned. Nothing here records in advance which of them can resolve: an entry whose
        // question the map later settles is followed from this call, and no test of this entry
        // would have to change for that to happen.
        LimitationOutcome[] limitations =
        [
            Outcome(
                MapEntries.SpeedWithinLimit,
                Speed.Within(groundspeed, waiver),
                finding => finding.WithinLimit),
            Outcome(
                MapEntries.AltitudeWithinLimit,
                Altitude.Within(altitudeAboveGroundLevelFeet, structure, waiver),
                finding => finding.WithinLimit),
            Outcome(
                MapEntries.WeatherMinimumsMet,
                Weather.MinimumsMet(flightVisibilityStatuteMiles, cloud, waiver),
                finding => finding.MinimumsMet),
        ];

        var blocking = Array.Find(limitations, limitation => limitation.Met is null);

        // "must comply with all of the following": a limitation resolved not met settles the whole
        // of it, whatever an undetermined constituent would have said, and every limitation
        // resolved leaves nothing open.
        return Array.Exists(limitations, limitation => limitation.Met == false) || blocking is null
            ? Resolution<OperatingLimitationsFinding>.FromValue(
                new OperatingLimitationsFinding(person, limitations, waiver))
            : Resolution<OperatingLimitationsFinding>.FromUnresolved(
                Undetermined(person, limitations, blocking));
    }

    /// <summary>
    /// One constituent entry's answer, as this entry records it: the verdict it resolved and the
    /// account it gave, or the reason and the account of its decline.
    /// </summary>
    private static LimitationOutcome Outcome<T>(MapEntry entry, Resolution<T> resolution, Func<T, bool> met)
        where T : notnull =>
        resolution.Match(
            value => new LimitationOutcome(entry, met(value), value.ToString() ?? string.Empty, null),
            unresolved => new LimitationOutcome(entry, null, unresolved.Attempted, unresolved.Reason));

    /// <summary>
    /// This entry's own decline for an operation no constituent settled: it names the entry the
    /// caller asked about and every constituent that did not resolve, and it carries the first of
    /// those constituents' reason and cites that constituent's locator.
    /// </summary>
    /// <remarks>
    /// It is not the constituent's decline handed back. The reason and the locator are the
    /// constituent's, because the question that blocks the answer is the constituent's question and
    /// a citation should lead to where that question is; what was attempted is this entry's, so
    /// that a caller who asked about the operating limitations can tell this decline from the
    /// decline of the entry it asked. The two locators differ here — <c>§ 107.51 introductory
    /// text</c> is this entry's alone, and no other entry in the map cites it — but that is not
    /// what carries the distinction: the citation on this decline is deliberately the
    /// constituent's, so what tells the two apart is <see cref="UnresolvedResult.Attempted"/>,
    /// which names both entries (<c>docs/decisions/0001</c> records the same blind spot for speed).
    /// </remarks>
    private static UnresolvedResult Undetermined(
        BoundPerson person,
        IReadOnlyList<LimitationOutcome> limitations,
        LimitationOutcome blocking)
    {
        var undetermined = limitations
            .Where(limitation => limitation.Met is null)
            .Select(limitation => $"'{limitation.Entry.Id}' [{limitation.Entry.Locator.Citation}]")
            .ToList();

        return new UnresolvedResult(
            blocking.Reason ?? UnresolvedReason.RequiresInterpretation,
            $"decide whether the map entry '{MapEntries.OperatingLimitations.Id}' is complied with by {person}: "
            + "§ 107.51's introductory text requires all of the limitations it conjoins, no limitation this engine "
            + $"resolved is broken, and the map {(undetermined.Count == 1 ? "entry" : "entries")} "
            + $"{string.Join(" and ", undetermined)} did not resolve whether the limitation it states is met",
            blocking.Entry.Locator);
    }
}
