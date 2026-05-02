# BigRationalLibrary

Immutable, arbitrary-precision rational numbers for .NET. A `BigRational` is a reduced fraction (`Numerator / Denominator`) backed by `BigInteger`, so it never rounds, never overflows on arithmetic, and stays exact through any chain of `+`, `-`, `*`, `/`.

```csharp
using BigRationalLibraryNamespace;

var third = new BigRational(1, 3);
var sixth = new BigRational(1, 6);
Console.WriteLine(third + sixth);   // 1/2
```

## Requirements

- .NET 10 (preview); C# 13
- No third-party dependencies

## Adding to your project

There is no NuGet package yet. Either:

**Option A — project reference**

```xml
<ItemGroup>
  <ProjectReference Include="path/to/BigRationalLibrary/BigRationalLibrary.csproj" />
</ItemGroup>
```

**Option B — drop the file in**

`BigRationalLibrary/BigRationalLibrary.cs` is a single self-contained file. Copy it into your project and you're done.

```csharp
using BigRationalLibraryNamespace;
```

## What you get

`BigRational` is a `readonly struct` that implements:

- `IEquatable<BigRational>`, `IComparable<BigRational>`
- `ISpanFormattable` — alloc-free `TryFormat` writing into a `Span<char>`
- `IParsable<BigRational>` — `Parse` / `TryParse` for both `string` and `ReadOnlySpan<char>`

### Construction

```csharp
new BigRational(3, 4);                       // 3/4 (reduced + sign-normalized)
BigRational.FromInteger(BigInteger.Parse("10000000000000000000000000")); // big int
BigRational r = 42;                          // implicit from int / long / BigInteger
BigRational.Zero;  BigRational.One;  BigRational.MinusOne;
```

Values are **always stored reduced** (`gcd = 1`) with a **positive denominator**. So `new BigRational(2, 4)` and `new BigRational(-1, -2)` both equal `new BigRational(1, 2)`.

### Arithmetic and comparison

All standard operators are overloaded: `+ - * /` (binary and unary), `== != < <= > >=`. Plus instance methods:

```csharp
r.Negate();      // -r
r.Abs();         // |r|
r.Reciprocal();  // 1/r  (throws DivideByZeroException if r is zero)
```

Cross-cancellation is applied during multiplication to keep intermediates small.

### Parsing

```csharp
BigRational.Parse("5");          // 5/1
BigRational.Parse(" 7/8 ");      // 7/8 (whitespace tolerated)
BigRational.TryParse("3/0", null, out _);  // false  (zero denominator rejected)
BigRational.TryParse("foo", null, out _);  // false
```

Both `string` and `ReadOnlySpan<char>` overloads are provided. The format is `<int>` or `<int>/<int>` — no decimal-point form.

### Formatting

```csharp
new BigRational(7, 9).ToString();                          // "7/9"
new BigRational(5, 1).ToString();                          // "5"  (integer case omits the slash)
new BigRational(255, 16).ToString("X", CultureInfo.InvariantCulture);  // "0FF/10"

Span<char> buf = stackalloc char[32];
new BigRational(7, 11).TryFormat(buf, out int written, default, null);
// buf[..written] == "7/11"  -- no allocations
```

The format string passes through to `BigInteger.ToString` for both halves — so anything `BigInteger` accepts (`"X"`, `"D"`, `"N"`, `"R"`, etc.) works.

### Conversions

| Direction | Operator | Notes |
|---|---|---|
| `int` / `long` / `BigInteger` → `BigRational` | implicit | exact |
| `BigRational` → `double` | explicit | lossy; large numerators/denominators round |
| `BigRational` → `decimal` | explicit | exact when value fits in 28-29 significant digits and a terminating decimal expansion is found within 28 fractional digits; otherwise truncates or throws `OverflowException` |

There are **no** conversions *from* `double` or `decimal` yet — you'd need to write `BigRational.FromDecimal(...)` yourself if you need that.

## Behavioral notes (read these)

- **Equality is structural.** Because every value is stored reduced with a positive denominator, `==` and `GetHashCode` use the `(Numerator, Denominator)` pair directly. Two `BigRational`s are equal iff they represent the same number.

- **`default(BigRational)` is safe.** It behaves as `0/1` (a getter masks the underlying `0/0` backing state). You can put `BigRational` in arrays, dictionaries, etc. without explicit initialization.

- **Division by zero throws `DivideByZeroException`** at three sites: constructor with zero denominator, `operator /` with a zero divisor, and `Reciprocal()` on `Zero`.

- **Operations cost grows with operand size.** `BigInteger` multiplication and GCD are super-linear. Long chains with no cancellation can produce very large numerators/denominators — `*` cross-cancels but `+` and `-` do not until the result is constructed.

- **`Sign` is the sign of the numerator** (denominator is always positive).

## Known gaps

These are not bugs, just things this library doesn't ship yet:

- No `Pow(int)`, `Floor`, `Ceiling`, `Round`, `Min`, `Max`
- No `INumber<T>` / generic-math interfaces
- No conversion *from* `double` or `decimal`
- No NuGet package (consume by project reference or source drop)
- Only a small XML-doc comment on the type itself; member-level docs are missing

## Tests

```bash
dotnet test
```

25 xUnit tests covering construction, reduction, sign normalization, all four arithmetic operators, comparisons, parsing, formatting (string + span), conversions, and the `default(BigRational)` guarantee.

## License

MIT — see [LICENSE](LICENSE).

## Issues / contributions

File at https://github.com/halheinrich/BigRationalLibrary/issues.
