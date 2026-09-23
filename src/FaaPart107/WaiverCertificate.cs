using System.Globalization;

namespace FaaPart107;

/// <summary>
/// A certificate of waiver as the caller describes it: when it was issued, and whether it
/// authorizes deviation from § 107.29. These are the two facts § 107.29(d)'s second sentence
/// identifies the certificates it terminates by, and neither is in the corpus.
/// </summary>
/// <remarks>
/// <para>
/// "The certificates of waiver issued prior to March 16, 2021 under § 107.200 that authorize
/// deviation from § 107.29 terminate on May 17, 2021." A certificate of waiver is an instrument
/// the Administrator issued to one operator under § 107.200, and this map puts that section out of
/// scope (<c>waiver-policy</c>): when it was issued and what it authorizes deviation from are
/// facts about the instrument, not text this engine can read. <c>night-waiver-termination</c>'s
/// own cross-reference records the second of them as the caller's — "Whether a waiver does is a
/// fact about the waiver the caller supplies, not a rule of this section evaluated through the
/// pointer" — and the first is a fact about the same instrument.
/// </para>
/// <para>
/// So the engine owes both what it owes any assertion (rules-factory decision 0021): demand them,
/// attribute them, record them alongside the outcome, and never infer them in either direction.
/// There is no default certificate, and an entry whose request carries none refuses to resolve.
/// </para>
/// <para>
/// This is a fact the caller states about one instrument, not a rule, so the map has no entry for
/// it. It is distinct from <see cref="WaiverStatement"/>, which states whether a waiver is
/// <em>in force</em> for the § 107.205 gate: whether a certificate has terminated is what
/// <c>night-waiver-termination</c> works out, so it cannot also be an input to it.
/// </para>
/// </remarks>
/// <param name="IssuedOn">The date the certificate was issued, as the caller states it.</param>
/// <param name="AuthorizesDeviationFromSection10729">
/// True when the certificate authorizes deviation from § 107.29, as the caller states it. The
/// engine does not read the certificate's terms: they are not published text.
/// </param>
/// <param name="StatedBy">Who is answerable for the statement. Free text; the engine does not parse it.</param>
public sealed record WaiverCertificate(DateOnly IssuedOn, bool AuthorizesDeviationFromSection10729, string StatedBy)
{
    /// <summary>Who stated it, checked to be non-empty.</summary>
    public string StatedBy { get; } = CheckText(StatedBy, nameof(StatedBy));

    private static string CheckText(string text, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, name);
        return text;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var authorizes = AuthorizesDeviationFromSection10729 ? "authorizes" : "does not authorize";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"a certificate of waiver issued on {IssuedOn:yyyy-MM-dd} that {authorizes} deviation from § 107.29, as stated by {StatedBy}");
    }
}
