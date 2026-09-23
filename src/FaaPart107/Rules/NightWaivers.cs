using System.Globalization;
using RulesKernel.Provenance;

namespace FaaPart107;

/// <summary>
/// Whether § 107.29(d)'s second sentence terminates a certificate of waiver, and whether it has
/// terminated as of a date the caller supplied: <see cref="MapEntries.NightWaiverTermination"/>,
/// "The certificates of waiver issued prior to March 16, 2021 under § 107.200 that authorize
/// deviation from § 107.29 terminate on May 17, 2021."
/// </summary>
/// <remarks>
/// <see cref="TerminatedByThisSentence"/> is false in two different situations and says the same
/// thing in both: this sentence does not reach the certificate. It is not a finding that the
/// certificate is in force or that it has not ended — the sentence names what it terminates and is
/// silent about everything else, including the first sentence of § 107.29(d) (<c>night-waiver-bar</c>,
/// a separate entry), which bars a night operation under a waiver issued before April 21, 2021 and
/// is not read here.
/// </remarks>
/// <param name="Certificate">The certificate of waiver, as the caller described it.</param>
/// <param name="AsOf">The date the question was asked as of, as the caller supplied it.</param>
/// <param name="TerminatedByThisSentence">
/// True when the certificate is one of "the certificates of waiver" this sentence terminates:
/// issued prior to March 16, 2021, and authorizing deviation from § 107.29.
/// </param>
public sealed record NightWaiverTerminationFinding(
    WaiverCertificate Certificate,
    DateOnly AsOf,
    bool TerminatedByThisSentence)
{
    /// <summary>Where the rule is stated: <c>§ 107.29(d)</c>.</summary>
    public SourceLocator Authority => MapEntries.NightWaiverTermination.Locator;

    /// <summary>
    /// The date this sentence terminates the certificate on, <see cref="NightWaivers.TerminationDate"/>;
    /// null when this sentence does not terminate it, because then it states no date for it.
    /// </summary>
    public DateOnly? TerminatesOn => TerminatedByThisSentence ? NightWaivers.TerminationDate : null;

    /// <summary>
    /// True when this sentence terminates the certificate and <see cref="AsOf"/> is on or after the
    /// date it terminates on. The sentence states a date, not a time of day, so the comparison is a
    /// comparison of dates: on May 17, 2021 the certificate has terminated, because that is the date
    /// it terminates on.
    /// </summary>
    public bool HasTerminated => TerminatedByThisSentence && AsOf >= NightWaivers.TerminationDate;

    /// <inheritdoc/>
    public override string ToString()
    {
        var outcome = (TerminatedByThisSentence, HasTerminated) switch
        {
            (false, _) => "§ 107.29(d) does not terminate it",
            (true, false) => string.Create(
                CultureInfo.InvariantCulture,
                $"§ 107.29(d) terminates it on {NightWaivers.TerminationDate:yyyy-MM-dd}, after {AsOf:yyyy-MM-dd}"),
            (true, true) => string.Create(
                CultureInfo.InvariantCulture,
                $"§ 107.29(d) terminated it on {NightWaivers.TerminationDate:yyyy-MM-dd}, on or before {AsOf:yyyy-MM-dd}"),
        };

        return $"{Certificate}: {outcome} [{Authority}]";
    }
}

/// <summary>§ 107.29(d), second sentence: the termination of night waivers issued before March 16, 2021.</summary>
/// <remarks>
/// <para>
/// This entry has no waiver gate. § 107.205 lists the regulations a certificate of waiver may
/// authorize deviation from, and § 107.29(d) is not among them — <c>waivable-regulations</c>'s note
/// names <c>night-waiver-termination</c> as one of the entries the list does not reach — so nothing
/// here is suspended by <see cref="MapEntries.WaivableRegulations"/>.
/// </para>
/// <para>
/// Nor does it have an unresolved case. The map records it <c>clarity: clear</c>; both facts it
/// needs about the certificate are the caller's (<see cref="WaiverCertificate"/>), and so is the
/// date it is asked as of. The two dates the sentence prints are ordinary comparisons against that
/// caller-supplied date; the engine reads no clock, here or anywhere (<c>AGENTS.md</c> §8).
/// </para>
/// </remarks>
public static class NightWaivers
{
    /// <summary>
    /// The date a certificate must have been issued prior to for this sentence to terminate it,
    /// as printed: March 16, 2021. A certificate issued on it was not issued prior to it.
    /// </summary>
    public static DateOnly IssuanceCutOff { get; } = new(2021, 3, 16);

    /// <summary>The date the certificates this sentence names terminate on, as printed: May 17, 2021.</summary>
    public static DateOnly TerminationDate { get; } = new(2021, 5, 17);

    /// <summary>
    /// <see cref="MapEntries.NightWaiverTermination"/>: whether "The certificates of waiver issued
    /// prior to March 16, 2021 under § 107.200 that authorize deviation from § 107.29 terminate on
    /// May 17, 2021" reaches <paramref name="certificate"/>, and whether it has terminated as of
    /// <paramref name="asOf"/>.
    /// </summary>
    /// <param name="certificate">The certificate of waiver, as the caller describes it. Never defaulted.</param>
    /// <param name="asOf">The date the question is asked as of. Never defaulted, and never the clock's.</param>
    /// <returns>The finding. This entry has no unresolved case; see the remarks on this class.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is null.</exception>
    public static NightWaiverTerminationFinding Termination(WaiverCertificate certificate, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var reached = certificate.AuthorizesDeviationFromSection10729 && certificate.IssuedOn < IssuanceCutOff;

        return new NightWaiverTerminationFinding(certificate, asOf, reached);
    }
}
