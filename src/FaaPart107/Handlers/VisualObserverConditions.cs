using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>visual-observer-conditions</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// Four, and not one of them is a verdict. The chapeau's condition, which decides whether
    /// § 107.33 applies at all; who exercised § 107.31(a)'s ability, which is
    /// <c>visual-line-of-sight</c>'s own input and is needed because § 107.33(b) is answered from
    /// that entry; and the two waiver statements the § 107.205 gate owes — one about § 107.33, which
    /// § 107.205(d) lists and which this entry and its (a) and (c) constituents share, and one about
    /// § 107.31, which § 107.205(c) lists separately and which <c>visual-line-of-sight</c> is asked
    /// under. A waiver of one is not a waiver of the other, so neither statement stands in for the
    /// other and neither is defaulted.
    /// </para>
    /// <para>
    /// There is nothing here about communication, and nothing about scanning or awareness.
    /// § 107.33(a)'s question the map holds open, so an input describing the communication would be
    /// surface suggesting the engine weighed it; § 107.33(c) is <c>kind: assertion</c>, and § 107.31(a)'s
    /// ability is too, so both arrive the way an assertion entry's value always arrives — through
    /// the assertions dictionary, under <em>that</em> entry's id
    /// (<see cref="RuleRequest.Assert"/>), never as an input this entry could default or restate.
    /// </para>
    /// <para>
    /// Every declared input is demanded. An operation the caller has not described is not an
    /// operation without a visual observer (rules-factory decision 0021, and
    /// <c>docs/decisions/0001</c>).
    /// </para>
    /// </remarks>
    public sealed partial class VisualObserverConditionsRequest
    {
        /// <summary>
        /// Whether a visual observer is used during the aircraft operation, as the caller states it:
        /// § 107.33's chapeau's condition. Required, and never inferred in either direction.
        /// </summary>
        public VisualObserverUse? Use { get; init; }

        /// <summary>
        /// Who exercised the ability described in § 107.31(a) throughout the entire flight, as the
        /// caller states it — <c>visual-line-of-sight</c>'s input, which § 107.33(b) is answered
        /// through. Required.
        /// </summary>
        public ExerciseOfTheAbility? Exercise { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.33 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }

        /// <summary>
        /// Whether a certificate of waiver authorizing deviation from § 107.31 is in force, as the
        /// caller states it: the statement <c>visual-line-of-sight</c> is asked under for
        /// § 107.33(b). Required, and separate from <see cref="Waiver"/> — § 107.205 lists the two
        /// regulations in different paragraphs.
        /// </summary>
        public WaiverStatement? VisualLineOfSightWaiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>visual-observer-conditions</c>: <see cref="Observers.Conditions"/> — this entry's own
        /// § 107.33 waiver gate, then the chapeau's condition, then each of the section's three
        /// requirements as the entry that answers it answers it.
        /// </summary>
        /// <remarks>
        /// The assertions dictionary is handed on untouched, because the values it must carry are
        /// <c>unaided-visual-contact</c>'s and <c>observer-coordination</c>'s and this entry asserts
        /// nothing of its own: the map gives <c>visual-observer-conditions</c> no <c>assertedBy</c>,
        /// so a value asserted under this entry's own id is not an input to anything here.
        /// <para>
        /// <see cref="Requests.VisualObserverConditionsRequest.Waiver"/> is demanded here, and only
        /// it, because <see cref="Waivers.Suspension"/> is what reads it and this entry's gate
        /// cannot run without it. The other three — the chapeau's condition, the exercise of
        /// § 107.31(a)'s ability, and § 107.31's own waiver statement — are handed over as the
        /// caller left them, so the rule demands each after the gate (<c>docs/decisions/0008</c>).
        /// <see cref="Requests.VisualObserverConditionsRequest.VisualLineOfSightWaiver"/> is among
        /// them because it is § 107.31's gate and not this entry's: a waiver of § 107.33 suspends
        /// this entry whatever § 107.31's statement says.
        /// </para>
        /// </remarks>
        internal static partial Resolution<object> VisualObserverConditions(Requests.VisualObserverConditionsRequest request) =>
            Answer(Observers.Conditions(
                request.Use,
                request.Exercise,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.VisualLineOfSightWaiver,
                request.Assertions));
    }
}
