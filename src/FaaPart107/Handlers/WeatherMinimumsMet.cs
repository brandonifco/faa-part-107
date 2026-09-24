using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>weather-minimums-met</c>'s rule reads.</summary>
    /// <remarks>
    /// The operation's own facts, and the waiver statement. The stated flight visibility is here
    /// because the entry is the one that applies § 107.51(c) and a caller must be able to state the
    /// situation it applies to; the rule records it and names it in its decline, and never treats it
    /// as the section's defined "flight visibility", which turns on a degree of prominence the
    /// corpus does not fix (<c>prominent-objects</c>).
    /// </remarks>
    public sealed partial class WeatherMinimumsMetRequest
    {
        /// <summary>
        /// The flight visibility observed from the location of the control station, in statute
        /// miles, as the caller states it. Required, and not negative.
        /// </summary>
        public decimal? FlightVisibilityStatuteMiles { get; init; }

        /// <summary>
        /// What the caller states about the cloud § 107.51(d)'s minimums are distances from: how far
        /// below it and how far horizontally from it the small unmanned aircraft is, or that the
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
        /// <summary><c>weather-minimums-met</c>: <see cref="Weather.MinimumsMet"/>, the finding, or the rule's decline.</summary>
        /// <remarks>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The flight visibility and the cloud statement are handed over as the caller left them,
        /// so the rule demands each after the gate (<c>docs/decisions/0008</c>).
        /// </remarks>
        internal static partial Resolution<object> WeatherMinimumsMet(Requests.WeatherMinimumsMetRequest request) =>
            Answer(Weather.MinimumsMet(
                request.FlightVisibilityStatuteMiles,
                request.Cloud,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
