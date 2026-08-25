using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// Regression test for the minimax stalemate-undo bug.
    ///
    /// With the white king off the first rank, Rh1 -> b1 stalemates the lone black king
    /// (the bishop on d4 covers a7, the rook covers b7/b8). A stalemate move is a *valid*
    /// move, so MinimaxHelper's board.MoveChessman applied it to the shared search board;
    /// the old code then skipped it with `continue` WITHOUT calling board.UndoMove, leaving
    /// the board mutated for every later candidate at that node. These seeds are ones where
    /// that corruption changed the move the search returned (to 18->3 / 18->0 instead of the
    /// uncorrupted 18->15). The RNG is seeded so the search is deterministic.
    ///
    /// This pins the corrected choice for specific seeds; if the evaluation is intentionally
    /// changed later these seeds may need to be re-derived, but a divergence here means the
    /// search board is being corrupted mid-node again.
    /// </summary>
    [TestFixture]
    public class MinimaxStalemateUndoTests {
        static MatchData Position () => new MatchData {
            currentTurn = ColorEnum.WHITE,
            matchId = 1,
            whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
            blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
            pieces = new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(23),
                TestScenarios.CreateRook(Constants.ID_WHITE_ROOK_2, 7),
                TestScenarios.CreateBishop(Constants.ID_WHITE_BISHOP_1, 27),
                TestScenarios.CreateBlackKing(56),
            },
        };

        [TestCase(1)]
        [TestCase(18)]
        [TestCase(42)]
        [TestCase(60)]
        public void SearchIsNotCorruptedByAnUnundoneStalemateMove (int seed) {
            List<MoveAttempt> best = new Match(Position(), null, seed).CalculateBestMove(0);

            Assert.That(best, Is.Not.Null.And.Not.Empty);
            Assert.That(best[0].pieceId, Is.EqualTo(Constants.ID_WHITE_ROOK_2));
            Assert.That(best[0].tileId, Is.EqualTo(15),
                "an un-undone stalemate move corrupted the search board and changed this seed's chosen move");
        }
    }
}
