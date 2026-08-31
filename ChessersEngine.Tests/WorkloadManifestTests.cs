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
        [Test]
        public void EveryIdMapsToAWorkloadRegistryKind () {
            WorkloadManifest manifest = Manifest(out string _);

            foreach (WorkloadSpec w in manifest.workloads) {
                Assert.That(
                    WorkloadManifest.KindOf(w.id, WorkloadRegistry.KnownIds()), Is.Not.Null,
                    $"{w.id}: no WorkloadRegistry kind implements this id"
                );
            }
        }

        [Test]
        public void ValidatePassesForTheShippedManifest () {
            WorkloadManifest manifest = Manifest(out string _);

            Assert.DoesNotThrow(
                () => manifest.Validate(WorkloadRegistry.KnownIds(), WorkloadRegistry.FixtureResolves)
            );
        }

        [TestCase("measured", 0)]
        [TestCase("samples", 0)]
        [TestCase("warmup", -1)]
        public void ValidateRejectsNonsenseIterationCounts (string field, int value) {
            WorkloadManifest manifest = Manifest(out string _);
            WorkloadSpec spec = manifest.workloads[0];
            switch (field) {
                case "measured": spec.measured = value; break;
                case "samples": spec.samples = value; break;
                case "warmup": spec.warmup = value; break;
            }

            InvalidDataException e = Assert.Throws<InvalidDataException>(
                () => manifest.Validate(WorkloadRegistry.KnownIds(), WorkloadRegistry.FixtureResolves)
            );
            Assert.That(e.Message, Does.Contain(field));
        }

        [Test]
        public void ValidateRejectsAnUnimplementedId () {
            WorkloadManifest manifest = Manifest(out string _);
            manifest.workloads[0].id = "bogus.thing";

            InvalidDataException e = Assert.Throws<InvalidDataException>(
                () => manifest.Validate(WorkloadRegistry.KnownIds(), WorkloadRegistry.FixtureResolves)
            );
            Assert.That(e.Message, Does.Contain("bogus.thing"));
        }

        [Test]
        public void ValidateRejectsAnUnknownLayerAndFixture () {
            WorkloadManifest manifest = Manifest(out string _);
            manifest.workloads[0].layer = "nano";
            manifest.workloads[0].fixture = "NoSuchScenario";

            InvalidDataException e = Assert.Throws<InvalidDataException>(
                () => manifest.Validate(WorkloadRegistry.KnownIds(), WorkloadRegistry.FixtureResolves)
            );
            Assert.Multiple(() => {
                Assert.That(e.Message, Does.Contain("nano"));
                Assert.That(e.Message, Does.Contain("NoSuchScenario"));
            });
        }

        [Test]
        public void OnlyRngDependentWorkloadsOptOutOfCounterComparison () {
            WorkloadManifest manifest = Manifest(out string _);

            List<string> opted = manifest.workloads
                .Where(w => !w.CountersAreComparable)
                .Select(w => w.id)
                .ToList();

            Assert.That(opted, Is.EquivalentTo(new[] {
                "search.level0.opening",
                "search.level1.Multijump1",
                "search.level2.opening",
                "search.level2.AlmostCheckmate1",
                "search.level2.Multijump1",
                "selfplay.level0",
            }));
        }

        [Test]
        public void TheReplayFixtureStillReplaysCleanly () {
            Match match = new Match(null, null, 1);

            foreach (List<MoveAttempt> turn in WorkloadRegistry.LoadReplaySequence()) {
                foreach (MoveAttempt attempt in turn) {
                    MoveResult result = match.MoveChessman(attempt);
                    Assert.That(result, Is.Not.Null, $"attempt {attempt} was rejected outright");
                    Assert.That(result.valid, Is.True, $"attempt {attempt} was invalid");
                }
                match.CommitTurn();
            }
        }
    }
}
