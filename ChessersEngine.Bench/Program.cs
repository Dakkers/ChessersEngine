using System;
using System.IO;
using System.Text.Json;

namespace ChessersEngine.Bench {
    static class Program {
        const string Usage = @"chessers-bench - performance oracle for ChessersEngine

Usage: chessers-bench [options]

  --out <dir>        output directory for perf.v1.json (default: perf)
  --manifest <path>  workload manifest (default: workloads.v1.json beside the binary)
  --filter <text>    only run workloads whose id contains <text>
  --list             print the workload ids and exit
  --smoke            run every workload once, with no warmup (a fast wiring check)
  --record-replay <path>
                     regenerate the replay.game move-sequence fixture and exit
  --help             show this message
";

        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = true };

        static int Main (string[] args) {
            string outDir = "perf";
            string manifestPath = Path.Combine(AppContext.BaseDirectory, "workloads.v1.json");
            string filter = null;
            bool list = false;
            bool smoke = false;
            string recordReplayPath = null;

            try {
                for (int i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--out": outDir = Next(args, ref i); break;
                        case "--manifest": manifestPath = Next(args, ref i); break;
                        case "--filter": filter = Next(args, ref i); break;
                        case "--list": list = true; break;
                        case "--smoke": smoke = true; break;
                        case "--record-replay": recordReplayPath = Next(args, ref i); break;
                        case "--help": Console.Write(Usage); return 0;
                        default:
                            Console.Error.WriteLine($"unknown option '{args[i]}'\n\n{Usage}");
                            return 2;
                    }
                }
            } catch (ArgumentException e) {
                Console.Error.WriteLine(e.Message);
                return 2;
            }

            if (recordReplayPath != null) {
                try {
                    WorkloadRegistry.WriteReplaySequence(recordReplayPath);
                } catch (Exception e) {
                    Console.Error.WriteLine(e.Message);
                    return 2;
                }
                Console.Error.WriteLine($"wrote {recordReplayPath}");
                return 0;
            }

            WorkloadManifest manifest;
            string manifestSha;
            try {
                manifest = WorkloadManifest.Load(manifestPath, out manifestSha);
                manifest.Validate(WorkloadRegistry.KnownIds(), WorkloadRegistry.FixtureResolves);
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                return 2;
            }

            if (list) {
                foreach (WorkloadSpec w in manifest.workloads) {
                    Console.WriteLine($"{w.layer,-6} {w.id}");
                }
                return 0;
            }

            if (!VerifyCounters()) {
                return 2;
            }

            PerfReport report = new PerfReport {
                environment = EnvironmentInfo.Capture(),
                manifest = new ManifestDto { version = manifest.version, sha256 = manifestSha },
            };

            foreach (WorkloadSpec spec in manifest.workloads) {
                if (filter != null && !spec.id.Contains(filter, StringComparison.Ordinal)) {
                    continue;
                }
                Console.Error.WriteLine($"running {spec.id}...");
                report.results.Add(Runner.Run(spec, smoke));
            }

            if (report.results.Count == 0) {
                Console.Error.WriteLine("no workloads matched the filter");
                return 2;
            }

            string outPath = Path.Combine(outDir, "perf.v1.json");
            try {
                Directory.CreateDirectory(outDir);
                File.WriteAllText(outPath, JsonSerializer.Serialize(report, JsonOpts));
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                return 2;
            }
            Console.Error.WriteLine($"wrote {outPath} ({report.results.Count} workloads)");
            return 0;
        }

        static string Next (string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                throw new ArgumentException($"{args[i]} needs a value");
            }
            return args[++i];
        }

        /// <summary>
        /// Refuses to emit a report whose counters are meaningless: either the build lacks BENCH,
        /// or a call site was lost in a refactor. Both would otherwise produce a plausible-looking
        /// file full of zeros. A level-0 search from the standard opening exercises all eight.
        /// </summary>
        static bool VerifyCounters () {
            if (!EngineCounters.Enabled) {
                Console.Error.WriteLine("chessers-bench was built without BENCH defined; counters would all be zero");
                return false;
            }

            EngineCounters.Reset();
            new Match(null, null, 1).CalculateBestMove(0);
            CounterSnapshot probe = EngineCounters.Take();

            (string, long)[] counters = {
                ("nodes", probe.nodes),
                ("moveGen", probe.moveGen),
                ("tilesProduced", probe.tilesProduced),
                ("boardClones", probe.boardClones),
                ("copyStates", probe.copyStates),
                ("moveApplies", probe.moveApplies),
                ("moveUndos", probe.moveUndos),
                ("evals", probe.evals),
            };

            foreach ((string name, long value) in counters) {
                if (value == 0) {
                    Console.Error.WriteLine(
                        $"counter call site is not wired: a level-0 search left '{name}' at zero"
                    );
                    return false;
                }
            }

            return true;
        }
    }
}
