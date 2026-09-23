namespace FaaPart107;

/// <summary>
/// Which of the periods § 107.29(c) calls civil twilight the operation is during, as the caller
/// states it — or neither of them. It is a fact the rule tests, not a rule, so the map has no entry
/// for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not <see cref="CivilTwilightPeriod"/>.</b> That type is a period as § 107.29(c)(1)
/// and (c)(2) print it — where it begins and where it ends — and it is
/// <see cref="MapEntries.CivilTwilightWindow"/>'s value. This one says which of those two printed
/// periods an operation was during, which is a fact about one flight. The two members that name a
/// period name it by its item of § 107.29(c) and carry none of its figures: the figures stay in the
/// entry that prints them, and <see cref="Twilight"/> reads the period off that entry's value.
/// </para>
/// <para>
/// <b>Why the caller states it.</b> When official sunrise and official sunset occur at a place on a
/// date is not in this corpus — <see cref="CivilTwilight"/> says so — so the engine cannot put a
/// flight inside or outside a period that is stated against them. What it can do, and does, is take
/// the caller's statement of which period the operation was during and apply § 107.29(b) to it.
/// </para>
/// <para>
/// <b>It is stated, never inferred and never defaulted.</b> <see cref="NeitherPeriod"/> is a case
/// the caller positively states, not the absence of a statement: an operation nobody has described
/// is not an operation outside civil twilight, and reading silence that way would lift
/// § 107.29(b)'s lighting requirement from every flight nobody described. That is the shape
/// <see cref="VisualObserverUse"/> uses for § 107.33's chapeau and
/// <see cref="EncounteredObject.NoneOfThem"/> for the case § 107.37(a) does not reach.
/// </para>
/// <para>
/// <b>The set is closed, and it holds the two periods § 107.29(c)(1)-(2) state.</b>
/// <see cref="All"/> is every member; the constructor is private and no factory takes a
/// designation, so a caller cannot make a third period. An operation <b>in Alaska</b> is not
/// answered from this value at all: both of those items begin "Except for Alaska", and what civil
/// twilight refers to there is § 107.29(c)(3)'s, <see cref="MapEntries.CivilTwilightAlaska"/>. So
/// <see cref="Twilight"/> reads <see cref="OperationPlace"/> before it reads this.
/// </para>
/// </remarks>
public sealed record OperationPeriod
{
    private OperationPeriod(string designation, string? item)
    {
        Designation = designation;
        Item = item;
    }

    /// <summary>
    /// The operation is during the period § 107.29(c)(1) states, as the caller states it: the one
    /// that ends at official sunrise.
    /// </summary>
    public static OperationPeriod BeforeOfficialSunrise { get; } =
        new("during the period § 107.29(c)(1) states", "(c)(1)");

    /// <summary>
    /// The operation is during the period § 107.29(c)(2) states, as the caller states it: the one
    /// that begins at official sunset.
    /// </summary>
    public static OperationPeriod AfterOfficialSunset { get; } =
        new("during the period § 107.29(c)(2) states", "(c)(2)");

    /// <summary>
    /// The operation is during neither period, as the caller states it: it is not during a period
    /// of civil twilight, so § 107.29(b) states no prohibition about it. That is not the same
    /// answer as the paragraph's requirement being met.
    /// </summary>
    public static OperationPeriod NeitherPeriod { get; } =
        new("during neither period § 107.29(c) states", null);

    /// <summary>The period as this engine states it, in § 107.29(c)'s own terms.</summary>
    public string Designation { get; }

    /// <summary>
    /// The item of § 107.29(c) that states this period — <c>(c)(1)</c> or <c>(c)(2)</c> — and null
    /// for <see cref="NeitherPeriod"/>, which is no item of it.
    /// </summary>
    public string? Item { get; }

    /// <summary>
    /// Every period this engine names, in § 107.29(c)'s own order: the item (c)(1) states, the item
    /// (c)(2) states, then the case neither reaches. There is no fourth.
    /// </summary>
    public static IReadOnlyList<OperationPeriod> All { get; } =
    [
        BeforeOfficialSunrise,
        AfterOfficialSunset,
        NeitherPeriod,
    ];

    /// <inheritdoc/>
    public override string ToString() => Designation;
}
