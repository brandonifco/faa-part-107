using FaaPart107.Requests;
using RulesKernel.Resolution;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// <c>night-training-completed</c>, § 107.29(a)(1). The map records the entry's question as
/// unresolved, <c>RequiresInterpretation</c>, so the engine declines and cites this entry's own
/// locator; these pin that it declines, why, and where — a decline citing the wrong place, or
/// borrowing its out-of-scope dependency's answer, says something false.
/// </summary>
public class NightTrainingCompletedEntryPointTests
{
    private static UnresolvedResult Decline(Resolution<object> resolution) =>
        Assert.IsType<Resolution<object>.Unresolved>(resolution).Result;

    [Fact]
    public void Resolving_it_declines_RequiresInterpretation_citing_107_29_a_1()
    {
        var unresolved = Decline(EntryPoints.NightTrainingCompleted.Resolve(new NightTrainingCompletedRequest()));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("cfr-14-107", unresolved.Locator.SourceId);
        Assert.Equal("§ 107.29(a)(1)", unresolved.Locator.Citation);
        Assert.Equal(EntryPoints.NightTrainingCompleted.Registered.Locator, unresolved.Locator);
        Assert.Contains("night-training-completed", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_decline_is_the_rules_own_and_names_what_the_corpus_leaves_open()
    {
        var unresolved = Decline(EntryPoints.NightTrainingCompleted.Resolve(NightTrainingCompletedRequest.Empty));

        Assert.Contains("as applicable", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("§ 107.65", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Contains("April 6, 2021", unresolved.Attempted, StringComparison.Ordinal);
        Assert.Equal(NightTraining.Completed(), EntryPoints.NightTrainingCompleted.Resolve(NightTrainingCompletedRequest.Empty));
    }

    [Fact]
    public void The_dictionary_dispatch_declines_the_same_way()
    {
        var unresolved = Decline(Registry.Resolve("night-training-completed", RuleRequest.Empty));

        Assert.Equal(UnresolvedReason.RequiresInterpretation, unresolved.Reason);
        Assert.Equal("§ 107.29(a)(1)", unresolved.Locator.Citation);
        Assert.Equal(Assert.Single(Registry.Citations("night-training-completed")), unresolved.Locator);
        Assert.Contains("as applicable", unresolved.Attempted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_out_of_scope_dependency_declines_on_its_own_terms_and_this_entry_does_not_borrow_that_answer()
    {
        var dependency = Decline(EntryPoints.KnowledgeRecency.Resolve(KnowledgeRecencyRequest.Empty));
        var unresolved = Decline(EntryPoints.NightTrainingCompleted.Resolve(NightTrainingCompletedRequest.Empty));

        Assert.Equal(UnresolvedReason.OutsideCurrentScope, dependency.Reason);
        Assert.Equal("§ 107.65", dependency.Locator.Citation);
        Assert.NotEqual(dependency.Reason, unresolved.Reason);
        Assert.NotEqual(UnresolvedReason.MissingRulesData, unresolved.Reason);
        Assert.NotEqual(dependency.Locator, unresolved.Locator);
        Assert.Equal(MapEntries.NightTrainingCompleted.Locator, unresolved.Locator);
    }
}
