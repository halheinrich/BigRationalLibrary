using System;
using System.Numerics;
using Xunit;

namespace HalHeinrich.Numerics.Tests;

public class BigRationalPowTests
{
    // ---- Zero exponent -----------------------------------------------------

    [Theory]
    [InlineData(0, 1)]      // 0^0 == 1, matching BigInteger.Pow
    [InlineData(1, 1)]
    [InlineData(-1, 1)]
    [InlineData(7, 9)]
    [InlineData(-7, 9)]
    [InlineData(123456789, 987654321)]
    public void Pow_ZeroExponent_IsOne(int numerator, int denominator)
    {
        var value = new BigRational(numerator, denominator);
        Assert.Equal(BigRational.One, BigRational.Pow(value, 0));
    }

    [Fact]
    public void Pow_ZeroBase_ZeroExponent_IsOne_MatchingBigInteger()
    {
        Assert.Equal(BigRational.One, BigRational.Pow(BigRational.Zero, 0));
        Assert.Equal(BigInteger.One, BigInteger.Pow(BigInteger.Zero, 0));
    }

    // ---- Zero base ---------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(1000)]
    public void Pow_ZeroBase_PositiveExponent_IsZero(int exponent)
    {
        Assert.Equal(BigRational.Zero, BigRational.Pow(BigRational.Zero, exponent));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-1000)]
    [InlineData(int.MinValue)]
    public void Pow_ZeroBase_NegativeExponent_Throws(int exponent)
    {
        Assert.Throws<DivideByZeroException>(() => BigRational.Pow(BigRational.Zero, exponent));
    }

    // ---- Sign of a negative base ------------------------------------------

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(100)]
    [InlineData(-2)]
    [InlineData(-4)]
    [InlineData(-100)]
    public void Pow_NegativeBase_EvenExponent_IsPositive(int exponent)
    {
        var result = BigRational.Pow(new BigRational(-2, 3), exponent);
        Assert.Equal(1, result.Sign);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(101)]
    [InlineData(-1)]
    [InlineData(-3)]
    [InlineData(-101)]
    public void Pow_NegativeBase_OddExponent_IsNegative(int exponent)
    {
        var result = BigRational.Pow(new BigRational(-2, 3), exponent);
        Assert.Equal(-1, result.Sign);
    }

    [Fact]
    public void Pow_NegativeBase_KnownValues()
    {
        Assert.Equal(new BigRational(4, 9), BigRational.Pow(new BigRational(-2, 3), 2));
        Assert.Equal(new BigRational(-8, 27), BigRational.Pow(new BigRational(-2, 3), 3));
        Assert.Equal(new BigRational(9, 4), BigRational.Pow(new BigRational(-2, 3), -2));
        Assert.Equal(new BigRational(-27, 8), BigRational.Pow(new BigRational(-2, 3), -3));
    }

    // ---- Negative exponents ------------------------------------------------

    [Fact]
    public void Pow_NegativeExponent_IsReciprocalOfPositivePower()
    {
        var value = new BigRational(2, 3);
        for (var exponent = 1; exponent <= 12; exponent++)
        {
            var positive = BigRational.Pow(value, exponent);
            var negative = BigRational.Pow(value, -exponent);
            Assert.Equal(BigRational.Reciprocal(positive), negative);
            Assert.Equal(BigRational.One, positive * negative);
        }
    }

    [Fact]
    public void Pow_NegativeExponent_OnInteger_ProducesFraction()
    {
        Assert.Equal(new BigRational(1, 2), BigRational.Pow(BigRational.FromInteger(2), -1));
        Assert.Equal(new BigRational(1, 8), BigRational.Pow(BigRational.FromInteger(2), -3));
        Assert.Equal(new BigRational(-1, 8), BigRational.Pow(BigRational.FromInteger(-2), -3));
    }

    // ---- int.MinValue: the exponent whose magnitude does not fit in an int --

    [Fact]
    public void Pow_IntMinValueExponent_DoesNotOverflow()
    {
        // |int.MinValue| is 2147483648, one past int.MaxValue. Only bases of
        // magnitude one stay computable; the exponent must not wrap to a
        // positive value or silently lose the extra factor.
        Assert.Equal(BigRational.One, BigRational.Pow(BigRational.One, int.MinValue));

        // int.MinValue is even, so a base of -1 must come back positive.
        Assert.Equal(BigRational.One, BigRational.Pow(BigRational.MinusOne, int.MinValue));
    }

    [Fact]
    public void Pow_IntMaxValueExponent_OnUnitBases()
    {
        // int.MaxValue is odd.
        Assert.Equal(BigRational.One, BigRational.Pow(BigRational.One, int.MaxValue));
        Assert.Equal(BigRational.MinusOne, BigRational.Pow(BigRational.MinusOne, int.MaxValue));
        Assert.Equal(BigRational.MinusOne, BigRational.Pow(BigRational.MinusOne, -int.MaxValue));
    }

    // ---- Large exponents ---------------------------------------------------

    [Fact]
    public void Pow_LargeExponent_MatchesBigIntegerPow()
    {
        const int exponent = 500;
        var result = BigRational.Pow(new BigRational(2, 3), exponent);
        Assert.Equal(BigInteger.Pow(2, exponent), result.Numerator);
        Assert.Equal(BigInteger.Pow(3, exponent), result.Denominator);
    }

    [Fact]
    public void Pow_LargeNegativeExponent_MatchesBigIntegerPow()
    {
        const int exponent = 500;
        var result = BigRational.Pow(new BigRational(2, 3), -exponent);
        Assert.Equal(BigInteger.Pow(3, exponent), result.Numerator);
        Assert.Equal(BigInteger.Pow(2, exponent), result.Denominator);
    }

    [Fact]
    public void Pow_LargeOddExponent_KeepsNegativeSign()
    {
        var result = BigRational.Pow(new BigRational(-2, 3), 501);
        Assert.Equal(-1, result.Sign);
        Assert.Equal(BigInteger.Negate(BigInteger.Pow(2, 501)), result.Numerator);
    }

    // ---- Agreement with repeated multiplication ----------------------------

    [Theory]
    [InlineData(2, 3)]
    [InlineData(-2, 3)]
    [InlineData(5, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 7)]
    [InlineData(-1, 7)]
    public void Pow_AgreesWithRepeatedMultiplication(int numerator, int denominator)
    {
        var value = new BigRational(numerator, denominator);
        var expected = BigRational.One;
        for (var exponent = 0; exponent <= 20; exponent++)
        {
            Assert.Equal(expected, BigRational.Pow(value, exponent));
            expected *= value;
        }
    }

    // ---- Invariants --------------------------------------------------------

    [Theory]
    [InlineData(6, 4, 3)]
    [InlineData(-6, 4, 3)]
    [InlineData(6, 4, -3)]
    [InlineData(-6, 4, -3)]
    [InlineData(10, 4, 7)]
    [InlineData(-10, 4, -7)]
    public void Pow_ResultIsReducedWithPositiveDenominator(int numerator, int denominator, int exponent)
    {
        var result = BigRational.Pow(new BigRational(numerator, denominator), exponent);

        Assert.True(result.Denominator > BigInteger.Zero);
        Assert.Equal(
            BigInteger.One,
            BigInteger.GreatestCommonDivisor(BigInteger.Abs(result.Numerator), result.Denominator));
    }

    [Fact]
    public void Pow_ExponentsAdd()
    {
        var value = new BigRational(-3, 7);
        for (var a = -6; a <= 6; a++)
        {
            for (var b = -6; b <= 6; b++)
            {
                Assert.Equal(
                    BigRational.Pow(value, a + b),
                    BigRational.Pow(value, a) * BigRational.Pow(value, b));
            }
        }
    }

    [Fact]
    public void Pow_OneAndMinusOne_Cycle()
    {
        for (var exponent = -10; exponent <= 10; exponent++)
        {
            Assert.Equal(BigRational.One, BigRational.Pow(BigRational.One, exponent));
            Assert.Equal(
                exponent % 2 == 0 ? BigRational.One : BigRational.MinusOne,
                BigRational.Pow(BigRational.MinusOne, exponent));
        }
    }
}
