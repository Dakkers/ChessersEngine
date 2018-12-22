using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessersEngine {
    public enum Directionalities {
        HORIZONTAL,
        VERTICAL,
        // Bottom-left <-> top-right
        POSITIVE_DIAGONAL,
        // Top-left <-> bottom-right
        NEGATIVE_DIAGONAL
    };

    public class Move {
        private Board board;

        private Chessman chessman;
        private Tile fromTile;
        private Tile toTile;
        private int movingColor;

        private int delta;

        private int fromRow;
        private int toRow;

        private int fromColumn;
        private int toColumn;

        private MoveResult moveResult;

        public Move (Board _board, MoveAttempt moveAttempt) {
            board = _board;

            chessman = board.GetChessman(moveAttempt.pieceId);
            toTile = board.GetTile(moveAttempt.tileId);
            fromTile = chessman.GetUnderlyingTile();
            movingColor = chessman.colorId;

            delta = toTile.id - fromTile.id;
            //Match.Log($"delta = {delta}");

            fromRow = Helpers.GetRow(fromTile.id);
            toRow = Helpers.GetRow(toTile.id);

            fromColumn = Helpers.GetColumn(fromTile.id);
            toColumn = Helpers.GetColumn(toTile.id);

            moveResult = new MoveResult {
                pieceGuid = chessman.guid,
                pieceId = chessman.id,
                tileId = toTile.id
            };
        }

        #region Move types

        private bool IsHorizontalMove () {
            return fromRow == toRow;
        }

        /// <summary>
        /// Determines if the move is a "positive diagonal" move. Positive Diagonal
        /// is the diagonal line from the bottom-left towards the top-right, or
        /// vice versa.
        /// </summary>
        /// <returns><c>true</c>, if positive diagonal move was ised, <c>false</c> otherwise.</returns>
        private bool IsPositiveDiagonalMove () {
            if ((Math.Abs(delta) % 9) != 0) {
                return false;
            }

            if (delta > 0) {
                // Bottom-left to top-right; check that the "to" column is to the right of the "from" column
                return toColumn > fromColumn;
            } else {
                // Top-right to bottom-left; check that the "to" column is to the left of the "from" column
                return toColumn < fromColumn;
            }
        }

        private bool IsNegativeDiagonalMove () {
            if ((Math.Abs(delta) % 7) != 0) {
                return false;
            }

            if (delta > 0) {
                // Bottom-right to top-left; check that the "to" column is to the right of the "from" column
                return toColumn < fromColumn;
            } else {
                // Top-left to bottom-right; check that the "to" column is to the left of the "from" column
                return toColumn > fromColumn;
            }
        }

        private bool IsVerticalMove () {
            return fromColumn == toColumn;
        }

        private bool IsRankIncreasing () {
            return (toRow - fromRow) > 0;
        }

        private bool IsRankDecreasing () {
            return !IsRankIncreasing();
        }

        #endregion

        #region Chessman kind-specific move validations

        private MoveResult IsValidBishopMove () {
            // Bishops can move diagonally by any amount, as long as there
            // is not a piece in between its current tile and its target tile

            int directionality;

            if (IsPositiveDiagonalMove()) {
                directionality = (int) Directionalities.POSITIVE_DIAGONAL;
            } else if (IsNegativeDiagonalMove()) {
                directionality = (int) Directionalities.NEGATIVE_DIAGONAL;
            } else {
                return null;
            }

            if (IsPathBlockedByChessman(directionality)) {
                return null;
            }

            if (toTile.IsOccupied()) {
                moveResult.capturedPieceId = toTile.occupant.id;
            }

            moveResult.valid = true;
            return moveResult;
        }

        private MoveResult IsValidCheckerMove () {
            // Spacing represents how many tiles the piece is attempting to move
            // diagonally. When jumping, this value will be 2. When moving regularly,
            // this value will be 1.
            int stepSize = 0;

            // If the piece is not kinged, it can only move in a specific direction.
            // White pieces can move in increasing rank (from bottom to top) whereas
            // black pieces can move in decreasing rank (from top to bottom) only.
            if (!chessman.isKinged) {
                if (chessman.IsWhite()) {
                    if (IsRankDecreasing()) {
                        return null;
                    }
                } else {
                    if (IsRankIncreasing()) {
                        return null;
                    }
                }
            }

            if (IsNegativeDiagonalMove()) {
                stepSize = 7;
            } else if (IsPositiveDiagonalMove()) {
                stepSize = 9;
            } else {
                return null;
            }

            int spacing = (Math.Abs(delta) / stepSize);

            if (spacing == 1) {
                if (toTile.IsOccupied()) {
                    return null;
                }
            } else if (spacing == 2) {
                Tile tileJumpingOver = board.GetTile(fromTile.id + stepSize);
                if (!tileJumpingOver.IsOccupied()) {
                    return null;
                }

                if (tileJumpingOver.GetPiece().colorId == chessman.colorId) {
                    return null;
                }

                moveResult.jumpedPieceId = tileJumpingOver.GetPiece().id;
            } else {
                return null;
            }

            moveResult.valid = true;
            return moveResult;
        }

        private MoveResult IsValidKnightMove () {
            // Knights move either by 2-columns 1-row OR 2-rows 1-column
            // Additionally, they can jump over pieces

            int rowAbsDelta = Math.Abs(fromRow - toRow);
            int columnAbsDelta = Math.Abs(fromColumn - toColumn);

            if (!(
                ((rowAbsDelta == 2) && (columnAbsDelta == 1)) ||
                ((rowAbsDelta == 1) && (columnAbsDelta == 2))
            )) {
                return null;
            }

            if (toTile.IsOccupied()) {
                moveResult.capturedPieceId = toTile.occupant.id;
            }

            moveResult.valid = true;
            return moveResult;
        }

        private MoveResult IsValidPawnMove () {
            List<int> validDeltas = new List<int>();
            bool checkPath = false;

            if (!toTile.IsOccupied()) {
                // Validate regular movement
                // If a pawn hasn't moved yet, it can move forward by 2 spaces for its first move
                validDeltas.Add(8);

                if (!chessman.hasMoved) {
                    validDeltas.Add(16);
                    checkPath = true;
                }
            } else {
                // Validate capturing movement
                validDeltas.Add(7);
                validDeltas.Add(9);

                if (!IsPositiveDiagonalMove() && !IsNegativeDiagonalMove()) {
                    return null;
                }

                moveResult.capturedPieceId = toTile.occupant.id;
            }

            if (chessman.IsBlack()) {
                validDeltas = validDeltas.Select((deltaVal) => -deltaVal).ToList();
            }

            if (!validDeltas.Contains(delta)) {
                return null;
            }

            if (checkPath) {
                if (IsPathBlockedByChessman((int) Directionalities.VERTICAL)) {
                    return null;
                }
            }

            moveResult.valid = true;
            return moveResult;
        }

        private MoveResult IsValidQueenMove () {
            // Queens can move directly up/down by any amount, or left/right by any amount,
            // or diagonally by any amount, as long as there is not a piece in between its
            // current tile and its target tile

            int directionality;

            if (IsHorizontalMove()) {
                directionality = (int) Directionalities.HORIZONTAL;
            } else if (IsVerticalMove()) {
                directionality = (int) Directionalities.VERTICAL;
            } else if (IsPositiveDiagonalMove()) {
                directionality = (int) Directionalities.POSITIVE_DIAGONAL;
            } else if (IsNegativeDiagonalMove()) {
                directionality = (int) Directionalities.NEGATIVE_DIAGONAL;
            } else {
                return null;
            }

            if (IsPathBlockedByChessman(directionality)) {
                return null;
            }

            if (toTile.IsOccupied()) {
                moveResult.capturedPieceId = toTile.occupant.id;
            }

            moveResult.valid = true;
            return moveResult;
        }

        private MoveResult IsValidRookMove () {
            // Rooks can move directly up/down by any amount, or left/right by any amount, as long as there
            // is not a piece in between its current tile and its target tile

            int directionality;

            if (IsHorizontalMove()) {
                directionality = (int) Directionalities.HORIZONTAL;
            } else if (IsVerticalMove()) {
                directionality = (int) Directionalities.VERTICAL;
            } else {
                return null;
            }

            if (IsPathBlockedByChessman(directionality)) {
                return null;
            }

            if (toTile.IsOccupied()) {
                moveResult.capturedPieceId = toTile.occupant.id;
            }

            moveResult.valid = true;
            return moveResult;
        }

        /// <summary>
        /// Determines if the move is a valid move for a king.
        ///
        /// Kings can only move forward or diagonally forward. They are able to
        /// capture pieces in the same manner.
        /// </summary>
        /// <returns>The valid king move.</returns>
        private MoveResult IsValidKingMove () {
            Match.Log("IsValidKingMove()");
            if (!IsVerticalMove() && !IsPositiveDiagonalMove() && !IsNegativeDiagonalMove()) {
                return null;
            }

            List<int> validDeltas = new List<int> { 7, 8, 9 };

            if (chessman.IsBlack()) {
                validDeltas = validDeltas.Select((deltaVal) => -deltaVal).ToList();
            }

            if (!validDeltas.Contains(delta)) {
                return null;
            }

            moveResult.valid = true;
            return moveResult;
        }

        #endregion

        private bool IsPathBlockedByChessman (int directionality) {
            int stepSize = 0;

            if (directionality == (int) Directionalities.HORIZONTAL) {
                stepSize = 1;
            } else if (directionality == (int) Directionalities.VERTICAL) {
                stepSize = 8;
            } else if (directionality == (int) Directionalities.POSITIVE_DIAGONAL) {
                stepSize = 9;
            } else if (directionality == (int) Directionalities.NEGATIVE_DIAGONAL) {
                stepSize = 7;
            }

            // Scenarios:
            //  HORIZONTAL
            //      - left-to-right: e.g. 1 --> 7. Need to check tiles 2 - 6.
            //      - right-to-left: e.g. 6 --> 2. Need to check tiles 5 - 3.
            //  VERTICAL
            //      - top-to-bottom: e.g. 32 --> 8. Need to check tiles [24, 16].
            //      - bottom-to-top: e.g. 16 --> 56. Need to check tiles [24, 32, 40, 48].
            //  POSITIVE_DIAGONAL
            //      - bottomleft-to-topright: e.g. 0 --> 27. Need to check tiles [9, 18]
            //      - topright-to-bottomleft: e.g. 59 --> 32. Need to check tiles [41, 50]
            //  NEGATIVE_DIAGONAL
            //      - bottomright-to-topleft: e.g. 7 --> 21. Need to check tiles [14]
            //      - topleft-to-bottomright: e.g. 60 --> 39. Need to check tiles [53, 46]

            int start, stop;
            if (delta > 0) {
                start = fromTile.id;
                stop = toTile.id;
            } else {
                start = toTile.id;
                stop = fromTile.id;
            }

            for (int tileId = start + stepSize; tileId <= stop - stepSize; tileId += stepSize) {
                if (board.GetTile(tileId).IsOccupied()) {
                    return true;
                }
            }

            return false;
        }

        private bool IsNotInCheck () {
            // -- DIRECT ATTACK
            // Check that the king cannot be attacked directly.

            Chessman kingChessman = (chessman.IsWhite()) ?
                board.GetWhiteKing() :
                     board.GetBlackKing();

            if (
                board.CanChessmanBeCapturedVertically(kingChessman) ||
                board.CanChessmanBeCapturedHorizontally(kingChessman) ||
                board.CanChessmanBeJumped(kingChessman)
            ) {
                return false;
            }

            // -- Validate jumping...
            Tile kingTile = kingChessman.GetUnderlyingTile();

            Queue<Tile> tilesToCheckForLanding = new Queue<Tile>();
            Dictionary<int, bool> checkedTilesForLanding = new Dictionary<int, bool>();
            Dictionary<int, bool> checkedTilesForJumpingOver = new Dictionary<int, bool>();

            //checkedTiles.Add(kingTile.id, true);

            // Note... the tiles we initially search are the diagonally adjacent tiles to the
            // king, but they must be unoccupied OR if they are, they must be of the same colour
            // as the king. We ignored diagonally adjacent tiles with the opposite colour pieces
            // because if they are able to attack the king in any way, the validation would fail
            // above. However, if the validation above passed and we are at this point in the
            // code, those pieces cannot attack the king directly. Due to the mechanics of the
            // game, they also cannot be used in multi-jumps or capture-multi-jump below.
            foreach (var tile in board.GetDiagonallyAdjacentTiles(kingTile)) {
                if (!tile.IsOccupied() || tile.GetPiece().colorId != kingChessman.colorId) {
                    tilesToCheckForLanding.Enqueue(tile);
                    checkedTilesForLanding.Add(tile.id, true);
                }
            }

            while (tilesToCheckForLanding.Count > 0) {
                Tile tileToCheck = tilesToCheckForLanding.Dequeue();
                Match.Log($"Checking tile: {tileToCheck.id}");

                if (tileToCheck.IsOccupied()) {
                    Chessman occupant = tileToCheck.GetPiece();

                    // We found a piece that is of the opposite colour.
                    // 
                    if (occupant.colorId != kingChessman.colorId) {
                        return false;
                    }

                    // If the tile is occupied by a piece of the same colour, then
                    // we have to check if it can be captured.
                    // If it can, then a capture-jump can occur.

                    //TODO -- check direct attack...
                    if (board.CanChessmanBeCaptured(occupant)) {
                        return false;
                    }

                    continue;
                }


                List<Tile> potentialJumpLocations = board.GetPotentialJumpLocationsForTile(tileToCheck);
                foreach (Tile potentialJumpTile in potentialJumpLocations) {
                    if (checkedTilesForLanding.ContainsKey(potentialJumpTile.id)) {
                        continue;
                    }

                    Match.Log($"    Potential jump location: {potentialJumpTile.id}");

                    tilesToCheckForLanding.Enqueue(potentialJumpTile);
                    checkedTilesForLanding.Add(potentialJumpTile.id, true);
                }

            }

            return true;
        }

        /// <summary>
        /// Gets the result of the move that was confiugred. This does NOT modify the
        /// match state in any way.
        /// </summary>
        /// <returns>The move result.</returns>
        public MoveResult GetMoveResult () {
            Func<MoveResult> pieceSpecificValidator = null;

            if (chessman.IsChecker()) {
                pieceSpecificValidator = IsValidCheckerMove;
            } else if (chessman.IsPawn()) {
                pieceSpecificValidator = IsValidPawnMove;
            } else if (chessman.IsRook()) {
                pieceSpecificValidator = IsValidRookMove;
            } else if (chessman.IsKnight()) {
                pieceSpecificValidator = IsValidKnightMove;
            } else if (chessman.IsBishop()) {
                pieceSpecificValidator = IsValidBishopMove;
            } else if (chessman.IsKing()) {
                pieceSpecificValidator = IsValidKingMove;
            } else if (chessman.IsQueen()) {
                pieceSpecificValidator = IsValidQueenMove;
            }

            if (pieceSpecificValidator == null) {
                return null;
            }

            pieceSpecificValidator();

            if (!moveResult.valid) {
                return null;
            }

            // -- Move the piece in a copy of the match's board.
            // We can then check for the Check status.

            chessman.SetHasMoved(true);
            fromTile.RemovePiece();
            toTile.SetPiece(chessman);
            chessman.SetUnderlyingTile(toTile);

            if (!IsNotInCheck()) {
                return null;
            }


            // -- Completely valid!

            // Handle polarity changes, if applicable
            if (chessman.IsWhite()) {
                if (!chessman.IsChecker() && toRow >= Constants.RANK_5) {
                    moveResult.polarityChanged = true;
                }
                if (chessman.IsChecker() && toRow <= Constants.RANK_4) {
                    moveResult.polarityChanged = true;
                }
            } else {
                if (!chessman.IsChecker() && toRow <= Constants.RANK_4) {
                    moveResult.polarityChanged = true;
                }
                if (chessman.IsChecker() && toRow >= Constants.RANK_5) {
                    moveResult.polarityChanged = true;
                }
            }

            // Handle promotion for pawn if not already promoted
            if (!chessman.isPromoted && chessman.IsPawn()) {
                if (chessman.IsWhite()) {
                    moveResult.promotionOccurred = (toRow == Constants.RANK_8);
                } else {
                    moveResult.promotionOccurred = (toRow == Constants.RANK_1);
                }
            }

            // Handle kinging for checker if not already kinged
            if (!chessman.isKinged) {
                if (chessman.IsWhite()) {
                    moveResult.kinged = (toRow == Constants.RANK_8);
                } else {
                    moveResult.kinged = (toRow == Constants.RANK_1);
                }
            }

            // TODO -- verify this move doesn't put in "check" state

            return moveResult;
        }
    }
}
