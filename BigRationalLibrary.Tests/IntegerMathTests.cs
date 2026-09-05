using System;
using System.Numerics;
using Xunit;

namespace HalHeinrich.Numerics.Tests;

public class IntegerMathTests
{
    private const int SweepBound = 2000;

    // 0..2000 contains 45 perfect squares: 0² through 44², since 44² = 1936 and
    // 45² = 2025. Used below to pin how often the modes must separate.
    private const int PerfectSquaresInSweep = 45;

    /// <summary>
    /// An independent reference: linear search rather than Newton-Raphson. Deliberately
    /// naive and deliberately not the algorithm under test, so agreement between the two
    /// is evidence rather than a restatement of one implementation against itself.
    /// </summary>
    private static BigInteger ReferenceFloorSqrt(int n)
    {
        var k = 0;
        while ((k + 1L) * (k + 1L) <= n)
            k++;
        return new BigInteger(k);
    }

    // ---------- Floor ----------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(8, 2)]
    [InlineData(9, 3)]
    [InlineData(10, 3)]
    [InlineData(99, 9)]
    [InlineData(100, 10)]
    [InlineData(101, 10)]
    [InlineData(120, 10)]
    [InlineData(121, 11)]
    public void Sqrt_Floor_MatchesExpectedValue(long input, long expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            IntegerMath.Sqrt(new BigInteger(input), IntegerSqrtRounding.Floor));
    }

    [Fact]
    public void Sqrt_OmittingRoundingIsFloor()
    {
        // The default is part of the public contract: every caller that omits the
        // argument depends on it, so it is pinned rather than read off the declaration.
        for (var n = 0; n <= SweepBound; n++)
        {
            Assert.Equal(
                IntegerMath.Sqrt(new BigInteger(n), IntegerSqrtRounding.Floor),
                IntegerMath.Sqrt(new BigInteger(n)));
        }
    }

    // ---------- Ceiling ----------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(8, 3)]
    [InlineData(9, 3)]
    [InlineData(10, 4)]
    [InlineData(99, 10)]
    [InlineData(100, 10)]
    [InlineData(101, 11)]
    public void Sqrt_Ceiling_MatchesExpectedValue(long input, long expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            IntegerMath.Sqrt(new BigInteger(input), IntegerSqrtRounding.Ceiling));
    }

    // ---------- Nearest ----------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]    // sqrt 2 is about 1.414
    [InlineData(3, 2)]    // sqrt 3 is about 1.732
    [InlineData(4, 2)]
    [InlineData(5, 2)]    // sqrt 5 is about 2.236
    [InlineData(6, 2)]    // sqrt 6 is about 2.449
    [InlineData(7, 3)]    // sqrt 7 is about 2.646
    [InlineData(8, 3)]    // sqrt 8 is about 2.828
    [InlineData(9, 3)]
    [InlineData(20, 4)]   // sqrt 20 is about 4.472
    [InlineData(30, 5)]   // sqrt 30 is about 5.477
    public void Sqrt_Nearest_MatchesExpectedValue(long input, long expected)
    {
        Assert.Equal(
            new BigInteger(expected),
            IntegerMath.Sqrt(new BigInteger(input), IntegerSqrtRounding.Nearest));
    }

    // ---------- Defining bounds, swept ----------

    [Fact]
    public void Sqrt_Floor_AgreesWithAnIndependentImplementation()
    {
        for (var n = 0; n <= SweepBound; n++)
            Assert.Equal(ReferenceFloorSqrt(n), IntegerMath.Sqrt(new BigInteger(n)));
    }

    [Fact]
    public void Sqrt_Floor_SatisfiesDefiningBoundAcrossSweep()
    {
        // Floor(sqrt n) = k is the unique k with k*k <= n < (k+1)*(k+1). Asserting both
        // halves is what makes this a bound rather than an observation: a too-large
        // result violates the upper half, a too-small result the lower.
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var k = IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor);

            Assert.True(k * k <= value, $"k*k <= n violated at n = {n}, k = {k}");
            Assert.True(value < (k + 1) * (k + 1), $"n < (k+1)*(k+1) violated at n = {n}, k = {k}");
        }
    }

    [Fact]
    public void Sqrt_Ceiling_SatisfiesDefiningBoundAcrossSweep()
    {
        // Ceiling(sqrt n) = k is the unique k with (k-1)*(k-1) < n <= k*k, the lower
        // half vacuous at k = 0.
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var k = IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling);

            Assert.True(k * k >= value, $"k*k >= n violated at n = {n}, k = {k}");
            if (!k.IsZero)
                Assert.True((k - 1) * (k - 1) < value, $"(k-1)*(k-1) < n violated at n = {n}, k = {k}");
        }
    }

    [Fact]
    public void Sqrt_Nearest_SatisfiesDefiningBoundAcrossSweep()
    {
        // k is nearest sqrt n iff k - 1/2 < sqrt n < k + 1/2, which clears of halves and
        // radicals to (2k-1)*(2k-1) < 4n < (2k+1)*(2k+1), the lower half vacuous at
        // k = 0. Held in integers, so the test introduces no floating point of its own.
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var k = IntegerMath.Sqrt(value, IntegerSqrtRounding.Nearest);

            Assert.True(
                4 * value < (2 * k + 1) * (2 * k + 1),
                $"4n < (2k+1)*(2k+1) violated at n = {n}, k = {k}");
            if (!k.IsZero)
            {
                Assert.True(
                    (2 * k - 1) * (2 * k - 1) < 4 * value,
                    $"(2k-1)*(2k-1) < 4n violated at n = {n}, k = {k}");
            }
        }
    }

    [Fact]
    public void Sqrt_Nearest_NeverReachesAMidpoint()
    {
        // The enum documents Nearest as needing no tie-breaking policy, on the grounds
        // that sqrt n never lands on a midpoint between consecutive integers. That claim
        // holds only if both bounds above are strict, so it is tested by trying to meet
        // them with equality rather than by observing that a result looked reasonable.
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var k = IntegerMath.Sqrt(value, IntegerSqrtRounding.Nearest);

            Assert.NotEqual((2 * k + 1) * (2 * k + 1), 4 * value);
            Assert.NotEqual((2 * k - 1) * (2 * k - 1), 4 * value);
        }
    }

    // ---------- What the modes make distinct ----------

    [Fact]
    public void Sqrt_ModesAreOrderedFloorNearestCeiling()
    {
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var floor = IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor);
            var nearest = IntegerMath.Sqrt(value, IntegerSqrtRounding.Nearest);
            var ceiling = IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling);

            Assert.True(floor <= nearest, $"Floor <= Nearest violated at n = {n}");
            Assert.True(nearest <= ceiling, $"Nearest <= Ceiling violated at n = {n}");
            Assert.True(ceiling - floor <= BigInteger.One, $"Ceiling - Floor > 1 at n = {n}");
        }
    }

    [Fact]
    public void Sqrt_ModesAgreeExactlyOnPerfectSquares()
    {
        // The characterisation runs both ways: agreement implies a perfect square and a
        // perfect square implies agreement. Testing one direction only would pass for an
        // implementation whose Ceiling was simply Floor.
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var floor = IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor);
            var nearest = IntegerMath.Sqrt(value, IntegerSqrtRounding.Nearest);
            var ceiling = IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling);

            var isPerfectSquare = floor * floor == value;
            Assert.Equal(isPerfectSquare, floor == ceiling);
            Assert.Equal(isPerfectSquare, floor == nearest && nearest == ceiling);
        }
    }

    [Fact]
    public void Sqrt_CeilingSeparatesFromFloorOnEveryNonSquare()
    {
        // Guards the degenerate implementation the bound sweeps would otherwise admit:
        // if Ceiling never differed from Floor, every bound above still holds on perfect
        // squares. Assert the separation occurs, and on exactly the right count.
        var separated = 0;
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            if (IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor)
                != IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling))
            {
                separated++;
            }
        }

        Assert.Equal(SweepBound + 1 - PerfectSquaresInSweep, separated);
    }

    [Fact]
    public void Sqrt_NearestSplitsTheIntervalBothWays()
    {
        // Nearest must sometimes agree with Floor and sometimes with Ceiling. An
        // implementation that always picked one of them would satisfy the ordering test.
        var withFloor = 0;
        var withCeiling = 0;
        for (var n = 0; n <= SweepBound; n++)
        {
            var value = new BigInteger(n);
            var floor = IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor);
            var ceiling = IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling);
            if (floor == ceiling)
                continue;

            var nearest = IntegerMath.Sqrt(value, IntegerSqrtRounding.Nearest);
            if (nearest == floor)
                withFloor++;
            else if (nearest == ceiling)
                withCeiling++;
            else
                Assert.Fail($"Nearest was neither Floor nor Ceiling at n = {n}");
        }

        Assert.True(withFloor > 0, "Nearest never agreed with Floor");
        Assert.True(withCeiling > 0, "Nearest never agreed with Ceiling");
    }

    [Theory]
    [InlineData(2, 1, 1, 2)]      // one above 1*1
    [InlineData(3, 1, 2, 2)]      // one below 2*2
    [InlineData(5, 2, 2, 3)]      // one above 2*2
    [InlineData(8, 2, 3, 3)]      // one below 3*3
    [InlineData(10, 3, 3, 4)]     // one above 3*3
    [InlineData(99, 9, 10, 10)]   // one below 10*10
    [InlineData(101, 10, 10, 11)] // one above 10*10
    public void Sqrt_EitherSideOfAPerfectSquare(long input, long floor, long nearest, long ceiling)
    {
        var value = new BigInteger(input);

        Assert.Equal(new BigInteger(floor), IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor));
        Assert.Equal(new BigInteger(nearest), IntegerMath.Sqrt(value, IntegerSqrtRounding.Nearest));
        Assert.Equal(new BigInteger(ceiling), IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling));
    }

    // ---------- Zero and one ----------

    [Theory]
    [InlineData(IntegerSqrtRounding.Floor)]
    [InlineData(IntegerSqrtRounding.Ceiling)]
    [InlineData(IntegerSqrtRounding.Nearest)]
    public void Sqrt_OfZeroIsZeroInEveryMode(IntegerSqrtRounding rounding)
    {
        Assert.Equal(BigInteger.Zero, IntegerMath.Sqrt(BigInteger.Zero, rounding));
    }

    [Theory]
    [InlineData(IntegerSqrtRounding.Floor)]
    [InlineData(IntegerSqrtRounding.Ceiling)]
    [InlineData(IntegerSqrtRounding.Nearest)]
    public void Sqrt_OfOneIsOneInEveryMode(IntegerSqrtRounding rounding)
    {
        Assert.Equal(BigInteger.One, IntegerMath.Sqrt(BigInteger.One, rounding));
    }

    // ---------- Sizes no floating-point implementation could reach ----------

    [Theory]
    [InlineData(IntegerSqrtRounding.Floor)]
    [InlineData(IntegerSqrtRounding.Ceiling)]
    [InlineData(IntegerSqrtRounding.Nearest)]
    public void Sqrt_OfALargePerfectSquareIsExactInEveryMode(IntegerSqrtRounding rounding)
    {
        // 10^60 is a perfect square far past double's 53 bits of mantissa, so no
        // floating-point implementation could return its root exactly.
        Assert.Equal(
            BigInteger.Pow(10, 30),
            IntegerMath.Sqrt(BigInteger.Pow(10, 60), rounding));
    }

    [Fact]
    public void Sqrt_EitherSideOfALargePerfectSquare()
    {
        var root = BigInteger.Pow(10, 30);
        var square = root * root;

        // One below: floor drops to root - 1; nearest and ceiling stay at root.
        Assert.Equal(root - 1, IntegerMath.Sqrt(square - 1, IntegerSqrtRounding.Floor));
        Assert.Equal(root, IntegerMath.Sqrt(square - 1, IntegerSqrtRounding.Nearest));
        Assert.Equal(root, IntegerMath.Sqrt(square - 1, IntegerSqrtRounding.Ceiling));

        // One above: floor and nearest stay at root; ceiling rises to root + 1.
        Assert.Equal(root, IntegerMath.Sqrt(square + 1, IntegerSqrtRounding.Floor));
        Assert.Equal(root, IntegerMath.Sqrt(square + 1, IntegerSqrtRounding.Nearest));
        Assert.Equal(root + 1, IntegerMath.Sqrt(square + 1, IntegerSqrtRounding.Ceiling));
    }

    [Fact]
    public void Sqrt_SatisfiesDefiningBoundOnAVeryLargeNonSquare()
    {
        // 2^1001 is not a perfect square, its exponent being odd, and it is far outside
        // any fixed-width integer as well as double's exact range.
        var value = BigInteger.Pow(2, 1001);
        var k = IntegerMath.Sqrt(value, IntegerSqrtRounding.Floor);

        Assert.True(k * k < value);
        Assert.True(value < (k + 1) * (k + 1));
        Assert.Equal(k + 1, IntegerMath.Sqrt(value, IntegerSqrtRounding.Ceiling));
    }

    // ---------- Domain: negative input is rejected ----------

    [Theory]
    [InlineData(IntegerSqrtRounding.Floor)]
    [InlineData(IntegerSqrtRounding.Ceiling)]
    [InlineData(IntegerSqrtRounding.Nearest)]
    public void Sqrt_RejectsNegativeInputInEveryMode(IntegerSqrtRounding rounding)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(BigInteger.MinusOne, rounding));

        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Sqrt_RejectsNegativeInputAtTheBoundaryAndFarBeyondIt()
    {
        // -1 is the boundary: the largest negative integer, adjacent to the smallest
        // value in the domain. The large one confirms the sign test does not depend on
        // the magnitude fitting into anything.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(BigInteger.MinusOne));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(-BigInteger.Pow(10, 60)));

        // ...and the value immediately above the boundary is accepted.
        Assert.Equal(BigInteger.Zero, IntegerMath.Sqrt(BigInteger.Zero));
    }

    [Fact]
    public void Sqrt_RejectsANegativeSquareRatherThanReturningItsRoot()
    {
        // -4 has a root in the Gaussian integers. The domain here is the non-negative
        // integers, so it is rejected like any other negative rather than answered with 2.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(new BigInteger(-4)));
    }

    // ---------- Domain: undefined rounding modes are rejected ----------

    [Theory]
    [InlineData(3)]              // one past the last defined member: the boundary
    [InlineData(-1)]             // below the first
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void Sqrt_RejectsUndefinedRoundingMode(int rounding)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(new BigInteger(4), (IntegerSqrtRounding)rounding));

        Assert.Equal("rounding", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]    // zero
    [InlineData(1)]    // one
    [InlineData(4)]    // a perfect square
    [InlineData(5)]    // a non-square
    public void Sqrt_RejectsUndefinedRoundingModeForEveryShapeOfInput(long input)
    {
        // Rejection is uniform across the shapes of input, rather than reaching only
        // those whose path happens to arrive at a validating branch. This does not
        // observe *where* the check sits: moving it into the switch's default arm leaves
        // behaviour identical and this test green, which was measured rather than
        // assumed. What it does catch is a check some inputs can bypass.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(new BigInteger(input), (IntegerSqrtRounding)42));
    }

    [Fact]
    public void Sqrt_ReportsTheValueBeforeTheModeWhenBothAreInvalid()
    {
        // Precedence is part of the contract: arguments are validated in declaration
        // order, so a caller passing two bad arguments is told about the value first.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => IntegerMath.Sqrt(BigInteger.MinusOne, (IntegerSqrtRounding)42));

        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void IntegerSqrtRounding_DefinesExactlyTheThreeDocumentedModes()
    {
        // Pins the enum's surface. Adding a member without deciding its rounding
        // behaviour would leave Sqrt accepting a mode nothing here covers.
        Assert.Equal(
            new[]
            {
                IntegerSqrtRounding.Floor,
                IntegerSqrtRounding.Ceiling,
                IntegerSqrtRounding.Nearest,
            },
            Enum.GetValues<IntegerSqrtRounding>());
    }
}
