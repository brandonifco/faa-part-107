using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>control-links-working</c>, § 107.49(c). The map records the entry's question as unresolved,
/// <c>RequiresInterpretation</c>, so the engine declines and cites this entry's own locator; these
/// pin that it declines, why, and where — a decline citing the wrong place says something false.
/// </summary>
public class ControlLinksWorkingEntryPointTests
{
    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Resolving_it_declines_RequiresInterpretation_citing_107_49_c()
    {
        var unresolved = Decline(EntryPoints.ControlLinksWorking.Resolve(new ControlLinksWorkingRequest()));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.49(c)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.ControlLinksWorking.Registered.Locator, unresolved.Locator);
        Assert.Contains("control-links-working", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_and_names_the_term_the_corpus_leaves_undefined()
    {
        var unresolved = Decline(EntryPoints.ControlLinksWorking.Resolve(ControlLinksWorkingRequest.Empty));

        Assert.Contains("working properly", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("does not define", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Equal(ControlLinks.Working(), EntryPoints.ControlLinksWorking.Resolve(ControlLinksWorkingRequest.Empty));
    }

    [Fact]
    public void The_dictionary_dispatch_declines_the_same_way()
    {
        var unresolved = Decline(Registry.Resolve("control-links-working", RuleRequest.Empty));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("§ 107.49(c)", unresolved.Locator.Citation);
        Assert.Equal(Assert.Single(Registry.Citations("control-links-working")), unresolved.Locator);
        Assert.Contains("working properly", unresolved.Attempted, StringComparison.Ordinal);
    }
}
