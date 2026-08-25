using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// A king may not castle through a square that is under attack. The old safety
    /// loop mutated a detached schema and then discarded it via CreateCopy(), so it
    /// never actually placed the king on the pass-through squares -> it was a no-op
    /// and castling through check was allowed. It also scanned every square between
    /// the rook and king, which (once it actually worked) would wrongly forbid a
    /// queenside castle when only the b-file square is attacked.
    /// </summary>
    [TestFixture]
    public class CastlingSafetyTests {
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

        static List<int> WhiteKingValidTiles (Match match) {
            Board board = match._GetPendingBoard();
            Chessman whiteKing = board.GetChessman(Constants.ID_WHITE_KING);
            return board.GetValidTilesForMovement(whiteKing).Select(t => t.id).ToList();
        }

        [Test]
        public void CannotCastleThroughAttackedSquare () {
            // White king e1(4), white rook h1(7). Black rook f8(61) attacks straight down
            // the f-file onto f1(5) - the square the king passes over when castling kingside.
            // g1(6) and e1(4) are not attacked, so this is specifically an illegal
            // pass-through, not castling out of / into check.
            Match match = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(4),
                TestScenarios.CreateRook(Constants.ID_WHITE_ROOK_2, 7),
                TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_1, 61),
                TestScenarios.CreateBlackKing(60),
            });

            Assert.That(WhiteKingValidTiles(match), Does.Not.Contain(6),
                "kingside castling passes over the attacked f1 square and must be forbidden");
        }

        [Test]
        public void CanCastleWhenNothingIsAttacked () {
            // Sanity: with no attackers, both castles are offered (tiles 2 and 6).
            Match match = new Match(TestScenarios.CastlingWhite());
            List<int> valid = WhiteKingValidTiles(match);
            Assert.That(valid, Does.Contain(6), "kingside castle should be legal");
            Assert.That(valid, Does.Contain(2), "queenside castle should be legal");
        }

        [Test]
        public void CanCastleQueensideWhenOnlyBFileSquareIsAttacked () {
            // White king e1(4), white rook a1(0). Black rook b8(57) attacks down the b-file
            // onto b1(1). The king's queenside path is d1(3) -> c1(2); it never touches b1,
            // so the castle is legal even though b1 is attacked.
            Match match = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(4),
                TestScenarios.CreateRook(Constants.ID_WHITE_ROOK_1, 0),
                TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_1, 57),
                TestScenarios.CreateBlackKing(60),
            });

            Assert.That(WhiteKingValidTiles(match), Does.Contain(2),
                "queenside castle is legal; b1 being attacked is irrelevant (king never crosses it)");
        }
    }
}
