using System;
using System.Collections.Generic;
using System.Linq;
using ChessersEngine;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class PortableRandomTests {
        // The vectors below are the cross-language contract (docs/portable-random.md).
        // They were produced by an independent implementation, not by this one.

        static readonly int[] BoundedBounds = { 2, 3, 5, 8, 13, 21, 32, 64, 100, 1000 };
        static readonly int[] BoundedFromSeedOne = { 1, 1, 0, 3, 5, 2, 5, 53, 20, 950 };
        static readonly int[] BoundedFromSeed42 = { 5, 5, 0, 2, 6, 4, 2, 6, 6, 5, 5, 6 };
        static readonly int[] ShuffledTenSeedOne = { 4, 2, 8, 1, 9, 3, 0, 6, 7, 5 };
        static readonly int[] ShuffledEightSeedSeven = { 1, 4, 5, 2, 6, 0, 3, 7 };

        [Test]
        public void SeedZeroMatchesTheSplitMix64Reference () {
            var rng = new PortableRandom(0);

            Assert.That(
                Enumerable.Range(0, 4).Select(_ => rng.NextUInt64()).ToArray(),
                Is.EqualTo(new ulong[] {
                    0xE220A8397B1DCDAF, 0x6E789E6AA1B965F4, 0x06C45D188009454F, 0xF88BB8A8724C81EC,
                })
            );
        }

        [Test]
        public void SeedOneRawOutputs () {
            var rng = new PortableRandom(1);

            Assert.That(
                Enumerable.Range(0, 4).Select(_ => rng.NextUInt64()).ToArray(),
                Is.EqualTo(new ulong[] {
                    0x910A2DEC89025CC1, 0xBEEB8DA1658EEC67, 0xF893A2EEFB32555E, 0x71C18690EE42C90B,
                })
            );
        }

        [Test]
        public void NegativeSeedIsItsTwosComplementBitsZeroExtended () {
            var rng = new PortableRandom(-1);

            Assert.That(rng.NextUInt64(), Is.EqualTo(0x73B13BA2AFF181C0));
            Assert.That(rng.NextUInt64(), Is.EqualTo(0x612043051340D3B4));
        }

        [Test]
        public void BoundedDrawsFromSeedOne () {
            var rng = new PortableRandom(1);
            Assert.That(
                BoundedBounds.Select(n => rng.Next(n)).ToArray(),
                Is.EqualTo(BoundedFromSeedOne)
            );
        }

        [Test]
        public void BoundedDrawsFromSeed42 () {
            var rng = new PortableRandom(42);

            Assert.That(
                Enumerable.Range(0, 12).Select(_ => rng.Next(7)).ToArray(),
                Is.EqualTo(BoundedFromSeed42)
            );
        }

        [Test]
        public void ShuffleVectors () {
            List<int> ten = Enumerable.Range(0, 10).ToList();
            Helpers.Shuffle(new PortableRandom(1), ten);
            Assert.That(ten, Is.EqualTo(ShuffledTenSeedOne));

            List<int> eight = Enumerable.Range(0, 8).ToList();
            Helpers.Shuffle(new PortableRandom(7), eight);
            Assert.That(eight, Is.EqualTo(ShuffledEightSeedSeven));
        }

        [Test]
        public void SameSeedSameSequence () {
            var a = new PortableRandom(123);
            var b = new PortableRandom(123);

            for (int i = 0; i < 100; i++) {
                Assert.That(a.Next(1000), Is.EqualTo(b.Next(1000)));
            }
        }

        [Test]
        public void BoundOfOneAlwaysDrawsZero () {
            var rng = new PortableRandom(5);

            for (int i = 0; i < 20; i++) {
                Assert.That(rng.Next(1), Is.Zero);
            }
        }

        [Test]
        public void NonPositiveBoundThrows () {
            var rng = new PortableRandom(5);

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(-3));
        }
    }
}
