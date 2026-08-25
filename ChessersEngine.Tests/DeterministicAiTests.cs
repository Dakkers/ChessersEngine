using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// The optional randomSeed on Match makes the minimax move search reproducible:
    /// the same seed must always yield the same result.
    /// </summary>
    [TestFixture]
    public class DeterministicAiTests {
        static string Signature (List<MoveAttempt> moves) =>
            moves == null ? "null" : string.Join(",", moves.Select(m => $"{m.pieceId}->{m.tileId}"));

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(123)]
        public void SameSeed_ProducesIdenticalBestMove (int seed) {
            List<MoveAttempt> a = new Match(null, null, seed).CalculateBestMove(0);
            List<MoveAttempt> b = new Match(null, null, seed).CalculateBestMove(0);

            Assert.That(a, Is.Not.Null.And.Not.Empty);
            Assert.That(Signature(a), Is.EqualTo(Signature(b)),
                "the same seed must make the move search fully reproducible");
        }
    }
}
