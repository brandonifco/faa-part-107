namespace FaaPart107;

/// <summary>
/// Whether a visual observer is used during the aircraft operation, as the caller states it: the
/// condition § 107.33's chapeau makes the whole section apply under. It is a fact the rule tests,
/// not a rule, so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// § 107.33 opens "<b>If a visual observer is used during the aircraft operation</b>, all of the
/// following requirements must be met:". That sentence is in
/// <see cref="MapEntries.VisualObserverConditions"/>'s evidence and in no other entry's — neither
/// <c>effective-communication</c> (§ 107.33(a)) nor <c>observer-coordination</c> (§ 107.33(c))
/// quotes it, and each of those answers its own paragraph as though the paragraph applied. The
/// condition is therefore this entry's to carry over all three, and the entry's own name says so:
/// "Visual observer requirements, <b>when one is used</b>".
/// </para>
/// <para>
/// <b>It is stated, never inferred and never defaulted.</b> The two members below are the
/// condition's two sides and a caller picks one. There is no absent third case that the engine
/// reads as either: an operation the caller has not described is not an operation without a visual
/// observer, and defaulting it either way would decide, without being told, whether a whole section
/// of the corpus applies. That is the shape <see cref="EncounteredObject"/> uses for the case
/// § 107.37(a) does not reach — a value the caller positively states, so the answer says which
/// operation it is about.
/// </para>
/// <para>
/// <b>The set is closed.</b> <see cref="All"/> is both values; the constructor is private and no
/// factory takes a designation, so a caller cannot make a third. "If a visual observer is used" is
/// a condition with two sides and no middle, and an engine that accepted "unknown" would have to
/// say what § 107.33 requires of an operation nobody has described.
/// </para>
/// </remarks>
public sealed record VisualObserverUse
{
    private VisualObserverUse(string designation) => Designation = designation;

    /// <summary>
    /// A visual observer is used during the aircraft operation, as the caller states it: § 107.33's
    /// chapeau is satisfied and all of the requirements the section states must be met.
    /// </summary>
    public static VisualObserverUse Used { get; } = new("a visual observer is used during the aircraft operation");

    /// <summary>
    /// No visual observer is used during the aircraft operation, as the caller states it: the
    /// chapeau's condition is not satisfied, so § 107.33 states no requirement about this operation
    /// at all. That is not the same answer as the section's requirements being met.
    /// </summary>
    public static VisualObserverUse NotUsed { get; } = new("no visual observer is used during the aircraft operation");

    /// <summary>The condition as this engine states it, in § 107.33's chapeau's own terms.</summary>
    public string Designation { get; }

    /// <summary>
    /// Both sides of the chapeau's condition, in the order this type declares them: used, then not
    /// used. There is no third.
    /// </summary>
    public static IReadOnlyList<VisualObserverUse> All { get; } =
    [
        Used,
        NotUsed,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
