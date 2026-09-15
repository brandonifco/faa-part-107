namespace FaaPart107;

/// <summary>
/// What a caller states about a certificate of waiver: whether one authorizing deviation from
/// <see cref="Regulation"/> is in force, and who is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// § 107.205 lists the regulations a certificate of waiver may authorize deviation from, and while
/// one is held the rules it covers do not apply (§ 107.200(d)(1)). The map records that as a gate,
/// <see cref="MapEntries.WaivableRegulations"/>, named in <c>suspendedBy</c> on every entry the list
/// reaches, and the gate is out of scope. Whether a waiver is held is not in the corpus: it is an
/// instrument the Administrator issued to one operator. So it is a fact the caller states
/// (rules-factory decision 0021), and the engine owes it what it owes an assertion: demand it,
/// attribute it, record it alongside the outcome, and never infer it, in either direction.
/// Defaulting to "no waiver" convicts a holder; defaulting to "waiver" excuses everyone.
/// </para>
/// <para>
/// There is no default statement. An entry whose request carries none refuses to resolve.
/// </para>
/// </remarks>
/// <param name="Regulation">
/// The regulation the statement is about, as § 107.205 designates it (for example <c>§ 107.51</c>).
/// An entry refuses a statement about a regulation other than the one that states it.
/// </param>
/// <param name="InForce">True when a certificate of waiver authorizing deviation from it is in force.</param>
/// <param name="StatedBy">Who is answerable for the statement. Free text; the engine does not parse it.</param>
/// <param name="Certificate">
/// The certificate the caller names, when it can name one. Null when there is none to name, and
/// saying so is honest. The engine does not read its terms: they are not published text.
/// </param>
public sealed record WaiverStatement(string Regulation, bool InForce, string StatedBy, string? Certificate = null)
{
    /// <summary>The regulation, checked to be non-empty.</summary>
    public string Regulation { get; } = CheckText(Regulation, nameof(Regulation));

    /// <summary>Who stated it, checked to be non-empty.</summary>
    public string StatedBy { get; } = CheckText(StatedBy, nameof(StatedBy));

    /// <summary>A statement that a certificate of waiver authorizing deviation from <paramref name="regulation"/> is in force.</summary>
    /// <param name="regulation">The regulation, as § 107.205 designates it.</param>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <param name="certificate">The certificate, when the caller can name one.</param>
    /// <returns>The statement.</returns>
    public static WaiverStatement Held(string regulation, string statedBy, string? certificate = null) =>
        new(regulation, InForce: true, statedBy, certificate);

    /// <summary>A statement that no certificate of waiver authorizing deviation from <paramref name="regulation"/> is in force.</summary>
    /// <param name="regulation">The regulation, as § 107.205 designates it.</param>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static WaiverStatement NoneHeld(string regulation, string statedBy) =>
        new(regulation, InForce: false, statedBy);

    private static string CheckText(string text, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, name);
        return text;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        (InForce, Certificate) switch
        {
            (true, { } certificate) => $"a certificate of waiver ({certificate}) authorizing deviation from {Regulation} is in force, as stated by {StatedBy}",
            (true, null) => $"a certificate of waiver authorizing deviation from {Regulation} is in force, as stated by {StatedBy}",
            _ => $"no certificate of waiver authorizing deviation from {Regulation} is in force, as stated by {StatedBy}",
        };
}
