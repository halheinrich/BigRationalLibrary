using System;
using System.Globalization;
using System.Numerics;
using BigRationalLibraryNamespace;
using Xunit;

namespace BigRationalLibraryNamespace.Tests
{
    public class BigRationalTests
    {
        [Fact]
        public void Reduction_Works()
        {
            var r = new BigRational(2, 4);
            Assert.Equal(new BigRational(1, 2), r);
        }

        [Fact]
        public void Sign_Normalizes()
        {
            var r = new BigRational(1, -2);
            Assert.Equal(new BigRational(-1, 2), r);
            Assert.True(r.Denominator > 0);
        }

        [Fact]
        public void Zero_Is_Singleton()
        {
            var r = new BigRational(0, 5);
            Assert.Equal(BigRational.Zero, r);
            Assert.True(r.IsZero);
            Assert.True(r.IsInteger);
        }

        [Fact]
        public void Constructor_DenominatorZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => new BigRational(1, 0));
        }

        [Fact]
        public void Addition_Works()
        {
            var a = new BigRational(1, 3);
            var b = new BigRational(1, 6);
            Assert.Equal(new BigRational(1, 2), a + b);
        }

        [Fact]
        public void Subtraction_Works()
        {
            var a = new BigRational(3, 4);
            var b = new BigRational(1, 4);
            Assert.Equal(new BigRational(1, 2), a - b);
        }

        [Fact]
        public void Multiplication_Works_With_CrossCancellation()
        {
            var a = new BigRational(2, 3);
            var b = new BigRational(9, 4);
            Assert.Equal(new BigRational(3, 2), a * b);
        }

        [Fact]
        public void Division_Works()
        {
            var a = new BigRational(2, 3);
            var b = new BigRational(5, 7);
            Assert.Equal(new BigRational(14, 15), a / b);
        }

        [Fact]
        public void Division_ByZero_Throws()
        {
            var a = new BigRational(1, 2);
            Assert.Throws<DivideByZeroException>(() => _ = a / BigRational.Zero);
        }

        [Fact]
        public void Reciprocal_Works()
        {
            var a = new BigRational(-3, 5);
            Assert.Equal(new BigRational(-5, 3), a.Reciprocal());
        }

        [Fact]
        public void Reciprocal_Zero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => BigRational.Zero.Reciprocal());
        }

        [Fact]
        public void Negate_And_Abs_Work()
        {
            var a = new BigRational(7, 9);
            Assert.Equal(new BigRational(-7, 9), a.Negate());
            Assert.Equal(a, a.Negate().Abs());
        }

        [Fact]
        public void Compare_Works()
        {
            var a = new BigRational(1, 2);
            var b = new BigRational(2, 3);
            Assert.True(a < b);
            Assert.True(b > a);
            Assert.True(a <= a);
            Assert.True(b >= b);
        }

        [Fact]
        public void Parse_Integer_And_Fraction()
        {
            Assert.Equal(new BigRational(5, 1), BigRational.Parse("5"));
            Assert.Equal(new BigRational(7, 8), BigRational.Parse(" 7/8 "));
        }

        [Fact]
        public void TryParse_Invalid_Fails()
        {
            Assert.False(BigRational.TryParse("foo", CultureInfo.InvariantCulture, out _));
            Assert.False(BigRational.TryParse("3/0", CultureInfo.InvariantCulture, out _));
        }

        [Fact]
        public void ToString_Formats()
        {
            var a = new BigRational(5, 1);
            var b = new BigRational(7, 9);
            Assert.Equal("5", a.ToString());
            Assert.Equal("7/9", b.ToString());
        }

        [Fact]
        public void TryFormat_Writes()
        {
            var a = new BigRational(7, 11);
            Span<char> buffer = stackalloc char[10];
            Assert.True(a.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture));
            Assert.Equal("7/11", buffer[..written].ToString());
        }

        [Fact]
        public void Explicit_Double()
        {
            var a = new BigRational(3, 4);
            var d = (double)a;
            Assert.InRange(d, 0.74, 0.76);
        }

        [Fact]
        public void Explicit_Decimal_RoundsWhenExactScaleFound()
        {
            var a = new BigRational(25, 100); // 0.25
            decimal dec = (decimal)a;
            Assert.Equal(0.25m, dec);
        }

        [Fact]
        public void Decimal_Overflow_Throws()
        {
            // Construct something larger than decimal max (~79e27)
            var big = new BigRational(BigInteger.Parse("1000000000000000000000000000000000"), BigInteger.One);
            Assert.Throws<OverflowException>(() => (decimal)big);
        }

        [Fact]
        public void Implicit_From_Int()
        {
            BigRational r = 42;
            Assert.Equal(new BigRational(42, 1), r);
        }

        [Fact]
        public void Deconstruct_Works()
        {
            var r = new BigRational(5, 9);
            var (n, d) = r;
            Assert.Equal(new BigInteger(5), n);
            Assert.Equal(new BigInteger(9), d);
        }
    }
}