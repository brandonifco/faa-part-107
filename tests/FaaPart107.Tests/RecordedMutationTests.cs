using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace FaaPart107.Tests;

/// <summary>
/// The converse of the gate's <c>named-tests</c> check: every test this engine runs is named by a
/// record that carries the mutation that reddens it.
/// </summary>
/// <remarks>
/// <para>
/// <c>engine-gate.py named-tests</c> asks that every test an implemented entry <em>names</em> exists
/// and ran. Nothing asked the other question, so a test nobody named passed silently — which is how
/// <c>OperationEvaluatorTests</c> and <c>DeterminismTests</c> came to hold forty-three tests with no
/// recorded mutation between them (<c>#98</c>), and how the <c>weather-minimums-met</c> row for
/// <c>The_dictionary_dispatch_refuses_rather_than_answering_from_defaults</c> could be dropped by an
/// overlay rewrite while the test still existed.
/// </para>
/// <para>
/// <b>Two records, and which one a test belongs in is a question about the test, not a preference.</b>
/// A test that proves one map entry's own rule is named by that entry's row in
/// <c>corpus-map.overlay.json</c>. A test that proves something no single entry owns — the
/// evaluator's own contract, a census over every entry, determinism across two evaluations — has no
/// overlay row available to it, because rules-factory decision 0015's merge rule 1 refuses an overlay
/// key that is not an entry id; it is named instead by <c>cross-cutting-mutations.json</c> beside this
/// file. <c>docs/decisions/0007-a-test-that-belongs-to-no-entry-records-its-mutation-beside-the-tests.md</c>
/// is that decision and the reasoning for it.
/// </para>
/// <para>
/// <b>The factory's own generated tests are out of scope, and structurally so.</b>
/// <c>tests/FaaPart107.Tests/Generated/*.g.cs</c> is a <c>generated</c> row in
/// <c>scripts/factory/ownership.py</c>: those tests are rewritten from the map by every
/// <c>factory produce</c> and are not this engine's to name a mutation for. The exemption is read off
/// that directory rather than written down as a list of class names, so a class cannot be exempted by
/// being added to a list here — it would have to be generated.
/// </para>
/// </remarks>
public class RecordedMutationTests
{
    private static readonly string EngineRoot = FindEngineRoot();

    /// <summary>
    /// Every <c>[Fact]</c> and <c>[Theory]</c> in the built test assembly, as
    /// <c>Class.Method</c> — the name both records and the gate's <c>named-tests</c> use.
    /// </summary>
    /// <remarks>
    /// Read off the assembly and never off the source, so a test in a file no scan thought to look
    /// at is still counted. <c>TheoryAttribute</c> derives from <c>FactAttribute</c>, so one test
    /// covers both.
    /// </remarks>
    private static IEnumerable<(string Name, Type Class)> EveryTest() =>
        typeof(RecordedMutationTests).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(FactAttribute), inherit: true))
            .Select(method => ($"{method.DeclaringType!.Name}.{method.Name}", method.DeclaringType!))
            .Distinct()
            .OrderBy(test => test.Item1, StringComparer.Ordinal);

    /// <summary>The test classes <c>factory produce</c> writes, read off the directory it writes them into.</summary>
    private static HashSet<string> FactoryGeneratedClasses()
    {
        var generated = Path.Combine(EngineRoot, "tests", "FaaPart107.Tests", "Generated");
        return
        [
            .. Directory.EnumerateFiles(generated, "*.g.cs")
                .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"\bclass\s+(\w+)"))
                .Select(match => match.Groups[1].Value),
        ];
    }

    /// <summary>Every test an entry's overlay row names, with the mutation that row records.</summary>
    private static Dictionary<string, string> NamedByAnEntry()
    {
        using var overlay = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(EngineRoot, "corpus-map.overlay.json")));
        var named = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in overlay.RootElement.EnumerateObject())
        {
            if (!entry.Value.TryGetProperty("tests", out var tests))
            {
                continue;
            }

            foreach (var test in tests.EnumerateArray())
            {
                named[test.GetProperty("test").GetString() ?? string.Empty] =
                    test.GetProperty("mutation").GetString() ?? string.Empty;
            }
        }

        return named;
    }

    /// <summary>Every test the cross-cutting record names, with the mutation it records.</summary>
    private static Dictionary<string, string> NamedByNoEntry()
    {
        var path = Path.Combine(EngineRoot, "tests", "FaaPart107.Tests", "cross-cutting-mutations.json");
        using var record = JsonDocument.Parse(File.ReadAllBytes(path));
        var named = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var test in record.RootElement.GetProperty("tests").EnumerateArray())
        {
            named[test.GetProperty("test").GetString() ?? string.Empty] =
                test.GetProperty("mutation").GetString() ?? string.Empty;
        }

        return named;
    }

    [Fact]
    public void Every_test_on_the_semantic_surface_is_named_by_a_record_that_carries_its_mutation()
    {
        var generatedByTheFactory = FactoryGeneratedClasses();
        var ours = EveryTest().Where(test => !generatedByTheFactory.Contains(test.Class.Name)).ToArray();

        // The census is only worth anything if it is looking at something: the two files #98 was
        // filed about, and one ordinary entry-point file, must all be inside it.
        Assert.Contains(ours, test => test.Class.Name == nameof(OperationEvaluatorTests));
        Assert.Contains(ours, test => test.Class.Name == nameof(DeterminismTests));
        Assert.Contains(ours, test => test.Class.Name == nameof(WeatherMinimumsMetEntryPointTests));
        Assert.NotEmpty(generatedByTheFactory);

        var byAnEntry = NamedByAnEntry();
        var byNoEntry = NamedByNoEntry();

        // 1. Nothing runs unnamed. This is the check that did not exist.
        var unnamed = ours
            .Select(test => test.Name)
            .Where(name => !byAnEntry.ContainsKey(name) && !byNoEntry.ContainsKey(name))
            .ToArray();
        Assert.True(
            unnamed.Length == 0,
            "these tests run and no record names the mutation that reddens them, so nobody has watched them "
            + "fail (AGENTS.md §7). A test that proves one map entry's rule belongs in that entry's `tests` in "
            + "corpus-map.overlay.json; one that proves something no single entry owns belongs in "
            + $"tests/FaaPart107.Tests/cross-cutting-mutations.json (docs/decisions/0007): {string.Join(", ", unnamed)}");

        // 2. And nothing is named that does not run: a test deleted or renamed out from under its row
        //    leaves the record claiming evidence for something that is not there. The gate's
        //    named-tests does this for the overlay; this does it for the cross-cutting record.
        var running = ours.Select(test => test.Name).ToHashSet(StringComparer.Ordinal);
        var gone = byNoEntry.Keys.Where(name => !running.Contains(name)).Order(StringComparer.Ordinal).ToArray();
        Assert.True(
            gone.Length == 0,
            "cross-cutting-mutations.json names tests that no longer exist: " + string.Join(", ", gone));

        // 3. One record each, so the two cannot disagree about the same test.
        var twice = byNoEntry.Keys.Where(byAnEntry.ContainsKey).Order(StringComparer.Ordinal).ToArray();
        Assert.True(
            twice.Length == 0,
            "these tests are named by an entry's overlay row and by cross-cutting-mutations.json, so two "
            + "records can disagree about one test: " + string.Join(", ", twice));

        // 4. A row with no mutation in it is a row that proves nothing. The packaged
        //    check-map.py --phase consumer says this of an overlay row; this says it of the other record.
        var empty = byNoEntry.Where(row => string.IsNullOrWhiteSpace(row.Value)).Select(row => row.Key)
            .Order(StringComparer.Ordinal).ToArray();
        Assert.True(
            empty.Length == 0,
            "these cross-cutting rows record no mutation, and a test nobody has seen go red is not evidence: "
            + string.Join(", ", empty));
    }

    private static string FindEngineRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FaaPart107.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"no engine root above {AppContext.BaseDirectory}: this test reads corpus-map.overlay.json and "
            + "cross-cutting-mutations.json from the engine's own working tree, as the generated "
            + "ProvenanceTests reads provenance.json");
    }
}
