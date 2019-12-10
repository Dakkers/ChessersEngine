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
        public int fromTileId { get; set; }
        public int tileId { get; set; }
        public bool turnChanged { get; set; }
        public int type { get; set; }

        // Whether or not this move triggers a polarity flip (checker piece to chess piece)
        public bool polarityChanged { get; set; }

        // Whether or not this move triggers a king-ing
        public bool kinged { get; set; }

        // Whether or not this move triggers a Promotion
        public bool promotionOccurred { get; set; }
        public ChessmanKindEnum promotionRank { get; set; }

        // Piece that gets jumped, if applicable
        public int jumpedPieceId { get; set; } = -1;

        // Piece that gets captured, if applicable
        public int capturedPieceId { get; set; } = -1;

        public bool isWinningMove { get; set; } = false;

        public bool valid { get; set; }

        public string notation { get; set; }
        public int fromRow { get; set; }
        public int toRow { get; set; }
        public int fromColumn { get; set; }
        public int toColumn { get; set; }
        public ChessmanKindEnum chessmanKind { get; set; }

        public override string ToString () {
            return $"{{ " +
                $"pieceId = {pieceId}, " +
                $"tileId = {tileId}, " +
                $"capturedPieceId = {capturedPieceId}, " +
                $"promotion = {promotionOccurred}, " +
                $"turnChanged = {turnChanged}, " +
                $"isWinningMove = {isWinningMove}, " +
            "}}";
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

        public string CreateNotation () {
            // Start with e.g. "Qh4"
            string moveNotation = (
                Helpers.ConvertChessmanKindToNotationSymbol(chessmanKind) +
                Helpers.ConvertRowToRank(fromRow) +
                Helpers.ConvertColumnToFile(fromColumn)
            );

            if (WasPieceCaptured()) {
                // "Qh4" becomes "Qh4x"
                moveNotation += "x";
            }
            if (WasPieceJumped()) {
                // "Qh4" becomes "Qh4y"
                moveNotation += "y";
            }

            // "Qh4" becomes "Qh4e7"
            moveNotation += (
                Helpers.ConvertRowToRank(toRow) +
                Helpers.ConvertColumnToFile(toColumn)
            );

            if (promotionOccurred) {
                // "h7h8" becomes "h7h8Q"
                moveNotation += Helpers.ConvertChessmanKindToNotationSymbol(promotionRank);
            }

            return moveNotation;
        }
    }
}
