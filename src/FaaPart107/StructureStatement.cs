using System.Globalization;

namespace FaaPart107;

/// <summary>
/// What a caller states about the structure § 107.51(b)'s exception is claimed under: whether the
/// small unmanned aircraft is flown near a structure at all, how far from it, what that structure's
/// immediate uppermost limit is, and who is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// § 107.51(b) lifts its 400 feet above ground level ceiling only where the aircraft "(1) Is flown
/// within a 400-foot radius of a structure; and (2) Does not fly higher than 400 feet above the
/// structure's immediate uppermost limit." Neither fact is in the corpus: where a structure stands,
/// how tall it is and how far the aircraft is from it are facts about one flight. So they are facts
/// the caller states, on the same terms as a waiver statement (rules-factory decision 0021, and this
/// engine's <c>docs/decisions/0001</c>): demanded, attributed, recorded alongside the outcome, and
/// never inferred.
/// </para>
/// <para>
/// In particular the engine does not assume there is no structure. A request for
/// <see cref="MapEntries.AltitudeWithinLimit"/> carrying no statement refuses to resolve, because
/// assuming none would convict an operator who was lawfully above 400 feet beside a tower, and
/// assuming one would excuse an operator who was not.
/// </para>
/// <para>
/// The engine applies the printed 400-foot radius itself, to the distance stated here; it does not
/// ask the caller whether paragraph (b)(1) is met. What the caller may state, with
/// <see cref="NoneWithinRadius"/>, is the other fact: that the aircraft is not flown within a
/// 400-foot radius of a structure at all.
/// </para>
/// </remarks>
public sealed record StructureStatement
{
    private StructureStatement(decimal? distanceFeet, decimal? immediateUppermostLimitFeet, string statedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statedBy);
        DistanceFeet = distanceFeet;
        ImmediateUppermostLimitFeet = immediateUppermostLimitFeet;
        StatedBy = statedBy;
    }

    /// <summary>
    /// How far the small unmanned aircraft is flown from the structure, in feet, as the caller
    /// measured it. Null when the statement is that it is flown within a 400-foot radius of no
    /// structure. § 107.51(b)(1) prints a "400-foot radius" and does not say how the distance is
    /// measured, so the measurement is the caller's and the engine does not re-derive it.
    /// </summary>
    public decimal? DistanceFeet { get; }

    /// <summary>
    /// The structure's immediate uppermost limit, in feet above ground level — the same reference
    /// § 107.51(b) measures the aircraft's altitude against, so that "400 feet above the structure's
    /// immediate uppermost limit" is one figure plus another. Null when no structure is stated.
    /// </summary>
    public decimal? ImmediateUppermostLimitFeet { get; }

    /// <summary>Who is answerable for the statement. Free text; the engine does not parse it.</summary>
    public string StatedBy { get; }

    /// <summary>True when the statement names a structure, with its distance and its immediate uppermost limit.</summary>
    public bool NamesAStructure => DistanceFeet is not null;

    /// <summary>
    /// A statement that the small unmanned aircraft is not flown within a 400-foot radius of a
    /// structure, so § 107.51(b)(1) is not met and the exception is not open to it.
    /// </summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static StructureStatement NoneWithinRadius(string statedBy) => new(null, null, statedBy);

    /// <summary>
    /// A statement naming the structure § 107.51(b)'s exception is claimed under: how far the
    /// aircraft is flown from it, and its immediate uppermost limit.
    /// </summary>
    /// <param name="distanceFeet">The distance from the structure, in feet, not negative.</param>
    /// <param name="immediateUppermostLimitFeet">
    /// The structure's immediate uppermost limit, in feet above ground level, not negative.
    /// </param>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static StructureStatement Near(decimal distanceFeet, decimal immediateUppermostLimitFeet, string statedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(distanceFeet);
        ArgumentOutOfRangeException.ThrowIfNegative(immediateUppermostLimitFeet);
        return new(distanceFeet, immediateUppermostLimitFeet, statedBy);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        (DistanceFeet, ImmediateUppermostLimitFeet) switch
        {
            ({ } distance, { } uppermost) => string.Create(
                CultureInfo.InvariantCulture,
                $"the small unmanned aircraft is flown {distance} feet from a structure whose immediate uppermost limit is {uppermost} feet above ground level, as stated by {StatedBy}"),
            _ => $"the small unmanned aircraft is not flown within a 400-foot radius of a structure, as stated by {StatedBy}",
        };
}
