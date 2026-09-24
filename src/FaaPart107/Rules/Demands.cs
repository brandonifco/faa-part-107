namespace FaaPart107;

/// <summary>
/// The one refusal a typed input the caller did not state gets, wherever it is demanded from.
/// </summary>
/// <remarks>
/// <para>
/// A missing input is the caller's error and not a gap in the corpus, so it is an
/// <see cref="ArgumentException"/> naming the request property — which
/// <c>OperationEvaluator</c> reports as <c>FactRequired</c> with that name on
/// <c>MissingInput</c> — and never an unresolved result, and never an answer from a default.
/// </para>
/// <para>
/// It lives here rather than in <c>Handlers</c> because, under
/// <c>docs/decisions/0008-the-waiver-gate-precedes-every-other-demand.md</c>, a gated entry's
/// non-waiver inputs are demanded by its rule after § 107.205's gate has been read, while an
/// ungated entry's are still demanded by its handler. Both say it the same way, because it is the
/// same thing being said, and <c>Handlers.Missing</c> is this method.
/// </para>
/// </remarks>
internal static class Demands
{
    /// <summary>The refusal for <paramref name="name"/>, unstated on <paramref name="entryId"/>'s request.</summary>
    /// <param name="entryId">The map entry being resolved.</param>
    /// <param name="name">The request property, as <c>nameof</c> spells it.</param>
    /// <returns>The exception to throw.</returns>
    internal static ArgumentException Missing(string entryId, string name) =>
        new($"resolving the map entry '{entryId}' needs its request's {name}, and it was not set", name);

    /// <summary>
    /// <paramref name="input"/>, or the refusal naming <paramref name="name"/> where the caller
    /// did not state it.
    /// </summary>
    /// <typeparam name="T">The input's type.</typeparam>
    /// <param name="input">What the caller stated, or null.</param>
    /// <param name="entry">The map entry being resolved.</param>
    /// <param name="name">The request property, as <c>nameof</c> spells it.</param>
    /// <returns>The stated value.</returns>
    /// <exception cref="ArgumentException"><paramref name="input"/> is null.</exception>
    internal static T Of<T>(T? input, MapEntry entry, string name)
        where T : class =>
        input ?? throw Missing(entry.Id, name);

    /// <inheritdoc cref="Of{T}(T, MapEntry, string)"/>
    internal static T Of<T>(T? input, MapEntry entry, string name)
        where T : struct =>
        input ?? throw Missing(entry.Id, name);
}
