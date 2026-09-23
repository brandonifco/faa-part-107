using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>civil-twilight-window</c>: <see cref="CivilTwilight.Windows"/>, the two periods as printed.
        /// </summary>
        /// <remarks>
        /// Nothing conditions the entry, so its request carries no inputs and this handler reads
        /// none: <c>CivilTwilightWindowRequest</c> keeps the generated shape alone, as
        /// <c>control-links-working</c>'s does. It demands no waiver statement because
        /// § 107.29(c)(1)-(2) is not suspended by <c>waivable-regulations</c> in this entry's map
        /// row; demanding one would make the engine refuse a request the map says is complete.
        /// </remarks>
        internal static partial Resolution<object> CivilTwilightWindow(Requests.CivilTwilightWindowRequest request) =>
            Resolution<object>.FromValue(CivilTwilight.Windows());
    }
}
