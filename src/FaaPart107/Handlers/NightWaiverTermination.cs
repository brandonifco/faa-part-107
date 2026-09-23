using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>night-waiver-termination</c>'s rule reads.</summary>
    public sealed partial class NightWaiverTerminationRequest
    {
        /// <summary>
        /// The certificate of waiver § 107.29(d) is asked about, as the caller describes it: when it
        /// was issued, and whether it authorizes deviation from § 107.29. Required.
        /// </summary>
        public WaiverCertificate? Certificate { get; init; }

        /// <summary>
        /// The date the question is asked as of, which the sentence's "terminate on May 17, 2021" is
        /// compared with. Required: the engine has no clock to fall back on, and a date it invented
        /// would be an answer the caller never asked for (<c>AGENTS.md</c> §8).
        /// </summary>
        public DateOnly? AsOf { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary>
        /// <c>night-waiver-termination</c>: <see cref="NightWaivers.Termination"/>, the finding. The
        /// entry has no unresolved case, so the handler resolves to a value or refuses an input it
        /// was not given.
        /// </summary>
        internal static partial Resolution<object> NightWaiverTermination(Requests.NightWaiverTerminationRequest request) =>
            Resolution<object>.FromValue(NightWaivers.Termination(
                Demand(request.Certificate, request.EntryId, nameof(request.Certificate)),
                Demand(request.AsOf, request.EntryId, nameof(request.AsOf))));
    }
}
