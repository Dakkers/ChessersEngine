using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// Pawn forward-move generation used GetTile(tile.id +/- 8/16), which throws
    /// KeyNotFoundException for ids outside the board (-36..63). A (non-checker)
    /// pawn loaded onto a far row via MatchData would crash any position scan.
    /// </summary>
    [TestFixture]
    public class PawnMovementTests {
        [Test]
        public void NonCheckerPawnOnFarRow_DoesNotThrow () {
            // A non-checker white pawn on row 6 (tile 48). row6 + 2 == row8 is off the
            // board, so the old unchecked GetTile(48 + 16 = 64) threw.
            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 48),
                    TestScenarios.CreateBlackKing(60),
                },
            };
            Match match = new Match(md);
            Board board = match._GetPendingBoard();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            Assert.DoesNotThrow(() => board.GetPotentialTilesForMovement(pawn));
        }

        [Test]
        public void PawnOnStartingRow_StillHasSingleAndDoubleAdvance () {
            // Regression guard: the default board's white pawn 1 (tile 8) advances to 16/24.
            Match match = new Match(null);
            Board board = match._GetPendingBoard();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            List<int> ids = board.GetPotentialTilesForMovement(pawn).Select(t => t.id).ToList();
            Assert.That(ids, Does.Contain(16), "single advance");
            Assert.That(ids, Does.Contain(24), "double advance");
        }
    }
}
