using System.Collections.Generic;

namespace ChessersEngine {
    public class MoveAttempt {
        public long playerId { get; set; }
        public int pieceId { get; set; }
        public long pieceGuid { get; set; }
        public int tileId { get; set; }

        public override string ToString () {
            return $"{{ " +
                $"pieceId = {pieceId}, " +
                $"tileId = {tileId}, " +
                $"playerId = {playerId}, " +
            "}}";
        }
    }

    public class MoveResult {
        public long playerId { get; set; }

        public int pieceId { get; set; }
        public long pieceGuid { get; set; }
        public int fromTileId { get; set; }
        public int tileId { get; set; }
        public bool turnChanged { get; set; }
        public int type { get; set; }

        /// <summary>
        /// Whether or not this move triggers a polarity flip (checker piece to chess piece or vice versa)
        /// </summary>
        /// <value><c>true</c> if polarity changed; otherwise, <c>false</c>.</value>
        public bool polarityChanged { get; set; }

        /// <summary>
        /// Whether or not this move was the first time the piece had moved.
        /// </summary>
        /// <value><c>true</c> if was first move for piece; otherwise, <c>false</c>.</value>
        public bool wasFirstMoveForPiece { get; set; }

        /// <summary>
        /// Whether or not this move triggers a king-ing
        /// </summary>
        /// <value><c>true</c> if kinged; otherwise, <c>false</c>.</value>
        public bool kinged { get; set; }

        // Whether or not this move triggers a Promotion
        public bool promotionOccurred { get; set; }
        public ChessmanKindEnum promotionRank { get; set; }

        // Piece that gets jumped, if applicable
        public int jumpedPieceId { get; set; } = -1;
        public int jumpedTileId { get; set; } = -1;

        // Piece that gets captured, if applicable
        public int capturedPieceId { get; set; } = -1;

        public bool isWinningMove { get; set; } = false;

        public bool valid { get; set; }

        public string notation { get; set; }
        public int fromRow { get; set; }
        public int toRow { get; set; }
        public int fromColumn { get; set; }
        public int toColumn { get; set; }
        /// <summary>
        /// This is the kind the chessman STARTED as. So, if a pawn was promoted
        /// then this would be PAWN and `promotionRank` would be e.g. QUEEN.
        /// </summary>
        /// <value>The kind of the chessman.</value>
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
                Helpers.ConvertColumnToFile(fromColumn) +
                Helpers.ConvertRowToRank(fromRow)
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
                Helpers.ConvertColumnToFile(toColumn) +
                Helpers.ConvertRowToRank(toRow)
            );

            if (promotionOccurred) {
                // "h7h8" becomes "h7h8Q"
                moveNotation += Helpers.ConvertChessmanKindToNotationSymbol(promotionRank);
            }

            return moveNotation;
        }

        /// <summary>
        /// Create a move result based off of <paramref name="notation"/>. It is NOT possible
        /// to know which color this move was for from the notation alone; more context is required.
        /// </summary>
        /// <returns>The notation.</returns>
        /// <param name="notation">Notation.</param>
        public static MoveResult CreatePartialMoveResultFromNotation (string notation) {
            MoveResult moveResult = new MoveResult();

            Queue<char> chars = new Queue<char>(notation);
            char letter;

            // -- First letter = the chessman kind that moved
            char kindChar = chars.Dequeue();
            ChessmanKindEnum kind = ChessmanKindEnum.PAWN;

            switch (kindChar) {
                case 'P':
                    kind = ChessmanKindEnum.PAWN;
                    break;
                case 'R':
                    kind = ChessmanKindEnum.ROOK;
                    break;
                case 'N':
                    kind = ChessmanKindEnum.KNIGHT;
                    break;
                case 'K':
                    kind = ChessmanKindEnum.KING;
                    break;
                case 'Q':
                    kind = ChessmanKindEnum.QUEEN;
                    break;
                case 'B':
                    kind = ChessmanKindEnum.BISHOP;
                    break;
                default:
                    throw new System.Exception($"Invalid chessman letter: {notation[0]}");
            }

            moveResult.chessmanKind = kind;

            // -- Second/third letter = FROM tile (file/rank)
            char fromFile = chars.Dequeue();
            moveResult.fromColumn = Helpers.ConvertFileToColumn(fromFile);
            char fromRank = chars.Dequeue();
            moveResult.fromRow = Helpers.ConvertRankToRow(fromRank);

            // -- Fourth letter can be jump or capture symbol
            char toFile = chars.Dequeue();
            if (toFile == 'x' || toFile == 'y') {
                toFile = chars.Dequeue();
            }

            moveResult.toColumn = Helpers.ConvertFileToColumn(toFile);
            char toRank = chars.Dequeue();
            moveResult.toRow = Helpers.ConvertRankToRow(toRank);

            return moveResult;
        }
    }
}
