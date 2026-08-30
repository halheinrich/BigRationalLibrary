using System;
using System.Globalization;
using System.Numerics;

namespace HalHeinrich.Numerics;

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

    /// <summary>Gets the numerator of the reduced fraction. Carries the sign of the value.</summary>
    public BigInteger Numerator { get; }

    /// <summary>Gets the denominator of the reduced fraction. Always strictly positive.</summary>
    // Mask the default(BigRational) state (where _denominator == 0) as 0/1 instead of an invalid 0/0.
    public BigInteger Denominator => _denominator.IsZero ? BigInteger.One : _denominator;

    /// <summary>The value zero, represented as 0/1.</summary>
    public static readonly BigRational Zero = new(BigInteger.Zero, BigInteger.One, alreadyNormalized: true);

    /// <summary>The value one, represented as 1/1.</summary>
    public static readonly BigRational One = new(BigInteger.One, BigInteger.One, alreadyNormalized: true);

    /// <summary>The value negative one, represented as -1/1.</summary>
    public static readonly BigRational MinusOne = new(BigInteger.MinusOne, BigInteger.One, alreadyNormalized: true);

    /// <summary>Gets a value indicating whether this value is zero.</summary>
    public bool IsZero => Numerator.IsZero;

    /// <summary>Gets a value indicating whether this value is an integer, i.e. has a denominator of one.</summary>
    public bool IsInteger => Denominator.IsOne;

    /// <summary>Gets a number indicating the sign of this value: -1, 0 or 1.</summary>
    public int Sign => Numerator.Sign;

    /// <summary>
    /// Initializes a new <see cref="BigRational"/> from the given numerator and denominator,
    /// reducing the fraction and normalizing the sign onto the numerator.
    /// </summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <exception cref="DivideByZeroException"><paramref name="denominator"/> is zero.</exception>
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

    /// <summary>Creates a <see cref="BigRational"/> representing the given integer value.</summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A <see cref="BigRational"/> equal to <paramref name="value"/>.</returns>
    public static BigRational FromInteger(BigInteger value) => new(value, BigInteger.One, alreadyNormalized: true);

    /// <summary>Creates a reduced <see cref="BigRational"/> from the given numerator and denominator.</summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <returns>The reduced fraction.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="denominator"/> is zero.</exception>
    public static BigRational Create(BigInteger numerator, BigInteger denominator) => new(numerator, denominator);

    /// <summary>Returns the additive inverse of a value.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The negation of <paramref name="value"/>.</returns>
    public static BigRational Negate(BigRational value) =>
        new(BigInteger.Negate(value.Numerator), value.Denominator, alreadyNormalized: true);

    /// <summary>Returns the absolute value of a value.</summary>
    /// <param name="value">The value whose magnitude is returned.</param>
    /// <returns>The absolute value of <paramref name="value"/>.</returns>
    public static BigRational Abs(BigRational value) =>
        value.Numerator.Sign >= 0 ? value : Negate(value);

    /// <summary>Returns the multiplicative inverse of a value.</summary>
    /// <param name="value">The value to invert.</param>
    /// <returns>The reciprocal of <paramref name="value"/>.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="value"/> is zero.</exception>
    public static BigRational Reciprocal(BigRational value)
    {
        if (value.IsZero) throw new DivideByZeroException("Cannot take reciprocal of zero.");
        return new BigRational(value.Denominator * (value.Numerator.Sign < 0 ? -1 : 1),
                               BigInteger.Abs(value.Numerator),
                               alreadyNormalized: true);
    }

    // Arithmetic

    /// <summary>Adds two rational numbers.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The sum.</returns>
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

    /// <summary>Subtracts one rational number from another.</summary>
    /// <param name="a">The minuend.</param>
    /// <param name="b">The subtrahend.</param>
    /// <returns>The difference.</returns>
    public static BigRational operator -(BigRational a, BigRational b)
    {
        if (b.IsZero) return a;
        if (a.IsZero) return Negate(b);
        if (a.Denominator == b.Denominator)
            return new BigRational(a.Numerator - b.Numerator, a.Denominator);

        var n = a.Numerator * b.Denominator - b.Numerator * a.Denominator;
        var d = a.Denominator * b.Denominator;
        return new BigRational(n, d);
    }

    /// <summary>Multiplies two rational numbers.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The product.</returns>
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

    /// <summary>Divides one rational number by another.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
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

    /// <summary>Returns the value unchanged.</summary>
    /// <param name="v">The value.</param>
    /// <returns><paramref name="v"/>.</returns>
    public static BigRational operator +(BigRational v) => v;

    /// <summary>Returns the additive inverse of the value.</summary>
    /// <param name="v">The value.</param>
    /// <returns>The negation of <paramref name="v"/>.</returns>
    public static BigRational operator -(BigRational v) => Negate(v);

    // Comparisons

    /// <summary>Determines whether two rational numbers are equal.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(BigRational a, BigRational b) => a.Numerator == b.Numerator && a.Denominator == b.Denominator;

    /// <summary>Determines whether two rational numbers are unequal.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> if the values are unequal; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(BigRational a, BigRational b) => !(a == b);

    /// <summary>Determines whether one rational number is less than another.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>.</returns>
    public static bool operator <(BigRational a, BigRational b) => a.CompareTo(b) < 0;

    /// <summary>Determines whether one rational number is greater than another.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>.</returns>
    public static bool operator >(BigRational a, BigRational b) => a.CompareTo(b) > 0;

    /// <summary>Determines whether one rational number is less than or equal to another.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>.</returns>
    public static bool operator <=(BigRational a, BigRational b) => a.CompareTo(b) <= 0;

    /// <summary>Determines whether one rational number is greater than or equal to another.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>.</returns>
    public static bool operator >=(BigRational a, BigRational b) => a.CompareTo(b) >= 0;

    /// <summary>Compares this value with another rational number.</summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>A negative number, zero, or a positive number as this value is less than, equal to, or greater than <paramref name="other"/>.</returns>
    public int CompareTo(BigRational other)
    {
        // Compare a/b ? c/d via cross product: ad ? cb
        // (a.n * o.d) and (o.n * a.d)
        var left = Numerator * other.Denominator;
        var right = other.Numerator * Denominator;
        return left.CompareTo(right);
    }

    /// <summary>Determines whether this value equals another rational number.</summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise <see langword="false"/>.</returns>
    public bool Equals(BigRational other) => this == other;

    /// <summary>Determines whether this value equals the given object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="BigRational"/> equal to this value.</returns>
    public override bool Equals(object? obj) => obj is BigRational br && Equals(br);

    /// <summary>Returns a hash code for this value.</summary>
    /// <returns>A hash code derived from the reduced numerator and denominator.</returns>
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    /// <summary>Deconstructs this value into its reduced numerator and denominator.</summary>
    /// <param name="numerator">Receives the numerator.</param>
    /// <param name="denominator">Receives the denominator.</param>
    public void Deconstruct(out BigInteger numerator, out BigInteger denominator)
    {
        numerator = Numerator;
        denominator = Denominator;
    }

    // Conversions

    /// <summary>Converts an <see cref="int"/> to a <see cref="BigRational"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator BigRational(int value) => new(value, 1, alreadyNormalized: true);

    /// <summary>Converts a <see cref="long"/> to a <see cref="BigRational"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator BigRational(long value) => new(value, 1, alreadyNormalized: true);

    /// <summary>Converts a <see cref="BigInteger"/> to a <see cref="BigRational"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator BigRational(BigInteger value) => new(value, 1, alreadyNormalized: true);

    /// <summary>Converts a <see cref="BigRational"/> to the nearest <see cref="double"/>.</summary>
    /// <param name="value">The value to convert.</param>
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

    /// <summary>Converts a <see cref="BigRational"/> to a <see cref="decimal"/>, rounded to the decimal scale limit.</summary>
    /// <param name="value">The value to convert.</param>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="decimal"/>.</exception>
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

    /// <summary>Returns the string representation of this value using the current culture.</summary>
    /// <returns>An integer string when the value is an integer; otherwise "numerator/denominator".</returns>
    public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

    /// <summary>Returns the string representation of this value.</summary>
    /// <param name="format">A format string applied to the numerator and denominator.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>An integer string when the value is an integer; otherwise "numerator/denominator".</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (IsInteger)
            return Numerator.ToString(format, formatProvider);
        return $"{Numerator.ToString(format, formatProvider)}/{Denominator.ToString(format, formatProvider)}";
    }

    /// <summary>Attempts to format this value into the given character span.</summary>
    /// <param name="destination">The span to write to.</param>
    /// <param name="charsWritten">Receives the number of characters written.</param>
    /// <param name="format">A format string applied to the numerator and denominator.</param>
    /// <param name="provider">The format provider.</param>
    /// <returns><see langword="true"/> if formatting succeeded; <see langword="false"/> if the destination was too small.</returns>
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

    /// <summary>Parses a rational number from a string.</summary>
    /// <param name="s">The string to parse, either "n" or "n/d".</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not in a recognized format.</exception>
    public static BigRational Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var value))
            return value;
        throw new FormatException("Input string was not in a correct BigRational format.");
    }

    /// <summary>Attempts to parse a rational number from a string.</summary>
    /// <param name="s">The string to parse, either "n" or "n/d".</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">Receives the parsed value, or the default value on failure.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out BigRational result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>Parses a rational number from a character span.</summary>
    /// <param name="s">The span to parse, either "n" or "n/d".</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not in a recognized format.</exception>
    public static BigRational Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var value))
            return value;
        throw new FormatException("Input span was not in a correct BigRational format.");
    }

    /// <summary>Attempts to parse a rational number from a character span.</summary>
    /// <param name="s">The span to parse, either "n" or "n/d".</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">Receives the parsed value, or the default value on failure.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
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
