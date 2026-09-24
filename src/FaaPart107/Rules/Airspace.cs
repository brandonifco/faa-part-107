using RulesKernel.Provenance;
using RulesKernel.Resolution;

namespace FaaPart107;

/// <summary>
/// Whether § 107.41 permits an operation in the airspace the caller states:
/// <see cref="MapEntries.AirspaceAuthorized"/>, "No person may operate a small unmanned aircraft in
/// Class B, Class C, or Class D airspace or within the lateral boundaries of the surface area of
/// Class E airspace designated for an airport unless that person has prior authorization from Air
/// Traffic Control (ATC)."
/// </summary>
/// <param name="Airspace">The airspace the operation is in, as the caller stated it.</param>
/// <param name="Authorization">What the caller stated about authorization from ATC, recorded whether or not the section asks for it.</param>
/// <param name="AuthorizationRequired">True when the airspace is one the section names, so prior ATC authorization is what lifts its prohibition.</param>
/// <param name="MayOperate">True when § 107.41 does not prohibit the operation. This section is the whole of the finding: another may prohibit it.</param>
/// <param name="Waiver">The caller's waiver statement the finding was resolved under, recorded with it.</param>
public sealed record AirspaceFinding(
    AirspaceClass Airspace,
    AtcAuthorization Authorization,
    bool AuthorizationRequired,
    bool MayOperate,
    WaiverStatement Waiver)
{
    /// <summary>Where the rule is stated: <c>§ 107.41</c>.</summary>
    public SourceLocator Authority => MapEntries.AirspaceAuthorized.Locator;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Airspace}: a small unmanned aircraft {(MayOperate ? "may" : "may not")} be operated there, and "
        + $"{(AuthorizationRequired ? "§ 107.41 names it, so prior authorization from Air Traffic Control (ATC) is what lifts its prohibition" : "§ 107.41 does not name it")} "
        + $"[{Authority}]; {Authorization}; {Waiver}";
}

/// <summary>§ 107.41: operation in certain airspace.</summary>
/// <remarks>
/// <para>
/// The section names four airspaces and no others: Class B, Class C, Class D, and — only "within
/// the lateral boundaries of the surface area of Class E airspace designated for an airport" —
/// Class E. In those, it prohibits the operation "unless that person has prior authorization from
/// Air Traffic Control (ATC)". Airspace it does not name it does not reach, and so does not
/// prohibit: Class G, which <see cref="MapEntries.AirspaceAuthorized"/>'s note records as needing
/// none, and Class E outside such a surface area, which the section's own restriction leaves out.
/// </para>
/// <para>
/// The table is closed over a closed set. <see cref="AirspaceClass.All"/> holds every airspace a
/// caller can state, and each is on exactly one side of it: the four the section names, and the two
/// it does not. There is no third case, and in particular no airspace this rule answers about
/// without having identified it — an airspace outside those six cannot be constructed, so it never
/// reaches the rule. This matters more than it looks: the alternative, reading a caller's arbitrary
/// designation, would answer "the section does not reach you" for a Class B operation written
/// "Class B", which is a permission § 107.41 does not give.
/// </para>
/// <para>
/// Both facts the rule turns on are the caller's, never the engine's. The airspace is an input
/// because the entry's note says so — "Determining it from a position requires a corpus this map has
/// not admitted" — and the authorization is an input because it is an instrument ATC issued, which
/// no reading of the corpus discovers. The rule reads what it is given, and refuses to resolve when
/// it is given neither.
/// </para>
/// </remarks>
public static class Airspace
{
    /// <summary>The regulation § 107.205(h) lists that states the entry: the whole of § 107.41.</summary>
    public const string Regulation = "§ 107.41";

    /// <summary>
    /// <see cref="MapEntries.AirspaceAuthorized"/>: whether § 107.41 permits operating in
    /// <paramref name="airspace"/> on the authorization <paramref name="authorization"/> states.
    /// </summary>
    /// <param name="airspace">
    /// The airspace the operation is in, as the caller states it. Never inferred; required once the
    /// entry is reachable, and demanded after the gate (<c>docs/decisions/0007</c>).
    /// </param>
    /// <param name="authorization">
    /// What the caller states about prior authorization from ATC. Never inferred; required once the
    /// entry is reachable, and demanded after the gate.
    /// </param>
    /// <param name="waiver">Whether a waiver of § 107.41 is in force, as the caller states it.</param>
    /// <returns>
    /// The finding; <see cref="UnresolvedReason.OutsideCurrentScope"/> citing § 107.205 while a waiver
    /// is in force.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The waiver statement is about another regulation; or no waiver is in force and
    /// <paramref name="airspace"/> or <paramref name="authorization"/> was not stated.
    /// </exception>
    public static Resolution<AirspaceFinding> Authorized(
        AirspaceClass? airspace,
        AtcAuthorization? authorization,
        WaiverStatement waiver)
    {
        if (Waivers.Suspension(MapEntries.AirspaceAuthorized, Regulation, waiver) is { } suspended)
        {
            return Resolution<AirspaceFinding>.FromUnresolved(suspended);
        }

        var stated = Demands.Of(
            airspace, MapEntries.AirspaceAuthorized, nameof(Requests.AirspaceAuthorizedRequest.Airspace));
        var held = Demands.Of(
            authorization, MapEntries.AirspaceAuthorized, nameof(Requests.AirspaceAuthorizedRequest.Authorization));

        var required = Names(stated);
        var mayOperate = !required || (held.Held && held.ObtainedBeforeTheOperation);
        return Resolution<AirspaceFinding>.FromValue(
            new AirspaceFinding(stated, held, required, mayOperate, waiver));
    }

    /// <summary>
    /// The four airspaces § 107.41 names, and nothing else. Every other member of
    /// <see cref="AirspaceClass.All"/> falls through to false, and nothing outside that list exists.
    /// </summary>
    private static bool Names(AirspaceClass airspace) =>
        airspace == AirspaceClass.ClassB
        || airspace == AirspaceClass.ClassC
        || airspace == AirspaceClass.ClassD
        || airspace == AirspaceClass.ClassESurfaceAreaDesignatedForAnAirport;
}
