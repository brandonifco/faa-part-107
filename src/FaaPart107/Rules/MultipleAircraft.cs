using System.Collections.Immutable;
using System.Globalization;
using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// What § 107.35 says about one person's engagements at one time:
/// <see cref="MapEntries.SingleAircraft"/>, "A person may not manipulate flight controls or act as
/// a remote pilot in command or visual observer in the operation of more than one unmanned
/// aircraft at the same time."
/// </summary>
/// <remarks>
/// The finding counts <see cref="Aircraft"/>, the distinct designations the caller gave, and not
/// the engagements: the sentence prohibits being in those roles "in the operation of more than one
/// unmanned aircraft", so one aircraft a person both controls and observes is one aircraft, and
/// two aircraft in any of the three roles are two.
/// </remarks>
/// <param name="Person">The person the statement is about, as the caller names them.</param>
/// <param name="Engagements">The engagements the caller stated, in the order stated.</param>
/// <param name="Aircraft">The distinct aircraft designations among them, in the order first stated.</param>
/// <param name="Permitted">
/// True when that is not more than one unmanned aircraft, which § 107.35 permits; false when it is
/// more than one, which § 107.35 prohibits.
/// </param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record MultipleAircraftFinding(
    string Person,
    ImmutableArray<AircraftEngagement> Engagements,
    ImmutableArray<string> Aircraft,
    bool Permitted,
    WaiverStatement Waiver)
{
    /// <summary>The engagements stated, never the uninitialised default.</summary>
    public ImmutableArray<AircraftEngagement> Engagements { get; } = Engagements.IsDefault ? [] : Engagements;

    /// <summary>The distinct aircraft among them, never the uninitialised default.</summary>
    public ImmutableArray<string> Aircraft { get; } = Aircraft.IsDefault ? [] : Aircraft;

    /// <summary>How many unmanned aircraft the person is in one of the three roles for, at the same time.</summary>
    public int AircraftCount => Aircraft.Length;

    /// <summary>Where the rule is stated: <c>§ 107.35</c>.</summary>
    public SourceLocator Authority => MapEntries.SingleAircraft.Locator;

    /// <summary>Whether this finding is the same as <paramref name="other"/>, comparing the sequences by element.</summary>
    /// <param name="other">The other finding.</param>
    /// <returns>True when both state the same person, the same engagements in the same order, and the same outcome.</returns>
    /// <remarks>
    /// A record's generated equality would compare the two <see cref="ImmutableArray{T}"/> members by
    /// the identity of the array behind them, so two resolutions of the same request would differ.
    /// Determinism is about what the engine says, so equality is by element (<c>AGENTS.md</c> §8).
    /// </remarks>
    public bool Equals(MultipleAircraftFinding? other) =>
        other is not null
        && string.Equals(Person, other.Person, StringComparison.Ordinal)
        && Permitted == other.Permitted
        && Waiver == other.Waiver
        && Engagements.SequenceEqual(other.Engagements)
        && Aircraft.SequenceEqual(other.Aircraft, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Person, StringComparer.Ordinal);
        hash.Add(Permitted);
        hash.Add(Waiver);
        foreach (var engagement in Engagements)
        {
            hash.Add(engagement);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Person}, {(Engagements.IsEmpty ? "in none of the three roles" : string.Join("; ", Engagements))}: {AircraftCount} unmanned aircraft at the same time, "
            + $"{(Permitted ? "not more than one" : "more than one")} [{Authority}]; {Waiver}");
}

/// <summary>§ 107.35: whether a person is in one of its three roles for more than one unmanned aircraft at the same time.</summary>
public static class MultipleAircraft
{
    /// <summary>The regulation § 107.205(e) lists that states the entry: the whole of § 107.35.</summary>
    public const string Regulation = "§ 107.35";

    /// <summary>
    /// <see cref="MapEntries.SingleAircraft"/>: whether "A person may not manipulate flight controls
    /// or act as a remote pilot in command or visual observer in the operation of more than one
    /// unmanned aircraft at the same time" is met.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sentence binds one person, so the engagements are one person's, and two people each in a
    /// role for one aircraft are outside it. It counts unmanned aircraft, so the finding is
    /// permitted at none and at one — whether the person fills one of the three roles for that
    /// aircraft or all three — and prohibited at two or more, in any mix of the three roles, which
    /// includes the person who is a remote pilot in command of one and a visual observer for a
    /// second.
    /// </para>
    /// <para>
    /// Everything the count turns on is the caller's: which aircraft are which
    /// (<see cref="AircraftEngagement.Aircraft"/>), which of the three roles the person fills, and
    /// which engagements are at the same time. The engine infers no fleet and no clock.
    /// </para>
    /// </remarks>
    /// <param name="person">
    /// The person the statement is about, as the caller names them. Required once the entry is
    /// reachable, and demanded after the gate (<c>docs/decisions/0008</c>).
    /// </param>
    /// <param name="engagements">
    /// Every unmanned aircraft the person is, at the same time, in one of the three roles for, each
    /// with the role. Empty says the person is in none of them, which is a statement; not supplying
    /// the list at all is not, and is demanded after the gate.
    /// </param>
    /// <param name="waiver">Whether a waiver of § 107.35 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver
    /// of § 107.35 is in force.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and
    /// <paramref name="person"/> or <paramref name="engagements"/> was not stated.
    /// </exception>
    public static Resolution<MultipleAircraftFinding> AtTheSameTime(
        string? person,
        IReadOnlyList<AircraftEngagement>? engagements,
        WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.SingleAircraft, Regulation, waiver) is { } suspended)
        {
            return Resolution<MultipleAircraftFinding>.FromUnresolved(suspended);
        }

        var named = Demands.Of(person, MapEntries.SingleAircraft, nameof(Requests.SingleAircraftRequest.Person));
        var stated = Demands.Of(
            engagements, MapEntries.SingleAircraft, nameof(Requests.SingleAircraftRequest.Engagements));

        ArgumentException.ThrowIfNullOrWhiteSpace(named, nameof(person));

        var aircraft = DistinctAircraft(stated);
        return Resolution<MultipleAircraftFinding>.FromValue(
            new MultipleAircraftFinding(named, [.. stated], aircraft, aircraft.Length <= 1, waiver));
    }

    private static ImmutableArray<string> DistinctAircraft(IReadOnlyList<AircraftEngagement> engagements)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var distinct = ImmutableArray.CreateBuilder<string>();
        foreach (var engagement in engagements)
        {
            ArgumentNullException.ThrowIfNull(engagement, nameof(engagements));
            if (seen.Add(engagement.Aircraft))
            {
                distinct.Add(engagement.Aircraft);
            }
        }

        return distinct.ToImmutable();
    }
}
