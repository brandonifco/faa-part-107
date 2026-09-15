using RulesKernel.Identity;

namespace FaaPart107;

/// <summary>The engine's ruleset.</summary>
public static class Ruleset
{
    /// <summary>
    /// The engine's replay identity: the ruleset, the replay schema and the pinned corpus. It names no
    /// random algorithm, because the corpus declares <c>randomness: none</c> (rules-factory decision 0019)
    /// and the engine draws no random value.
    /// </summary>
    public static ReplayCompatibilityIdentity Identity { get; } = new(
        ruleset: new RulesetVersion("faa-part-107", 1),
        replaySchema: new ReplaySchemaVersion(1),
        sourceBaselines: [MapEntries.Baseline]);
}
