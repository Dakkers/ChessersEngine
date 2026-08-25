using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// A pawn that crosses the midline becomes a checker (kind stays PAWN). Reaching the far back
    /// rank KINGS it like any checker, and - by design in Chessers - it can ALSO be promoted. The
    /// promotion only takes real effect once the (kinged) checker makes it back to its home half and
    /// flips back to a chess piece, at which point it is the promoted kind (e.g. a queen).
    ///
    /// The only genuine defect here was that a kinging move with no promotion rank produced a
    /// MoveResult that crashed CreateNotation; that is fixed separately (see NotationTests).
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
        public void CheckerPawnReachingBackRank_IsKinged () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            // b7 (49) -> c8 (58): forward-diagonal checker move onto the back rank.
            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = 58,
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True);
            Assert.That(result.kinged, Is.True, "a checker reaching the back rank is kinged");

            Chessman piece = match.GetPendingChessman(Constants.ID_WHITE_PAWN_1);
            Assert.That(piece.isKinged, Is.True);
            Assert.That(piece.isChecker, Is.True, "it is still a checker until it crosses back");
        }

        [Test]
        public void CommittingKingingMove_DoesNotThrow () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = 58,
            });

            // No promotion rank was supplied, so notation must not crash on commit.
            Assert.DoesNotThrow(() => match.CommitTurn());
        }

        [Test]
        public void CheckerPawnOnBackRank_CanBePromoted () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            Board board = match._GetPendingBoard();
            Chessman piece = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Tile backRankTile = board.GetTile(58);

            Assert.That(Helpers.CanBePromoted(piece, backRankTile), Is.True,
                "a checker reaching the back rank may (by design) also be promoted");
        }

        [Test]
        public void CheckerPawnPromotedToQueen_IsQueenKindButStaysAChecker () {
            Match match = MakeMatch(ScenarioWithCheckerPawnOnB7());

            match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = 58,
                promotionRank = (int) ChessmanKindEnum.QUEEN,
            });
            match.CommitTurn();

            Chessman piece = match.GetCommittedChessman(Constants.ID_WHITE_PAWN_1);
            Assert.That(piece.kind, Is.EqualTo(ChessmanKindEnum.QUEEN), "the promotion is recorded on the piece");
            Assert.That(piece.isPromoted, Is.True);
            Assert.That(piece.isKinged, Is.True, "it was also kinged");
            Assert.That(piece.isChecker, Is.True, "but it is still a checker until it crosses back");
        }

        [Test]
        public void PromotedCheckerBecomesChessQueen_WhenCrossingBackToHomeHalf () {
            // A kinged, promoted white checker-queen on b5 (33, row 4 = top half). As a checker it
            // moves diagonally; kinged, so it can move back toward row 0. Moving to a3 (24, row 3 =
            // home half) flips its polarity back to a chess piece - now a real queen.
            ChessmanSchema checkerQueen = new ChessmanSchema {
                id = Constants.ID_WHITE_PAWN_1, colorId = 0, location = 33,
                kind = (int) ChessmanKindEnum.QUEEN, isChecker = true, isKinged = true, isPromoted = true, hasMoved = true,
            };
            Match match = MakeMatch(new List<ChessmanSchema> {
                TestScenarios.CreateWhiteKing(4),
                checkerQueen,
                TestScenarios.CreateBlackKing(60),
            });

            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = 24, // a3
            });
            Assert.That(result.valid, Is.True);
            Assert.That(result.polarityChanged, Is.True, "crossing back to the home half flips the checker to a chess piece");

            Chessman piece = match.GetPendingChessman(Constants.ID_WHITE_PAWN_1);
            Assert.That(piece.isChecker, Is.False, "back in its home half it is a chess piece again");
            Assert.That(piece.kind, Is.EqualTo(ChessmanKindEnum.QUEEN), "and it is the promoted kind: a queen");
        }
    }
}
