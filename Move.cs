using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessersEngine {
    public enum Directionalities {
        HORIZONTAL,
        VERTICAL,
        // Bottom-left <-> top-right, i.e. parallel with the line y = x
        POSITIVE_DIAGONAL,
        // Top-left <-> bottom-right, i.e. parallel with the line y = -x
        NEGATIVE_DIAGONAL
    };

    public struct MovementValidationEndResult {
        public readonly Board modifiedBoard;
        public readonly MoveResult moveResult;

        public MovementValidationEndResult (Board b, MoveResult mr) => (modifiedBoard, moveResult) = (b, mr);
    }

    public class Move {
        readonly Board board;

        readonly Chessman chessman;
        readonly Tile fromTile;
        readonly Tile toTile;
        readonly List<Tile> potentialTilesForMovement;

        readonly int delta;

        public readonly int fromRow;
        public readonly int toRow;

        public readonly int fromColumn;
        public readonly int toColumn;

        readonly MoveResult moveResult;

        public Move (Board _board, MoveAttempt moveAttempt) {
            board = _board;

            chessman = board.GetChessman(moveAttempt.pieceId);
            toTile = board.GetTile(moveAttempt.tileId);
            fromTile = chessman.GetUnderlyingTile();

            delta = toTile.id - fromTile.id;

            (fromRow, fromColumn) = board.GetRowColumn(fromTile);
            (toRow, toColumn) = board.GetRowColumn(toTile);

            moveResult = new MoveResult {
                pieceGuid = chessman.guid,
                pieceId = chessman.id,
                tileId = toTile.id,
                fromTileId = fromTile.id,

                fromRow = fromRow,
                toRow = toRow,
                fromColumn = fromColumn,
                toColumn = toColumn,
                chessmanKind = chessman.kind,
            };

            potentialTilesForMovement = board.GetPotentialTilesForMovement(chessman);
            //Match.Log($"Potential for chessman @ {fromTile.id} ({toTile.id}): {string.Join(", ", potentialTilesForMovement.Select((t) => t.id))}");
        }

        #region Move types

        bool IsHorizontalMove () {
            return fromRow == toRow;
        }

        /// <summary>
        /// Determines if the move is a "positive diagonal" move. Positive Diagonal
        /// is the diagonal line from the bottom-left towards the top-right, or
        /// vice versa.
        /// </summary>
        /// <returns><c>true</c>, if positive diagonal move was ised, <c>false</c> otherwise.</returns>
        bool IsPositiveDiagonalMove () {
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

        bool IsNegativeDiagonalMove () {
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

        bool IsVerticalMove () {
            return fromColumn == toColumn;
        }

        bool IsRankIncreasing () {
            return (toRow - fromRow) > 0;
        }

        bool IsRankDecreasing () {
            return !IsRankIncreasing();
        }

        #endregion

        #region Check/Checkmate
        /*

        int ConvertChessmanToSortScore (Chessman c) {
            if (c.IsChecker()) {
                return 1;
            } else if (c.IsPawn()) {
                return 0;
            } else if (c.IsKnight()) {
                return 2;
            } else if (c.IsBishop()) {
                return 3;
            } else if (c.IsRook()) {
                return 4;
            } else if (c.IsQueen()) {
                return 5;
            } else if (c.IsKing()) {
                return 6;
            } else {
                return 0;
            }
        }

        void DetermineCheckMovements () {

        }

        List<Tile> CalculateCheckTiles (int colorId, bool exitEarly) {
            // -- DIRECT ATTACK
            // Check that the king cannot be attacked directly.

            List<Tile> result = new List<Tile>();

            Chessman kingChessman = (colorId == Constants.ID_WHITE) ?
                board.GetWhiteKing() :
                board.GetBlackKing();

            result = result.Concat(board.CanChessmanBeCaptured(kingChessman, exitEarly)).ToList();
            if (exitEarly && result.Count > 0) {
                return result;
            }

            result = result.Concat(board.CanChessmanBeJumped(kingChessman, exitEarly)).ToList();
            if (exitEarly && result.Count > 0) {
                return result;
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
                if (!tile.IsOccupied() || !(tile.GetPiece().IsSameColor(kingChessman))) {
                    tilesToCheckForLanding.Enqueue(tile);
                    checkedTilesForLanding.Add(tile.id, true);
                }
            }

            while (tilesToCheckForLanding.Count > 0) {
                Tile tileToCheck = tilesToCheckForLanding.Dequeue();
                //Match.Log($"Checking tile: {tileToCheck.id}");

                if (tileToCheck.IsOccupied()) {
                    Chessman occupant = tileToCheck.GetPiece();

                    // We found a piece that is of the opposite colour.
                    //
                    if (occupant.colorId != kingChessman.colorId) {
                        result.Add(tileToCheck);
                        if (exitEarly) {
                            return result;
                        } else {
                            continue;
                        }
                    }

                    // If the tile is occupied by a piece of the same colour, then
                    // we have to check if it can be captured.
                    // If it can, then a capture-jump can occur.

                    result = result.Concat(board.CanChessmanBeCaptured(occupant, exitEarly)).ToList();
                    if (exitEarly && result.Count > 0) {
                        return result;
                    }

                    continue;
                }


                List<Tile> potentialJumpLocations = board.GetPotentialJumpLocationsForTile(tileToCheck);
                foreach (Tile potentialJumpTile in potentialJumpLocations) {
                    if (checkedTilesForLanding.ContainsKey(potentialJumpTile.id)) {
                        continue;
                    }

                    //Match.Log($"    Potential jump location: {potentialJumpTile.id}");

                    tilesToCheckForLanding.Enqueue(potentialJumpTile);
                    checkedTilesForLanding.Add(potentialJumpTile.id, true);
                }

            }

            return result;
        }

        /// <summary>
        /// Determines if the moving player is in check. We don't need to know the details of
        /// how they're in check - just whether or not they're in check.
        /// </summary>
        /// <returns><c>true</c>, if moving player in check was ised, <c>false</c> otherwise.</returns>
        bool IsMovingPlayerInCheck () {
            List<Tile> result = (CalculateCheckTiles(chessman.colorId, exitEarly: true));
            return result.Count > 0;
        }

        bool IsOpposingPlayerCheckmated (List<Tile> tilesThatCheckOpposingPlayer) {
            List<Chessman> chessmenForOpposingPlayer = board.GetActiveChessmenOfColor(Helpers.ConvertColorIntToEnum(opposingColor));
            Board boardClone = new Board(board.GetChessmanSchemas());
            boardClone.CopyState(board);
            Match.Log($"Checking for checkmate...");

            // Sort by King, Queen, Rook, Bishop, Knight, Pawn.

            chessmenForOpposingPlayer.Sort((c1, c2) => {
                return ConvertChessmanToSortScore(c1) - ConvertChessmanToSortScore(c2);
            });

            foreach (Chessman c in chessmenForOpposingPlayer) {
                List<Tile> potentialTiles = boardClone.GetPotentialTilesForMovement(c);
                Match.Log($"    Chessman: {c.id} {c.kind}");

                // TODO -- need to recurse
                foreach (Tile t in potentialTiles) {
                    Match.Log($"        Tile: {t.id}");
                    Move testMove = new Move(boardClone, new MoveAttempt {
                        // Don't need a real player ID for pseudo-legal checks.
                        playerId = -1,
                        pieceId = c.id,
                        pieceGuid = c.guid,
                        tileId = t.id
                    });
                    MoveResult mr = testMove.GetPseudoLegalMoveResult();
                    if (mr != null && mr.valid) {
                        return false;
                    }

                    // If this move results in a possible jump, we need to check that too

                    while (!mr.turnChanged) {
                        //TODO
                    }

                    boardClone.CopyState(board);
                }
            }

            return true;
        }

        */

        #endregion

        /// <summary>
        /// Gets the result of the move that was confiugred. This does NOT modify the
        /// match state in any way.
        /// </summary>
        /// <returns>The move result.</returns>
        void ExecuteLegalMove () {
            if (
                !(potentialTilesForMovement.Exists((t) => t.id == toTile.id))
            ) {
                // Not a legal move to the specified tile.
                return;
            }

            moveResult.valid = true;

            if (chessman.IsChecker()) {
                // This was a valid movement - if the piece moved +/- 2 rows/columns, then
                // a piece was jumped.
                if (
                    (Math.Abs(toRow - fromRow) == 2) ||
                    (Math.Abs(toColumn - fromColumn) == 2)
                ) {
                    // `delta` will always be even in this case; this value will represent
                    // the movement amount from `fromTile` to the tile that the jumped piece
                    // was occupying
                    int halfDelta = delta / 2;
                    Tile jumpedTile = board.GetTile(fromTile.id + halfDelta);

                    moveResult.jumpedPieceId = jumpedTile.GetPiece().id;
                    moveResult.jumpedTileId = jumpedTile.id;
                }
            } else {
                if (toTile.IsOccupied()) {
                    moveResult.capturedPieceId = toTile.GetPiece().id;
                }
            }


            // -- Move the piece in a copy of the match's board.
            // We can then check for the Check status.

            if (!chessman.hasMoved) {
                moveResult.wasFirstMoveForPiece = true;
                chessman.SetHasMoved(true);
            }

            fromTile.RemovePiece();
            toTile.SetPiece(chessman);
            chessman.SetUnderlyingTile(toTile);

            //if (IsMovingPlayerInCheck()) {
            //    moveResult.valid = false;
            //    Match.Log("Moving player is in check.");
            //    return;
            //}

            // -- Completely valid!
            PostValidationHandler();

            //List<Tile> tilesThatCheckOpposingPlayer = CalculateCheckTiles(opposingColor, exitEarly: false);

            //// -- Now see if this player has won.
            //if (tilesThatCheckOpposingPlayer.Count > 0) {
            //    Match.Log("Opposing player is in check.");
            //    if (IsOpposingPlayerCheckmated(tilesThatCheckOpposingPlayer)) {
            //        //moveResult.
            //        Match.Log("  Opposing player checkmated!");
            //    }
            //}
        }

        MoveResult GetPseudoLegalMoveResult () {
            // -- Skip all piece-specific validation.

            chessman.SetHasMoved(true);
            fromTile.RemovePiece();
            toTile.SetPiece(chessman);
            chessman.SetUnderlyingTile(toTile);

            //if (IsMovingPlayerInCheck()) {
            //    Match.Log("            Player is in check.");
            //    return null;
            //}

            PostValidationHandler();
            moveResult.valid = true;

            return moveResult;
        }

        public MovementValidationEndResult ExecuteMove () {
            ExecuteLegalMove();

            return new MovementValidationEndResult(board, moveResult);
        }

        /// <summary>
        /// Modifies the MoveResult in-place. Common between pseudo-legal and legal
        /// movement validations. Called after validation succeeds.
        /// </summary>
        void PostValidationHandler () {
            // -- Handle polarity (checker <--> chess piece) changes, if applicable
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

            if (moveResult.polarityChanged) {
                chessman.TogglePolarity();
            }

            // -- Handle promotion for pawn if not already promoted
            if (!chessman.isPromoted && chessman.IsPawn()) {
                if (chessman.IsWhite()) {
                    moveResult.promotionOccurred = (toRow == Constants.RANK_8);
                } else {
                    moveResult.promotionOccurred = (toRow == Constants.RANK_1);
                }
            }

            // -- Handle kinging for checker if not already kinged
            if (!chessman.isKinged) {
                if (chessman.IsWhite()) {
                    moveResult.kinged = (toRow == Constants.RANK_8);
                } else {
                    moveResult.kinged = (toRow == Constants.RANK_1);
                }

                if (moveResult.kinged) {
                    chessman.isKinged = true;
                }
            }

            // -- Handle capturing/jump chessman removal
            Chessman pieceToRemove = null;

            if (moveResult.WasPieceJumped()) {
                pieceToRemove = board.GetChessman(moveResult.jumpedPieceId);
            } else if (moveResult.WasPieceCaptured()) {
                pieceToRemove = board.GetChessman(moveResult.capturedPieceId);
            }

            if (pieceToRemove != null) {
                pieceToRemove.Deactivate();
                if (pieceToRemove.IsKing()) {
                    moveResult.isWinningMove = true;
                }
            }

            // Most moves result in a change of whose turn it is, EXCEPT for jumping
            bool shouldChangeTurns = !moveResult.WasPieceJumped();
            if (
                moveResult.WasPieceCaptured() &&
                moveResult.polarityChanged &&
                chessman.IsChecker()
            ) {
                // The piece moved, and captured another piece, and became a checker in the process
                // We need to check for the potential of a capture-jump
                List<Tile> nextMovePotentialTiles = board.GetPotentialTilesForMovement(chessman);
                foreach (Tile nextTile in nextMovePotentialTiles) {
                    (int nextRowDelta, int nextColDelta) = board.CalculateRowColumnDelta(nextTile, toTile);

                    if (nextRowDelta == 2 || nextColDelta == 2) {
                        shouldChangeTurns = false;
                        break;
                    }
                }
            }

            if (shouldChangeTurns) {
                moveResult.turnChanged = true;
            }
        }
    }
}
