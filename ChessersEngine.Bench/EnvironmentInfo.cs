using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ChessersEngine.Bench {
    /// <summary>
    /// The machine and build a run happened on. Times from two different `environment` blocks are
    /// not comparable, which is what compare-perf.ts checks. Deliberately records no hostname or
    /// username.
    /// </summary>
    static class EnvironmentInfo {
        public static EnvironmentDto Capture () => new EnvironmentDto {
            os = OsName(),
            arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            cpuModel = CpuModel(),
            logicalCores = Environment.ProcessorCount,
            runtime = RuntimeInformation.FrameworkDescription,
            buildConfig =
#if DEBUG
                "Debug",
#else
                "Release",
#endif
            gitSha = GitSha(),
            jitMode = JitMode(),
            countersEnabled = EngineCounters.Enabled,
        };

        /// <summary>
        /// How the code under measurement was compiled. Tiered compilation promotes methods on a
        /// background, elapsed-time-gated delay, which makes a workload's timing depend on where it
        /// sits in the run; the bench disables it, and records that here so a Rust run can be held
        /// to the same standard.
        /// </summary>
        static string JitMode () {
            bool known = AppContext.TryGetSwitch("System.Runtime.TieredCompilation", out bool tiered);
            return (known && !tiered) ? "tiered-disabled" : "tiered";
        }

        static string OsName () {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "darwin";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
            return "unknown";
        }

        static string CpuModel () {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    return RunCommand("sysctl", "-n machdep.cpu.brand_string") ?? "unknown";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    foreach (string line in File.ReadLines("/proc/cpuinfo")) {
                        if (line.StartsWith("model name", StringComparison.Ordinal)) {
                            return line.Substring(line.IndexOf(':') + 1).Trim();
                        }
                    }
                    return "unknown";
                }
                return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown";
            } catch (Exception) {
                return "unknown";
            }
        }

        static string GitSha () => RunCommand("git", "rev-parse --short HEAD") ?? "unknown";

        /// <summary>Runs a command and returns its trimmed stdout, or null if it cannot be run.</summary>
        static string RunCommand (string file, string args) {
            try {
                ProcessStartInfo psi = new ProcessStartInfo(file, args) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using (Process p = Process.Start(psi)) {
                    Task<string> stdoutTask = p.StandardOutput.ReadToEndAsync();
                    Task<string> stderrTask = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(5000)) {
                        try { p.Kill(); } catch (Exception) { }
                        return null;
                    }
                    string stdout = stdoutTask.GetAwaiter().GetResult().Trim();
                    return (p.ExitCode == 0 && stdout.Length > 0) ? stdout : null;
                }
            } catch (Exception) {
                return null;
            }
        }
    }
}
