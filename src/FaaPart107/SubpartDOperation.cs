namespace FaaPart107;

/// <summary>
/// Whether the operation will be conducted over human beings under subpart D of this part, as the
/// caller states it: the condition § 107.49(f) states its obligation under. It is a fact the rule
/// tests, not a rule, so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>,
/// gate 1).
/// </summary>
/// <remarks>
/// <para>
/// § 107.49(f) reads "<b>If the operation will be conducted over human beings under subpart D of
/// this part</b>, ensure that the aircraft meets the requirements of § 107.110, § 107.120(a),
/// § 107.130(a), or § 107.140, as applicable." That condition is in
/// <see cref="MapEntries.PreflightActions"/>'s evidence and in no constituent's:
/// <see cref="MapEntries.SubpartDCategories"/>' evidence is subpart D's own applicability sentence
/// and does not carry it, and no other entry in the map cites § 107.49(f). The entry's note says
/// (f) "is reachable only through subpart D and is a dependency on <c>subpart-d-categories</c>
/// rather than a second reason on this entry", so what makes it reachable has to be stated
/// somewhere, and the corpus states it as a condition about the operation.
/// </para>
/// <para>
/// <b>It is stated, never inferred and never defaulted.</b> The two members below are the
/// condition's two sides and a caller picks one. There is no absent third case that the engine
/// reads as either: an operation the caller has not described is not an operation outside subpart D,
/// and defaulting it either way would decide, without being told, whether a paragraph of the corpus
/// reaches the operation. That is the shape <see cref="VisualObserverUse"/> uses for § 107.33's
/// chapeau, which is the same kind of condition stated once in a composite entry's own evidence.
/// </para>
/// <para>
/// <b>Why <c>over-human-beings</c> takes no such input for the same dependency.</b> That entry
/// reaches <see cref="MapEntries.SubpartDCategories"/> through § 107.39(c), a bare disjunct of the
/// section's "unless—" that states no antecedent at all, so there is no condition for a caller to
/// state and the entry is asked on every request. § 107.49(f) states one. Both entries' notes agree
/// on what the dependency's <see cref="RulesKernel.Resolution.UnresolvedReason.OutsideCurrentScope"/>
/// does — it stays that
/// entry's, recorded on the outcome and named in the account, and never becomes the composite's
/// reason — and <see cref="Preflight"/> does exactly that. Where this entry's note goes further is
/// the word "reachable": (f) "is <b>reachable only through subpart D</b>", which
/// <c>over-human-beings</c>' note does not say of (c) and could not, because (c) is reached
/// whatever the operation is. So what this condition adds is reachability, and it adds it because
/// the corpus wrote an "If" in one place and not in the other.
/// </para>
/// <para>
/// <b>It is the condition, and never the answer.</b> What subpart D then requires is
/// <see cref="MapEntries.SubpartDCategories"/>', which the map puts <c>scope: out</c>; a caller who
/// states that the operation is conducted over human beings under subpart D has said which
/// operation this is, not that the aircraft meets § 107.110, § 107.120(a), § 107.130(a) or
/// § 107.140. This engine maps subpart B, and it neither works subpart D's requirements nor decides
/// them from this statement.
/// </para>
/// <para>
/// <b>The set is closed.</b> <see cref="All"/> is both values; the constructor is private and no
/// factory takes a designation, so a caller cannot make a third. "If the operation will be
/// conducted over human beings under subpart D of this part" is a condition with two sides and no
/// middle, and an engine that accepted "unknown" would have to say what § 107.49(f) requires of an
/// operation nobody has described.
/// </para>
/// </remarks>
public sealed record SubpartDOperation
{
    private SubpartDOperation(string designation) => Designation = designation;

    /// <summary>
    /// The operation will be conducted over human beings under subpart D of this part, as the caller
    /// states it: § 107.49(f)'s condition is satisfied, so the paragraph states an obligation about
    /// this operation and <see cref="MapEntries.SubpartDCategories"/> is asked what it is.
    /// </summary>
    public static SubpartDOperation OverHumanBeings { get; } =
        new("the operation will be conducted over human beings under subpart D of this part");

    /// <summary>
    /// The operation will not be conducted over human beings under subpart D of this part, as the
    /// caller states it: § 107.49(f)'s condition is not satisfied, so the paragraph states no
    /// obligation about this operation and the entry it defers to is not asked. That is not the same
    /// answer as § 107.49(f) being satisfied.
    /// </summary>
    public static SubpartDOperation NotOverHumanBeings { get; } =
        new("the operation will not be conducted over human beings under subpart D of this part");

    /// <summary>The condition as this engine states it, in § 107.49(f)'s own terms.</summary>
    public string Designation { get; }

    /// <summary>
    /// Both sides of § 107.49(f)'s condition, in the order this type declares them: over human
    /// beings under subpart D, then not. There is no third.
    /// </summary>
    public static IReadOnlyList<SubpartDOperation> All { get; } =
    [
        OverHumanBeings,
        NotOverHumanBeings,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
