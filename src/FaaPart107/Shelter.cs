namespace FaaPart107;

/// <summary>
/// Which of the two things § 107.39(b) states its standard over the assertion is about, as the
/// caller states it: a covered structure, or a stationary vehicle. It is a fact the rule records,
/// not a rule, so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// § 107.39(b) prints two, joined by "or" — "located under a covered structure or inside a
/// stationary vehicle that can provide reasonable protection from a falling small unmanned
/// aircraft" — and the entry's note distributes the assertion over both: it "is asserted and
/// withheld <b>for each of</b> a covered structure and a stationary vehicle, attributed and
/// recorded with the outcome, and never defaulted to true when absent".
/// </para>
/// <para>
/// <b>Why "for each of" needs this type.</b> The map uses a fixed formula across its assertion
/// entries and varies it deliberately. <see cref="MapEntries.ObserverCoordination"/>, whose
/// <c>assertedBy</c> names three people, says the assertion is "asserted and withheld, <b>attributed
/// to</b> the three named persons and recorded with the outcome" — one joint fact any of them may
/// attribute. <see cref="MapEntries.UnaidedVisualContact"/>, also naming three people, says "for
/// each named person", because § 107.31(a)'s ability is one each of them must have separately. So
/// "for each X" is the map's phrase for a proposition that distributes over X, and "attributed to X"
/// is its phrase for one that does not. Whether a carport can provide reasonable protection from a
/// falling small unmanned aircraft is simply a different fact from whether a parked van can, and
/// this entry's note uses the distributive phrase. Without this type the rider would have no effect
/// at all, and an assertion made about a stationary vehicle would answer for a covered structure
/// nobody said anything about — which is what "never defaulted to true when absent" forbids.
/// </para>
/// <para>
/// <b>The set is closed, and it holds exactly the paragraph's two.</b> <see cref="All"/> is every
/// member; the constructor is private and no factory takes a designation, so a caller cannot make a
/// third. That follows <see cref="EncounteredObject"/>, and for the same reason: an engine that
/// accepted an arbitrary designation would have to say what § 107.39(b) does about a word it cannot
/// identify. There is deliberately no "neither of them" member, unlike
/// <see cref="EncounteredObject.NoneOfThem"/>: § 107.37(a)'s note says in terms that an engine told
/// the object is none of the three decides the case, and this entry's note says no such thing. A
/// human being under neither a covered structure nor a stationary vehicle is a case
/// § 107.39(b) does not reach at all, and <see cref="MapEntries.OverHumanBeings"/>'s note keeps it —
/// "a plain overflight with none of them" is in that entry's evidence, not this one's.
/// </para>
/// <para>
/// <b>What this is not.</b> It is not a measure of protection, a structure's strength or a vehicle's
/// construction: the corpus states none, and the fact that the thing meets the standard is the
/// caller's assertion, not anything read off this value. Nor is it where the human being is —
/// § 107.39(b) requires the person to be located under or inside the very thing that can provide
/// reasonable protection, and whether they are is <see cref="MapEntries.OverHumanBeings"/>'s, which
/// depends on this entry and must check its own limb against the one recorded here. This value says
/// only which of the paragraph's two the assertion in hand is about. The engine classifies nothing:
/// it holds no register of structures or vehicles and inspects neither.
/// </para>
/// </remarks>
public sealed record Shelter
{
    private Shelter(string designation) => Designation = designation;

    /// <summary>"a covered structure", the first of the two § 107.39(b) states the standard over.</summary>
    public static Shelter CoveredStructure { get; } = new("under a covered structure");

    /// <summary>"a stationary vehicle", the second of the two § 107.39(b) states the standard over.</summary>
    public static Shelter StationaryVehicle { get; } = new("inside a stationary vehicle");

    /// <summary>
    /// Both of them, in the paragraph's own order and its own words. There is no third: see the
    /// remarks on this type for why there is no "neither of them" either.
    /// </summary>
    public static IReadOnlyList<Shelter> All { get; } =
    [
        CoveredStructure,
        StationaryVehicle,
    ];

    /// <summary>The place as § 107.39(b) prints it, verbatim.</summary>
    public string Designation { get; }

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
