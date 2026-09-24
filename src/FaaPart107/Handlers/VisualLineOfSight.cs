using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>visual-line-of-sight</c>'s rule reads.</summary>
    /// <remarks>
    /// Two, and neither of them is § 107.31(a)'s ability. That value belongs to
    /// <c>unaided-visual-contact</c>, which this entry depends on, and it arrives the way an
    /// assertion entry's value always arrives — through the assertions dictionary, under
    /// <em>that</em> entry's id (<see cref="RuleRequest.Assert"/>), never as an input this entry
    /// could default or restate. What is an input here is § 107.31(b)'s own subject, who exercised
    /// the ability throughout the entire flight, and the waiver statement the entry's own
    /// <c>suspendedBy</c> row owes.
    /// </remarks>
    public sealed partial class VisualLineOfSightRequest
    {
        /// <summary>
        /// Who exercised the ability described in § 107.31(a) throughout the entire flight, as the
        /// caller states it. Required.
        /// </summary>
        public ExerciseOfTheAbility? Exercise { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.31 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>visual-line-of-sight</c>: <see cref="LineOfSight.Maintained"/> — this entry's own
        /// waiver gate, then § 107.31(a) through the dependency, then § 107.31(b)'s membership test.
        /// </summary>
        /// <remarks>
        /// The assertions dictionary is handed on untouched, because the value it must carry is
        /// <c>unaided-visual-contact</c>'s and this entry asserts nothing of its own: the map gives
        /// <c>visual-line-of-sight</c> no <c>assertedBy</c>, so a value asserted under this entry's
        /// own id is not an input to anything here.
        /// <para>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The exercise of the ability is handed over as the caller left it, so the rule demands it
        /// after the gate (<c>docs/decisions/0007</c>).
        /// </para>
        /// </remarks>
        internal static partial Resolution<object> VisualLineOfSight(Requests.VisualLineOfSightRequest request) =>
            Answer(LineOfSight.Maintained(
                request.Exercise,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver)),
                request.Assertions));
    }
}
