using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>night-training-completed</c>: <see cref="NightTraining.Completed"/>, the rule's decline.
        /// </summary>
        /// <remarks>
        /// The entry declines whatever it is asked, so its request carries no inputs and this
        /// handler reads none: <c>NightTrainingCompletedRequest</c> keeps the generated shape
        /// alone. A completion date, a test record or a training record would be surface this
        /// engine cannot justify from the map — the rule never reads it, because which completion
        /// satisfies § 107.29(a)(1) is the entry's open question. The decline is the rule's own,
        /// naming what the corpus leaves open, rather than the correspondence row's bare default.
        /// </remarks>
        internal static partial Resolution<object> NightTrainingCompleted(Requests.NightTrainingCompletedRequest request) =>
            NightTraining.Completed();
    }
}
