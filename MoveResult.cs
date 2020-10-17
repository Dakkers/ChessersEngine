using System.Collections.Generic;

namespace ChessersEngine {
    public class MoveAttempt {
        public int pieceGuid { get; set; }
        public int pieceId { get; set; }
        public int playerId { get; set; }
        public int promotionRank { get; set; } = -1;
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
        public int playerId { get; set; }

        public int pieceId { get; set; }
        public int pieceGuid { get; set; }
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
        /// Whether or not this move triggers a king-ing of a checkers piece
        /// </summary>
        /// <value><c>true</c> if kinged; otherwise, <c>false</c>.</value>
        public bool kinged { get; set; }

        /// <summary>
        /// Whether or not this move is a castle-ing of the king.
        /// </summary>
        /// <value><c>true</c> if is castle; otherwise, <c>false</c>.</value>
        public bool isCastle { get; set; }

        // Whether or not this move triggers a Promotion
        public bool promotionOccurred { get; set; }
        public ChessmanKindEnum? promotionRank { get; set; }

        // Piece that gets jumped, if applicable
        public bool wasPieceJumped { get; set; } = false;
        public int jumpedPieceId { get; set; } = -1;
        public int jumpedTileId { get; set; } = -1;

        // Piece that gets captured, if applicable
        public bool wasPieceCaptured { get; set; } = false;
        public int capturedPieceId { get; set; } = -1;

        public bool isWinningMove { get; set; } = false;
        public bool isInCheck { get; set; } = false;
        public bool isStalemate { get; set; } = false;

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
                //$"promotion = {promotionOccurred}, " +
                $"turnChanged = {turnChanged}, " +
                $"isInCheck = {isInCheck}, " +
                $"isWinningMove = {isWinningMove}, " +
                $"isCastle = {isCastle}, " +
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

        #region Notation

        public string CreateNotation () {
            string moveNotation = pieceId.ToString() + "_";

            int colDelta = (toColumn - fromColumn);
            if (isCastle) {
                if (colDelta > 0) {
                    // Kingside/"short" castle
                    moveNotation += "O-O";
                } else {
                    moveNotation += "O-O-O";
                }

                return moveNotation;
            }

            // Start with e.g. "11_Qh4"
            moveNotation += (
                Helpers.ConvertChessmanKindToNotationSymbol(chessmanKind) +
                Helpers.ConvertColumnToFile(fromColumn) +
                Helpers.ConvertRowToRank(fromRow)
            );

            if (WasPieceCaptured()) {
                // "11_Qh4" becomes "11_Qh4x"
                moveNotation += "x";
            }
            if (WasPieceJumped()) {
                // "11_Qh4" becomes "11_Qh4y"
                moveNotation += "y";
            }

            if (tileId < 0) {
                // Deathjump; "11_Qh4" becomes "11_Qh409"
                moveNotation += System.Math.Abs(tileId).ToString().PadLeft(2, '0');
            } else {
                // "Qh4" becomes "Qh4e7"
                moveNotation += (
                    Helpers.ConvertColumnToFile(toColumn) +
                    Helpers.ConvertRowToRank(toRow)
                );
            }

            if (promotionOccurred) {
                // "h7h8" becomes "h7h8Q"
                moveNotation += Helpers.ConvertChessmanKindToNotationSymbol((ChessmanKindEnum) promotionRank);
            }

            return moveNotation;
        }

        /// <summary>
        /// Create a move result based off of <paramref name="_notation"/>. It is NOT possible
        /// to know which color this move was for from the notation alone; more context is required.
        /// </summary>
        /// <returns>The notation.</returns>
        /// <param name="_notation">Notation.</param>
        public static MoveResult CreatePartialMoveResultFromNotation (string _notation) {
            MoveResult moveResult = new MoveResult();
            string[] notationSplit = _notation.Split('_');

            moveResult.pieceId = int.Parse(notationSplit[0]);
            string notation = notationSplit[1];

            bool _isCastle = (notation == "O-O") || (notation == "O-O-O");

            if (_isCastle) {
                moveResult.chessmanKind = ChessmanKindEnum.KING;
                moveResult.isCastle = true;
                moveResult.fromColumn = 4;
                moveResult.toColumn = (notation == "O-O") ? 6 : 2;

                return moveResult;
            }

            Queue<char> chars = new Queue<char>(notation);

            // -- First letter = the chessman kind that moved
            char kindChar = chars.Dequeue();
            moveResult.chessmanKind = Helpers.ConvertNotationSymbolToChessmanKind(kindChar);

            // -- Second/third letter = FROM tile (file/rank)
            char fromFile = chars.Dequeue();
            moveResult.fromColumn = Helpers.ConvertFileToColumn(fromFile);
            char fromRank = chars.Dequeue();
            moveResult.fromRow = Helpers.ConvertRankToRow(fromRank);

            // -- Fourth letter can be capture or jump symbol (x, y respectively) so if it's either
            // of those, the "to" file is actually the letter after that.
            {
                char nextChar = chars.Peek();
                if (nextChar == 'x') {
                    moveResult.wasPieceCaptured = true;
                    chars.Dequeue();
                } else if (nextChar == 'y') {
                    moveResult.wasPieceJumped = true;
                    chars.Dequeue();
                }
            }

            // For the "to" tile, if it was a deathjump then it will be 2 numbers.
            // If it's any other kind of movement then it will be letter-number.
            {
                char nextChar = chars.Dequeue();
                bool isInt = int.TryParse(nextChar.ToString(), out int charAsInt);

                if (isInt) {
                    int secondDigit = int.Parse(chars.Dequeue().ToString());
                    int deathjumpTileId = -1 * ((10 * charAsInt) + secondDigit);

                    moveResult.toColumn = Helpers.GetColumn(deathjumpTileId);
                    moveResult.toRow = Helpers.GetRow(deathjumpTileId);
                } else {
                    moveResult.toColumn = Helpers.ConvertFileToColumn(nextChar);
                    moveResult.toRow = Helpers.ConvertRankToRow(chars.Dequeue());
                }
            }

            // -- Last letter could be promotion
            if (chars.Count > 0) {
                char promotionRank = chars.Dequeue();
                moveResult.promotionRank = Helpers.ConvertNotationSymbolToChessmanKind(promotionRank);
            }

            return moveResult;
        }

        #endregion

        #region Castling

        public (int, int) GetRookCastleTileFromCoords () {
            if (!isCastle) {
                return (-1, -1);
            }
            return toColumn == 6 ? (toRow, 7) : (toRow, 0);
        }

        public (int, int) GetRookCastleTileToCoords () {
            if (!isCastle) {
                return (-1, -1);
            }
            return toColumn == 6 ? (toRow, 5) : (toRow, 3);
        }

        public int GetCastlingRookId () {
            if (!isCastle) {
                return -1;
            } else if (Helpers.GetColorFromPieceId(pieceId) == ColorEnum.BLACK) {
                return (toColumn == 6) ? Constants.ID_BLACK_ROOK_2 : Constants.ID_BLACK_ROOK_1;
            } else {
                return (toColumn == 6) ? Constants.ID_WHITE_ROOK_2 : Constants.ID_WHITE_ROOK_1;
            }
        }

        #endregion

        public MoveAttempt CreateMoveAttempt () {
            return new MoveAttempt {
                pieceId = pieceId,
                playerId = playerId,
                pieceGuid = pieceGuid,
                tileId = tileId,

                promotionRank = promotionRank == null ? -1 : (int) promotionRank,
            };
        }
    }
}
