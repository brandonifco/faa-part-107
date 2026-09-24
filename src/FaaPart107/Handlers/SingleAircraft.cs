using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>single-aircraft</c>'s rule reads.</summary>
    public sealed partial class SingleAircraftRequest
    {
        /// <summary>
        /// The person § 107.35 binds, as the caller names them. Free text; the engine does not parse
        /// it. Required: the rule prohibits what one person does at the same time, so a set of
        /// engagements belongs to somebody or it is not the sentence's subject.
        /// </summary>
        public string? Person { get; init; }

        /// <summary>
        /// Every unmanned aircraft that person is, at the same time, manipulating the flight controls
        /// of, acting as remote pilot in command in the operation of, or acting as visual observer in
        /// the operation of — each with the role. Required; empty states that there are none.
        /// </summary>
        public IReadOnlyList<AircraftEngagement>? Engagements { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.35 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>single-aircraft</c>: <see cref="MultipleAircraft.AtTheSameTime"/>, the finding, or the rule's decline.</summary>
        /// <remarks>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The person and the engagements are handed over as the caller left them, so the rule
        /// demands each after the gate (<c>docs/decisions/0007</c>).
        /// </remarks>
        internal static partial Resolution<object> SingleAircraft(Requests.SingleAircraftRequest request) =>
            Answer(MultipleAircraft.AtTheSameTime(
                request.Person,
                request.Engagements,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
