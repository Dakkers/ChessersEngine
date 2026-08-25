using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// A jump that captures a king wins the game, so the turn (and game) must end there.
    /// PostValidationHandler still ran its multi-jump continuation check, so if the capturing
    /// checker had a further jump available the move reported turnChanged = false and play
    /// continued past the win - onto a board whose king is gone, which later crashed check
    /// detection. Found by the random-game fuzzer.
    /// </summary>
    [TestFixture]
    public class WinningMoveTests {
        [Test]
        public void CheckerJumpThatCapturesKing_EndsTheTurn () {
            // White checker d4(27) jumps the black king e5(36) and lands f6(45); from there it
            // still has a further jump (over the black pawn g7(54) to h8(63)).
            ChessmanSchema whiteChecker = TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 27);
            whiteChecker.isChecker = true;

            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE, matchId = 1,
                whitePlayerId = 0, blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(4),
                    whiteChecker,
                    TestScenarios.CreateBlackKing(36),
                    TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_1, 54),
                },
            };
            Match match = new Match(md);

            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = 0, pieceId = Constants.ID_WHITE_PAWN_1, tileId = 45,
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True);
            Assert.That(result.isWinningMove, Is.True, "the jump captured the king");
            Assert.That(result.turnChanged, Is.True, "capturing a king ends the game; there is no multi-jump continuation");

            match.CommitTurn();
            Assert.That(match.HasWinner(), Is.True, "the game must register the win");
        }
    }
}
