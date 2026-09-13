using System;
using System.Diagnostics;

namespace ChessersEngine.Bench {
    /// <summary>
    /// Measures one workload. The method is part of the cross-language contract:
    /// `warmup` untimed iterations, then `samples` batches of `measured` timed iterations, with a
    /// forced collection between batches (a .NET-local step, outside the timed region). Counters
    /// come from a separate single-iteration pass so a counter value is per-op, not per-batch.
    /// </summary>
    static class Runner {
        public static WorkloadResult Run (WorkloadSpec spec, bool smoke) {
            int warmup = smoke ? 0 : spec.warmup;
            int measured = smoke ? 1 : spec.measured;
            int samples = smoke ? 1 : spec.samples;

            Action body = WorkloadRegistry.Build(spec);

            for (int i = 0; i < warmup; i++) {
                body();
            }

            double[] nsPerOp = new double[samples];
            for (int s = 0; s < samples; s++) {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < measured; i++) {
                    body();
                }
                sw.Stop();

                nsPerOp[s] = sw.Elapsed.TotalNanoseconds / measured;
            }

            EngineCounters.Reset();
            body();
            CounterSnapshot counters = EngineCounters.Take();

            double min = Stats.Min(nsPerOp);
            return new WorkloadResult {
                id = spec.id,
                layer = spec.layer,
                description = spec.description,
                iterations = new IterationsDto { warmup = warmup, measured = measured, samples = samples },
                time = new TimeDto {
                    minNsPerOp = min,
                    medianNsPerOp = Stats.Median(nsPerOp),
                    p90NsPerOp = Stats.Percentile(nsPerOp, 90),
                    maxNsPerOp = Stats.Max(nsPerOp),
                    meanNsPerOp = Stats.Mean(nsPerOp),
                    stddevNsPerOp = Stats.StdDev(nsPerOp),
                    opsPerSecond = min > 0 ? 1e9 / min : 0,
                },
                countersComparable = spec.CountersAreComparable,
                counters = new CountersDto {
                    nodes = counters.nodes,
                    moveGen = counters.moveGen,
                    tilesProduced = counters.tilesProduced,
                    boardClones = counters.boardClones,
                    copyStates = counters.copyStates,
                    moveApplies = counters.moveApplies,
                    moveUndos = counters.moveUndos,
                    evals = counters.evals,
                },
            };
        }
    }
}
