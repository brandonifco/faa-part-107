using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The designation of the area an operation is in, as § 107.45 names them: the two the section
/// states, and neither of them.
/// </summary>
/// <remarks>
/// <para>
/// § 107.45 says which areas its prohibition reaches; it does not say which area any operation is
/// in, and part 107 does not define "prohibited area" or "restricted area" at all. The map records
/// that by giving <see cref="MapEntries.RestrictedAreaPermitted"/> no cross-reference and no
/// dependency, and its note says the designation "is an input, not something derived here" — the
/// same shape as <c>airspace-authorized</c>'s airspace class. So the caller states it. Nothing in
/// this engine looks at a position, a chart or an airspace map, and nothing here decides whether an
/// area is prohibited or restricted.
/// </para>
/// <para>
/// No member is zero, so <c>default</c> is not a designation and
/// <see cref="ProhibitedAndRestrictedAreas.Permitted"/> refuses it rather than reading it as the
/// designation that excuses the operation.
/// </para>
/// </remarks>
public enum AreaDesignation
{
    /// <summary>A prohibited area: the first designation § 107.45 states.</summary>
    Prohibited = 1,

    /// <summary>A restricted area: the second designation § 107.45 states.</summary>
    Restricted = 2,

    /// <summary>Neither designation § 107.45 states, so the section's prohibition does not reach the operation.</summary>
    NeitherProhibitedNorRestricted = 3,
}

/// <summary>
/// What a caller states about the permission § 107.45 names: whether the person operating the small
/// unmanned aircraft "has permission from the using or controlling agency, as appropriate", and who
/// is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// A permission under § 107.45 is not in the corpus: it is granted by an agency to one operator for
/// one area, and no published text says whether any particular person holds one. That is the same
/// class of fact as a certificate of waiver, and the engine owes it what rules-factory decision 0021
/// says an assertion is owed — demand it, attribute it, record it alongside the outcome, and never
/// infer it, in either direction. Defaulting to "no permission" convicts a holder; defaulting to
/// "permission" excuses everyone. There is no default statement, and an entry whose request carries
/// none refuses to resolve.
/// </para>
/// <para>
/// Which of the two agencies the section names is the appropriate one is § 107.45's own "as
/// appropriate", and the corpus states no test for it. The engine therefore does not judge
/// <see cref="Agency"/>: the statement is that the permission the section requires is held, and the
/// agency is free text recorded for the record.
/// </para>
/// </remarks>
/// <param name="Held">True when the person has the permission § 107.45 requires.</param>
/// <param name="StatedBy">Who is answerable for the statement. Free text; the engine does not parse it.</param>
/// <param name="Agency">
/// The using or controlling agency the caller names, when it can name one. Null when there is none
/// to name, and saying so is honest. The engine does not read it, and does not decide which agency
/// is the appropriate one.
/// </param>
public sealed record PermissionStatement(bool Held, string StatedBy, string? Agency = null)
{
    /// <summary>Who stated it, checked to be non-empty.</summary>
    public string StatedBy { get; } = CheckText(StatedBy, nameof(StatedBy));

    /// <summary>A statement that the permission § 107.45 requires is held.</summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <param name="agency">The using or controlling agency, when the caller can name one.</param>
    /// <returns>The statement.</returns>
    public static PermissionStatement Granted(string statedBy, string? agency = null) =>
        new(Held: true, statedBy, agency);

    /// <summary>A statement that the permission § 107.45 requires is not held.</summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static PermissionStatement NotGranted(string statedBy) => new(Held: false, statedBy);

    private static string CheckText(string text, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, name);
        return text;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        (Held, Agency) switch
        {
            (true, { } agency) => $"permission from {agency} is held, as stated by {StatedBy}",
            (true, null) => $"permission from the using or controlling agency is held, as stated by {StatedBy}",
            (false, { } agency) => $"no permission from {agency} is held, as stated by {StatedBy}",
            _ => $"no permission from the using or controlling agency is held, as stated by {StatedBy}",
        };
}

/// <summary>
/// Whether § 107.45 permits the operation: <see cref="MapEntries.RestrictedAreaPermitted"/>.
/// </summary>
/// <param name="Designation">The designation of the area the operation is in, as the caller stated it.</param>
/// <param name="Permitted">
/// True when § 107.45 does not bar the operation: either the area is neither prohibited nor
/// restricted, so the section's prohibition does not reach it, or it is one of those and the stated
/// permission is held.
/// </param>
/// <param name="Permission">The caller's permission statement the finding was resolved under, recorded with it.</param>
public sealed record AreaPermissionFinding(AreaDesignation Designation, bool Permitted, PermissionStatement Permission)
{
    /// <summary>
    /// Whether § 107.45 requires permission at all: true in a prohibited or a restricted area, and
    /// false outside them, because the section's prohibition is only on operating "in prohibited or
    /// restricted areas".
    /// </summary>
    public bool PermissionRequired => Designation != AreaDesignation.NeitherProhibitedNorRestricted;

    /// <summary>Where the rule is stated: <c>§ 107.45</c>.</summary>
    public SourceLocator Authority => MapEntries.RestrictedAreaPermitted.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Area(Designation)}: {(Permitted ? "permitted" : "not permitted")} by § 107.45 [{Authority}]; {Permission}";

    private static string Area(AreaDesignation designation) =>
        designation switch
        {
            AreaDesignation.Prohibited => "a prohibited area",
            AreaDesignation.Restricted => "a restricted area",
            _ => "neither a prohibited nor a restricted area",
        };
}

/// <summary>
/// § 107.45: <see cref="MapEntries.RestrictedAreaPermitted"/>, "No person may operate a small
/// unmanned aircraft in prohibited or restricted areas unless that person has permission from the
/// using or controlling agency, as appropriate."
/// </summary>
/// <remarks>
/// <para>
/// Two facts decide it, and the caller supplies both: the designation of the area the operation is
/// in, and whether the permission the section names is held. The engine derives neither. It reads no
/// airspace map, consults no chart, and does not define "prohibited area" or "restricted area" —
/// part 107 defines neither, this map records no cross-reference for the entry, and a definition
/// taken from anywhere else would be this engine mapping a corpus it has not admitted.
/// </para>
/// <para>
/// The entry's <c>suspendedBy</c> is empty, so there is no waiver gate here and no regulation
/// constant to name one: § 107.205's list reaches § 107.41 and § 107.51, and the map does not give
/// this entry the gate it gives those. An entry gets the gate its own map row gives it, and no gate
/// by analogy with the section next to it.
/// </para>
/// </remarks>
public static class ProhibitedAndRestrictedAreas
{
    /// <summary>
    /// <see cref="MapEntries.RestrictedAreaPermitted"/>: whether § 107.45 permits the operation.
    /// </summary>
    /// <remarks>
    /// The prohibition is on operating "in prohibited or restricted areas", and the "unless" is its
    /// only exception. So in either designated area the operation is permitted exactly when the
    /// stated permission is held; and where the area is neither, the section states no prohibition
    /// on the operation and asks for no permission.
    /// </remarks>
    /// <param name="designation">The designation of the area the operation is in, as the caller states it.</param>
    /// <param name="permission">Whether the permission § 107.45 names is held, as the caller states it.</param>
    /// <returns>The finding. This entry has no open question and no waiver gate, so it always resolves.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="designation"/> is none § 107.45 states.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="permission"/> is null.</exception>
    public static Resolution<AreaPermissionFinding> Permitted(AreaDesignation designation, PermissionStatement permission)
    {
        if (!Enum.IsDefined(designation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(designation),
                designation,
                "§ 107.45 states a prohibited area and a restricted area, and the caller may state neither of them");
        }

        ArgumentNullException.ThrowIfNull(permission);

        var required = designation is AreaDesignation.Prohibited or AreaDesignation.Restricted;
        return Resolution<AreaPermissionFinding>.FromValue(
            new AreaPermissionFinding(designation, !required || permission.Held, permission));
    }
}
