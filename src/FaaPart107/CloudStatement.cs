using System.Globalization;

namespace FaaPart107;

/// <summary>
/// What a caller states about the cloud § 107.51(d)'s minimums are distances from: whether the
/// small unmanned aircraft is operated near a cloud at all, how far below one it is, how far
/// horizontally from it, and who is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// § 107.51(d) states "The minimum distance of the small unmanned aircraft from clouds must be no
/// less than: (1) 500 feet below the cloud; and (2) 2,000 feet horizontally from the cloud." Where
/// the cloud is, and how far the aircraft is from it, are facts about one flight and are not in the
/// corpus. So they are facts the caller states, on the same terms as a waiver statement, a
/// <see cref="StructureStatement"/> and a <see cref="LightingStatement"/> (rules-factory decision
/// 0021, and this engine's <c>docs/decisions/0001</c>): demanded, attributed, recorded alongside
/// the outcome, and never inferred.
/// </para>
/// <para>
/// <b>Two distances, or no cloud, and the caller says which.</b> The two measured distances alone
/// cannot express an operation with no cloud to measure from: the value a caller would reach for is
/// zero feet, and zero feet below a cloud is a measurement — the closest an aircraft can be to one
/// — not the absence of one. <see cref="NoCloud"/> is that second case, and it is a value the
/// caller positively states. That is the shape <see cref="EncounteredObject"/> uses for the object
/// § 107.37(a) does not reach and <see cref="StructureStatement.NoneWithinRadius"/> uses for the
/// structure § 107.51(b)'s exception is not claimed under, which is the same kind of fact one
/// paragraph up: is there a thing here at all, and if so, how far away is it.
/// </para>
/// <para>
/// <b>It states the fact and never the conclusion.</b> A caller who states that the aircraft is not
/// operated near a cloud has said there is no distance from a cloud to measure on this operation.
/// They have not said that § 107.51(d) is met, and this type does not say so either: what
/// § 107.51(d) requires of an operation with no cloud is not something this engine decides, and
/// <see cref="Weather.MinimumsMet"/> declines rather than deciding it (the entry's own note, and
/// <c>AGENTS.md</c> §6).
/// </para>
/// </remarks>
public sealed record CloudStatement
{
    private CloudStatement(decimal? feetBelowCloud, decimal? feetHorizontallyFromCloud, string statedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statedBy);
        FeetBelowCloud = feetBelowCloud;
        FeetHorizontallyFromCloud = feetHorizontallyFromCloud;
        StatedBy = statedBy;
    }

    /// <summary>
    /// How far below the cloud the small unmanned aircraft is, in feet, as the caller measured it.
    /// Null when the statement is that it is not operated near a cloud. § 107.51(d)(1) prints a
    /// distance and does not say how it is measured, so the measurement is the caller's and the
    /// engine does not re-derive it.
    /// </summary>
    public decimal? FeetBelowCloud { get; }

    /// <summary>
    /// How far horizontally from the cloud the small unmanned aircraft is, in feet, as the caller
    /// measured it. Null when the statement is that it is not operated near a cloud.
    /// </summary>
    public decimal? FeetHorizontallyFromCloud { get; }

    /// <summary>Who is answerable for the statement. Free text; the engine does not parse it.</summary>
    public string StatedBy { get; }

    /// <summary>
    /// True when the statement names a cloud, with the two distances § 107.51(d) states minimums
    /// for. False when the statement is that the aircraft is not operated near a cloud, in which
    /// case there is no measured distance for either minimum to be compared with.
    /// </summary>
    public bool NamesACloud => FeetBelowCloud is not null;

    /// <summary>
    /// A statement that the small unmanned aircraft is not operated near a cloud, so there is no
    /// cloud its distance from could be measured and § 107.51(d)'s two minimums have no measured
    /// quantity on this operation.
    /// </summary>
    /// <remarks>
    /// This is distinct from a measured distance of zero, which states that the aircraft is at the
    /// cloud. It is not a claim that § 107.51(d) is met: what the paragraph requires of an
    /// operation with no cloud is not settled here, and the entry declines.
    /// </remarks>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static CloudStatement NoCloud(string statedBy) => new(null, null, statedBy);

    /// <summary>
    /// A statement naming the cloud § 107.51(d)'s minimums are measured from: how far below it the
    /// small unmanned aircraft is, and how far horizontally from it.
    /// </summary>
    /// <param name="feetBelowCloud">How far below the cloud the aircraft is, in feet, not negative.</param>
    /// <param name="feetHorizontallyFromCloud">How far horizontally from the cloud the aircraft is, in feet, not negative.</param>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static CloudStatement Measured(decimal feetBelowCloud, decimal feetHorizontallyFromCloud, string statedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(feetBelowCloud);
        ArgumentOutOfRangeException.ThrowIfNegative(feetHorizontallyFromCloud);
        return new(feetBelowCloud, feetHorizontallyFromCloud, statedBy);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        (FeetBelowCloud, FeetHorizontallyFromCloud) switch
        {
            ({ } below, { } horizontal) => string.Create(
                CultureInfo.InvariantCulture,
                $"the small unmanned aircraft is {below} feet below a cloud and {horizontal} feet horizontally from it, as stated by {StatedBy}"),
            _ => $"the small unmanned aircraft is not operated near a cloud, as stated by {StatedBy}",
        };
}
