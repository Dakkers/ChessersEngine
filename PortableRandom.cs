using System;

namespace ChessersEngine {
    /// <summary>
    /// The move-search RNG. A SplitMix64 generator with an unbiased bounded draw, specified
    /// exactly so that another language can reproduce every value from the seed alone.
    /// The algorithm and its test vectors are part of the cross-language contract; see
    /// docs/portable-random.md.
    /// </summary>
    public sealed class PortableRandom {
        ulong state;

        public PortableRandom (int seed) {
            state = (uint) seed;
        }

        /// <summary>The next 64 random bits.</summary>
        public ulong NextUInt64 () {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>A uniform integer in [0, maxExclusive), drawn by rejection sampling.</summary>
        public int Next (int maxExclusive) {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);

            ulong bound = (ulong) maxExclusive;
            ulong threshold = unchecked(0UL - bound) % bound;
            while (true) {
                ulong draw = NextUInt64();
                if (draw >= threshold) {
                    return (int) (draw % bound);
                }
            }
        }
    }
}
