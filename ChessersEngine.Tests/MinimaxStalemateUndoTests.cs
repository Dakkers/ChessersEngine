using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// that corruption changed the move the search returned (to 18->4 / 18->5 / 18->2 / 18->3 instead of the
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

        [TestCase(17)]
        [TestCase(164)]
        [TestCase(214)]
        [TestCase(262)]
        public void SearchIsNotCorruptedByAnUnundoneStalemateMove (int seed) {
            List<MoveAttempt> best = new Match(Position(), null, seed).CalculateBestMove(0);

            Assert.That(best, Is.Not.Null.And.Not.Empty);
            Assert.That(best[0].pieceId, Is.EqualTo(Constants.ID_WHITE_ROOK_2));
            Assert.That(best[0].tileId, Is.EqualTo(15),
                "an un-undone stalemate move corrupted the search board and changed this seed's chosen move");
        }

        static List<string> Snapshot (Board board) =>
            board.GetChessmanSchemas()
                .Select((s) => $"{s.id}:{s.colorId}:{s.kind}:{s.location}:{s.isActive}:{s.hasMoved}:{s.isChecker}:{s.isKinged}:{s.isPromoted}")
                .ToList();

        /// <summary>
        /// The search MUST leave the board it was given exactly as it found it.
        /// Independent of the RNG seed and of iteration order.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void SearchLeavesTheSearchBoardUnchanged (int seed) {
            Match match = new Match(Position(), null, seed);
            Board board = match._GetCommittedBoard().CreateCopy();
            List<string> before = Snapshot(board);

            MethodInfo minimax = typeof(Match).GetMethod("MinimaxHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            object config = System.Activator.CreateInstance(minimax.GetParameters()[1].ParameterType);
            config.GetType().GetField("maxDepth").SetValue(config, 2);

            minimax.Invoke(match, new object[] { board, config, 0, int.MinValue, int.MaxValue, true, null });

            Assert.That(Snapshot(board), Is.EqualTo(before),
                "the search left the board mutated; a skipped move was not undone");
        }
    }
}
