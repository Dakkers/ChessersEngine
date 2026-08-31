# Performance Oracle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a benchmark harness that measures the C# ChessersEngine and emits a versioned `perf.v1.json` baseline, plus a comparator, so the future Rust port can be measured identically and compared.

**Architecture:** A new `ChessersEngine.Bench` console project source-compiles the root engine with `BENCH` defined, activating `[Conditional("BENCH")]` counter call sites that are erased in every other build. A checked-in `workloads.v1.json` manifest (read by both languages) fixes the workload list and iteration counts. The runner times each workload, takes counters in a separate pass, and writes `perf.v1.json`, validated by a TypeScript+Zod checker alongside the existing oracle/codec checkers. `compare-perf.ts` joins two result files.

**Tech Stack:** C# / .NET 10 (`net10.0`), NUnit 4 for tests, System.Text.Json for emission, TypeScript 6 + Zod 4 + commander (run directly by Node 26, no bundler) for the schema check and comparator, pnpm.

**Spec:** `docs/superpowers/specs/2026-08-29-performance-oracle-design.md`

## Global Constraints

- Target framework is `net10.0`. There is no system-wide dotnet; the SDK lives in `~/.dotnet`. Every command below assumes `export PATH="$HOME/.dotnet:$PATH"` has been run first.
- Engine C# style: 4-space indent, a space before the parameter list in declarations (`public void Foo (int x)`), Allman-ish braces on the same line as the declaration. Match surrounding code exactly.
- Comments only where the code cannot speak for itself. No narration comments. Docstrings say WHAT a member does for its caller, never HOW.
- Commits: imperative subject line. A body only for a fact the diff cannot show. Every commit ends with the trailer `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- Schema URI base is exactly `https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas`.
- Released schema versions are immutable. This plan only ever adds `perf.v1.*`.
- The `environment` block MUST NOT record hostname or username.
- Node version is pinned by `ChessersEngine.Cli/schemas/.nvmrc` (26.1.0); pnpm is `11.1.2`; `save-exact=true` is set, so add dependencies with exact versions.
- TypeScript files are run directly by Node (`node check-perf.ts`) — no build step. Use `.ts` extensions in imports, matching `check-oracle.ts`.

---

### Task 1: Engine counters

**Files:**
- Create: `EngineCounters.cs`
- Modify: `Board.cs:67` (`CopyState`), `Board.cs:113` (`CreateCopy`), `Board.cs:1350` (`GetPotentialTilesForMovement`), `Board.cs:1433` (`UndoMove`), `Board.cs:1516` (`CalculateBoardValue`), `Match.cs:361` (`MinimaxHelper`), `Move.cs:785` (`GetPseudoLegalMoveResult`)
- Test: `ChessersEngine.Tests/EngineCountersTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `ChessersEngine.EngineCounters` with `static bool Enabled { get; }`, `static void Reset ()`, `static CounterSnapshot Take ()`, and the `[Conditional("BENCH")]` void methods `Node()`, `MoveGen(int tilesProduced)`, `BoardClone()`, `CopyState()`, `MoveApply()`, `MoveUndo()`, `Eval()`. `ChessersEngine.CounterSnapshot` is a struct with public `long` fields `nodes, moveGen, tilesProduced, boardClones, copyStates, moveApplies, moveUndos, evals`.

- [ ] **Step 1: Write the failing test**

Create `ChessersEngine.Tests/EngineCountersTests.cs`:

```csharp
using ChessersEngine;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class EngineCountersTests {
        [SetUp]
        public void SetUp () => EngineCounters.Reset();

        [Test]
        public void DisabledInTheTestAssembly () {
            // The test project compiles the engine without BENCH, so every call site is erased.
            Assert.That(EngineCounters.Enabled, Is.False);
        }

        [Test]
        public void TakeReturnsZerosWhenDisabled () {
            Board board = new Match(null)._GetCommittedBoard();
            board.CreateCopy();
            board.CalculateBoardValue(0);

            CounterSnapshot snapshot = EngineCounters.Take();

            Assert.Multiple(() => {
                Assert.That(snapshot.nodes, Is.Zero);
                Assert.That(snapshot.moveGen, Is.Zero);
                Assert.That(snapshot.tilesProduced, Is.Zero);
                Assert.That(snapshot.boardClones, Is.Zero);
                Assert.That(snapshot.copyStates, Is.Zero);
                Assert.That(snapshot.moveApplies, Is.Zero);
                Assert.That(snapshot.moveUndos, Is.Zero);
                Assert.That(snapshot.evals, Is.Zero);
            });
        }

        [Test]
        public void ResetClearsEverySnapshotField () {
            EngineCounters.Reset();

            CounterSnapshot snapshot = EngineCounters.Take();

            Assert.That(
                snapshot.nodes + snapshot.moveGen + snapshot.tilesProduced + snapshot.boardClones +
                snapshot.copyStates + snapshot.moveApplies + snapshot.moveUndos + snapshot.evals,
                Is.Zero
            );
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet test ChessersEngine.sln --filter EngineCountersTests
```

Expected: FAIL — compile error, `EngineCounters` and `CounterSnapshot` do not exist.

- [ ] **Step 3: Create `EngineCounters.cs`**

```csharp
using System.Diagnostics;

namespace ChessersEngine {
    /// <summary>A tally of engine work performed since the last <see cref="EngineCounters.Reset"/>.</summary>
    public struct CounterSnapshot {
        public long nodes;
        public long moveGen;
        public long tilesProduced;
        public long boardClones;
        public long copyStates;
        public long moveApplies;
        public long moveUndos;
        public long evals;
    }

    /// <summary>
    /// Hardware-independent tallies of engine work, for the performance oracle. Every increment
    /// method is <c>[Conditional("BENCH")]</c>, so the compiler erases the calls -- argument
    /// evaluation included -- in any build without that symbol. Only ChessersEngine.Bench defines it.
    ///
    /// The increment points are a cross-language contract; see ChessersEngine.Bench/README.md.
    /// Single-threaded by design: the harness never runs workloads concurrently.
    /// </summary>
    public static class EngineCounters {
        static long _nodes;
        static long _moveGen;
        static long _tilesProduced;
        static long _boardClones;
        static long _copyStates;
        static long _moveApplies;
        static long _moveUndos;
        static long _evals;

        /// <summary>Whether counting was compiled into this build.</summary>
        public static bool Enabled =>
#if BENCH
            true;
#else
            false;
#endif

        [Conditional("BENCH")]
        public static void Node () => _nodes++;

        [Conditional("BENCH")]
        public static void MoveGen (int tilesProduced) {
            _moveGen++;
            _tilesProduced += tilesProduced;
        }

        [Conditional("BENCH")]
        public static void BoardClone () => _boardClones++;

        [Conditional("BENCH")]
        public static void CopyState () => _copyStates++;

        [Conditional("BENCH")]
        public static void MoveApply () => _moveApplies++;

        [Conditional("BENCH")]
        public static void MoveUndo () => _moveUndos++;

        [Conditional("BENCH")]
        public static void Eval () => _evals++;

        public static void Reset () {
            _nodes = 0;
            _moveGen = 0;
            _tilesProduced = 0;
            _boardClones = 0;
            _copyStates = 0;
            _moveApplies = 0;
            _moveUndos = 0;
            _evals = 0;
        }

        public static CounterSnapshot Take () => new CounterSnapshot {
            nodes = _nodes,
            moveGen = _moveGen,
            tilesProduced = _tilesProduced,
            boardClones = _boardClones,
            copyStates = _copyStates,
            moveApplies = _moveApplies,
            moveUndos = _moveUndos,
            evals = _evals,
        };
    }
}
```

- [ ] **Step 4: Add the call sites**

Each is a single added line. Do not reformat surrounding code.

`Board.cs`, first statement of `CopyState (Board otherBoard)`:

```csharp
        public void CopyState (Board otherBoard) {
            EngineCounters.CopyState();
            this.matchConfig = otherBoard.matchConfig;
```

`Board.cs`, first statement of `CreateCopy`:

```csharp
        public Board CreateCopy (List<ChessmanSchema> _pieces = null) {
            EngineCounters.BoardClone();
            Board otherBoard = new Board(_pieces ?? this.GetChessmanSchemas(), this.matchConfig);
```

`Board.cs`, both returns of `GetPotentialTilesForMovement`. The early return currently reads `return new List<Tile>();` — it must also count, or `moveGen` under-reports:

```csharp
            if (potentialTilesGetter == null) {
                EngineCounters.MoveGen(0);
                return new List<Tile>();
            }

            var result = potentialTilesGetter(chessman);
            if (!chessman.IsChecker()) {
                result = result.Where((Tile t) => !t.IsDeathjumpTile()).ToList();
            }

            EngineCounters.MoveGen(result.Count);
            return result;
```

(The guard's exact condition is whatever is already there — add only the counting line above the existing early `return new List<Tile>();`.)

`Board.cs`, first statement of `UndoMove (MoveResult moveResult)`:

```csharp
        public void UndoMove (MoveResult moveResult) {
            EngineCounters.MoveUndo();
            Chessman movedChessman = GetChessman(moveResult.pieceId);
```

`Board.cs`, first statement of `CalculateBoardValue (int numMoves)`:

```csharp
        public int CalculateBoardValue (
            int numMoves
        ) {
            EngineCounters.Eval();
            Chessman whiteKing = GetKingOfColorIfExists(ColorEnum.WHITE);
```

`Move.cs`, first statement of `GetPseudoLegalMoveResult ()`:

```csharp
        public MoveResult GetPseudoLegalMoveResult () {
            EngineCounters.MoveApply();
```

`Match.cs`, first statement of the `MinimaxHelper` body (before `bool isMultipleMoves = ...`):

```csharp
        ) {
            EngineCounters.Node();
            bool isMultipleMoves = (movingChessman != null);
```

- [ ] **Step 5: Run the full test suite**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet test ChessersEngine.sln
```

Expected: PASS, including all pre-existing engine tests. If any pre-existing test now fails, a counting line was inserted in the wrong place — revert and re-place it.

- [ ] **Step 6: Commit**

```bash
git add EngineCounters.cs Board.cs Match.cs Move.cs ChessersEngine.Tests/EngineCountersTests.cs && git commit -m "Add BENCH-gated engine work counters

The increment points are the cross-language contract the Rust port mirrors.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: Bench project scaffold and statistics

**Files:**
- Create: `ChessersEngine.Bench/ChessersEngine.Bench.csproj`, `ChessersEngine.Bench/Stats.cs`, `ChessersEngine.Bench/Program.cs`
- Modify: `ChessersEngine.csproj` (`DefaultItemExcludes`), `ChessersEngine.sln`, `ChessersEngine.Tests/ChessersEngine.Tests.csproj`
- Test: `ChessersEngine.Tests/StatsTests.cs`

**Interfaces:**
- Consumes: `ChessersEngine.EngineCounters.Enabled` (Task 1).
- Produces: `ChessersEngine.Bench.Stats` with `static double Min (double[] xs)`, `Max`, `Mean`, `StdDev`, `Median`, and `static double Percentile (double[] xs, double p)`. A `chessers-bench` executable.

- [ ] **Step 1: Write the failing test**

Create `ChessersEngine.Tests/StatsTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet test ChessersEngine.sln --filter StatsTests
```

Expected: FAIL — `ChessersEngine.Bench` namespace does not exist.

- [ ] **Step 3: Create the bench project**

`ChessersEngine.Bench/ChessersEngine.Bench.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <RootNamespace>ChessersEngine.Bench</RootNamespace>
    <AssemblyName>chessers-bench</AssemblyName>
    <InvariantGlobalization>true</InvariantGlobalization>
    <!-- The engine sources are compiled INTO this project rather than referenced, because
         DefineConstants does not flow across a ProjectReference: a referenced engine would
         link without BENCH and silently report all-zero counters. -->
    <DefineConstants>$(DefineConstants);BENCH</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\*.cs" />
  </ItemGroup>

</Project>
```

`ChessersEngine.Bench/Stats.cs`:

```csharp
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
```

`ChessersEngine.Bench/Program.cs` (a stub that Task 7 replaces):

```csharp
using System;

namespace ChessersEngine.Bench {
    static class Program {
        static int Main (string[] args) {
            Console.WriteLine($"chessers-bench (counters enabled: {EngineCounters.Enabled})");
            return 0;
        }
    }
}
```

- [ ] **Step 4: Wire the project into the engine's exclusions, the solution, and the tests**

In `ChessersEngine.csproj`, extend the existing exclusion so the engine project does not glob the new directory:

```xml
    <DefaultItemExcludes>$(DefaultItemExcludes);ChessersEngine.Tests/**;ChessersEngine.Cli/**;ChessersEngine.Bench/**</DefaultItemExcludes>
```

Add the project to the solution:

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet sln ChessersEngine.sln add ChessersEngine.Bench/ChessersEngine.Bench.csproj
```

In `ChessersEngine.Tests/ChessersEngine.Tests.csproj`, add a new `ItemGroup`. The tests source-include only the engine-independent bench files; a `ProjectReference` to the bench project would drag in a second copy of every engine type and fail to compile.

```xml
  <ItemGroup>
    <!-- Engine-independent bench sources, compiled in directly: a ProjectReference to
         ChessersEngine.Bench would duplicate every engine type. -->
    <Compile Include="..\ChessersEngine.Bench\Stats.cs" />
  </ItemGroup>
```

- [ ] **Step 5: Run the tests and the bench**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet test ChessersEngine.sln --filter StatsTests
```

Expected: PASS (7 tests).

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet run --project ChessersEngine.Bench
```

Expected: `chessers-bench (counters enabled: True)`. If it prints `False`, `DefineConstants` is not reaching the compile — check the csproj.

- [ ] **Step 6: Commit**

```bash
git add ChessersEngine.Bench ChessersEngine.csproj ChessersEngine.sln ChessersEngine.Tests && git commit -m "Add ChessersEngine.Bench project with timing statistics

Compiles the engine sources in with BENCH defined; DefineConstants does not
propagate across a ProjectReference.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: Workload manifest

**Files:**
- Create: `ChessersEngine.Bench/Workloads.cs`, `ChessersEngine.Bench/workloads.v1.json`
- Modify: `ChessersEngine.Bench/ChessersEngine.Bench.csproj`, `ChessersEngine.Tests/ChessersEngine.Tests.csproj`
- Test: `ChessersEngine.Tests/WorkloadManifestTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `ChessersEngine.Bench.WorkloadSpec` (public fields `string id, layer, description, fixture; int level; int warmup, measured, samples`) and `ChessersEngine.Bench.WorkloadManifest` (public fields `int version; List<WorkloadSpec> workloads`) with `static WorkloadManifest Load (string path, out string sha256)` and `static WorkloadManifest Parse (string json, byte[] rawBytes, out string sha256)`. `level` is `-1` when not applicable.

- [ ] **Step 1: Write the failing test**

Create `ChessersEngine.Tests/WorkloadManifestTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChessersEngine;
using ChessersEngine.Bench;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class WorkloadManifestTests {
        static readonly string[] KnownLayers = { "micro", "search", "e2e" };

        static WorkloadManifest Manifest (out string sha256) =>
            WorkloadManifest.Load(
                Path.Combine(TestContext.CurrentContext.TestDirectory, "workloads.v1.json"),
                out sha256
            );

        [Test]
        public void LoadsAndReportsVersionOne () {
            WorkloadManifest manifest = Manifest(out string _);

            Assert.That(manifest.version, Is.EqualTo(1));
            Assert.That(manifest.workloads, Is.Not.Empty);
        }

        [Test]
        public void ShaIsStableLowercaseHex () {
            Manifest(out string first);
            Manifest(out string second);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Has.Length.EqualTo(64));
            Assert.That(first, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void WorkloadIdsAreUnique () {
            WorkloadManifest manifest = Manifest(out string _);

            List<string> duplicates = manifest.workloads
                .GroupBy(w => w.id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.That(duplicates, Is.Empty, "duplicate workload ids: " + string.Join(", ", duplicates));
        }

        [Test]
        public void EveryLayerIsKnown () {
            WorkloadManifest manifest = Manifest(out string _);

            foreach (WorkloadSpec w in manifest.workloads) {
                Assert.That(KnownLayers, Does.Contain(w.layer), $"{w.id} has unknown layer '{w.layer}'");
            }
        }

        [Test]
        public void EveryIterationCountIsPositive () {
            WorkloadManifest manifest = Manifest(out string _);

            foreach (WorkloadSpec w in manifest.workloads) {
                Assert.Multiple(() => {
                    Assert.That(w.warmup, Is.GreaterThanOrEqualTo(0), $"{w.id}.warmup");
                    Assert.That(w.measured, Is.GreaterThan(0), $"{w.id}.measured");
                    Assert.That(w.samples, Is.GreaterThan(0), $"{w.id}.samples");
                });
            }
        }

        [Test]
        public void EveryFixtureResolvesToATestScenariosMethod () {
            WorkloadManifest manifest = Manifest(out string _);

            foreach (WorkloadSpec w in manifest.workloads) {
                if (w.fixture == null) {
                    continue; // the standard opening
                }

                MethodInfo m = typeof(TestScenarios).GetMethod(
                    w.fixture, BindingFlags.Public | BindingFlags.Static
                );
                Assert.That(m, Is.Not.Null, $"{w.id}: no TestScenarios.{w.fixture}");
                Assert.That(m.ReturnType, Is.EqualTo(typeof(MatchData)), $"{w.id}: {w.fixture} is not a MatchData fixture");
                Assert.That(m.GetParameters(), Is.Empty, $"{w.id}: {w.fixture} takes parameters");
            }
        }

        [Test]
        public void EverySearchFixtureIsAPlayablePosition () {
            WorkloadManifest manifest = Manifest(out string _);

            foreach (WorkloadSpec w in manifest.workloads.Where(w => w.layer == "search")) {
                MatchData data = w.fixture == null
                    ? null
                    : (MatchData) typeof(TestScenarios)
                        .GetMethod(w.fixture, BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, null);

                Board board = new Match(data)._GetCommittedBoard();

                Assert.That(
                    board.IsGameOver(), Is.False,
                    $"{w.id}: fixture '{w.fixture}' is already over, so the search would return immediately"
                );
            }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet test ChessersEngine.sln --filter WorkloadManifestTests
```

Expected: FAIL — `WorkloadManifest` does not exist.

- [ ] **Step 3: Create `ChessersEngine.Bench/Workloads.cs`**

```csharp
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
    }

    /// <summary>
    /// The benchmark suite definition. Iteration counts are declared here rather than calibrated
    /// at runtime: adaptive counts would make two language implementations do different amounts
    /// of work, which is exactly what the oracle exists to rule out.
    /// </summary>
    public sealed class WorkloadManifest {
        public int version;
        public List<WorkloadSpec> workloads = new List<WorkloadSpec>();

        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions {
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        /// <summary>Reads the manifest, also yielding the SHA-256 of the file's exact bytes.</summary>
        public static WorkloadManifest Load (string path, out string sha256) {
            byte[] raw = File.ReadAllBytes(path);
            return Parse(Encoding.UTF8.GetString(raw), raw, out sha256);
        }

        public static WorkloadManifest Parse (string json, byte[] rawBytes, out string sha256) {
            sha256 = Convert.ToHexStringLower(SHA256.HashData(rawBytes));

            WorkloadManifest manifest = JsonSerializer.Deserialize<WorkloadManifest>(json, JsonOpts);
            if (manifest == null || manifest.workloads == null) {
                throw new InvalidDataException("workload manifest is empty or malformed");
            }
            return manifest;
        }
    }
}
```

- [ ] **Step 4: Create `ChessersEngine.Bench/workloads.v1.json`**

```json
{
  "version": 1,
  "workloads": [
    {
      "id": "counter.overhead",
      "layer": "micro",
      "description": "1000 bare counter increments; quantifies what instrumentation costs the timed runs.",
      "fixture": null,
      "warmup": 100,
      "measured": 1000,
      "samples": 7
    },
    {
      "id": "movegen.potential.opening",
      "layer": "micro",
      "description": "GetPotentialTilesForMovement over every active piece, standard opening.",
      "fixture": null,
      "warmup": 50,
      "measured": 500,
      "samples": 7
    },
    {
      "id": "movegen.potential.Multijump1",
      "layer": "micro",
      "description": "GetPotentialTilesForMovement over every active piece, a checker-heavy position.",
      "fixture": "Multijump1",
      "warmup": 50,
      "measured": 500,
      "samples": 7
    },
    {
      "id": "movegen.valid.opening",
      "layer": "micro",
      "description": "GetValidTilesForMovement over every active piece: clone plus pseudo-legal plus undo per tile.",
      "fixture": null,
      "warmup": 10,
      "measured": 50,
      "samples": 7
    },
    {
      "id": "board.clone.opening",
      "layer": "micro",
      "description": "Board.CreateCopy of the standard opening.",
      "fixture": null,
      "warmup": 100,
      "measured": 2000,
      "samples": 7
    },
    {
      "id": "board.copystate.opening",
      "layer": "micro",
      "description": "Board.CopyState onto a pre-built board.",
      "fixture": null,
      "warmup": 100,
      "measured": 2000,
      "samples": 7
    },
    {
      "id": "board.apply-undo.opening",
      "layer": "micro",
      "description": "Move.GetPseudoLegalMoveResult followed by Board.UndoMove, one legal opening move.",
      "fixture": null,
      "warmup": 100,
      "measured": 2000,
      "samples": 7
    },
    {
      "id": "eval.boardvalue.opening",
      "layer": "micro",
      "description": "Board.CalculateBoardValue of the standard opening.",
      "fixture": null,
      "warmup": 100,
      "measured": 5000,
      "samples": 7
    },
    {
      "id": "notation.roundtrip.opening",
      "layer": "micro",
      "description": "MoveResult.CreateNotation for one opening move result.",
      "fixture": null,
      "warmup": 100,
      "measured": 5000,
      "samples": 7
    },
    {
      "id": "search.level0.opening",
      "layer": "search",
      "description": "CalculateBestMove level 0 from the standard opening.",
      "fixture": null,
      "level": 0,
      "warmup": 2,
      "measured": 5,
      "samples": 5
    },
    {
      "id": "search.level1.opening",
      "layer": "search",
      "description": "CalculateBestMove level 1 from the standard opening.",
      "fixture": null,
      "level": 1,
      "warmup": 2,
      "measured": 5,
      "samples": 5
    },
    {
      "id": "search.level2.opening",
      "layer": "search",
      "description": "CalculateBestMove level 2 (depth 4) from the standard opening.",
      "fixture": null,
      "level": 2,
      "warmup": 1,
      "measured": 2,
      "samples": 3
    },
    {
      "id": "search.level2.AlmostCheckmate1",
      "layer": "search",
      "description": "CalculateBestMove level 2 from a sparse near-mate position.",
      "fixture": "AlmostCheckmate1",
      "level": 2,
      "warmup": 1,
      "measured": 2,
      "samples": 3
    },
    {
      "id": "search.level2.Multijump1",
      "layer": "search",
      "description": "CalculateBestMove level 2 from a position with checker multi-jumps available.",
      "fixture": "Multijump1",
      "level": 2,
      "warmup": 1,
      "measured": 2,
      "samples": 3
    },
    {
      "id": "replay.game",
      "layer": "e2e",
      "description": "Replay a fixed 40-turn move sequence through a fresh Match, from the standard opening.",
      "fixture": null,
      "level": 0,
      "warmup": 1,
      "measured": 3,
      "samples": 5
    },
    {
      "id": "selfplay.level0",
      "layer": "e2e",
      "description": "One seeded AI-vs-AI game at level 0, capped at 40 turns.",
      "fixture": null,
      "level": 0,
      "warmup": 1,
      "measured": 1,
      "samples": 3
    }
  ]
}
```

- [ ] **Step 5: Copy the manifest to both output directories**

In `ChessersEngine.Bench/ChessersEngine.Bench.csproj`, add to the existing `ItemGroup`:

```xml
    <Content Include="workloads.v1.json" CopyToOutputDirectory="PreserveNewest" />
```

In `ChessersEngine.Tests/ChessersEngine.Tests.csproj`, extend the bench `ItemGroup` added in Task 2:

```xml
    <Compile Include="..\ChessersEngine.Bench\Workloads.cs" />
    <Content Include="..\ChessersEngine.Bench\workloads.v1.json" CopyToOutputDirectory="PreserveNewest" Link="workloads.v1.json" />
```

- [ ] **Step 6: Run the tests**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet test ChessersEngine.sln --filter WorkloadManifestTests
```

Expected: PASS (7 tests). If `EverySearchFixtureIsAPlayablePosition` fails, swap the offending fixture in `workloads.v1.json` for another `TestScenarios` fixture that has both kings active (`dotnet run --project ChessersEngine.Cli -- --list-scenarios` lists them all).

- [ ] **Step 7: Commit**

```bash
git add ChessersEngine.Bench ChessersEngine.Tests && git commit -m "Add workload manifest for the performance oracle

Iteration counts are declared, never calibrated at runtime, so both language
implementations perform identical work.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: Workload registry

**Files:**
- Create: `ChessersEngine.Bench/WorkloadRegistry.cs`
- Test: exercised end-to-end by Task 5's runner and by `--smoke`; no unit test of its own (each body is a one-line engine call whose behaviour the engine's own tests already cover).

**Deviation from the spec:** spec section 3 describes `replay.game` as replaying "a
recorded oracle JSON". This plan instead records a seeded self-play move sequence
in the workload's untimed setup and replays *that*. It exercises the identical
engine path, stays deterministic, and avoids committing a multi-hundred-ply corpus
file to the repo purely as bench input. Update the spec's section 3 wording when
this task lands.

**Interfaces:**
- Consumes: `WorkloadSpec` (Task 3), engine types `Match`, `Board`, `Move`, `MoveAttempt`, `MoveResult`, `TestScenarios`, `MatchData`, `Chessman`, `Tile`, `ColorEnum`.
- Produces: `ChessersEngine.Bench.WorkloadRegistry` with `public static Action Build (WorkloadSpec spec)` — runs the workload's untimed setup and returns the timed body — and `public static IEnumerable<string> KnownIds ()`.

- [ ] **Step 1: Create `ChessersEngine.Bench/WorkloadRegistry.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ChessersEngine.Bench {
    /// <summary>
    /// Turns a <see cref="WorkloadSpec"/> into the callable it measures. <see cref="Build"/>
    /// performs the workload's setup (untimed) and returns only the body the runner times.
    /// </summary>
    public static class WorkloadRegistry {
        const int ReplayTurns = 40;
        const int ReplaySeed = 1;
        const int SelfPlayTurns = 40;
        const int SelfPlaySeed = 1;

        public static IEnumerable<string> KnownIds () => new string[] {
            "counter.overhead",
            "movegen.potential",
            "movegen.valid",
            "board.clone",
            "board.copystate",
            "board.apply-undo",
            "eval.boardvalue",
            "notation.roundtrip",
            "search",
            "replay.game",
            "selfplay.level0",
        };

        public static Action Build (WorkloadSpec spec) {
            string kind = Kind(spec.id);

            switch (kind) {
                case "counter.overhead": return BuildCounterOverhead();
                case "movegen.potential": return BuildMovegenPotential(spec);
                case "movegen.valid": return BuildMovegenValid(spec);
                case "board.clone": return BuildBoardClone(spec);
                case "board.copystate": return BuildBoardCopyState(spec);
                case "board.apply-undo": return BuildApplyUndo(spec);
                case "eval.boardvalue": return BuildEval(spec);
                case "notation.roundtrip": return BuildNotation(spec);
                case "search": return BuildSearch(spec);
                case "replay.game": return BuildReplay(spec);
                case "selfplay.level0": return BuildSelfPlay(spec);
                default:
                    throw new ArgumentException($"no workload implementation for id '{spec.id}'");
            }
        }

        /// <summary>The implementation key: an id is `<kind>.<variant>`, and only the kind dispatches.</summary>
        static string Kind (string id) {
            foreach (string known in KnownIds()) {
                if (id == known || id.StartsWith(known + ".", StringComparison.Ordinal)) {
                    return known;
                }
            }
            throw new ArgumentException($"workload id '{id}' does not start with a known kind");
        }

        /// <summary>A fresh MatchData for the spec's fixture; null means the standard opening.</summary>
        static MatchData Fixture (WorkloadSpec spec) {
            if (spec.fixture == null) {
                return null;
            }
            MethodInfo m = typeof(TestScenarios).GetMethod(
                spec.fixture, BindingFlags.Public | BindingFlags.Static
            );
            if (m == null || m.ReturnType != typeof(MatchData) || m.GetParameters().Length != 0) {
                throw new ArgumentException($"unknown TestScenarios fixture '{spec.fixture}'");
            }
            return (MatchData) m.Invoke(null, null);
        }

        static Board FixtureBoard (WorkloadSpec spec) => new Match(Fixture(spec))._GetCommittedBoard();

        static Action BuildCounterOverhead () => () => {
            for (int i = 0; i < 1000; i++) {
                EngineCounters.Node();
            }
        };

        static Action BuildMovegenPotential (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => {
                List<Chessman> chessmen = board.GetActiveChessmen();
                for (int i = 0; i < chessmen.Count; i++) {
                    board.GetPotentialTilesForMovement(chessmen[i]);
                }
            };
        }

        static Action BuildMovegenValid (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => {
                List<Chessman> chessmen = board.GetActiveChessmen();
                for (int i = 0; i < chessmen.Count; i++) {
                    board.GetValidTilesForMovement(chessmen[i]);
                }
            };
        }

        static Action BuildBoardClone (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => board.CreateCopy();
        }

        static Action BuildBoardCopyState (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            Board target = board.CreateCopy();
            return () => target.CopyState(board);
        }

        static Action BuildApplyUndo (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            (int pieceId, int tileId) = FirstLegalMove(board);
            return () => {
                Move move = new Move(board, pieceId, tileId);
                MoveResult result = move.GetPseudoLegalMoveResult();
                board.UndoMove(result);
            };
        }

        static Action BuildEval (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => board.CalculateBoardValue(0);
        }

        static Action BuildNotation (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            (int pieceId, int tileId) = FirstLegalMove(board);
            MoveResult result = new Move(board, pieceId, tileId).GetPseudoLegalMoveResult();
            board.UndoMove(result);
            return () => result.CreateNotation();
        }

        static Action BuildSearch (WorkloadSpec spec) {
            int level = spec.level;
            // A fresh Match per iteration: CalculateBestMove shuffles with the match RNG, so a
            // reused Match would search a differently-ordered move list each iteration.
            return () => new Match(Fixture(spec), null, ReplaySeed).CalculateBestMove(level);
        }

        static Action BuildReplay (WorkloadSpec spec) {
            List<List<MoveAttempt>> turns = RecordSelfPlay(ReplaySeed, spec.level, ReplayTurns);
            return () => {
                Match match = new Match(null, null, ReplaySeed);
                foreach (List<MoveAttempt> turn in turns) {
                    foreach (MoveAttempt attempt in turn) {
                        if (match.MoveChessman(attempt) == null) {
                            return;
                        }
                    }
                    match.CommitTurn();
                }
            };
        }

        static Action BuildSelfPlay (WorkloadSpec spec) {
            int level = spec.level;
            return () => RecordSelfPlay(SelfPlaySeed, level, SelfPlayTurns);
        }

        /// <summary>
        /// Plays a seeded AI-vs-AI game and returns the accepted move attempts, turn by turn.
        /// Mirrors the AI turn loop in ChessersEngine.Cli so the replay exercises the same path.
        /// </summary>
        static List<List<MoveAttempt>> RecordSelfPlay (int seed, int level, int maxTurns) {
            Match match = new Match(null, null, seed);
            List<List<MoveAttempt>> turns = new List<List<MoveAttempt>>();

            while (!match.IsGameOver() && turns.Count < maxTurns) {
                ColorEnum moverColor = match.GetCommittedTurnColor();
                int moverPlayerId = moverColor == ColorEnum.WHITE ? match.whitePlayerId : match.blackPlayerId;

                List<MoveAttempt> attempts = match.CalculateBestMove(level);
                if (attempts == null || attempts.Count == 0 || attempts[0] == null) {
                    break;
                }

                List<MoveAttempt> accepted = new List<MoveAttempt>();
                foreach (MoveAttempt attempt in attempts) {
                    attempt.playerId = moverPlayerId;
                    MoveResult result = match.MoveChessman(attempt);
                    if (result == null || !result.valid) {
                        break;
                    }
                    accepted.Add(attempt);
                }

                if (accepted.Count == 0) {
                    break;
                }

                match.CommitTurn();
                turns.Add(accepted);
            }

            return turns;
        }

        /// <summary>Any legal (pieceId, tileId) on the board, for the single-move micro workloads.</summary>
        static (int, int) FirstLegalMove (Board board) {
            foreach (Chessman chessman in board.GetActiveChessmenOfColor(ColorEnum.WHITE)) {
                List<Tile> tiles = board.GetValidTilesForMovement(chessman);
                if (tiles.Count > 0) {
                    return (chessman.id, tiles[0].id);
                }
            }
            throw new InvalidOperationException("fixture has no legal white move");
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet build ChessersEngine.sln
```

Expected: build succeeds. If `match.whitePlayerId` / `blackPlayerId` are not accessible, use `Constants.DEFAULT_WHITE_PLAYER_ID` / `Constants.DEFAULT_BLACK_PLAYER_ID` instead — the standard opening uses those.

- [ ] **Step 3: Commit**

```bash
git add ChessersEngine.Bench/WorkloadRegistry.cs && git commit -m "Add workload registry for the performance oracle

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Runner

**Files:**
- Create: `ChessersEngine.Bench/Runner.cs`, `ChessersEngine.Bench/PerfReport.cs`
- Test: `ChessersEngine.Tests/RunnerContractTests.cs` is NOT added — the runner touches `EngineCounters` under `BENCH`, which the test assembly cannot compile. It is verified by `--smoke` in Task 7 and CI in Task 10.

**Interfaces:**
- Consumes: `Stats` (Task 2), `WorkloadSpec` (Task 3), `WorkloadRegistry.Build` (Task 4), `EngineCounters` (Task 1).
- Produces: `ChessersEngine.Bench.Runner` with `public static WorkloadResult Run (WorkloadSpec spec, bool smoke)`; and the report DTOs in `PerfReport.cs`: `PerfReport`, `EnvironmentDto`, `ManifestDto`, `WorkloadResult`, `IterationsDto`, `TimeDto`, `CountersDto`.

- [ ] **Step 1: Create `ChessersEngine.Bench/PerfReport.cs`**

```csharp
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
```

- [ ] **Step 2: Create `ChessersEngine.Bench/Runner.cs`**

```csharp
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
```

- [ ] **Step 3: Verify it compiles**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet build ChessersEngine.sln
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add ChessersEngine.Bench/Runner.cs ChessersEngine.Bench/PerfReport.cs && git commit -m "Add benchmark runner and perf report DTOs

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: Environment capture

**Files:**
- Create: `ChessersEngine.Bench/EnvironmentInfo.cs`

**Interfaces:**
- Consumes: `EnvironmentDto` (Task 5), `EngineCounters.Enabled` (Task 1).
- Produces: `ChessersEngine.Bench.EnvironmentInfo` with `public static EnvironmentDto Capture ()`.

- [ ] **Step 1: Create `ChessersEngine.Bench/EnvironmentInfo.cs`**

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

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
            countersEnabled = EngineCounters.Enabled,
        };

        static string OsName () {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "darwin";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
            return "unknown";
        }

        static string CpuModel () {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    return Capture("sysctl", "-n machdep.cpu.brand_string") ?? "unknown";
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

        static string GitSha () => Capture("git", "rev-parse --short HEAD") ?? "unknown";

        /// <summary>Runs a command and returns its trimmed stdout, or null if it cannot be run.</summary>
        static string Capture (string file, string args) {
            try {
                ProcessStartInfo psi = new ProcessStartInfo(file, args) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using (Process p = Process.Start(psi)) {
                    string stdout = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(5000);
                    return (p.ExitCode == 0 && stdout.Length > 0) ? stdout : null;
                }
            } catch (Exception) {
                return null;
            }
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet build ChessersEngine.sln
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add ChessersEngine.Bench/EnvironmentInfo.cs && git commit -m "Capture run environment for the perf report

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: CLI

**Files:**
- Modify: `ChessersEngine.Bench/Program.cs`

**Interfaces:**
- Consumes: `WorkloadManifest.Load` (Task 3), `Runner.Run` (Task 5), `EnvironmentInfo.Capture` (Task 6), `PerfReport` (Task 5).
- Produces: the `chessers-bench` command line: `--out <dir>` (default `perf`), `--manifest <path>` (default `workloads.v1.json` beside the binary), `--filter <substring>`, `--list`, `--smoke`.

- [ ] **Step 1: Replace `ChessersEngine.Bench/Program.cs`**

```csharp
using System;
using System.Collections.Generic;
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
  --help             show this message
";

        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = true };

        static int Main (string[] args) {
            string outDir = "perf";
            string manifestPath = Path.Combine(AppContext.BaseDirectory, "workloads.v1.json");
            string filter = null;
            bool list = false;
            bool smoke = false;

            for (int i = 0; i < args.Length; i++) {
                switch (args[i]) {
                    case "--out": outDir = Next(args, ref i); break;
                    case "--manifest": manifestPath = Next(args, ref i); break;
                    case "--filter": filter = Next(args, ref i); break;
                    case "--list": list = true; break;
                    case "--smoke": smoke = true; break;
                    case "--help": Console.Write(Usage); return 0;
                    default:
                        Console.Error.WriteLine($"unknown option '{args[i]}'\n\n{Usage}");
                        return 2;
                }
            }

            WorkloadManifest manifest = WorkloadManifest.Load(manifestPath, out string manifestSha);

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

            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "perf.v1.json");
            File.WriteAllText(outPath, JsonSerializer.Serialize(report, JsonOpts));
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
        /// file full of zeros.
        /// </summary>
        static bool VerifyCounters () {
            if (!EngineCounters.Enabled) {
                Console.Error.WriteLine("chessers-bench was built without BENCH defined; counters would all be zero");
                return false;
            }

            EngineCounters.Reset();
            new Match(null)._GetCommittedBoard().CreateCopy();
            if (EngineCounters.Take().boardClones == 0) {
                Console.Error.WriteLine("counter call sites are not wired: Board.CreateCopy did not count");
                return false;
            }
            return true;
        }
    }
}
```

- [ ] **Step 2: Run the smoke pass**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet run --project ChessersEngine.Bench --configuration Release -- --smoke --out /tmp/perf-smoke
```

Expected: one `running <id>...` line per workload, then `wrote /tmp/perf-smoke/perf.v1.json (16 workloads)`.

- [ ] **Step 3: Confirm the counters are non-zero**

```bash
grep -A9 '"id": "search.level0.opening"' /tmp/perf-smoke/perf.v1.json
```

Expected: a `counters` block with `nodes` greater than zero. If every counter is zero, a call site from Task 1 is missing.

- [ ] **Step 4: Run the real suite once**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet run --project ChessersEngine.Bench --configuration Release -- --out /tmp/perf-full
```

Expected: completes in under a few minutes and writes `/tmp/perf-full/perf.v1.json`. If `search.level2.*` takes longer than about a minute each, lower their `measured`/`samples` in `workloads.v1.json` and commit that change with this task.

- [ ] **Step 5: Commit**

```bash
git add ChessersEngine.Bench && git commit -m "Add chessers-bench command line

Refuses to emit a report when counters are unwired, rather than writing a
plausible file full of zeros.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 8: Published schema and checker

**Files:**
- Create: `ChessersEngine.Cli/schemas/perf.v1.schema.json`, `ChessersEngine.Cli/schemas/check-perf.ts`
- Modify: `ChessersEngine.Cli/schemas/schemas.ts`, `ChessersEngine.Cli/schemas/check-common.ts`, `ChessersEngine.Cli/schemas/package.json`

**Interfaces:**
- Consumes: the `perf.v1.json` shape from Task 5.
- Produces: `perfV1` and `perfSchemas` exports in `schemas.ts`; a `check-perf.ts` entry point; a `check:perf` package script. `CheckConfig.family` widens to include `"perf"`.

- [ ] **Step 1: Create `ChessersEngine.Cli/schemas/perf.v1.schema.json`**

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/perf.v1.schema.json",
  "title": "Chessers performance oracle (v1)",
  "description": "Benchmark results emitted by `chessers-bench`: per-workload wall-clock statistics and deterministic work counters, plus the machine and manifest they were produced with. A sibling artifact to the game oracle so the Rust port can be measured identically and compared. The format version lives in the `$schema` URI.",
  "type": "object",
  "required": ["$schema", "schemaVersion", "engine", "environment", "manifest", "results"],
  "additionalProperties": false,
  "properties": {
    "$schema": {
      "const": "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/perf.v1.schema.json"
    },
    "schemaVersion": { "const": 1 },
    "engine": { "type": "string" },
    "environment": {
      "type": "object",
      "additionalProperties": false,
      "required": ["os", "arch", "cpuModel", "logicalCores", "runtime", "buildConfig", "gitSha", "countersEnabled"],
      "properties": {
        "os": { "enum": ["darwin", "linux", "windows", "unknown"] },
        "arch": { "type": "string" },
        "cpuModel": { "type": "string" },
        "logicalCores": { "type": "integer", "minimum": 1 },
        "runtime": { "type": "string", "description": "Language runtime or toolchain identification." },
        "buildConfig": { "type": "string" },
        "gitSha": { "type": "string" },
        "countersEnabled": { "type": "boolean" }
      }
    },
    "manifest": {
      "type": "object",
      "additionalProperties": false,
      "required": ["version", "sha256"],
      "properties": {
        "version": { "type": "integer", "minimum": 1 },
        "sha256": {
          "type": "string",
          "pattern": "^[0-9a-f]{64}$",
          "description": "SHA-256 of the workloads.vN.json bytes the run used; two runs are only comparable when these match."
        }
      }
    },
    "results": {
      "type": "array",
      "minItems": 1,
      "items": {
        "type": "object",
        "additionalProperties": false,
        "required": ["id", "layer", "description", "iterations", "time", "counters"],
        "properties": {
          "id": { "type": "string" },
          "layer": { "enum": ["micro", "search", "e2e"] },
          "description": { "type": "string" },
          "iterations": {
            "type": "object",
            "additionalProperties": false,
            "required": ["warmup", "measured", "samples"],
            "properties": {
              "warmup": { "type": "integer", "minimum": 0 },
              "measured": { "type": "integer", "minimum": 1 },
              "samples": { "type": "integer", "minimum": 1 }
            }
          },
          "time": {
            "type": "object",
            "additionalProperties": false,
            "required": ["minNsPerOp", "medianNsPerOp", "p90NsPerOp", "maxNsPerOp", "meanNsPerOp", "stddevNsPerOp", "opsPerSecond"],
            "properties": {
              "minNsPerOp": { "type": "number", "minimum": 0, "description": "The headline figure: the least noise-contaminated sample." },
              "medianNsPerOp": { "type": "number", "minimum": 0 },
              "p90NsPerOp": { "type": "number", "minimum": 0 },
              "maxNsPerOp": { "type": "number", "minimum": 0 },
              "meanNsPerOp": { "type": "number", "minimum": 0 },
              "stddevNsPerOp": { "type": "number", "minimum": 0 },
              "opsPerSecond": { "type": "number", "minimum": 0 }
            }
          },
          "counters": {
            "type": "object",
            "additionalProperties": false,
            "required": ["nodes", "moveGen", "tilesProduced", "boardClones", "copyStates", "moveApplies", "moveUndos", "evals"],
            "description": "Per-op work tallies from a single dedicated pass. A difference between two implementations is a porting bug, not a performance result.",
            "properties": {
              "nodes": { "type": "integer", "minimum": 0 },
              "moveGen": { "type": "integer", "minimum": 0 },
              "tilesProduced": { "type": "integer", "minimum": 0 },
              "boardClones": { "type": "integer", "minimum": 0 },
              "copyStates": { "type": "integer", "minimum": 0 },
              "moveApplies": { "type": "integer", "minimum": 0 },
              "moveUndos": { "type": "integer", "minimum": 0 },
              "evals": { "type": "integer", "minimum": 0 }
            }
          }
        }
      }
    }
  }
}
```

- [ ] **Step 2: Add the Zod mirror to `ChessersEngine.Cli/schemas/schemas.ts`**

Append before the `export const oracleSchemas` line:

```ts
// ===========================================================================
// perf.v1
// ===========================================================================
const nonNegative = () => z.number().min(0);

const perfCounters = z.strictObject({
  nodes: int().min(0),
  moveGen: int().min(0),
  tilesProduced: int().min(0),
  boardClones: int().min(0),
  copyStates: int().min(0),
  moveApplies: int().min(0),
  moveUndos: int().min(0),
  evals: int().min(0),
});

const perfTime = z.strictObject({
  minNsPerOp: nonNegative(),
  medianNsPerOp: nonNegative(),
  p90NsPerOp: nonNegative(),
  maxNsPerOp: nonNegative(),
  meanNsPerOp: nonNegative(),
  stddevNsPerOp: nonNegative(),
  opsPerSecond: nonNegative(),
});

const perfResult = z.strictObject({
  id: z.string(),
  layer: z.enum(["micro", "search", "e2e"]),
  description: z.string(),
  iterations: z.strictObject({
    warmup: int().min(0),
    measured: int().min(1),
    samples: int().min(1),
  }),
  time: perfTime,
  counters: perfCounters,
});

export const perfV1 = z.strictObject({
  $schema: z.literal(`${SCHEMA_BASE}/perf.v1.schema.json`),
  schemaVersion: z.literal(1),
  engine: z.string(),
  environment: z.strictObject({
    os: z.enum(["darwin", "linux", "windows", "unknown"]),
    arch: z.string(),
    cpuModel: z.string(),
    logicalCores: int().min(1),
    runtime: z.string(),
    buildConfig: z.string(),
    gitSha: z.string(),
    countersEnabled: z.boolean(),
  }),
  manifest: z.strictObject({
    version: int().min(1),
    sha256: z.string().regex(/^[0-9a-f]{64}$/),
  }),
  results: z.array(perfResult).min(1),
});

export type PerfV1 = z.infer<typeof perfV1>;
```

Then add the registry export beside the existing two:

```ts
export const perfSchemas: Record<number, z.ZodType> = { 1: perfV1 };
```

- [ ] **Step 3: Widen the family union in `ChessersEngine.Cli/schemas/check-common.ts`**

Change the interface field and the label expression:

```ts
export interface CheckConfig {
  family: "oracle" | "codec" | "perf";
  uriRe: RegExp; // one capture group = the version integer
  schemaByVersion: Record<number, z.ZodType>;
}
```

```ts
  const label = cfg.family[0].toUpperCase() + cfg.family.slice(1);
```

(The previous `cfg.family === "oracle" ? "Oracle" : "Codec"` ternary is replaced by that line — it produced the same strings for the two existing families.)

- [ ] **Step 4: Create `ChessersEngine.Cli/schemas/check-perf.ts`**

```ts
// Mirrors check-oracle.ts for the `perf.vN.schema.json` benchmark artifact.

import { Command } from "commander";
import { runCheck } from "./check-common.ts";
import { perfSchemas } from "./schemas.ts";

new Command()
  .name("check-perf")
  .description("Validate perf reports and enforce version-marker sync.")
  .argument("<schemasDir>", "directory holding the *.vN.schema.json files")
  .argument("<files...>", "perf JSON files to validate")
  .action((schemasDir: string, files: string[]) => {
    process.exit(
      runCheck(schemasDir, files, {
        family: "perf",
        uriRe: /perf\.v(\d+)\.schema\.json$/,
        schemaByVersion: perfSchemas,
      }),
    );
  })
  .parse();
```

- [ ] **Step 5: Add the script to `ChessersEngine.Cli/schemas/package.json`**

Add to `"scripts"`, after `"check:codec"`:

```json
    "check:perf": "node check-perf.ts",
```

- [ ] **Step 6: Verify**

```bash
cd ChessersEngine.Cli/schemas && pnpm install --frozen-lockfile && pnpm typecheck && pnpm lint:check && pnpm fmt:check
```

Expected: all pass. If `fmt:check` fails, run `pnpm fmt` and re-check.

```bash
cd ChessersEngine.Cli/schemas && node check-perf.ts . /tmp/perf-smoke/perf.v1.json
```

Expected: `Perf schema check OK: 1 file(s) valid and version-synced.`

Confirm the `check-common.ts` label change did not break the existing checkers:

```bash
export PATH="$HOME/.dotnet:$PATH" && out="$(mktemp -d)" && \
  dotnet run --project ChessersEngine.Cli --configuration Release -- --dump-codec --out "$out" && \
  (cd ChessersEngine.Cli/schemas && node check-codec.ts . "$out"/codec.v1.json)
```

Expected: `Codec schema check OK: 1 file(s) valid and version-synced.`

- [ ] **Step 7: Commit**

```bash
git add ChessersEngine.Cli/schemas && git commit -m "Add perf.v1 schema and checker

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 9: Comparator

**Files:**
- Create: `ChessersEngine.Cli/schemas/compare-perf.ts`
- Modify: `ChessersEngine.Cli/schemas/package.json`

**Interfaces:**
- Consumes: `perfV1`, `PerfV1` (Task 8).
- Produces: `compare-perf <baseline.json> <candidate.json>` and a `compare:perf` package script. Exit codes: `0` comparable, `1` counter mismatch or missing workloads, `2` unreadable input or manifest hash mismatch.

- [ ] **Step 1: Create `ChessersEngine.Cli/schemas/compare-perf.ts`**

```ts
// Joins two perf reports -- typically the C# baseline and the Rust candidate -- on workload id.
//
// Two signals are kept apart on purpose:
//   - a counter difference means the two implementations did different algorithmic work; that is a
//     porting bug, and it fails the run.
//   - a time difference is just a result, and never fails on its own.

import { readFileSync } from "node:fs";
import { Command } from "commander";
import { type PerfV1, perfV1 } from "./schemas.ts";

const COUNTER_KEYS = [
  "nodes",
  "moveGen",
  "tilesProduced",
  "boardClones",
  "copyStates",
  "moveApplies",
  "moveUndos",
  "evals",
] as const;

function load(path: string): PerfV1 {
  let doc: unknown;
  try {
    doc = JSON.parse(readFileSync(path, "utf8"));
  } catch (e) {
    process.stderr.write(`${path}: could not read/parse JSON: ${(e as Error).message}\n`);
    process.exit(2);
  }

  const parsed = perfV1.safeParse(doc);
  if (!parsed.success) {
    process.stderr.write(`${path}: not a valid perf.v1 report:\n`);
    for (const issue of parsed.error.issues) {
      process.stderr.write(`  - /${issue.path.join("/")}: ${issue.message}\n`);
    }
    process.exit(2);
  }
  return parsed.data;
}

function envMismatch(a: PerfV1["environment"], b: PerfV1["environment"]): string[] {
  const differing: string[] = [];
  if (a.os !== b.os) differing.push(`os: ${a.os} vs ${b.os}`);
  if (a.arch !== b.arch) differing.push(`arch: ${a.arch} vs ${b.arch}`);
  if (a.cpuModel !== b.cpuModel) differing.push(`cpuModel: ${a.cpuModel} vs ${b.cpuModel}`);
  return differing;
}

function compare(baselinePath: string, candidatePath: string): number {
  const baseline = load(baselinePath);
  const candidate = load(candidatePath);

  if (baseline.manifest.sha256 !== candidate.manifest.sha256) {
    process.stderr.write(
      "manifest mismatch: the two runs used different workload manifests, so their results " +
        `are not comparable (${baseline.manifest.sha256.slice(0, 12)} vs ` +
        `${candidate.manifest.sha256.slice(0, 12)})\n`,
    );
    return 2;
  }

  const differing = envMismatch(baseline.environment, candidate.environment);
  if (differing.length) {
    process.stdout.write(
      "!! DIFFERENT MACHINES -- the times below are NOT comparable !!\n" +
        differing.map((d) => `   ${d}\n`).join("") +
        "   Counter comparisons remain valid.\n\n",
    );
  }

  const byId = new Map(candidate.results.map((r) => [r.id, r]));
  const missing: string[] = [];
  const extra = new Set(byId.keys());
  const counterProblems: string[] = [];

  process.stdout.write(
    `${"workload".padEnd(34)}${"baseline ns".padStart(14)}${"candidate ns".padStart(14)}` +
      `${"speedup".padStart(10)}  counters\n`,
  );

  for (const base of baseline.results) {
    const cand = byId.get(base.id);
    if (!cand) {
      missing.push(base.id);
      continue;
    }
    extra.delete(base.id);

    const deltas = COUNTER_KEYS.filter((k) => base.counters[k] !== cand.counters[k]).map(
      (k) => `${k} ${base.counters[k]}->${cand.counters[k]}`,
    );
    if (deltas.length) counterProblems.push(`${base.id}: ${deltas.join(", ")}`);

    const speedup = cand.time.minNsPerOp > 0 ? base.time.minNsPerOp / cand.time.minNsPerOp : 0;
    process.stdout.write(
      base.id.padEnd(34) +
        base.time.minNsPerOp.toFixed(1).padStart(14) +
        cand.time.minNsPerOp.toFixed(1).padStart(14) +
        `${speedup.toFixed(2)}x`.padStart(10) +
        (deltas.length ? "  DIFFER" : "  match") +
        "\n",
    );
  }

  let failed = false;

  if (missing.length) {
    failed = true;
    process.stdout.write(`\nmissing from ${candidatePath}:\n`);
    for (const id of missing) process.stdout.write(`  - ${id}\n`);
  }
  if (extra.size) {
    failed = true;
    process.stdout.write(`\nnot present in ${baselinePath}:\n`);
    for (const id of extra) process.stdout.write(`  - ${id}\n`);
  }
  if (counterProblems.length) {
    failed = true;
    process.stdout.write("\ncounter mismatches (the two engines did different work):\n");
    for (const p of counterProblems) process.stdout.write(`  - ${p}\n`);
  }

  if (!failed) {
    process.stdout.write("\nCounters match on every workload.\n");
  }
  return failed ? 1 : 0;
}

new Command()
  .name("compare-perf")
  .description("Compare two perf.v1 reports; fails on counter mismatch, never on time alone.")
  .argument("<baseline>", "baseline perf.v1.json (e.g. the C# engine)")
  .argument("<candidate>", "candidate perf.v1.json (e.g. the Rust port)")
  .action((baseline: string, candidate: string) => process.exit(compare(baseline, candidate)))
  .parse();
```

- [ ] **Step 2: Add the script to `ChessersEngine.Cli/schemas/package.json`**

Add to `"scripts"`, after `"check:perf"`:

```json
    "compare:perf": "node compare-perf.ts",
```

- [ ] **Step 3: Verify it runs and detects a mismatch**

```bash
cd ChessersEngine.Cli/schemas && pnpm typecheck && pnpm lint:check && pnpm fmt:check
```

Expected: all pass.

```bash
cd ChessersEngine.Cli/schemas && node compare-perf.ts /tmp/perf-smoke/perf.v1.json /tmp/perf-smoke/perf.v1.json; echo "exit=$?"
```

Expected: a table where every speedup is `1.00x`, `Counters match on every workload.`, `exit=0`.

```bash
python3 -c "
import json
d = json.load(open('/tmp/perf-smoke/perf.v1.json'))
d['results'][0]['counters']['nodes'] += 1
json.dump(d, open('/tmp/perf-tampered.json','w'))
"
```

```bash
cd ChessersEngine.Cli/schemas && node compare-perf.ts /tmp/perf-smoke/perf.v1.json /tmp/perf-tampered.json; echo "exit=$?"
```

Expected: the first workload's row ends in `DIFFER`, a `counter mismatches` section names it, `exit=1`.

- [ ] **Step 4: Commit**

```bash
git add ChessersEngine.Cli/schemas && git commit -m "Add perf report comparator

Counter differences fail the comparison; time differences never do.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 10: CI and documentation

**Files:**
- Create: `ChessersEngine.Bench/README.md`
- Modify: `.github/workflows/build.yml`, `ChessersEngine.Cli/schemas/README.md`

**Interfaces:**
- Consumes: everything above.
- Produces: no code. A CI step that smoke-runs the bench and validates its output, and the portable methodology document the Rust bench is written against.

- [ ] **Step 1: Extend `.github/workflows/build.yml`**

Rename the existing schema-check step and add the bench to it. Replace the step named `Oracle & codec schema checks` with:

```yaml
      - name: Oracle, codec & perf schema checks
        run: |
          out="$(mktemp -d)"
          dotnet run --project ChessersEngine.Cli --configuration Release --no-restore --no-build -- \
            --ai-vs-ai --games 1 --seed 1 --quiet --max-plies 6 --out "$out"
          dotnet run --project ChessersEngine.Cli --configuration Release --no-restore --no-build -- \
            --scenario Checkmate1 --ai-vs-ai --seed 1 --quiet --out "$out"
          dotnet run --project ChessersEngine.Cli --configuration Release --no-restore --no-build -- \
            --dump-codec --out "$out"
          # --smoke runs every workload once: this checks that the bench is wired, not that it is fast.
          # Shared runners are far too noisy to gate on timings, so nothing here compares them.
          dotnet run --project ChessersEngine.Bench --configuration Release --no-restore --no-build -- \
            --smoke --out "$out"
          cd ChessersEngine.Cli/schemas
          pnpm install --frozen-lockfile
          node check-oracle.ts . "$out"/chessers-*.json "$out"/scenario-*.json
          node check-codec.ts . "$out"/codec.v1.json
          node check-perf.ts . "$out"/perf.v1.json
```

- [ ] **Step 2: Extend `ChessersEngine.Cli/schemas/README.md`**

After the paragraph describing the codec family, add:

```markdown
A third artifact family lives here: `perf.vN.schema.json` for the `chessers-bench`
performance oracle, validated by [`check-perf.ts`](check-perf.ts) and diffed by
[`compare-perf.ts`](compare-perf.ts). It follows the same policy below. See
[`../../ChessersEngine.Bench/README.md`](../../ChessersEngine.Bench/README.md)
for the measurement methodology the numbers depend on.
```

Then extend the "Current version" section:

```markdown
`perf.v1.schema.json` — see [../../ChessersEngine.Bench/README.md](../../ChessersEngine.Bench/README.md).
```

- [ ] **Step 3: Create `ChessersEngine.Bench/README.md`**

```markdown
# chessers-bench — the performance oracle

The sibling of the correctness oracle in [`../ChessersEngine.Cli`](../ChessersEngine.Cli).
That one proves the Rust port *behaves* like the C# engine; this one measures how
fast each of them is, and whether they do the same amount of work getting there.

This document is the portable specification. A Rust benchmark that follows it
produces a `perf.v1.json` the comparator can diff against the C# baseline.

## Running

```bash
export PATH="$HOME/.dotnet:$PATH"

# the full suite (a few minutes), writing ./perf/perf.v1.json
dotnet run --project ChessersEngine.Bench --configuration Release

# every workload once, no warmup — a wiring check, not a measurement
dotnet run --project ChessersEngine.Bench --configuration Release -- --smoke

# just the search layer
dotnet run --project ChessersEngine.Bench --configuration Release -- --filter search
```

**Always use `--configuration Release`.** A Debug build measures the absence of
the optimiser.

Options: `--out <dir>` (default `perf`), `--manifest <path>`, `--filter <text>`,
`--list`, `--smoke`, `--help`.

## Comparing two runs

```bash
cd ChessersEngine.Cli/schemas
pnpm install --frozen-lockfile
node compare-perf.ts path/to/csharp/perf.v1.json path/to/rust/perf.v1.json
```

The comparator keeps two signals apart:

- **counter mismatch** — the two engines did different algorithmic work. That is a
  porting bug, not a performance result, and it exits non-zero.
- **time delta** — reported as a speedup factor. It never fails on its own, and it
  is prefixed with a warning banner when the two `environment` blocks disagree on
  OS, architecture, or CPU model.

It refuses outright to compare runs whose `manifest.sha256` differs.

## How counting works

`EngineCounters` ([`../EngineCounters.cs`](../EngineCounters.cs)) is a set of
`static long` tallies. Every increment method is `[Conditional("BENCH")]`, so the
C# compiler erases the call — argument evaluation included — in any build without
that symbol. Only this project defines it, by compiling the engine sources in
directly rather than referencing the engine project (`DefineConstants` does not
flow across a `ProjectReference`, so a reference would link an uninstrumented
engine and report zeros).

`chessers-bench` refuses to emit a report if `EngineCounters.Enabled` is false or
if a probe clone fails to register — a report full of plausible zeros is worse
than no report.

### The increment contract

A Rust port MUST increment at semantically equivalent points, behind a
`bench-counters` cargo feature.

| Counter         | Incremented at                                  | Once per |
| --------------- | ----------------------------------------------- | -------- |
| `nodes`         | entry to `Match.MinimaxHelper`                  | call     |
| `moveGen`       | every return of `Board.GetPotentialTilesForMovement` | call |
| `tilesProduced` | same site, by the returned list's length        | tile     |
| `boardClones`   | `Board.CreateCopy`                              | call     |
| `copyStates`    | `Board.CopyState`                               | call     |
| `moveApplies`   | `Move.GetPseudoLegalMoveResult`                 | call     |
| `moveUndos`     | `Board.UndoMove`                                | call     |
| `evals`         | `Board.CalculateBoardValue`                     | call     |

`CreateCopy` calls `CopyState`, so `copyStates >= boardClones` always holds. That
relationship is part of the contract, not an accident of the current code.

Counters stay live during the timed runs — there is one build, not two. Rather
than assume that is free, the suite includes a `counter.overhead` workload that
times a bare increment loop, so the cost is a measured number in the output.

## The workload manifest

[`workloads.v1.json`](workloads.v1.json) declares every workload and its
iteration counts. Both language implementations read this same file, and its
SHA-256 goes into the report so the comparator can refuse mismatched runs.

Iteration counts are **declared, never calibrated at runtime**. An adaptive
"run until N milliseconds elapse" scheme would have the two languages perform
different amounts of work, which is precisely what this oracle exists to rule out.

Fields: `id`, `layer` (`micro` | `search` | `e2e`), `description`, `fixture` (a
`TestScenarios` method name, or `null` for the standard opening), `level` (AI
search level, `-1` when not applicable), `warmup`, `measured`, `samples`.

An id is `<kind>.<variant>`; only the kind selects an implementation, so adding a
`movegen.potential.<newFixture>` entry needs no code change.

The three layers exist so that a disappointing speedup can be localised: if the
primitives are 20x faster but end-to-end play is only 3x, the difference is in
how they are composed, not in the primitives.

## Measurement method

For each workload:

1. Build the workload once. Setup — constructing boards, recording the replay
   move sequence — happens here and is **not** timed.
2. Run `warmup` untimed iterations.
3. Repeat `samples` times: force a collection (outside the timed region), then
   time a batch of `measured` iterations. The sample's value is batch time
   divided by `measured`.
4. Reset the counters, run one further iteration, and snapshot them. Counters are
   therefore per-op, and their pass is separate from every timed pass.

The forced collection in step 3 is a .NET-local step. A Rust implementation has
no equivalent and needs none; this is a difference in the implementations, not in
the methodology.

Reported per workload: `min`, `median`, `p90`, `max`, `mean`, `stddev` of
nanoseconds per op, plus `opsPerSecond` derived from `min`.

**`min` is the headline figure** — the sample least contaminated by scheduler
noise, garbage collection, and CPU frequency scaling.

`stddev` is the *sample* standard deviation (n−1 denominator), zero for a single
sample. Percentiles use linear interpolation between the two closest ranks, where
`rank = p/100 × (n−1)` over the ascending-sorted samples. Both definitions are
part of the contract; see [`Stats.cs`](Stats.cs).

## Output

`perf.v1.json`, following the same conventions as the game oracle: the format
version lives in the `$schema` URI, a machine-readable `schemaVersion` mirrors it,
and [`check-perf.ts`](../ChessersEngine.Cli/schemas/check-perf.ts) enforces that
the two agree and that the file validates. The published JSON Schema is
[`perf.v1.schema.json`](../ChessersEngine.Cli/schemas/perf.v1.schema.json).

The `environment` block records OS, architecture, CPU model, logical core count,
runtime, build configuration, git SHA, and whether counters were compiled in. It
deliberately records no hostname or username.

## Not in scope

There is no CI performance gate. Shared runners are too noisy for wall-clock
gating, and gating on counters alone would be a correctness check wearing a
performance hat — the oracle's job is to inform a comparison, not to block a
merge. CI runs `--smoke` only, to prove the harness still executes.
```

- [ ] **Step 4: Verify the CI step locally**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet build ChessersEngine.sln --configuration Release && \
  out="$(mktemp -d)" && \
  dotnet run --project ChessersEngine.Bench --configuration Release --no-build -- --smoke --out "$out" && \
  (cd ChessersEngine.Cli/schemas && node check-perf.ts . "$out"/perf.v1.json)
```

Expected: `Perf schema check OK: 1 file(s) valid and version-synced.`

- [ ] **Step 5: Run the whole suite one final time**

```bash
export PATH="$HOME/.dotnet:$PATH" && dotnet build ChessersEngine.sln --configuration Release && dotnet test ChessersEngine.sln --configuration Release --no-build
```

Expected: build succeeds, every test passes.

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/build.yml ChessersEngine.Bench/README.md ChessersEngine.Cli/schemas/README.md && git commit -m "Document the performance oracle and smoke-run it in CI

The README is the portable methodology spec the Rust bench is written against.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```
