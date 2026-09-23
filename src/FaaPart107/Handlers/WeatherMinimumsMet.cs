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
        /// How far below the cloud the small unmanned aircraft is, in feet, as the caller states it.
        /// Required, and not negative.
        /// </summary>
        public decimal? FeetBelowCloud { get; init; }

        /// <summary>
        /// How far horizontally from the cloud the small unmanned aircraft is, in feet, as the
        /// caller states it. Required, and not negative.
        /// </summary>
        public decimal? FeetHorizontallyFromCloud { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.51 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>weather-minimums-met</c>: <see cref="Weather.MinimumsMet"/>, the finding, or the rule's decline.</summary>
        internal static partial Resolution<object> WeatherMinimumsMet(Requests.WeatherMinimumsMetRequest request) =>
            Answer(Weather.MinimumsMet(
                Demand(request.FlightVisibilityStatuteMiles, request.EntryId, nameof(request.FlightVisibilityStatuteMiles)),
                Demand(request.FeetBelowCloud, request.EntryId, nameof(request.FeetBelowCloud)),
                Demand(request.FeetHorizontallyFromCloud, request.EntryId, nameof(request.FeetHorizontallyFromCloud)),
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
