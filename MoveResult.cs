using System.Collections.Generic;

namespace ChessersEngine {
    public class MoveAttempt {
        public long playerId { get; set; }
        public int pieceId { get; set; }
        public long pieceGuid { get; set; }
        public int tileId { get; set; }
    }

    public class MoveResult {
        public long playerId { get; set; }

        public int pieceId { get; set; }
        public long pieceGuid { get; set; }
        public int tileId { get; set; }
        public bool turnChanged { get; set; }
        public int type { get; set; }

        // Whether or not this move triggers a king-ing
        public bool kinged { get; set; }

        // Whether or not this move triggers a Promotion
        public bool promotionOccurred { get; set; }
        public ChessmanKindEnum promotionRank { get; set; }

        // Piece that gets jumped, if applicable
        public int jumpedPieceId { get; set; } = -1;

        // Piece that gets captured, if applicable
        public int capturedPieceId { get; set; } = -1;

        public bool valid { get; set; }

        public override string ToString () {
            return $"{{ pieceId = {pieceId}, tileId = {tileId}, capturedPieceId = {capturedPieceId}, promotion = {promotionOccurred} }}";
        }

        public bool IsRegular () {
            return type == Constants.MOVE_TYPE_REGULAR;
        }

        public bool WasPieceCaptured () {
            return capturedPieceId != -1;
        }

        public bool WasPieceJumped () {
            return jumpedPieceId != -1;
        }
    }
}
