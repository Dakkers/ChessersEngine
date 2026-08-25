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
    }
}
