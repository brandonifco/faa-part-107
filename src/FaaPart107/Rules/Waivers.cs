using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// The waiver gate, <see cref="MapEntries.WaivableRegulations"/> (§ 107.205), as an entry it
/// suspends reads it (rules-factory decision 0021, and the reading beside the correspondence table
/// in <c>docs/corpus-map.md</c>).
/// </summary>
public static class Waivers
{
    /// <summary>
    /// The decline <paramref name="entry"/> answers while <paramref name="statement"/> says a waiver of
    /// <paramref name="regulation"/> is in force: <see cref="UnresolvedReason.OutsideCurrentScope"/>, citing
    /// the gate, with the statement recorded in what was attempted. Null while the statement says none is,
    /// and the entry is then evaluated as though it had no gate.
    /// </summary>
    /// <param name="entry">The suspended entry.</param>
    /// <param name="regulation">The regulation § 107.205 lists that states <paramref name="entry"/>.</param>
    /// <param name="statement">What the caller states. Never defaulted.</param>
    /// <returns>The decline, or null.</returns>
    /// <exception cref="ArgumentException">The statement is about another regulation.</exception>
    public static UnresolvedResult? Suspension(MapEntry entry, string regulation, WaiverStatement statement)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(statement);
        if (!string.Equals(statement.Regulation, regulation, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"the map entry '{entry.Id}' [{entry.Locator.Citation}] is stated by {regulation}, "
                + $"and the waiver statement is about {statement.Regulation}",
                nameof(statement));
        }

        return statement.InForce
            ? new UnresolvedResult(
                UnresolvedReason.OutsideCurrentScope,
                $"resolve the map entry '{entry.Id}' [{entry.Locator.Citation}] while {statement}",
                MapEntries.WaivableRegulations.Locator)
            : null;
    }
}
