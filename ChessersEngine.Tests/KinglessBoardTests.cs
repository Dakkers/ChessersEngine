using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// Some positions (e.g. partial TestScenarios fixtures) have only one king on
    /// the board. Board.IsGameOver() / Board.CalculateBoardValue() used to look the
    /// king up via chessmenById[id], which threw KeyNotFoundException when a king was
    /// absent. A missing king is now treated the same as a captured one: that side
    /// has lost.
    /// </summary>
    [TestFixture]
    public class KinglessBoardTests {
        static Match MakeMatch (List<ChessmanSchema> pieces) {
            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                pieces = pieces,
            };
            return new Match(md);
        }

        [Test]
        public void IsGameOverDoesNotThrowWhenBlackKingIsMissing () {
            // InCheckFromJump has a white king but no black king - the fixture that
            // originally triggered the KeyNotFoundException crash.
            Board board = new Match(TestScenarios.InCheckFromJump())._GetPendingBoard();

            bool gameOver = false;
            Assert.DoesNotThrow(() => gameOver = board.IsGameOver(),
                "a board missing the black king must not throw");
            Assert.That(gameOver, Is.True, "a side with no king has lost, so the game is over");
        }

        [Test]
        public void CalculateBoardValueFavorsSideWhoseOpponentHasNoKing () {
            // White king present, black king missing -> black has lost -> maximally
            // good for white.
            Board board = new Match(TestScenarios.InCheckFromJump())._GetPendingBoard();

            int value = 0;
            Assert.DoesNotThrow(() => value = board.CalculateBoardValue(0),
                "a board missing the black king must not throw");
            Assert.That(value, Is.EqualTo(int.MaxValue),
                "black king gone means black has lost -> best possible value for white");
        }

        [Test]
        public void CalculateBoardValuePenalizesSideWithNoKing () {
            // Black king present, white king missing -> white has lost -> maximally
            // bad for white.
            Board board = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateBlackKing(60),
                TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 8),
            })._GetPendingBoard();

            int value = 0;
            Assert.DoesNotThrow(() => value = board.CalculateBoardValue(0),
                "a board missing the white king must not throw");
            Assert.That(board.IsGameOver(), Is.True, "white has no king, so the game is over");
            Assert.That(value, Is.EqualTo(int.MinValue),
                "white king gone means white has lost -> worst possible value for white");
        }

        [Test]
        public void IsGameOverIsFalseWithBothKingsPresent () {
            // Regression guard: normal two-king positions still report an ongoing game.
            Board board = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(4),
                TestScenarios.CreateBlackKing(60),
            })._GetPendingBoard();

            Assert.That(board.IsGameOver(), Is.False,
                "both kings active means the game is not over");
        }
    }
}
