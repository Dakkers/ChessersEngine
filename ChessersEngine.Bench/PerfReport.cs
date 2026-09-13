using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChessersEngine.Bench {
    // The emitted perf.v1.json. The format version lives in the `$schema` URI, mirrored by the
    // machine-readable `schemaVersion`; ChessersEngine.Cli/schemas/check-perf.ts enforces that the
    // two agree. Field names are the wire contract -- keep them lowerCamelCase and unabbreviated.

    class PerfReport {
        [JsonPropertyName("$schema")]
        public string Schema { get; set; } =
            "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/perf.v1.schema.json";

        public int schemaVersion { get; set; } = 1;
        public string engine { get; set; } = "ChessersEngine (C#)";
        public EnvironmentDto environment { get; set; }
        public ManifestDto manifest { get; set; }
        public List<WorkloadResult> results { get; set; } = new List<WorkloadResult>();
    }

    class EnvironmentDto {
        public string os { get; set; }          // "darwin" | "linux" | "windows" | "unknown"
        public string arch { get; set; }        // "arm64" | "x64" | ...
        public string cpuModel { get; set; }
        public int logicalCores { get; set; }
        public string runtime { get; set; }
        public string buildConfig { get; set; } // "Debug" | "Release"
        public string gitSha { get; set; }
        public string jitMode { get; set; }     // "tiered-disabled" | "tiered" | "aot" | "n/a"
        public bool countersEnabled { get; set; }
    }

    class ManifestDto {
        public int version { get; set; }
        public string sha256 { get; set; }
    }

    class WorkloadResult {
        public string id { get; set; }
        public string layer { get; set; }
        public string description { get; set; }
        public IterationsDto iterations { get; set; }
        public TimeDto time { get; set; }
        public bool countersComparable { get; set; }
        public CountersDto counters { get; set; }
    }

    class IterationsDto {
        public int warmup { get; set; }
        public int measured { get; set; }
        public int samples { get; set; }
    }

    class TimeDto {
        public double minNsPerOp { get; set; }
        public double medianNsPerOp { get; set; }
        public double p90NsPerOp { get; set; }
        public double maxNsPerOp { get; set; }
        public double meanNsPerOp { get; set; }
        public double stddevNsPerOp { get; set; }
        public double opsPerSecond { get; set; }
    }

    class CountersDto {
        public long nodes { get; set; }
        public long moveGen { get; set; }
        public long tilesProduced { get; set; }
        public long boardClones { get; set; }
        public long copyStates { get; set; }
        public long moveApplies { get; set; }
        public long moveUndos { get; set; }
        public long evals { get; set; }
    }
}
