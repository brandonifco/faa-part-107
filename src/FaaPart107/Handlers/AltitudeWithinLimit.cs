using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>altitude-within-limit</c>'s rule reads.</summary>
    public sealed partial class AltitudeWithinLimitRequest
    {
        /// <summary>The small unmanned aircraft's altitude, in feet above ground level. Required.</summary>
        public decimal? AltitudeAboveGroundLevelFeet { get; init; }

        /// <summary>
        /// What the caller states about the structure § 107.51(b)'s exception is claimed under. Required:
        /// the engine does not assume there is no structure, and does not infer one.
        /// </summary>
        public StructureStatement? Structure { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>altitude-within-limit</c>: <see cref="Altitude.Within"/>, the finding, or the rule's decline.</summary>
        /// <remarks>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The altitude and the structure statement are handed over as the caller left them, so the
        /// rule demands each after the gate (<c>docs/decisions/0007</c>).
        /// </remarks>
        internal static partial Resolution<object> AltitudeWithinLimit(Requests.AltitudeWithinLimitRequest request) =>
            Answer(Altitude.Within(
                request.AltitudeAboveGroundLevelFeet,
                request.Structure,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
