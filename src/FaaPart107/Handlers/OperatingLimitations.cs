using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>operating-limitations</c>'s rule reads.</summary>
    /// <remarks>
    /// <para>
    /// Which of the two people § 107.51's introductory text names is asking, the waiver statement,
    /// and the facts each constituent limitation is tested against — the same facts those entries'
    /// own requests declare, stated once here because this entry asks all three of them about one
    /// operation. Nothing here is a verdict: a caller does not tell this entry whether a limitation
    /// is met, and this entry does not take it when offered.
    /// </para>
    /// <para>
    /// Every input is demanded and none is defaulted. An operation the caller has not described is
    /// not an operation that complies (rules-factory decision 0021, and <c>docs/decisions/0001</c>).
    /// </para>
    /// </remarks>
    public sealed partial class OperatingLimitationsRequest
    {
        /// <summary>
        /// Which of the two people § 107.51's introductory text binds the caller is asking about, as
        /// the caller states it. Required, and never inferred.
        /// </summary>
        public BoundPerson? Person { get; init; }

        /// <summary>The small unmanned aircraft's groundspeed, for § 107.51(a). Required.</summary>
        public Groundspeed? Groundspeed { get; init; }

        /// <summary>The small unmanned aircraft's altitude, in feet above ground level, for § 107.51(b). Required.</summary>
        public decimal? AltitudeAboveGroundLevelFeet { get; init; }

        /// <summary>
        /// What the caller states about the structure § 107.51(b)'s exception is claimed under.
        /// Required: the engine does not assume there is no structure, and does not infer one.
        /// </summary>
        public StructureStatement? Structure { get; init; }

        /// <summary>
        /// The flight visibility observed from the location of the control station, in statute
        /// miles, as the caller states it, for § 107.51(c). Required, and not negative.
        /// </summary>
        public decimal? FlightVisibilityStatuteMiles { get; init; }

        /// <summary>
        /// What the caller states about the cloud § 107.51(d)'s two minimums are distances from: how
        /// far below it and how far horizontally from it the small unmanned aircraft is, or that the
        /// aircraft is not operated near a cloud at all. Required: the engine neither invents a
        /// cloud nor assumes there is none.
        /// </summary>
        public CloudStatement? Cloud { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>operating-limitations</c>: <see cref="Compliance.CompliedWith"/>, the finding, or the rule's decline.</summary>
        /// <remarks>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The six operational facts are handed over as the caller left them, so the rule demands
        /// each after the gate, in the order § 107.51 states its limitations
        /// (<c>docs/decisions/0007</c>).
        /// </remarks>
        internal static partial Resolution<object> OperatingLimitations(Requests.OperatingLimitationsRequest request) =>
            Answer(Compliance.CompliedWith(
                request.Person,
                request.Groundspeed,
                request.AltitudeAboveGroundLevelFeet,
                request.Structure,
                request.FlightVisibilityStatuteMiles,
                request.Cloud,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
