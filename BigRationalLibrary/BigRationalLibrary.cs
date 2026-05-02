using System;
using System.Globalization;
using System.Numerics;

namespace HalHeinrich.Numerics
{
    /// <summary>
    /// Immutable arbitrary-precision rational number represented as a reduced fraction (Numerator / Denominator).
    /// Denominator is always positive. Zero is represented as 0/1.
    /// </summary>
    public readonly struct BigRational :
        IEquatable<BigRational>,
        IComparable<BigRational>,
        ISpanFormattable,
        IParsable<BigRational>
    {
        private readonly BigInteger _denominator;

        public BigInteger Numerator { get; }
        // Mask the default(BigRational) state (where _denominator == 0) as 0/1 instead of an invalid 0/0.
        public BigInteger Denominator => _denominator.IsZero ? BigInteger.One : _denominator;

        public static readonly BigRational Zero = new(BigInteger.Zero, BigInteger.One, alreadyNormalized: true);
        public static readonly BigRational One = new(BigInteger.One, BigInteger.One, alreadyNormalized: true);
        public static readonly BigRational MinusOne = new(BigInteger.MinusOne, BigInteger.One, alreadyNormalized: true);

        public bool IsZero => Numerator.IsZero;
        public bool IsInteger => Denominator.IsOne;
        public int Sign => Numerator.Sign;

        public BigRational(BigInteger numerator, BigInteger denominator)
            : this(numerator, denominator, alreadyNormalized: false) { }

        private BigRational(BigInteger numerator, BigInteger denominator, bool alreadyNormalized)
        {
            if (denominator.IsZero)
                throw new DivideByZeroException("Denominator cannot be zero.");

            if (!alreadyNormalized)
            {
                // Normalize sign to denominator
                if (denominator.Sign < 0)
                {
                    numerator = BigInteger.Negate(numerator);
                    denominator = BigInteger.Negate(denominator);
                }

                if (numerator.IsZero)
                {
                    Numerator = BigInteger.Zero;
                    _denominator = BigInteger.One;
                    return;
                }

                var g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
                if (g > BigInteger.One)
                {
                    numerator /= g;
                    denominator /= g;
                }
            }

            Numerator = numerator;
            _denominator = denominator;
        }

        public static BigRational FromInteger(BigInteger value) => new(value, BigInteger.One, alreadyNormalized: true);

        public static BigRational Create(BigInteger numerator, BigInteger denominator) => new(numerator, denominator);

        public BigRational Negate() => new(BigInteger.Negate(Numerator), Denominator, alreadyNormalized: true);
        public BigRational Abs() => Numerator.Sign >= 0 ? this : Negate();
        public BigRational Reciprocal()
        {
            if (IsZero) throw new DivideByZeroException("Cannot take reciprocal of zero.");
            return new BigRational(Denominator * (Numerator.Sign < 0 ? -1 : 1),
                                   BigInteger.Abs(Numerator),
                                   alreadyNormalized: true);
        }

        // Arithmetic
        public static BigRational operator +(BigRational a, BigRational b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            if (a.Denominator == b.Denominator)
                return new BigRational(a.Numerator + b.Numerator, a.Denominator);

            // (a/b)+(c/d) = (ad+bc)/bd
            var n = a.Numerator * b.Denominator + b.Numerator * a.Denominator;
            var d = a.Denominator * b.Denominator;
            return new BigRational(n, d);
        }

        public static BigRational operator -(BigRational a, BigRational b)
        {
            if (b.IsZero) return a;
            if (a.IsZero) return b.Negate();
            if (a.Denominator == b.Denominator)
                return new BigRational(a.Numerator - b.Numerator, a.Denominator);

            var n = a.Numerator * b.Denominator - b.Numerator * a.Denominator;
            var d = a.Denominator * b.Denominator;
            return new BigRational(n, d);
        }

        public static BigRational operator *(BigRational a, BigRational b)
        {
            if (a.IsZero || b.IsZero) return Zero;

            // Attempt cross cancellation to minimize size:
            var gcd1 = BigInteger.GreatestCommonDivisor(BigInteger.Abs(a.Numerator), b.Denominator);
            var gcd2 = BigInteger.GreatestCommonDivisor(BigInteger.Abs(b.Numerator), a.Denominator);

            var n = (a.Numerator / gcd1) * (b.Numerator / gcd2);
            var d = (a.Denominator / gcd2) * (b.Denominator / gcd1);
            return new BigRational(n, d, alreadyNormalized: true);
        }

        public static BigRational operator /(BigRational a, BigRational b)
        {
            if (b.IsZero) throw new DivideByZeroException();
            if (a.IsZero) return Zero;

            // a/b = (a.n / a.d) / (b.n / b.d) = (a.n * b.d) / (a.d * b.n)
            var num = a.Numerator * b.Denominator;
            var den = a.Denominator * b.Numerator;
            if (den.Sign < 0)
            {
                num = BigInteger.Negate(num);
                den = BigInteger.Negate(den);
            }
            return new BigRational(num, den);
        }

        public static BigRational operator +(BigRational v) => v;
        public static BigRational operator -(BigRational v) => v.Negate();

        // Comparisons
        public static bool operator ==(BigRational a, BigRational b) => a.Numerator == b.Numerator && a.Denominator == b.Denominator;
        public static bool operator !=(BigRational a, BigRational b) => !(a == b);
        public static bool operator <(BigRational a, BigRational b) => a.CompareTo(b) < 0;
        public static bool operator >(BigRational a, BigRational b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigRational a, BigRational b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigRational a, BigRational b) => a.CompareTo(b) >= 0;

        public int CompareTo(BigRational other)
        {
            // Compare a/b ? c/d via cross product: ad ? cb
            // (a.n * o.d) and (o.n * a.d)
            var left = Numerator * other.Denominator;
            var right = other.Numerator * Denominator;
            return left.CompareTo(right);
        }

        public bool Equals(BigRational other) => this == other;
        public override bool Equals(object? obj) => obj is BigRational br && Equals(br);
        public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

        public void Deconstruct(out BigInteger numerator, out BigInteger denominator)
        {
            numerator = Numerator;
            denominator = Denominator;
        }

        // Conversions
        public static implicit operator BigRational(int value) => new(value, 1, alreadyNormalized: true);
        public static implicit operator BigRational(long value) => new(value, 1, alreadyNormalized: true);
        public static implicit operator BigRational(BigInteger value) => new(value, 1, alreadyNormalized: true);

        public static explicit operator double(BigRational value)
        {
            if (value.IsZero) return 0d;
            // Use double parsing from decimal; fallback to scaled division
            var sign = value.Sign;
            var absNum = BigInteger.Abs(value.Numerator);
            var absDen = value.Denominator;
            // If fits in double via direct cast
            double numD = (double)absNum;
            double denD = (double)absDen;
            return sign * (numD / denD);
        }

        public static explicit operator decimal(BigRational value)
        {
            if (value.IsZero) return 0m;

            // Attempt exact conversion (limit 28-29 digits)
            var scale = 0;
            var num = BigInteger.Abs(value.Numerator);
            var den = value.Denominator;

            // Try to scale numerator to divide evenly while scale <= 28
            while (scale < 28)
            {
                var rem = BigInteger.Remainder(num, den);
                if (rem.IsZero) break;
                num *= 10;
                scale++;
            }

            var quotient = BigInteger.Divide(num, den);
            // BigInteger->decimal cast already throws OverflowException when out of range.
            decimal result = (decimal)quotient;
            if (scale > 0)
                result /= Pow10Decimal(scale);

            if (value.Sign < 0)
                result = -result;
            return result;
        }

        private static decimal Pow10Decimal(int exp)
        {
            decimal v = 1m;
            for (int i = 0; i < exp; i++) v *= 10m;
            return v;
        }

        // Formatting
        public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (IsInteger)
                return Numerator.ToString(format, formatProvider);
            return $"{Numerator.ToString(format, formatProvider)}/{Denominator.ToString(format, formatProvider)}";
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            if (IsInteger)
                return Numerator.TryFormat(destination, out charsWritten, format, provider);

            if (!Numerator.TryFormat(destination, out int numWritten, format, provider))
            {
                charsWritten = 0;
                return false;
            }
            if (numWritten >= destination.Length)
            {
                charsWritten = 0;
                return false;
            }
            destination[numWritten] = '/';
            if (!Denominator.TryFormat(destination[(numWritten + 1)..], out int denWritten, format, provider))
            {
                charsWritten = 0;
                return false;
            }
            charsWritten = numWritten + 1 + denWritten;
            return true;
        }

        // Parsing
        public static BigRational Parse(string s, IFormatProvider? provider = null)
        {
            if (TryParse(s, provider, out var value))
                return value;
            throw new FormatException("Input string was not in a correct BigRational format.");
        }

        public static bool TryParse(string? s, IFormatProvider? provider, out BigRational result)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                result = default;
                return false;
            }
            return TryParse(s.AsSpan(), provider, out result);
        }

        public static BigRational Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (TryParse(s, provider, out var value))
                return value;
            throw new FormatException("Input span was not in a correct BigRational format.");
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigRational result)
        {
            var span = s.Trim();
            var slashIndex = span.IndexOf('/');
            if (slashIndex < 0)
            {
                if (BigInteger.TryParse(span, NumberStyles.Integer, provider, out var n))
                {
                    result = new BigRational(n, BigInteger.One, alreadyNormalized: true);
                    return true;
                }
                result = default;
                return false;
            }
            var left = span[..slashIndex].Trim();
            var right = span[(slashIndex + 1)..].Trim();
            if (BigInteger.TryParse(left, NumberStyles.Integer, provider, out var num) &&
                BigInteger.TryParse(right, NumberStyles.Integer, provider, out var den) &&
                !den.IsZero)
            {
                result = new BigRational(num, den);
                return true;
            }
            result = default;
            return false;
        }

        // IParsable interface explicit implementations
        static BigRational IParsable<BigRational>.Parse(string s, IFormatProvider? provider) => Parse(s, provider);
        static bool IParsable<BigRational>.TryParse(string? s, IFormatProvider? provider, out BigRational result) =>
            TryParse(s, provider, out result);
    }
}
