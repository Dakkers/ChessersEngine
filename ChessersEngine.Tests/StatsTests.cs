using ChessersEngine.Bench;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class StatsTests {
        [Test]
        public void PercentileInterpolatesBetweenRanks () {
            // rank = p/100 * (n-1) = 1.5 -> halfway between 2 and 3.
            Assert.That(Stats.Percentile(new double[] { 1, 2, 3, 4 }, 50), Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void PercentileHandlesP90 () {
            // rank = 0.9 * 4 = 3.6 -> 4 + 0.6 * (5 - 4).
            Assert.That(Stats.Percentile(new double[] { 1, 2, 3, 4, 5 }, 90), Is.EqualTo(4.6).Within(1e-9));
        }

        [Test]
        public void PercentileOfSingleSampleIsThatSample () {
            Assert.That(Stats.Percentile(new double[] { 7 }, 90), Is.EqualTo(7).Within(1e-9));
        }

        [Test]
        public void PercentileDoesNotMutateInput () {
            double[] xs = new double[] { 3, 1, 2 };
            Stats.Percentile(xs, 50);
            Assert.That(xs, Is.EqualTo(new double[] { 3, 1, 2 }));
        }

        [Test]
        public void StdDevIsTheSampleStandardDeviation () {
            // mean 5, sum of squared deviations 32, /(8-1) = 4.571428..., sqrt = 2.138089...
            double[] xs = new double[] { 2, 4, 4, 4, 5, 5, 7, 9 };
            Assert.That(Stats.StdDev(xs), Is.EqualTo(2.13808993529939).Within(1e-9));
        }

        [Test]
        public void StdDevOfSingleSampleIsZero () {
            Assert.That(Stats.StdDev(new double[] { 5 }), Is.Zero);
        }

        [Test]
        public void MinMaxMeanMedian () {
            double[] xs = new double[] { 4, 1, 3, 2 };
            Assert.Multiple(() => {
                Assert.That(Stats.Min(xs), Is.EqualTo(1).Within(1e-9));
                Assert.That(Stats.Max(xs), Is.EqualTo(4).Within(1e-9));
                Assert.That(Stats.Mean(xs), Is.EqualTo(2.5).Within(1e-9));
                Assert.That(Stats.Median(xs), Is.EqualTo(2.5).Within(1e-9));
            });
        }
    }
}
