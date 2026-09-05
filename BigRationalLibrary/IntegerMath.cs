using System.Numerics;

namespace HalHeinrich.Numerics;

/// <summary>
/// Rounding mode for <see cref="IntegerMath.Sqrt(BigInteger, IntegerSqrtRounding)"/>.
/// </summary>
/// <remarks>
/// Top-level rather than nested in <see cref="IntegerMath"/>, matching
/// <see cref="MidpointRounding"/>'s relationship to <see cref="Math"/>: the name already
/// carries its own qualification, and a public nested type would read as
/// <c>IntegerMath.IntegerSqrtRounding</c>.
/// </remarks>
public enum IntegerSqrtRounding
{
    /// <summary>
    /// Round toward negative infinity: the largest non-negative integer <c>k</c> with
    /// <c>k² ≤ n</c>. This is the default, and the mode integer algorithms working in
    /// the interval <c>(s, s+1)</c> around <c>√n</c> want.
    /// </summary>
    Floor,

    /// <summary>
    /// Round toward positive infinity: the smallest non-negative integer <c>k</c> with
    /// <c>k² ≥ n</c>.
    /// </summary>
    Ceiling,

    /// <summary>
    /// Round to the nearest integer. Unlike <see cref="MidpointRounding"/> this mode needs
    /// no tie-breaking policy: for non-square <c>n</c> the value <c>√n</c> is irrational,
    /// so it never lands on the midpoint between two consecutive integers, and for square
    /// <c>n</c> it lands exactly on one. There are no ties to break.
    /// </summary>
    Nearest,
}

/// <summary>
/// Exact integer arithmetic over <see cref="BigInteger"/> that has no rational counterpart —
/// helpers that operate on whole numbers rather than on <see cref="BigRational"/>.
/// </summary>
/// <remarks>
/// A peer of <see cref="BigRational"/> rather than a member of it: these operations are
/// about integers, and nesting them inside the rational type would make the rational a
/// prerequisite for reaching them.
/// </remarks>
public static class IntegerMath
{
    /// <summary>
    /// Computes the integer square root of <paramref name="value"/> under the given
    /// <paramref name="rounding"/> mode, in exact integer arithmetic with no
    /// floating-point intermediate.
    /// </summary>
    /// <param name="value">A non-negative integer.</param>
    /// <param name="rounding">
    /// How to round when <paramref name="value"/> is not a perfect square. The three modes
    /// agree on perfect squares and on zero, and differ everywhere else.
    /// </param>
    /// <returns>
    /// The non-negative integer <c>k</c> obtained by applying <paramref name="rounding"/>
    /// to <c>√value</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The domain is the non-negative integers. A negative <paramref name="value"/> is
    /// rejected rather than given a substitute result: no integer squares to a negative
    /// number, and <see cref="BigInteger"/> has no NaN or imaginary value to return in
    /// place of one. This matches how <see cref="BigInteger"/> itself refuses an
    /// out-of-domain argument — <see cref="BigInteger.Pow"/> and
    /// <see cref="BigInteger.ModPow"/> both throw
    /// <see cref="ArgumentOutOfRangeException"/> on a negative exponent.
    /// </para>
    /// <para>
    /// Exact in the strong sense: the result is not an approximation that happens to be
    /// correct at the sizes tested. Every intermediate is a <see cref="BigInteger"/>, so
    /// there is no precision above which the answer degrades.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> is negative, or <paramref name="rounding"/> is not a
    /// defined <see cref="IntegerSqrtRounding"/> value.
    /// </exception>
    public static BigInteger Sqrt(BigInteger value, IntegerSqrtRounding rounding = IntegerSqrtRounding.Floor)
    {
        if (value.Sign < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Cannot take the square root of a negative integer.");

        // Validated here rather than only in the switch's default arm, matching
        // BigRational.Round. Today the two are equivalent — every input reaches the
        // switch — so this buys two things rather than a behaviour change: an undefined
        // mode costs an argument check instead of a full iteration on a possibly huge
        // value, and validation stops depending on the switch staying the only exit. Add
        // a short-circuit above it later and a check living in the default arm would
        // silently stop covering the inputs that take it.
        if (!Enum.IsDefined(rounding))
            throw new ArgumentOutOfRangeException(nameof(rounding), rounding, "Unknown rounding mode.");

        var floor = FloorSqrt(value);

        return rounding switch
        {
            IntegerSqrtRounding.Floor => floor,

            IntegerSqrtRounding.Ceiling =>
                floor * floor == value ? floor : floor + BigInteger.One,

            // value lies in [floor², (floor+1)²), whose midpoint is floor² + floor + ½.
            // So value - floor² ≤ floor puts it below the midpoint and floor is nearer;
            // otherwise floor + 1 is. The comparison is exact and never ties, because the
            // midpoint is not an integer and value is.
            IntegerSqrtRounding.Nearest =>
                value - floor * floor <= floor ? floor : floor + BigInteger.One,

            // Unreachable: the mode was validated above.
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, "Unknown rounding mode."),
        };
    }

    /// <summary>
    /// Computes <c>⌊√n⌋</c> for non-negative <paramref name="n"/> by Newton-Raphson
    /// iteration in integer arithmetic.
    /// </summary>
    /// <remarks>
    /// The sequence starts at <c>n</c>, which is at or above <c>√n</c> for every
    /// <c>n ≥ 1</c>, and decreases monotonically until <c>y ≥ x</c> signals that it has
    /// settled; at that point <c>x = ⌊√n⌋</c>. From a seed that large the early steps
    /// roughly halve <c>x</c>, so the iteration count is <c>O(log n)</c> — measured at
    /// approximately <c>½·log₂ n</c> for <c>n = 10^k</c>, <c>k</c> from 4 to 480.
    /// </remarks>
    private static BigInteger FloorSqrt(BigInteger n)
    {
        if (n.IsZero)
            return BigInteger.Zero;
        if (n.IsOne)
            return BigInteger.One;

        var x = n;
        var y = (x + BigInteger.One) >> 1;
        while (y < x)
        {
            x = y;
            y = (x + n / x) >> 1;
        }
        return x;
    }
}
