# Portable Random

The move-search RNG (`PortableRandom`, used by `Match` for the AI's piece and tile
shuffles) is specified exactly so that any implementation reproduces every draw from
the seed alone. It replaces `System.Random`, whose sequence is a .NET implementation
detail a port would otherwise have to clone.

This is part of the cross-language contract, alongside the oracle corpus and the
counter contract in `ChessersEngine.Bench/README.md`.

## Algorithm

State is one unsigned 64-bit integer, all arithmetic wraps modulo 2^64.

**Seeding.** The `int` seed is reinterpreted as its 32-bit two's-complement bit pattern,
then zero-extended: `state = (uint) seed` (Rust: `seed as u32 as u64`).

**`NextUInt64`** is SplitMix64:

```
state += 0x9E3779B97F4A7C15
z = state
z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9
z = (z ^ (z >> 27)) * 0x94D049BB133111EB
return z ^ (z >> 31)
```

**`Next(n)`** returns a uniform integer in `[0, n)`, `n > 0`, by rejection sampling over
integers only (no floating point):

```
bound = n as u64
threshold = (2^64 - bound) % bound   // (0 - bound) % bound, wrapping
loop:
    draw = NextUInt64()
    if draw >= threshold: return draw % bound
```

**`Shuffle(list)`** is Fisher-Yates from the back, one `Next` per step:

```
n = list.length
while n > 1:
    k = Next(n); n -= 1
    swap(list[n], list[k])
```

Unseeded matches pick their seed from system entropy; only seeded matches are
reproducible.

## Test vectors

Produced by an independent implementation, not by the C# code under test. The seed-0
raw outputs match the published SplitMix64 reference. `PortableRandomTests` asserts
all of these; a port MUST assert the same table.

Raw `NextUInt64` outputs:

| seed | outputs                                                                      |
| ---- | ---------------------------------------------------------------------------- |
| 0    | `E220A8397B1DCDAF 6E789E6AA1B965F4 06C45D188009454F F88BB8A8724C81EC`         |
| 1    | `910A2DEC89025CC1 BEEB8DA1658EEC67 F893A2EEFB32555E 71C18690EE42C90B`         |
| -1   | `73B13BA2AFF181C0 612043051340D3B4`                                           |

Bounded draws:

| seed | calls                                                   | results                  |
| ---- | ------------------------------------------------------- | ------------------------ |
| 1    | `Next(n)` for n = 2, 3, 5, 8, 13, 21, 32, 64, 100, 1000 | 1 1 0 3 5 2 5 53 20 950  |
| 42   | `Next(7)` x 12                                          | 5 5 0 2 6 4 2 6 6 5 5 6  |

Shuffles (input is `0..n-1` in order):

| seed | n  | result               |
| ---- | -- | -------------------- |
| 1    | 10 | 4 2 8 1 9 3 0 6 7 5  |
| 7    | 8  | 1 4 5 2 6 0 3 7      |
