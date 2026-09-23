using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>night-waiver-termination</c>, § 107.29(d)'s second sentence, resolved through
/// <see cref="EntryPoints.NightWaiverTermination"/> only: "The certificates of waiver issued prior
/// to March 16, 2021 under § 107.200 that authorize deviation from § 107.29 terminate on
/// May 17, 2021."
/// </summary>
/// <remarks>
/// <para>
/// The cases the entry's note names: a waiver issued before March 16, 2021; one issued between
/// March 16 and April 21, 2021, which this sentence does not terminate; and the termination date
/// itself. Both dates the sentence prints are checked at, just below and just above — the issuance
/// cut-off against the certificate's issuance date, the termination date against the date the
/// caller asks as of.
/// </para>
/// <para>
/// April 21, 2021 is the first sentence's cut-off (<c>night-waiver-bar</c>, a separate entry), not
/// this one's. It appears below only as an issuance date this sentence still does not reach.
/// </para>
/// </remarks>
public class NightWaiverTerminationEntryPointTests
{
    private const string Caller = nameof(NightWaiverTerminationEntryPointTests);

    private static readonly DateOnly IssuanceCutOff = new(2021, 3, 16);

    private static readonly DateOnly TerminationDate = new(2021, 5, 17);

    /// <summary>A date well after both, so a case about issuance is not also a case about the termination date.</summary>
    private static readonly DateOnly LongAfter = new(2026, 1, 1);

    private static NightWaiverTerminationFinding Finding(DateOnly issuedOn, bool authorizes, DateOnly asOf) =>
        Assert.IsType<NightWaiverTerminationFinding>(
            Assert.IsType<Resolution<object>.Resolved>(EntryPoints.NightWaiverTermination.Resolve(
                new NightWaiverTerminationRequest
                {
                    Certificate = new WaiverCertificate(issuedOn, authorizes, Caller),
                    AsOf = asOf,
                })).Value);

    public static TheoryData<DateOnly> IssuedPriorToTheCutOff => new()
    {
        new DateOnly(2016, 8, 29),
        new DateOnly(2020, 1, 1),
        new DateOnly(2021, 1, 1),

        // Just below March 16, 2021.
        new DateOnly(2021, 3, 15),
    };

    [Theory]
    [MemberData(nameof(IssuedPriorToTheCutOff))]
    public void A_certificate_issued_before_March_16_2021_that_authorizes_deviation_from_107_29_is_terminated_on_May_17_2021_citing_107_29_d(DateOnly issuedOn)
    {
        var finding = Finding(issuedOn, authorizes: true, LongAfter);

        Assert.True(finding.TerminatedByThisSentence);
        Assert.Equal(new DateOnly(2021, 5, 17), finding.TerminatesOn);
        Assert.True(finding.HasTerminated);
        Assert.Equal("cfr-14-107", finding.Authority.SourceId);
        Assert.Equal("§ 107.29(d)", finding.Authority.Citation);
        Assert.Equal(EntryPoints.NightWaiverTermination.Registered.Locator, finding.Authority);
    }

    public static TheoryData<DateOnly> IssuedOnOrAfterTheCutOff => new()
    {
        // At March 16, 2021: issued on it is not issued prior to it.
        new DateOnly(2021, 3, 16),

        // Just above.
        new DateOnly(2021, 3, 17),

        // Between March 16 and April 21, 2021 — the note's second case.
        new DateOnly(2021, 4, 1),
        new DateOnly(2021, 4, 20),

        // The first sentence's cut-off, and the day after it: still not this sentence's.
        new DateOnly(2021, 4, 21),
        new DateOnly(2021, 4, 22),

        // The termination date, as an issuance date.
        new DateOnly(2021, 5, 17),
    };

    [Theory]
    [MemberData(nameof(IssuedOnOrAfterTheCutOff))]
    public void A_certificate_issued_on_or_after_March_16_2021_is_not_terminated_by_this_sentence(DateOnly issuedOn)
    {
        var finding = Finding(issuedOn, authorizes: true, LongAfter);

        Assert.False(finding.TerminatedByThisSentence);
        Assert.Null(finding.TerminatesOn);
        Assert.False(finding.HasTerminated);
        Assert.Equal("§ 107.29(d)", finding.Authority.Citation);
    }

    [Theory]
    [InlineData(2016, 8, 29)]
    [InlineData(2021, 3, 15)]
    [InlineData(2021, 3, 16)]
    [InlineData(2021, 4, 20)]
    public void A_certificate_that_does_not_authorize_deviation_from_107_29_is_not_terminated_by_this_sentence(int year, int month, int day)
    {
        var finding = Finding(new DateOnly(year, month, day), authorizes: false, LongAfter);

        Assert.False(finding.TerminatedByThisSentence);
        Assert.Null(finding.TerminatesOn);
        Assert.False(finding.HasTerminated);
    }

    public static TheoryData<DateOnly, bool> AsOfTheTerminationDate => new()
    {
        { new DateOnly(2021, 3, 16), false },

        // Just below May 17, 2021.
        { new DateOnly(2021, 5, 16), false },

        // The termination date itself: the date it terminates on is the date it has terminated.
        { new DateOnly(2021, 5, 17), true },

        // Just above.
        { new DateOnly(2021, 5, 18), true },

        { new DateOnly(2026, 1, 1), true },
    };

    [Theory]
    [MemberData(nameof(AsOfTheTerminationDate))]
    public void It_has_terminated_on_and_after_May_17_2021_and_not_before(DateOnly asOf, bool hasTerminated)
    {
        var finding = Finding(new DateOnly(2021, 3, 15), authorizes: true, asOf);

        Assert.True(finding.TerminatedByThisSentence);
        Assert.Equal(new DateOnly(2021, 5, 17), finding.TerminatesOn);
        Assert.Equal(hasTerminated, finding.HasTerminated);
        Assert.Equal(asOf, finding.AsOf);
    }

    [Fact]
    public void The_finding_records_the_certificate_and_the_date_it_was_asked_as_of()
    {
        var certificate = new WaiverCertificate(new DateOnly(2021, 3, 15), AuthorizesDeviationFromSection10729: true, "the remote pilot in command");
        var asOf = new DateOnly(2021, 5, 16);

        var finding = Assert.IsType<NightWaiverTerminationFinding>(
            Assert.IsType<Resolution<object>.Resolved>(EntryPoints.NightWaiverTermination.Resolve(
                new NightWaiverTerminationRequest { Certificate = certificate, AsOf = asOf })).Value);

        Assert.Same(certificate, finding.Certificate);
        Assert.Equal(asOf, finding.AsOf);
        Assert.Equal("the remote pilot in command", finding.Certificate.StatedBy);
        Assert.Equal(new DateOnly(2021, 3, 15), finding.Certificate.IssuedOn);
        Assert.True(finding.Certificate.AuthorizesDeviationFromSection10729);
    }

    [Fact]
    public void Without_a_certificate_it_refuses_rather_than_infer_one()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.NightWaiverTermination.Resolve(
            new NightWaiverTerminationRequest { AsOf = new DateOnly(2026, 1, 1) }));

        Assert.Equal(nameof(NightWaiverTerminationRequest.Certificate), error.ParamName);
    }

    [Fact]
    public void Without_a_date_it_refuses_rather_than_read_the_clock()
    {
        var error = Assert.Throws<ArgumentException>(() => EntryPoints.NightWaiverTermination.Resolve(
            new NightWaiverTerminationRequest
            {
                Certificate = new WaiverCertificate(new DateOnly(2021, 3, 15), AuthorizesDeviationFromSection10729: true, Caller),
            }));

        Assert.Equal(nameof(NightWaiverTerminationRequest.AsOf), error.ParamName);
    }

    [Fact]
    public void The_dictionary_dispatch_refuses_rather_than_answering_from_defaults()
    {
        var error = Assert.Throws<ArgumentException>(() => Registry.Resolve("night-waiver-termination", RuleRequest.Empty));

        Assert.Equal(nameof(NightWaiverTerminationRequest.Certificate), error.ParamName);
        Assert.Contains("night-waiver-termination", error.Message, StringComparison.Ordinal);
        Assert.Equal(
            new RulesKernel.Provenance.SourceLocator("cfr-14-107", "§ 107.29(d)"),
            Assert.Single(Registry.Citations("night-waiver-termination")));
    }
}
