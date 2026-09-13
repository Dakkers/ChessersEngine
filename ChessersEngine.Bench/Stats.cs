using System;

namespace ChessersEngine.Bench {
    /// <summary>
    /// Summary statistics over per-sample timings. The percentile definition is part of the
    /// cross-language contract: linear interpolation between the two closest ranks, where
    /// rank = p/100 * (n-1) over the ascending-sorted samples.
    /// </summary>
    public static class Stats {
        public static double Min (double[] xs) {
            double result = xs[0];
            for (int i = 1; i < xs.Length; i++) {
                if (xs[i] < result) result = xs[i];
            }
            return result;
        }

        public static double Max (double[] xs) {
            double result = xs[0];
            for (int i = 1; i < xs.Length; i++) {
                if (xs[i] > result) result = xs[i];
            }
            return result;
        }

        public static double Mean (double[] xs) {
            double sum = 0;
            for (int i = 0; i < xs.Length; i++) {
                sum += xs[i];
            }
            return sum / xs.Length;
        }

        public static double Percentile (double[] xs, double p) {
            double[] sorted = (double[]) xs.Clone();
            Array.Sort(sorted);

            if (sorted.Length == 1) {
                return sorted[0];
            }

            double rank = (p / 100.0) * (sorted.Length - 1);
            int lower = (int) Math.Floor(rank);
            int upper = (int) Math.Ceiling(rank);
            return sorted[lower] + (rank - lower) * (sorted[upper] - sorted[lower]);
        }

        public static double Median (double[] xs) => Percentile(xs, 50);

        /// <summary>Sample standard deviation (n-1 denominator); zero for a single sample.</summary>
        public static double StdDev (double[] xs) {
            if (xs.Length < 2) {
                return 0;
            }

            double mean = Mean(xs);
            double sumSquares = 0;
            for (int i = 0; i < xs.Length; i++) {
                double d = xs[i] - mean;
                sumSquares += d * d;
            }
            return Math.Sqrt(sumSquares / (xs.Length - 1));
        }
    }
}
