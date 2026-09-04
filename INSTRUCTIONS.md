# BigRationalLibrary

> Collaboration contract → `../AGENTS.md`.
> Cross-cutting status & dependency graph → `../INSTRUCTIONS.md`.
> Mission, principles & repo conventions → `../VISION.md`.

The deep working reference for this submodule. The investigation its consumers
serve is specified in `../SPEC-rational-ratio.md`.

## Stack

A C# class library and its xUnit test project; language version, target
framework and namespace conventions are umbrella-wide and live in
`../VISION.md` and `Directory.Build.props`.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\Math\BigRationalLibrary\BigRationalLibrary.sln`

## Repo

`https://github.com/halheinrich/BigRationalLibrary`, branch `main`.

## Depends on

**Standalone.** The library project has no `PackageReference` and no
`ProjectReference`; it is built on `System.Numerics.BigInteger` and nothing
else. This is the root of the umbrella's dependency graph, and the only member
that a fresh clone can build on its own.

That independence is worth keeping. A dependency here propagates to every
member, and to whatever external consumers the published package has.

## Layout

- **`BigRationalLibrary`** — the library. A single public type, `BigRational`,
  and its private helpers. This is the whole published surface.
- **`BigRationalLibrary.Tests`** — xUnit. Split by concern rather than by a
  single file per type: the general surface, `Pow`, and `Round`. It has **no**
  `InternalsVisibleTo` and does not need one — everything it exercises is
  public, because everything the package ships is public.

## Architecture

`BigRational` is a `readonly struct`: an immutable arbitrary-precision rational
stored as a reduced fraction whose denominator is always strictly positive. The
sign lives on the numerator. It implements `IEquatable<BigRational>`,
`IComparable<BigRational>`, `ISpanFormattable` and `IParsable<BigRational>`.

### The two invariants everything else rests on

**Always reduced, denominator always positive.** Every value that reaches a
consumer satisfies both. They are not defensive habits — real behaviour is built
on them:

- `operator ==` compares numerator and denominator directly rather than
  cross-multiplying, which is only correct because both sides are reduced.
  `GetHashCode` combines the same two parts, so equal values hash equally for
  the same reason.
- `Pow` skips a gcd pass entirely (see below).
- Consumers depend on it too. `RationalApproximation`'s `RationalCandidate.Height`
  is a property of the reduced fraction, taken straight from these parts.

Break either invariant and all three break quietly, returning wrong answers
rather than throwing.

### `default(BigRational)` is a valid zero

A struct can always be default-constructed, which would give a denominator of
zero — an invalid `0/0`. Rather than accept that, the denominator is held in a
private field `_denominator` and exposed through a `Denominator` property that
masks a zero field as `BigInteger.One`. So `default(BigRational)` reads as `0/1`
and behaves as exact zero everywhere.

**This is why the property, not the field, is what internal code must read.**
The field is the raw storage and is zero for a defaulted instance; every
member that needs the denominator goes through `Denominator`.

### Construction is closed

The public constructor takes `(BigInteger numerator, BigInteger denominator)`
and always normalises: it moves a negative sign off the denominator, reduces by
the gcd, and collapses a zero numerator to `0/1`. A zero denominator throws
`DivideByZeroException`.

A **private** constructor carries an extra `alreadyNormalized` flag and skips
that work. It is the internal fast path for the many operations that can prove
their result is already in lowest terms — the constants, the conversions from
integral types, `Negate`, `Pow`, and multiplication after cross-cancellation.
It is deliberately private: there is no public hatch for skipping validation,
and adding one would make the invariants above optional.

`FromInteger` and `Create` are the named factories over the same two paths.

### Arithmetic

`operator *` cross-cancels before multiplying — it takes the gcd of each
numerator against the *other* operand's denominator — so the intermediate
values stay as small as the result allows rather than blowing up and being
reduced afterwards. That is what lets it use the normalised-already path.
Addition and subtraction take the common-denominator shortcut when the
denominators match, and otherwise go through `ad ± bc / bd` and reduce.

`CompareTo` cross-multiplies and compares the two products, which is exact and
needs no division. All six relational operators route through it.

### The unary surface is static, and that is load-bearing

`Abs`, `Negate` and `Reciprocal` are `static` methods taking a `BigRational`,
not instance methods. Every generic-math interface member is `static abstract`,
so an instance `Abs()` could never satisfy `INumberBase<T>.Abs` — and
`INumber<T>` support is a recorded goal (§ Subproject-internal next steps).
Shipping static members beside the old instance ones would have encoded one rule
in two spellings, so the instance forms were removed rather than kept as shims.

`operator -` delegates to `Negate` rather than duplicating it.

### `Pow`

Negative exponents are supported by taking the reciprocal and then raising to
the positive power. A zero base with a negative exponent throws
`DivideByZeroException`; `Pow(x, 0)` is one for every base including zero,
matching `BigInteger.Pow`.

Two details that are easy to lose in a rewrite:

- **The gcd pass is skipped, provably.** `gcd(n, d) == 1` implies
  `gcd(n^e, d^e) == 1`, and `d > 0` implies `d^e > 0`, so a reduced fraction
  stays reduced and sign-normalised under exponentiation. The private
  already-normalised constructor is therefore correct here, not merely faster.
- **`int.MinValue` does not fit its own negation.** `-(int.MinValue)` overflows,
  and `|int.MinValue|` is one past `int.MaxValue`, so `BigInteger.Pow` cannot be
  handed the magnitude directly. The magnitude is carried in a `long` and that
  single exponent is split as `x^(2^31 - 1) * x`. Wrapping it into a positive
  `int` instead would silently return a wrong answer.

### `Round`

`Round(BigRational, MidpointRounding = ToEven)` returns a `BigInteger`. **No
midpoint policy is baked in**, deliberately: the umbrella spec's rounding is
directed-up at one site and nearest at another, so a type that chose for its
callers would be wrong at one of them.

`MidpointRounding` is not five flavours of tie-breaking. **Two of the modes are
nearest-rounding and differ only at an exact half; the other three are directed
and apply to every value.** `ToZero` truncates, `ToNegativeInfinity` floors and
`ToPositiveInfinity` ceilings, whatever the fractional part is. So the spec's
"round half up, correct for negative values" is `ToPositiveInfinity` — which
gives `5/2 -> 3` and `-5/2 -> -2`, and also sends `7/3` to `3`.
`AwayFromZero` is the mode that gets negative halves wrong for that purpose,
sending `-5/2` to `-3`.

All five are correct for negative values, and the implementation stays in exact
integer arithmetic throughout: `BigInteger.DivRem` gives a quotient truncated
toward zero and a remainder carrying the numerator's sign, and the fractional
part is compared against one half as `2*|remainder|` against the denominator —
never by dividing.

The mode is validated **before** the exact-integer short-circuit, so an
undefined `MidpointRounding` is rejected for every input rather than only for
values that happen to have a fractional part. That matches `Math.Round`.

### Formatting and parsing

`ToString` renders an integer as just its numerator and anything else as
`"numerator/denominator"`; `TryFormat` writes the same shape into a span and
returns `false` rather than throwing when the destination is too small. Parsing
accepts both shapes, trims around the parts, and rejects a zero denominator.
`IParsable<BigRational>`'s members are implemented explicitly, so the public
`Parse` overload can keep its optional `provider` parameter.

The conversion to `decimal` scales the numerator by ten until the division is
exact or 28 digits are consumed, then divides — so it is exact when the value is
representable and correctly rounded when it is not, and relies on the
`BigInteger`-to-`decimal` cast to throw `OverflowException` out of range.

## Public API

Namespace `HalHeinrich.Numerics`.

```csharp
public readonly struct BigRational :
    IEquatable<BigRational>, IComparable<BigRational>,
    ISpanFormattable, IParsable<BigRational>
{
    public BigInteger Numerator { get; }      // carries the sign
    public BigInteger Denominator { get; }    // always > 0; default reads as 1

    public static readonly BigRational Zero;
    public static readonly BigRational One;
    public static readonly BigRational MinusOne;

    public bool IsZero { get; }
    public bool IsInteger { get; }            // Denominator is one
    public int Sign { get; }                  // -1, 0 or 1

    public BigRational(BigInteger numerator, BigInteger denominator);
    public static BigRational FromInteger(BigInteger value);
    public static BigRational Create(BigInteger numerator, BigInteger denominator);

    public static BigRational Negate(BigRational value);
    public static BigRational Abs(BigRational value);
    public static BigRational Reciprocal(BigRational value);

    public static BigRational Pow(BigRational value, int exponent);
    public static BigInteger Round(
        BigRational value, MidpointRounding mode = MidpointRounding.ToEven);

    // operators + - * / (binary), + - (unary), == != < > <= >=
    // implicit from int, long, BigInteger; explicit to double, decimal

    public int CompareTo(BigRational other);
    public void Deconstruct(out BigInteger numerator, out BigInteger denominator);

    public string ToString(string? format, IFormatProvider? formatProvider);
    public bool TryFormat(Span<char> destination, out int charsWritten,
                          ReadOnlySpan<char> format, IFormatProvider? provider);

    public static BigRational Parse(string s, IFormatProvider? provider = null);
    public static BigRational Parse(ReadOnlySpan<char> s, IFormatProvider? provider);
    public static bool TryParse(string? s, IFormatProvider? provider, out BigRational result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigRational result);
}
```

The constructor and `Create` throw `DivideByZeroException` on a zero
denominator, as does `Reciprocal` of zero, `operator /` by zero, and `Pow` of
zero to a negative exponent. `Round` throws `ArgumentOutOfRangeException` on an
undefined mode. `Parse` throws `FormatException`; the `TryParse` pair returns
`false` and yields `default`, which is exact zero rather than an invalid value.

Equality is value equality over the reduced parts, so `==`, `Equals` and
`GetHashCode` agree. `CompareTo` is a total order.

## Pitfalls

- **This is the only member published as a package**, so a breaking change
  costs something nobody here can survey: whether it has external consumers is
  unknowable from inside the repo, and that is precisely what makes the cost
  real. Additive surface is free while it is at preview; removing or resigning a
  member is a deliberate decision, not a cleanup. `Abs`/`Negate`/`Reciprocal`
  going from instance to static was such a decision, taken with no compatibility
  shims on purpose. See `AGENTS.md` § Writing code.
- **Read `Denominator`, never `_denominator`.** The field is zero for a
  defaulted instance; the property is what masks that as one. A new member that
  reads the field will be correct for every value a test constructs explicitly
  and wrong for `default(BigRational)`.
- **`alreadyNormalized: true` is a promise, not a hint.** The private
  constructor performs no checking when it is set. Passing an unreduced pair, or
  one with a negative denominator, mints an invalid value that then propagates —
  and the equality and hashing paths above will disagree about it rather than
  throw. Only set it where the reduction is provable from the inputs.
- **Do not bake a midpoint policy into `Round`,** and do not assume the mode
  only matters at exact halves. Three of the five modes are directed and apply
  to every value; the spec's two rounding sites want different modes, which is
  why the parameter exists.
- **Do not let `Pow` negate its own exponent.** `int.MinValue` is the input that
  breaks the obvious implementation, and it breaks it silently.
- **CA1515 is suppressed for test files and should stay suppressed** even
  though the current tooling no longer fires it on classes holding tests. The
  analyzer's exemption is keyed on test-method attributes, so a public shared
  fixture or `[CollectionDefinition]` marker still trips it — and those types
  must be public for xUnit to use them. Obeying the rule would not turn the
  build red; xUnit would discover less and still report green. The suppression's
  own comment carries the dated measurement behind this.
- **Locked-mode restore is dormant in CI here.** `Directory.Build.props` gates
  `RestoreLockedMode` on `ContinuousIntegrationBuild`, and nothing in this repo
  sets that property — `publish.yml` does not pass it. So the committed
  `packages.lock.json` files are honoured by convention rather than enforced: a
  package added without regenerating them will not fail the workflow the way it
  would in a member whose CI passes the flag. Regenerate them by hand
  (`dotnet restore --force-evaluate`) in the same change as any package edit.
- **`nuget.config` deliberately has no `github` source.** This repo produces
  `HalHeinrich.Numerics.BigRational` and consumes no `HalHeinrich.*` package, so
  the PAT the consuming members need would be a failure mode here with nothing
  behind it. Publishing is unaffected because `publish.yml` names its feed
  inline. Add the source, and a source mapping for the pattern, only if a
  `HalHeinrich.*` reference is ever added.

## Subproject-internal next steps

- **`INumber<T>` and the generic-math interfaces.** Desirable, not urgent, and
  entirely internal to this repo. The unary surface was already made static to
  make it reachable, so the remaining work is the interface set itself —
  `INumberBase<T>`, the operator interfaces, and deciding which members have an
  honest meaning for an exact rational. `Round`'s existing shape should be
  checked against `IFloatingPoint<T>`-style rounding members before either is
  fixed.
- **CA2225's named operator alternates are permanently suppressed, not
  deferred** (`halheinrich/Math#17`). Its ten hits fall into three groups with
  a distinct reason each. All three reasons live in `.editorconfig`, beside the
  `dotnet_diagnostic.CA2225.severity` line, and are deliberately not repeated
  here — the same reasoning kept in two files is what let this entry drift into
  contradicting that one. `halheinrich/Math#34` carries the open generic-math
  interface work; the suppression stands either way and does not wait on it.

Cross-cutting items — the package version ruling, the shared workflow baseline,
and this member's place in the dependency graph — are tracked in
`../INSTRUCTIONS.md` and are deliberately not repeated here.
