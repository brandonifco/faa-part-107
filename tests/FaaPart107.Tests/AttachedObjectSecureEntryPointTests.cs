using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>attached-object-secure</c>, § 107.49(e). The map records the entry's question as unresolved,
/// <c>RequiresInterpretation</c>, so the engine declines and cites this entry's own locator; these
/// pin that it declines, why, and where — a decline citing the wrong place says something false —
/// and that it answers this constituent alone, because § 107.49(e) is one sentence under two ids.
/// </summary>
public class AttachedObjectSecureEntryPointTests
{
    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Resolving_it_declines_RequiresInterpretation_citing_107_49_e()
    {
        var unresolved = Decline(EntryPoints.AttachedObjectSecure.Resolve(new AttachedObjectSecureRequest()));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.49(e)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.AttachedObjectSecure.Registered.Locator, unresolved.Locator);
        Assert.Contains("attached-object-secure", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_and_names_the_term_the_corpus_leaves_undefined()
    {
        var unresolved = Decline(EntryPoints.AttachedObjectSecure.Resolve(AttachedObjectSecureRequest.Empty));

        Assert.Contains("\"secure\"", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Equal(AttachedObject.Secure(), EntryPoints.AttachedObjectSecure.Resolve(AttachedObjectSecureRequest.Empty));
    }

    [Fact]
    public void The_decline_names_this_constituent_though_107_49_e_is_one_sentence_under_two_ids()
    {
        var unresolved = Decline(EntryPoints.AttachedObjectSecure.Resolve(AttachedObjectSecureRequest.Empty));

        // The second conjunct is the map's own separate entry, and it carries the same locator: the
        // citation cannot tell the two apart, so what the decline names has to.
        Assert.Equal(MapEntries.AttachedObjectNoAdverseEffect.Locator, unresolved.Locator);
        Assert.Contains("attached-object-secure", unresolved.Attempted, StringComparison.Ordinal);
        Assert.DoesNotContain("attached-object-no-adverse-effect", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_dictionary_dispatch_declines_the_same_way()
    {
        var unresolved = Decline(Registry.Resolve("attached-object-secure", RuleRequest.Empty));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("§ 107.49(e)", unresolved.Locator.Citation);
        Assert.Equal(Assert.Single(Registry.Citations("attached-object-secure")), unresolved.Locator);
        Assert.Contains("\"secure\"", unresolved.Attempted, StringComparison.Ordinal);
    }
}
