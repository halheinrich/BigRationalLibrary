using System.Numerics;
using Xunit;

namespace HalHeinrich.Numerics.Tests;

public class BigRationalRoundTests
{
    // The expected values below were produced independently of the implementation,
    // from a decimal-arithmetic oracle, and cover positive, negative, exact-half
    // and negative-exact-half inputs for every MidpointRounding mode.
    //
    // Note the three directed modes (ToZero, ToNegativeInfinity, ToPositiveInfinity)
    // apply to every value, not only to exact halves, matching Math.Round.

    // ---- ToEven (nearest, ties to even) ------------------------------------

    [Theory]
    [InlineData(5, 2, 2)]       // exact half, positive
    [InlineData(-5, 2, -2)]     // exact half, negative
    [InlineData(3, 2, 2)]
    [InlineData(-3, 2, -2)]
    [InlineData(1, 2, 0)]
    [InlineData(-1, 2, 0)]
    [InlineData(7, 2, 4)]
    [InlineData(-7, 2, -4)]
    [InlineData(7, 3, 2)]
    [InlineData(-7, 3, -2)]
    [InlineData(1, 3, 0)]
    [InlineData(-1, 3, 0)]
    [InlineData(4, 1, 4)]
    [InlineData(-4, 1, -4)]
    [InlineData(0, 1, 0)]
    public void Round_ToEven(int numerator, int denominator, int expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            BigRational.Round(new BigRational(numerator, denominator), MidpointRounding.ToEven));
    }

    // ---- AwayFromZero (nearest, ties away from zero) -----------------------

    [Theory]
    [InlineData(5, 2, 3)]
    [InlineData(-5, 2, -3)]
    [InlineData(3, 2, 2)]
    [InlineData(-3, 2, -2)]
    [InlineData(1, 2, 1)]
    [InlineData(-1, 2, -1)]
    [InlineData(7, 2, 4)]
    [InlineData(-7, 2, -4)]
    [InlineData(7, 3, 2)]
    [InlineData(-7, 3, -2)]
    [InlineData(1, 3, 0)]
    [InlineData(-1, 3, 0)]
    [InlineData(4, 1, 4)]
    [InlineData(-4, 1, -4)]
    [InlineData(0, 1, 0)]
    public void Round_AwayFromZero(int numerator, int denominator, int expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            BigRational.Round(new BigRational(numerator, denominator), MidpointRounding.AwayFromZero));
    }

    // ---- ToZero (truncate) -------------------------------------------------

    [Theory]
    [InlineData(5, 2, 2)]
    [InlineData(-5, 2, -2)]
    [InlineData(3, 2, 1)]
    [InlineData(-3, 2, -1)]
    [InlineData(1, 2, 0)]
    [InlineData(-1, 2, 0)]
    [InlineData(7, 2, 3)]
    [InlineData(-7, 2, -3)]
    [InlineData(7, 3, 2)]
    [InlineData(-7, 3, -2)]
    [InlineData(1, 3, 0)]
    [InlineData(-1, 3, 0)]
    [InlineData(4, 1, 4)]
    [InlineData(-4, 1, -4)]
    [InlineData(0, 1, 0)]
    public void Round_ToZero(int numerator, int denominator, int expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            BigRational.Round(new BigRational(numerator, denominator), MidpointRounding.ToZero));
    }

    // ---- ToNegativeInfinity (floor) ----------------------------------------

    [Theory]
    [InlineData(5, 2, 2)]
    [InlineData(-5, 2, -3)]
    [InlineData(3, 2, 1)]
    [InlineData(-3, 2, -2)]
    [InlineData(1, 2, 0)]
    [InlineData(-1, 2, -1)]
    [InlineData(7, 2, 3)]
    [InlineData(-7, 2, -4)]
    [InlineData(7, 3, 2)]
    [InlineData(-7, 3, -3)]
    [InlineData(1, 3, 0)]
    [InlineData(-1, 3, -1)]
    [InlineData(4, 1, 4)]
    [InlineData(-4, 1, -4)]
    [InlineData(0, 1, 0)]
    public void Round_ToNegativeInfinity(int numerator, int denominator, int expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            BigRational.Round(new BigRational(numerator, denominator), MidpointRounding.ToNegativeInfinity));
    }

    // ---- ToPositiveInfinity (ceiling) --------------------------------------

    [Theory]
    [InlineData(5, 2, 3)]
    [InlineData(-5, 2, -2)]
    [InlineData(3, 2, 2)]
    [InlineData(-3, 2, -1)]
    [InlineData(1, 2, 1)]
    [InlineData(-1, 2, 0)]
    [InlineData(7, 2, 4)]
    [InlineData(-7, 2, -3)]
    [InlineData(7, 3, 3)]
    [InlineData(-7, 3, -2)]
    [InlineData(1, 3, 1)]
    [InlineData(-1, 3, 0)]
    [InlineData(4, 1, 4)]
    [InlineData(-4, 1, -4)]
    [InlineData(0, 1, 0)]
    public void Round_ToPositiveInfinity(int numerator, int denominator, int expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            BigRational.Round(new BigRational(numerator, denominator), MidpointRounding.ToPositiveInfinity));
    }

    // ---- The umbrella spec's "round-half-up, correct for negative values" ---

    [Fact]
    public void Round_ToPositiveInfinity_IsHalfUpAcrossZero()
    {
        // SPEC-rational-ratio.md asks for round-half-up that stays correct for
        // negative values: halves move up the number line, not away from zero.
        Assert.Equal(new BigInteger(3), BigRational.Round(new BigRational(5, 2), MidpointRounding.ToPositiveInfinity));
        Assert.Equal(new BigInteger(-2), BigRational.Round(new BigRational(-5, 2), MidpointRounding.ToPositiveInfinity));

        // AwayFromZero is the mode that gets negative halves "wrong" for this
        // purpose; pinning the difference keeps the two from being confused.
        Assert.Equal(new BigInteger(-3), BigRational.Round(new BigRational(-5, 2), MidpointRounding.AwayFromZero));
    }

    // ---- Default mode ------------------------------------------------------

    [Theory]
    [InlineData(5, 2)]
    [InlineData(-5, 2)]
    [InlineData(3, 2)]
    [InlineData(-3, 2)]
    [InlineData(7, 3)]
    [InlineData(-7, 3)]
    public void Round_DefaultMode_IsToEven(int numerator, int denominator)
    {
        var value = new BigRational(numerator, denominator);
        Assert.Equal(BigRational.Round(value, MidpointRounding.ToEven), BigRational.Round(value));
    }

    // ---- Integers are unchanged under every mode ---------------------------

    [Theory]
    [InlineData(MidpointRounding.ToEven)]
    [InlineData(MidpointRounding.AwayFromZero)]
    [InlineData(MidpointRounding.ToZero)]
    [InlineData(MidpointRounding.ToNegativeInfinity)]
    [InlineData(MidpointRounding.ToPositiveInfinity)]
    public void Round_Integers_AreUnchanged(MidpointRounding mode)
    {
        for (var i = -5; i <= 5; i++)
        {
            Assert.Equal(new BigInteger(i), BigRational.Round(BigRational.FromInteger(i), mode));
        }
    }

    // ---- Invalid mode ------------------------------------------------------

    [Theory]
    [InlineData(7, 3)]      // non-integer
    [InlineData(-7, 3)]
    [InlineData(4, 1)]      // integer: must still validate, not short-circuit
    [InlineData(0, 1)]
    public void Round_UndefinedMode_Throws(int numerator, int denominator)
    {
        var value = new BigRational(numerator, denominator);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BigRational.Round(value, (MidpointRounding)999));
    }

    // ---- Cross-mode invariants ---------------------------------------------

    [Fact]
    public void Round_DirectedModes_BracketTheValue()
    {
        for (var numerator = -30; numerator <= 30; numerator++)
        {
            foreach (var denominator in new[] { 1, 2, 3, 4, 7 })
            {
                var value = new BigRational(numerator, denominator);

                var floor = BigRational.Round(value, MidpointRounding.ToNegativeInfinity);
                var ceiling = BigRational.Round(value, MidpointRounding.ToPositiveInfinity);
                var truncated = BigRational.Round(value, MidpointRounding.ToZero);

                // floor <= value <= ceiling, and they differ by at most one.
                Assert.True(BigRational.FromInteger(floor) <= value);
                Assert.True(BigRational.FromInteger(ceiling) >= value);
                Assert.True(ceiling - floor <= BigInteger.One);

                // Truncation is the floor for non-negative values, the ceiling otherwise.
                Assert.Equal(value.Sign < 0 ? ceiling : floor, truncated);

                // Both nearest modes land on one of the two brackets.
                foreach (var mode in new[] { MidpointRounding.ToEven, MidpointRounding.AwayFromZero })
                {
                    var nearest = BigRational.Round(value, mode);
                    Assert.True(nearest == floor || nearest == ceiling);
                }
            }
        }
    }

    [Fact]
    public void Round_NearestModes_AreWithinAHalf()
    {
        var half = new BigRational(1, 2);
        foreach (var mode in new[] { MidpointRounding.ToEven, MidpointRounding.AwayFromZero })
        {
            for (var numerator = -30; numerator <= 30; numerator++)
            {
                foreach (var denominator in new[] { 1, 2, 3, 4, 7 })
                {
                    var value = new BigRational(numerator, denominator);
                    var rounded = BigRational.FromInteger(BigRational.Round(value, mode));
                    Assert.True(BigRational.Abs(value - rounded) <= half);
                }
            }
        }
    }

    [Fact]
    public void Round_NegatingValue_NegatesResult_ForSymmetricModes()
    {
        // ToEven and AwayFromZero are symmetric about zero; the directed modes are not.
        foreach (var mode in new[] { MidpointRounding.ToEven, MidpointRounding.AwayFromZero, MidpointRounding.ToZero })
        {
            for (var numerator = -30; numerator <= 30; numerator++)
            {
                foreach (var denominator in new[] { 1, 2, 3, 4, 7 })
                {
                    var value = new BigRational(numerator, denominator);
                    Assert.Equal(
                        BigInteger.Negate(BigRational.Round(value, mode)),
                        BigRational.Round(BigRational.Negate(value), mode));
                }
            }
        }
    }

    [Fact]
    public void Round_FloorAndCeilingSwapUnderNegation()
    {
        for (var numerator = -30; numerator <= 30; numerator++)
        {
            foreach (var denominator in new[] { 1, 2, 3, 4, 7 })
            {
                var value = new BigRational(numerator, denominator);
                Assert.Equal(
                    BigInteger.Negate(BigRational.Round(value, MidpointRounding.ToNegativeInfinity)),
                    BigRational.Round(BigRational.Negate(value), MidpointRounding.ToPositiveInfinity));
            }
        }
    }

    // ---- Large values ------------------------------------------------------

    [Fact]
    public void Round_LargeExactHalf_HonoursMode()
    {
        // (2*10^40 + 1) / 2 is an exact half well beyond any primitive width.
        var big = BigInteger.Pow(10, 40);
        var value = new BigRational(2 * big + BigInteger.One, 2);

        Assert.Equal(big + BigInteger.One, BigRational.Round(value, MidpointRounding.AwayFromZero));
        Assert.Equal(big, BigRational.Round(value, MidpointRounding.ToEven));
        Assert.Equal(big, BigRational.Round(value, MidpointRounding.ToZero));
        Assert.Equal(big, BigRational.Round(value, MidpointRounding.ToNegativeInfinity));
        Assert.Equal(big + BigInteger.One, BigRational.Round(value, MidpointRounding.ToPositiveInfinity));

        var negated = BigRational.Negate(value);
        Assert.Equal(BigInteger.Negate(big + BigInteger.One), BigRational.Round(negated, MidpointRounding.AwayFromZero));
        Assert.Equal(BigInteger.Negate(big), BigRational.Round(negated, MidpointRounding.ToEven));
        Assert.Equal(BigInteger.Negate(big), BigRational.Round(negated, MidpointRounding.ToZero));
        Assert.Equal(BigInteger.Negate(big + BigInteger.One), BigRational.Round(negated, MidpointRounding.ToNegativeInfinity));
        Assert.Equal(BigInteger.Negate(big), BigRational.Round(negated, MidpointRounding.ToPositiveInfinity));
    }

    [Fact]
    public void Round_AgreesWithMathRound_OnRepresentableValues()
    {
        // Cross-check the nearest modes against the framework on values a double
        // represents exactly, so the two implementations must agree.
        foreach (var mode in new[] { MidpointRounding.ToEven, MidpointRounding.AwayFromZero })
        {
            for (var quarters = -40; quarters <= 40; quarters++)
            {
                var value = new BigRational(quarters, 4);
                var expected = (BigInteger)Math.Round(quarters / 4d, mode);
                Assert.Equal(expected, BigRational.Round(value, mode));
            }
        }
    }
}
