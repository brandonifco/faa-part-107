using RulesKernel.Resolution;

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>attached-object-secure</c>: <see cref="AttachedObject.Secure"/>, the rule's decline.
        /// </summary>
        /// <remarks>
        /// The entry declines whatever it is asked, so its request carries no inputs and this
        /// handler reads none: <c>AttachedObjectSecureRequest</c> keeps the generated shape alone,
        /// and an input the rule never reads — whether an object is attached, how it is fastened —
        /// would be surface this engine cannot justify from the map. The decline is the rule's own,
        /// naming the term the corpus leaves undefined, rather than the correspondence row's bare
        /// default. It answers this constituent only: the second conjunct of § 107.49(e) is the map's
        /// separate <c>attached-object-no-adverse-effect</c>, which this handler neither answers nor
        /// consults.
        /// </remarks>
        internal static partial Resolution<object> AttachedObjectSecure(Requests.AttachedObjectSecureRequest request) =>
            AttachedObject.Secure();
    }
}
