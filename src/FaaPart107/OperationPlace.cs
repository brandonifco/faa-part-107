namespace FaaPart107;

/// <summary>
/// Where the operation is, as the caller states it: in Alaska, or outside it — the fact
/// § 107.29(c) makes the definition of civil twilight turn on. It is a fact the rule tests, not a
/// rule, so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// § 107.29(c) states civil twilight three times and qualifies each by place: "(1) <b>Except for
/// Alaska</b>, a period of time that begins 30 minutes before official sunrise …; (2) <b>Except for
/// Alaska</b>, a period of time that begins at official sunset …; and (3) <b>In Alaska</b>, the
/// period of civil twilight as defined in the Air Almanac." The two sides are the corpus's own, and
/// they are what selects the entry that answers: <see cref="MapEntries.CivilTwilightWindow"/>
/// outside Alaska — whose value carries the same word, <see cref="CivilTwilightWindows.ExceptFor"/>
/// — and <see cref="MapEntries.CivilTwilightAlaska"/> in it.
/// </para>
/// <para>
/// <b>It is stated, never inferred and never defaulted.</b> There is no absent third case the
/// engine reads as either. Defaulting to "outside Alaska" would answer an Alaskan operation from a
/// definition its own paragraph excepts; defaulting to "in Alaska" would decline every operation in
/// the country over a book this map has not admitted. That is the shape
/// <see cref="VisualObserverUse"/> uses for § 107.33's chapeau, and for the same reason: which
/// paragraph of the corpus reaches an operation is not something this engine decides from an
/// absence.
/// </para>
/// <para>
/// <b>The set is closed.</b> <see cref="All"/> is both values; the constructor is private and no
/// factory takes a designation, so a caller cannot make a third. "Except for Alaska" and "In
/// Alaska" divide every operation between them, and an engine that accepted "somewhere else" would
/// have to say which of § 107.29(c)'s definitions applies to it.
/// </para>
/// </remarks>
public sealed record OperationPlace
{
    private OperationPlace(string designation) => Designation = designation;

    /// <summary>
    /// The operation is outside Alaska, as the caller states it: the place § 107.29(c)(1) and
    /// (c)(2) state their two periods for, "Except for Alaska".
    /// </summary>
    public static OperationPlace OutsideAlaska { get; } = new("the operation is outside Alaska");

    /// <summary>
    /// The operation is in Alaska, as the caller states it: the place § 107.29(c)(3) defers to the
    /// Air Almanac for, "In Alaska, the period of civil twilight as defined in the Air Almanac".
    /// </summary>
    public static OperationPlace InAlaska { get; } = new("the operation is in Alaska");

    /// <summary>The place as this engine states it, in § 107.29(c)'s own terms.</summary>
    public string Designation { get; }

    /// <summary>
    /// Both sides of the place § 107.29(c) divides on, in the paragraph's own order: the place its
    /// first two items except, then the place its third names. There is no third.
    /// </summary>
    public static IReadOnlyList<OperationPlace> All { get; } =
    [
        OutsideAlaska,
        InAlaska,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
