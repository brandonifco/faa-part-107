using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>control-links-working</c>: <see cref="ControlLinks.Working"/>, the rule's decline.
        /// </summary>
        /// <remarks>
        /// The entry declines whatever it is asked, so its request carries no inputs and this
        /// handler reads none: <c>ControlLinksWorkingRequest</c> keeps the generated shape alone,
        /// and an input the rule never reads would be surface this engine cannot justify from the
        /// map. The decline is the rule's own, naming the term the corpus leaves undefined, rather
        /// than the correspondence row's bare default.
        /// </remarks>
        internal static partial Resolution<object> ControlLinksWorking(Requests.ControlLinksWorkingRequest request) =>
            ControlLinks.Working();
    }
}
