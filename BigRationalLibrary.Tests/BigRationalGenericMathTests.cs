using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace HalHeinrich.Numerics.Tests;

/// <remarks>
/// Every test here calls a generic method whose only knowledge of its type argument
/// comes from an interface constraint. That is the point: the operators themselves
/// already worked, so a test that called them directly would prove nothing about the
/// declarations added for halheinrich/Math#34. These tests stop compiling if the
/// corresponding interface is dropped from BigRational's base list, which is the
/// property under test.
/// </remarks>
public class BigRationalGenericMathTests
{
    // The generic surface under test. Nothing in here names BigRational, so each
    // helper compiles only against what its constraint promises.
    private static class Generic
    {
        public static T Add<T>(T a, T b) where T : IAdditionOperators<T, T, T> => a + b;

        public static T Subtract<T>(T a, T b) where T : ISubtractionOperators<T, T, T> => a - b;

        public static T Multiply<T>(T a, T b) where T : IMultiplyOperators<T, T, T> => a * b;

        public static T Divide<T>(T a, T b) where T : IDivisionOperators<T, T, T> => a / b;

        public static T Negate<T>(T value) where T : IUnaryNegationOperators<T, T> => -value;

        public static T Plus<T>(T value) where T : IUnaryPlusOperators<T, T> => +value;

        public static bool AreEqual<T>(T a, T b) where T : IEqualityOperators<T, T, bool> => a == b;

        public static bool AreUnequal<T>(T a, T b) where T : IEqualityOperators<T, T, bool> => a != b;

        public static T Max<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => a > b ? a : b;

        public static T Min<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => a < b ? a : b;

        public static bool IsWithin<T>(T value, T low, T high)
            where T : IComparisonOperators<T, T, bool> => value >= low && value <= high;

        public static T AdditiveIdentityOf<T>() where T : IAdditiveIdentity<T, T> => T.AdditiveIdentity;

        public static T MultiplicativeIdentityOf<T>() where T : IMultiplicativeIdentity<T, T> =>
            T.MultiplicativeIdentity;

        // The consumer shape the ruling on halheinrich/Math#34 names as the payoff:
        // a fold needs the operator and the seed, and nothing else.
        public static T Sum<T>(IEnumerable<T> values)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            var total = T.AdditiveIdentity;
            foreach (var value in values)
                total += value;
            return total;
        }

        public static T Product<T>(IEnumerable<T> values)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            var total = T.MultiplicativeIdentity;
            foreach (var value in values)
                total *= value;
            return total;
        }

        public static T ParseSpan<T>(ReadOnlySpan<char> s, IFormatProvider? provider)
            where T : ISpanParsable<T> => T.Parse(s, provider);

        // The out parameter carries the interface's own annotation: ISpanParsable
        // promises nothing about result on failure, and a helper that dropped that
        // would be claiming more than the contract it forwards to.
        public static bool TryParseSpan<T>(
            ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out T result)
            where T : ISpanParsable<T> => T.TryParse(s, provider, out result);
    }

    // ---- Arithmetic operator interfaces ------------------------------------

    [Fact]
    public void IAdditionOperators_Constrained_Add_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(17, 12), Generic.Add(new BigRational(3, 4), new BigRational(2, 3)));
    }

    [Fact]
    public void ISubtractionOperators_Constrained_Subtract_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(1, 12), Generic.Subtract(new BigRational(3, 4), new BigRational(2, 3)));
    }

    [Fact]
    public void IMultiplyOperators_Constrained_Multiply_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(1, 2), Generic.Multiply(new BigRational(3, 4), new BigRational(2, 3)));
    }

    [Fact]
    public void IDivisionOperators_Constrained_Divide_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(9, 8), Generic.Divide(new BigRational(3, 4), new BigRational(2, 3)));
    }

    [Fact]
    public void IDivisionOperators_Constrained_Divide_By_Zero_Still_Throws()
    {
        // The interface does not soften the documented contract: a generic caller
        // sees the same DivideByZeroException a direct one does.
        Assert.Throws<DivideByZeroException>(
            () => Generic.Divide(new BigRational(3, 4), BigRational.Zero));
    }

    [Fact]
    public void IUnaryNegationOperators_Constrained_Negate_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(-3, 4), Generic.Negate(new BigRational(3, 4)));
        Assert.Equal(new BigRational(3, 4), Generic.Negate(new BigRational(-3, 4)));
    }

    [Fact]
    public void IUnaryPlusOperators_Constrained_Plus_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(-3, 4), Generic.Plus(new BigRational(-3, 4)));
    }

    // ---- Equality and comparison -------------------------------------------

    [Fact]
    public void IEqualityOperators_Constrained_Comparison_Accepts_BigRational()
    {
        // Unreduced input: equality is decided on the reduced form, and the generic
        // caller gets that behaviour unchanged.
        Assert.True(Generic.AreEqual(new BigRational(3, 4), new BigRational(6, 8)));
        Assert.False(Generic.AreUnequal(new BigRational(3, 4), new BigRational(6, 8)));
        Assert.True(Generic.AreUnequal(new BigRational(3, 4), new BigRational(2, 3)));
    }

    [Fact]
    public void IComparisonOperators_Constrained_Ordering_Accepts_BigRational()
    {
        var twoThirds = new BigRational(2, 3);
        var threeQuarters = new BigRational(3, 4);

        Assert.Equal(threeQuarters, Generic.Max(twoThirds, threeQuarters));
        Assert.Equal(twoThirds, Generic.Min(twoThirds, threeQuarters));
    }

    [Fact]
    public void IComparisonOperators_Constrained_Ordering_Spans_The_Sign_Change()
    {
        var negative = new BigRational(-7, 2);
        var positive = new BigRational(1, 1000000);

        Assert.Equal(positive, Generic.Max(negative, positive));
        Assert.Equal(negative, Generic.Min(negative, positive));
    }

    [Fact]
    public void IComparisonOperators_Constrained_Range_Check_Includes_Its_Endpoints()
    {
        var low = new BigRational(-1, 2);
        var high = new BigRational(1, 2);

        Assert.True(Generic.IsWithin(low, low, high));
        Assert.True(Generic.IsWithin(high, low, high));
        Assert.True(Generic.IsWithin(BigRational.Zero, low, high));
        Assert.False(Generic.IsWithin(new BigRational(501, 1000), low, high));
    }

    // ---- Identity elements --------------------------------------------------

    [Fact]
    public void IAdditiveIdentity_Constrained_Accessor_Yields_Zero()
    {
        Assert.Equal(BigRational.Zero, Generic.AdditiveIdentityOf<BigRational>());
    }

    [Fact]
    public void IMultiplicativeIdentity_Constrained_Accessor_Yields_One()
    {
        Assert.Equal(BigRational.One, Generic.MultiplicativeIdentityOf<BigRational>());
    }

    [Fact]
    public void Generic_Sum_Of_Empty_Sequence_Is_The_Additive_Identity()
    {
        Assert.Equal(BigRational.Zero, Generic.Sum(Array.Empty<BigRational>()));
    }

    [Fact]
    public void Generic_Sum_Of_Single_Value_Leaves_It_Unchanged()
    {
        Assert.Equal(new BigRational(3, 4), Generic.Sum([new BigRational(3, 4)]));
    }

    [Fact]
    public void Generic_Sum_Folds_Exactly()
    {
        // 1/2 + 1/3 + 1/6 == 1 exactly; a floating-point fold would not land on it.
        BigRational[] values = [new(1, 2), new(1, 3), new(1, 6)];
        Assert.Equal(BigRational.One, Generic.Sum(values));
    }

    [Fact]
    public void Generic_Product_Of_Empty_Sequence_Is_The_Multiplicative_Identity()
    {
        Assert.Equal(BigRational.One, Generic.Product(Array.Empty<BigRational>()));
    }

    [Fact]
    public void Generic_Product_Telescopes_Exactly()
    {
        // (1/2)(2/3)(3/4)(4/5) telescopes to 1/5.
        BigRational[] values = [new(1, 2), new(2, 3), new(3, 4), new(4, 5)];
        Assert.Equal(new BigRational(1, 5), Generic.Product(values));
    }

    // ---- ISpanParsable ------------------------------------------------------

    [Fact]
    public void ISpanParsable_Constrained_Parse_Accepts_BigRational()
    {
        Assert.Equal(new BigRational(3, 4),
                     Generic.ParseSpan<BigRational>("3/4", CultureInfo.InvariantCulture));
        Assert.Equal(new BigRational(-5, 1),
                     Generic.ParseSpan<BigRational>("-5", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ISpanParsable_Constrained_Parse_Rejects_Malformed_Input()
    {
        Assert.Throws<FormatException>(
            () => Generic.ParseSpan<BigRational>("3/", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ISpanParsable_Constrained_TryParse_Reports_Both_Outcomes()
    {
        Assert.True(Generic.TryParseSpan<BigRational>("6/8", CultureInfo.InvariantCulture, out var parsed));
        Assert.Equal(new BigRational(3, 4), parsed);

        Assert.False(Generic.TryParseSpan<BigRational>("3/0", CultureInfo.InvariantCulture, out var rejected));
        Assert.Equal(default, rejected);
    }

    // ---- The declared set, composed -----------------------------------------

    [Fact]
    public void Declared_Set_Composes_Into_One_Generic_Computation()
    {
        // Parses, folds, divides, negates and orders without any helper naming
        // BigRational - which is the composability the declarations were added for.
        BigRational[] values =
        [
            Generic.ParseSpan<BigRational>("1/2", CultureInfo.InvariantCulture),
            Generic.ParseSpan<BigRational>("1/3", CultureInfo.InvariantCulture),
            Generic.ParseSpan<BigRational>("-1/6", CultureInfo.InvariantCulture),
        ];

        var sum = Generic.Sum(values);
        Assert.Equal(new BigRational(2, 3), sum);

        var halved = Generic.Divide(sum, Generic.Add(BigRational.One, BigRational.One));
        Assert.Equal(new BigRational(1, 3), halved);

        Assert.Equal(halved, Generic.Max(halved, Generic.Negate(halved)));
        Assert.True(Generic.AreEqual(Generic.Plus(halved), halved));
    }
}
