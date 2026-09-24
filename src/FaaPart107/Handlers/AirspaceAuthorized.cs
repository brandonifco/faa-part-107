using RulesKernel.Resolution;

namespace FaaPart107.Requests
{
    /// <summary>The inputs <c>airspace-authorized</c>'s rule reads.</summary>
    public sealed partial class AirspaceAuthorizedRequest
    {
        /// <summary>The airspace the operation is in, as the caller states it. Required, and never inferred: the entry's note makes it an input.</summary>
        public AirspaceClass? Airspace { get; init; }

        /// <summary>What the caller states about prior authorization from Air Traffic Control. Required, and never inferred in either direction.</summary>
        public AtcAuthorization? Authorization { get; init; }

        /// <summary>Whether a certificate of waiver authorizing deviation from § 107.41 is in force, as the caller states it. Required.</summary>
        public WaiverStatement? Waiver { get; init; }
    }
}

namespace FaaPart107
{
    internal static partial class Handlers
    {
        /// <summary><c>airspace-authorized</c>: <see cref="Airspace.Authorized"/>, the finding, or the rule's decline.</summary>
        /// <remarks>
        /// The waiver statement is demanded here, and only it, because
        /// <see cref="Waivers.Suspension"/> is what reads it and the gate cannot run without it.
        /// The airspace and the ATC authorization statement are handed over as the caller left
        /// them, so the rule demands each after the gate (<c>docs/decisions/0007</c>).
        /// </remarks>
        internal static partial Resolution<object> AirspaceAuthorized(Requests.AirspaceAuthorizedRequest request) =>
            Answer(Airspace.Authorized(
                request.Airspace,
                request.Authorization,
                Demand(request.Waiver, request.EntryId, nameof(request.Waiver))));
    }
}
