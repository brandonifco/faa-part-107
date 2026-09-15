using System.Globalization;

namespace FaaPart107;

/// <summary>The two units § 107.51(a) prints its groundspeed limit in.</summary>
public enum SpeedUnit
{
    /// <summary>Knots: international nautical miles per hour, 1.852 kilometres per hour exactly.</summary>
    Knots,

    /// <summary>Statute miles per hour, 1.609344 kilometres per hour exactly.</summary>
    MilesPerHour,
}

/// <summary>
/// A groundspeed, in the unit the caller measured it in. It is a parameter the rule tests, not a
/// rule, so the map has no entry for it (rules-factory <c>docs/corpus-map.md</c>, gate 1).
/// </summary>
/// <remarks>
/// The comparison with a printed figure is exact: both units are defined in kilometres per hour
/// by terminating decimals (the international knot, 1.852 km/h; the international mile per hour,
/// 1.609344 km/h), so no conversion rounds and no floating point is involved. Part 107 does not
/// define the units; the map's own question about <c>speed-limit</c> ("87 knots is 100.12 miles
/// per hour") reads them the same way. See <c>docs/decisions/0001</c>.
/// </remarks>
public readonly record struct Groundspeed
{
    private const decimal KilometresPerKnot = 1.852m;
    private const decimal KilometresPerMile = 1.609344m;

    /// <summary>A groundspeed of <paramref name="value"/> <paramref name="unit"/>.</summary>
    /// <param name="value">The speed, not negative.</param>
    /// <param name="unit">The unit it is measured in.</param>
    public Groundspeed(decimal value, SpeedUnit unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        if (!Enum.IsDefined(unit))
        {
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "a groundspeed is in knots or miles per hour");
        }

        Value = value;
        Unit = unit;
    }

    /// <summary>The speed, in <see cref="Unit"/>.</summary>
    public decimal Value { get; }

    /// <summary>The unit <see cref="Value"/> is measured in.</summary>
    public SpeedUnit Unit { get; }

    /// <summary>The same speed in kilometres per hour, exactly.</summary>
    internal decimal KilometresPerHour => Unit == SpeedUnit.Knots ? Value * KilometresPerKnot : Value * KilometresPerMile;

    /// <summary>A groundspeed in knots.</summary>
    /// <param name="value">The speed in knots.</param>
    /// <returns>The groundspeed.</returns>
    public static Groundspeed InKnots(decimal value) => new(value, SpeedUnit.Knots);

    /// <summary>A groundspeed in miles per hour.</summary>
    /// <param name="value">The speed in miles per hour.</param>
    /// <returns>The groundspeed.</returns>
    public static Groundspeed InMilesPerHour(decimal value) => new(value, SpeedUnit.MilesPerHour);

    /// <summary>Whether this speed is greater than <paramref name="figure"/>, compared exactly across units.</summary>
    /// <param name="figure">The figure compared against.</param>
    /// <returns>True when this speed exceeds it.</returns>
    public bool Exceeds(Groundspeed figure) => KilometresPerHour > figure.KilometresPerHour;

    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Value} {(Unit == SpeedUnit.Knots ? "knots" : "miles per hour")}");
}
