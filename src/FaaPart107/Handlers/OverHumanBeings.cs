using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>over-human-beings</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// Three, and not one of them is a verdict. A caller does not tell this entry whether § 107.39
    /// prohibits the operation, whether a human being is directly participating, or whether subpart D
    /// is met, and this entry does not take any of those when offered: (a) and (c) are other entries'
    /// and neither takes an input at all, and (b)'s standard arrives through the assertions
    /// dictionary under <see cref="MapEntries.ReasonableProtection"/>'s own id.
    /// </para>
    /// <para>
    /// <see cref="OverHumanBeingsRequest.Location"/> is this entry's own fact —
    /// <b>where the human being is</b> — and <see cref="OverHumanBeingsRequest.Shelter"/> is
    /// <c>reasonable-protection</c>'s — <b>which place the standard was asserted over</b>. They are
    /// two inputs because they are two facts, and § 107.39(b)'s relative clause binds them: the rule
    /// compares them and refuses an assertion about the place the human being is not. Deriving one
    /// from the other would be the engine deciding which place the caller's assertion was about,
    /// which <c>reasonable-protection</c>'s note forbids in terms.
    /// </para>
    /// </remarks>
    public sealed partial class OverHumanBeingsRequest
    {
        /// <summary>
        /// Where the human being the aircraft is operated over is located, as the caller states it:
        /// under a covered structure, inside a stationary vehicle, or under neither. Required once
        /// the entry is reachable, and never inferred.
        /// </summary>
        public HumanBeingLocation? Location { get; init; }

        /// <summary>
        /// Which of § 107.39(b)'s two places the caller's assertion for
        /// <c>reasonable-protection</c> is about, as the caller states it. Required when the human
        /// being is located under one of the two, and never inferred — not even from
        /// <see cref="Location"/>.
        /// </summary>
        public Shelter? Shelter { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.39 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>over-human-beings</c>: <see cref="Overflight.OverAHumanBeing"/>, the finding, or the
        /// rule's decline.
        /// </summary>
        /// <remarks>
        /// The waiver statement is demanded here and never defaulted, because
        /// <see cref="Waivers.Suspension"/> is what reads it and § 107.205(g) lists § 107.39
        /// (rules-factory decision 0021). The other two are passed in unresolved so the rule can
        /// demand each at the point it is owed: a waiver in force makes both irrelevant, and a human
        /// being located under neither of § 107.39(b)'s two places makes the place the standard was
        /// asserted over irrelevant too.
        /// </remarks>
        internal static partial Resolution<object> OverHumanBeings(Requests.OverHumanBeingsRequest request) =>
            Answer(Overflight.OverAHumanBeing(
                request.Location,
                request.Shelter,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
