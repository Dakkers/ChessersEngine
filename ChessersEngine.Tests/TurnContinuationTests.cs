using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// A jump/polarity move keeps the turn open for a multi-jump only if a *legal*
    /// continuation jump exists. PostValidationHandler decided this from
    /// GetPotentialTilesForMovement (jumps that are NOT check-validated), so when the
    /// only continuation was an illegal jump the turn never changed and the player was
    /// left with no legal move and the game not over -> a deadlock.
    /// </summary>
    [TestFixture]
    public class TurnContinuationTests {
        // White rook e4(28) is pinned to the white king e1(4) by the black rook e8(60).
        // Moving it to e5(36) crosses into the top half, so it becomes a checker. As a
        // checker it has one potential jump (e5 x f6-knight -> g7), but that jump is
        // illegal: leaving the e-file exposes the king. So there is NO legal continuation
        // and the turn must end.
        static Match PinnedRookAboutToCross () {
            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE, matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(4),
                    TestScenarios.CreateRook(Constants.ID_WHITE_ROOK_1, 28),
                    TestScenarios.CreateKnight(Constants.ID_BLACK_KNIGHT_1, 45),
                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_1, 60),
                    TestScenarios.CreateBlackKing(63),
                },
            };
            return new Match(md);
        }

        [Test]
        public void PolarityMoveWithOnlyIllegalContinuation_EndsTheTurn () {
            Match match = PinnedRookAboutToCross();

            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_ROOK_1,
                tileId = 36, // e5
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True, "Re4-e5 is a legal move (the rook stays between the pinning rook and the king)");
            Assert.That(result.polarityChanged, Is.True, "crossing into the top half turns the rook into a checker");

            // The single potential continuation jump (e5 x f6 -> g7) is illegal (pin), so
            // there is no legal continuation and the turn must change.
            Board board = match._GetPendingBoard();
            Chessman rook = board.GetChessman(Constants.ID_WHITE_ROOK_1);
            Assert.That(board.GetValidTilesForMovement(rook, jumpsOnly: true), Is.Empty,
                "the only continuation jump is illegal");

            Assert.That(result.turnChanged, Is.True,
                "with no legal continuation the turn must end; otherwise the game deadlocks");
        }
    }
}
