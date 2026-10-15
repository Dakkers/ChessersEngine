using System;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// Board.UndoMove must fully restore the board. Castling set the rook's hasMoved
    /// flag but UndoMove never reset it, so after the AI search tried and undid a
    /// castle, that rook was stuck "moved" and castling was wrongly suppressed for
    /// the rest of the search.
    /// </summary>
    [TestFixture]
    public class UndoMoveTests {
        [Test]
        public void UndoCastle_RestoresRookHasMovedAndPositions () {
            Match match = new Match(TestScenarios.CastlingWhite());
            Board board = match._GetPendingBoard();

            // Kingside castle: king e1 (4) -> g1 (6); rook h1 (7, rook 2) -> f1 (5).
            MoveResult mr = board.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_KING,
                tileId = 6,
            });
            Assert.That(mr.valid, Is.True);
            Assert.That(mr.isCastle, Is.True);

            board.UndoMove(mr);

            Chessman king = board.GetChessman(Constants.ID_WHITE_KING);
            Chessman rook = board.GetChessman(Constants.ID_WHITE_ROOK_2);

            Assert.That(king.hasMoved, Is.False, "the king's hasMoved is restored");
            Assert.That(rook.hasMoved, Is.False, "the castling rook's hasMoved must be restored too");
            Assert.That(king.GetUnderlyingTile().id, Is.EqualTo(4), "king back on e1");
            Assert.That(rook.GetUnderlyingTile().id, Is.EqualTo(7), "rook back on h1");
        }

        [Test]
        public void UndoCastle_WithEmptyRookTile_ThrowsAndLeavesBoardUntouched () {
            Match match = new Match(TestScenarios.CastlingWhite());
            Board board = match._GetPendingBoard();

            MoveResult mr = board.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_KING,
                tileId = 6,
            });
            Assert.That(mr.isCastle, Is.True);

            board.Clear(board.GetTile(5));

            Assert.Throws<InvalidOperationException>(() => board.UndoMove(mr));

            Chessman king = board.GetChessman(Constants.ID_WHITE_KING);
            Assert.That(board.TileOf(king).id, Is.EqualTo(6), "king stays on g1");
            Assert.That(board.PieceAt(board.GetTile(6)), Is.SameAs(king), "g1 still holds the king");
            Assert.That(board.IsOccupied(board.GetTile(4)), Is.False, "e1 stays empty");
            Assert.That(king.hasMoved, Is.True, "king hasMoved is not reset");
        }

        static void AssertUndoCastleThrowsWhenRookReplacedBy (int replacementId) {
            MatchData md = TestScenarios.CastlingWhite();
            md.pieces.Add(TestScenarios.CreateWhiteQueen(3));
            md.pieces.Add(TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_1, 56));
            Match match = new Match(md);
            Board board = match._GetPendingBoard();

            MoveResult mr = board.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_KING,
                tileId = 6,
            });
            Assert.That(mr.isCastle, Is.True);

            Tile rookTile = board.GetTile(5);
            Chessman replacement = board.GetChessman(replacementId);
            board.Clear(rookTile);
            board.Clear(board.TileOf(replacement));
            board.Place(replacement, rookTile);

            Assert.Throws<InvalidOperationException>(() => board.UndoMove(mr));

            Chessman king = board.GetChessman(Constants.ID_WHITE_KING);
            Assert.That(board.TileOf(king).id, Is.EqualTo(6), "king stays on g1");
            Assert.That(board.PieceAt(rookTile).id, Is.EqualTo(replacementId), "f1 keeps the replacement");
        }

        [Test]
        public void UndoCastle_WithQueenOnRookTile_Throws () {
            AssertUndoCastleThrowsWhenRookReplacedBy(Constants.ID_WHITE_QUEEN);
        }

        [Test]
        public void UndoCastle_WithOpponentRookOnRookTile_Throws () {
            AssertUndoCastleThrowsWhenRookReplacedBy(Constants.ID_BLACK_ROOK_1);
        }
    }
}
