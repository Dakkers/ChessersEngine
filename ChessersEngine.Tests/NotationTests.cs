using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// CreateNotation dereferenced promotionRank whenever promotionOccurred was set. The engine
    /// no longer sets promotionOccurred for a checker being kinged, but a MoveResult that has
    /// promotionOccurred == true with a null promotionRank must still not crash notation.
    /// </summary>
    [TestFixture]
    public class NotationTests {
        [Test]
        public void CreateNotation_DoesNotThrow_WhenPromotionOccurredButNoRank () {
            MoveResult mr = new MoveResult {
                pieceId = 0,
                chessmanKind = ChessmanKindEnum.PAWN,
                fromColumn = 0,
                fromRow = 6,
                toColumn = 0,
                toRow = 7,
                tileId = 56,
                promotionOccurred = true,
                // promotionRank intentionally left null
            };

            Assert.DoesNotThrow(() => mr.CreateNotation());
        }
    }
}
