namespace FaaPart107;

/// <summary>
/// What a caller states about authorization from Air Traffic Control: whether the person operating
/// has it, whether it was obtained before the operation, and who is answerable for saying so.
/// </summary>
/// <remarks>
/// <para>
/// § 107.41 excepts an operation only where "that person has prior authorization from Air Traffic
/// Control (ATC)". Whether such an authorization exists is not in the corpus: it is an instrument
/// ATC issued to one operator for one operation. So the engine owes it what it owes the waiver
/// statement beside it (rules-factory decision 0021): demand it, attribute it, record it alongside
/// the outcome, and never infer it, in either direction. Fabricating one clears an operator who was
/// never authorized; defaulting its absence silently to "denied" turns an unstated fact into a
/// finding the caller did not make.
/// </para>
/// <para>
/// There is no default statement. The entry's request carrying none refuses to resolve.
/// </para>
/// <para>
/// The section attaches two qualifiers to the authorization, and this type states both. It must be
/// <em>from Air Traffic Control</em>: that is what this type is, and an authorization from anyone
/// else is not one the caller states here. And it must be <em>prior</em>: <see cref="Prior"/> says
/// it was obtained before the operation, and <see cref="NotPrior"/> says an authorization was
/// obtained but not before — a distinction the section's word "prior" makes and the engine keeps.
/// The engine does not read the authorization's terms: they are not published text.
/// </para>
/// </remarks>
public sealed record AtcAuthorization
{
    private AtcAuthorization(bool held, bool obtainedBeforeTheOperation, string statedBy, string? reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statedBy);
        Held = held;
        ObtainedBeforeTheOperation = obtainedBeforeTheOperation;
        StatedBy = statedBy;
        Reference = reference;
    }

    /// <summary>True when the person operating has authorization from ATC, whenever it was obtained.</summary>
    public bool Held { get; }

    /// <summary>True when that authorization was obtained before the operation, as § 107.41's "prior" requires.</summary>
    public bool ObtainedBeforeTheOperation { get; }

    /// <summary>Who is answerable for the statement. Free text; the engine does not parse it.</summary>
    public string StatedBy { get; }

    /// <summary>
    /// The authorization the caller names, when it can name one. Null when there is none to name, and
    /// saying so is honest. Free text; the engine does not parse it.
    /// </summary>
    public string? Reference { get; }

    /// <summary>A statement that the person has prior authorization from ATC: held, and obtained before the operation.</summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <param name="reference">The authorization, when the caller can name one.</param>
    /// <returns>The statement.</returns>
    public static AtcAuthorization Prior(string statedBy, string? reference = null) =>
        new(held: true, obtainedBeforeTheOperation: true, statedBy, reference);

    /// <summary>
    /// A statement that the person has authorization from ATC that was not obtained before the
    /// operation. § 107.41 excepts prior authorization, so this is not the exception; it is recorded
    /// rather than collapsed into <see cref="None"/>, because the two are not the same fact.
    /// </summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <param name="reference">The authorization, when the caller can name one.</param>
    /// <returns>The statement.</returns>
    public static AtcAuthorization NotPrior(string statedBy, string? reference = null) =>
        new(held: true, obtainedBeforeTheOperation: false, statedBy, reference);

    /// <summary>A statement that the person has no authorization from ATC.</summary>
    /// <param name="statedBy">Who is answerable for the statement.</param>
    /// <returns>The statement.</returns>
    public static AtcAuthorization None(string statedBy) =>
        new(held: false, obtainedBeforeTheOperation: false, statedBy, reference: null);

    /// <inheritdoc/>
    public override string ToString() =>
        (Held, ObtainedBeforeTheOperation, Reference) switch
        {
            (true, true, { } reference) => $"prior authorization from Air Traffic Control ({reference}) was obtained, as stated by {StatedBy}",
            (true, true, null) => $"prior authorization from Air Traffic Control was obtained, as stated by {StatedBy}",
            (true, false, { } reference) => $"authorization from Air Traffic Control ({reference}) was obtained, but not before the operation, as stated by {StatedBy}",
            (true, false, null) => $"authorization from Air Traffic Control was obtained, but not before the operation, as stated by {StatedBy}",
            _ => $"no authorization from Air Traffic Control was obtained, as stated by {StatedBy}",
        };
}
