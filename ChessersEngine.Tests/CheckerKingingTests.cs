using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// A pawn that crosses the midline becomes a checker but keeps kind == PAWN.
    /// When such a checker reaches the far back rank it must be KINGED (like any
    /// checker), NOT promoted like a pawn. The old code flagged promotionOccurred
    /// for it, which (a) crashed CreateNotation() at commit (null promotionRank) and
    /// (b) let the AI promote a checker into a chess queen.
    /// </summary>
    [TestFixture]
    public class CheckerKingingTests {
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

        static ChessmanSchema WhiteCheckerPawnAt (int location) {
            ChessmanSchema cs = TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, location);
            cs.isChecker = true;
            return cs;
        }

        static List<ChessmanSchema> ScenarioWithCheckerPawnOnB7 () {
            return new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(0),
                WhiteCheckerPawnAt(49), // b7
                TestScenarios.CreateBlackKing(60),
            };
        }

        [Test]
        public void CheckerPawnReachingBackRank_IsKingedNotPromoted () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            // b7 (49) -> c8 (58): forward-diagonal checker move onto the back rank.
            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = 58,
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True, "reaching the back rank as a checker is a legal move");
            Assert.That(result.kinged, Is.True, "a checker reaching the back rank should be kinged");
            Assert.That(result.promotionOccurred, Is.False, "a checker is kinged, not promoted");
        }

        [Test]
        public void CommittingKingingMove_DoesNotThrow () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = 58,
            });

            // CommitTurn -> CreateNotation, which threw InvalidOperationException on the
            // null promotionRank when promotionOccurred was wrongly set.
            Assert.DoesNotThrow(() => match.CommitTurn());

            Chessman piece = match.GetCommittedChessman(Constants.ID_WHITE_PAWN_1);
            Assert.That(piece.isKinged, Is.True);
            Assert.That(piece.isChecker, Is.True);
            Assert.That(piece.kind, Is.EqualTo(ChessmanKindEnum.PAWN), "a kinged checker stays a checker, not a promoted piece");
            Assert.That(piece.isPromoted, Is.False);
        }

        [Test]
        public void CanBePromoted_IsFalseForChecker () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            Board board = match._GetPendingBoard();
            Chessman piece = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Tile backRankTile = board.GetTile(58);

            Assert.That(Helpers.CanBePromoted(piece, backRankTile), Is.False,
                "a checker should never be treated as a promotable pawn");
        }
    }
}
