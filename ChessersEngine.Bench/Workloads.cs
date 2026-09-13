using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessersEngine.Bench {
    /// <summary>One benchmark, as declared in workloads.vN.json.</summary>
    public sealed class WorkloadSpec {
        public string id;
        public string layer;          // "micro" | "search" | "e2e"
        public string description;
        public string fixture;        // TestScenarios method name; null = standard opening
        public int level = -1;        // AI search level; -1 when not applicable
        public int warmup;
        public int measured;
        public int samples;

        // Absent means true. False marks a workload whose counters depend on the host language's
        // seeded RNG stream, so a difference between two implementations is expected, not a bug.
        public bool? countersComparable;

        /// <summary>Whether a counter difference against another implementation is a porting bug.</summary>
        public bool CountersAreComparable => countersComparable ?? true;
    }

    /// <summary>
    /// The benchmark suite definition. Iteration counts are declared here rather than calibrated
    /// at runtime: adaptive counts would make two language implementations do different amounts
    /// of work, which is exactly what the oracle exists to rule out.
    /// </summary>
    public sealed class WorkloadManifest {
        public int version;
        public List<WorkloadSpec> workloads = new List<WorkloadSpec>();

        public static readonly string[] KnownLayers = { "micro", "search", "e2e" };

        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions {
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        /// <summary>Reads the manifest, also yielding the SHA-256 of the file's exact bytes.</summary>
        public static WorkloadManifest Load (string path, out string sha256) {
            byte[] raw = File.ReadAllBytes(path);
            sha256 = Convert.ToHexStringLower(SHA256.HashData(raw));

            WorkloadManifest manifest = JsonSerializer.Deserialize<WorkloadManifest>(
                Encoding.UTF8.GetString(raw), JsonOpts
            );
            if (manifest == null || manifest.workloads == null) {
                throw new InvalidDataException("workload manifest is empty or malformed");
            }
            return manifest;
        }

        /// <summary>
        /// The implementation key for an id: an id is `&lt;kind&gt;.&lt;variant&gt;`, and only the
        /// kind dispatches. Null when no kind matches.
        /// </summary>
        public static string KindOf (string id, IEnumerable<string> knownKinds) {
            if (id == null) {
                return null;
            }
            foreach (string kind in knownKinds) {
                if (id == kind || id.StartsWith(kind + ".", StringComparison.Ordinal)) {
                    return kind;
                }
            }
            return null;
        }

        /// <summary>
        /// Rejects a manifest the runner could not execute meaningfully, reporting every problem at
        /// once. The known kinds and the fixture check arrive as arguments because this file is
        /// source-included by the test project, which cannot see the bench's engine-typed sources.
        /// </summary>
        public void Validate (IEnumerable<string> knownKinds, Func<string, bool> fixtureResolves) {
            List<string> errors = new List<string>();

            if (workloads.Count == 0) {
                errors.Add("the manifest declares no workloads");
            }

            foreach (WorkloadSpec w in workloads) {
                string id = w.id ?? "<missing id>";

                if (w.id == null) {
                    errors.Add("a workload has no id");
                } else if (KindOf(w.id, knownKinds) == null) {
                    errors.Add($"{id}: no workload implementation for this id");
                }

                if (Array.IndexOf(KnownLayers, w.layer) < 0) {
                    errors.Add($"{id}: layer '{w.layer}' is not one of {string.Join(", ", KnownLayers)}");
                }

                if (w.warmup < 0) {
                    errors.Add($"{id}: warmup must be >= 0, got {w.warmup}");
                }
                if (w.measured < 1) {
                    errors.Add($"{id}: measured must be >= 1, got {w.measured}");
                }
                if (w.samples < 1) {
                    errors.Add($"{id}: samples must be >= 1, got {w.samples}");
                }

                if (w.fixture != null && !fixtureResolves(w.fixture)) {
                    errors.Add($"{id}: unknown fixture '{w.fixture}'");
                }
            }

            if (errors.Count > 0) {
                throw new InvalidDataException(
                    "invalid workload manifest:" + Environment.NewLine + "  - " +
                    string.Join(Environment.NewLine + "  - ", errors)
                );
            }
        }
    }
}
