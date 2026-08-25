using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// The enemy king attacks all 8 adjacent squares. The check primitive
    /// (CanChessmanBeCaptured) used to ignore the king entirely, so two kings
    /// could stand adjacent and a "checkmated" king could escape by capturing a
    /// piece defended only by the enemy king.
    /// </summary>
    [TestFixture]
    public class KingThreatTests {
        static Match MakeMatch (List<ChessmanSchema> pieces, ColorEnum turn) {
            MatchData md = new MatchData {
                currentTurn = turn,
                matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                pieces = pieces,
            };
            return new Match(md);
        }

        [Test]
        public void King_CannotMoveAdjacentToEnemyKing () {
            // White king a1 (0), black king c1 (2), white to move.
            Match match = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(0),
                TestScenarios.CreateBlackKing(2),
            }, ColorEnum.WHITE);

            Board board = match._GetPendingBoard();
            Chessman whiteKing = board.GetChessman(Constants.ID_WHITE_KING);
            List<int> validTileIds = board.GetValidTilesForMovement(whiteKing).Select(t => t.id).ToList();

            // b1 (1) and b2 (9) are both adjacent to the black king on c1 -> illegal.
            Assert.That(validTileIds, Does.Not.Contain(1), "b1 is adjacent to the enemy king");
            Assert.That(validTileIds, Does.Not.Contain(9), "b2 is diagonally adjacent to the enemy king");
            // a2 (8) is two columns from c1 -> legal.
            Assert.That(validTileIds, Does.Contain(8), "a2 is a legal, non-adjacent king move");
        }

        [Test]
        public void KingDefendedEscapeSquares_YieldCheckmate () {
            // All white pieces stay in the bottom half (rows 0-3) so none flip to checkers.
            // Black king on tile 32 (row4,col0). White king tile 26 (row3,col2) guards the
            // escape squares 33 and 25 (adjacent to it); white bishop tile 27 guards 41;
            // white rook slides to tile 0 giving check up column 0, which also guards 24/40.
            // With the king counted as a defender, black has no escape -> checkmate.
            // Without it, black escapes to 33 or 25.
            Match match = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(26),
                TestScenarios.CreateRook(Constants.ID_WHITE_ROOK_1, 2),
                TestScenarios.CreateBishop(Constants.ID_WHITE_BISHOP_1, 27),
                TestScenarios.CreateBlackKing(32),
            }, ColorEnum.WHITE);

            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_ROOK_1,
                tileId = 0,
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True);
            Assert.That(result.isInCheck, Is.True, "the rook checks the black king up column 0");
            Assert.That(result.isWinningMove, Is.True,
                "mate: the black king's only escapes (33, 25) are defended by the white king");
        }
    }
}
