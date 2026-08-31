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
