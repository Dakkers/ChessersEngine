using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessersEngine {
    public enum Directionality {
        HORIZONTAL,
        VERTICAL,
        // Bottom-left <-> top-right, i.e. parallel with the line y = x
        POSITIVE_DIAGONAL,
        // Top-left <-> bottom-right, i.e. parallel with the line y = -x
        NEGATIVE_DIAGONAL,
        HORIZONTAL_LEFT,
        HORIZONTAL_RIGHT,
        VERTICAL_ABOVE,
        VERTICAL_BELOW,
        POSITIVE_DIAGONAL_ABOVE,
        POSITIVE_DIAGONAL_BELOW,
        NEGATIVE_DIAGONAL_ABOVE,
        NEGATIVE_DIAGONAL_BELOW,
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

        void _RecursivelyFuckMeUp (
            List<List<int>> result,
            List<int> currentPath,
            ColorEnum targetColor,
            Tile tile,
            HashSet<int> tilesToIgnore,
            int depth = 1
        ) {
            //Match.Log($"Current: {tile.id}", depth);
            if (tile.IsOccupied()) {
                // Regardless of which color piece is occupying the tile, this branch of the search
                // tree ends as there is no way to jump onto an occupied tile.

                if (!tile.GetPiece().IsSameColor(targetColor)) {
                    Match.Log("Found a potential multijump...", depth + 1);
                    // This is the piece that could capture the king!
                    result.Add(new List<int>(currentPath));
                } else {
                    //Match.Log("Found a potential capturejump...", depth + 1);
                    // This piece could lead to a capture jump if it has the potential to be captured

                    List<Tile> capturableTiles = board.CanChessmanBeCaptured(tile.GetPiece());
                    foreach (Tile capturableTile in capturableTiles) {
                        // For each of the tiles this piece can be captured from, add a copy of the
                        // current path with this capturable tile included - we care about all potential
                        // paths instead of just 1 because we need to check later if these paths are
                        // valid (i.e. won't put attacking player in check)
                        result.Add(new List<int>(currentPath) { capturableTile.id });
                    }

                    if (board.CanChessmanBeCaptured(tile.GetPiece()).Count > 0) {
                        result.Add(new List<int>(currentPath));
                    }
                }

                return;
            }

            // Generate tiles that are 2 diagonal spaces away
            (int row, int col) = board.GetRowColumn(tile);

            int deltaNeg = board.GetNegativeDiagonalDelta();
            int deltaPos = board.GetPositiveDiagonalDelta();

            List<Tile> diagTiles = new List<Tile> {
                board.GetTileByRowColumn(row + 2, col + 2),
                board.GetTileByRowColumn(row + 2, col - 2),
                board.GetTileByRowColumn(row - 2, col + 2),
                board.GetTileByRowColumn(row - 2, col - 2),
            };

            int halfwayPointThreshold = (board.GetNumberOfRows() / 2) * board.GetNumberOfColumns();
            //Match.Log($"{row},{col} -- {string.Join(" | ", diagTiles.Select((t) => $"{board.GetRow(t)},{board.GetColumn(t)}"))}");

            // >= 32 is black... 4 * row length

            foreach (Tile diagTile in diagTiles) {
                if (diagTile == null || tilesToIgnore.Contains(diagTile.id)) {
                    continue;
                }

                // The tile needs to be on the opposite side of the attacker in order for the
                // attacker's pieces to actually jump

                if (
                    ((targetColor == ColorEnum.BLACK) && (diagTile.id < halfwayPointThreshold)) ||
                    ((targetColor == ColorEnum.WHITE) && (diagTile.id >= halfwayPointThreshold))
                ) {
                    continue;
                }

                currentPath.Add(diagTile.id);

                _RecursivelyFuckMeUp(
                    result,
                    currentPath,
                    targetColor,
                    diagTile,
                    new HashSet<int>(tilesToIgnore) { diagTile.id },
                    depth: depth + 1
                );

                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }

        List<List<int>> RecursivelyFuckMeUp (
            Tile baseTile,
            ColorEnum targetColor,
            HashSet<int> tilesToIgnore
        ) {
            // Each nested list is a path that the attacking player would take
            // to (capture-)multijump
            List<List<int>> result = new List<List<int>>();

            _RecursivelyFuckMeUp(
                result,
                currentPath: new List<int>(),
                targetColor,
                baseTile,
                tilesToIgnore: new HashSet<int>(tilesToIgnore) { baseTile.id }
            );

            return result;
        }

        /// <summary>
        /// Determine which tiles the specified player is in check from. For jumps,
        /// the tile the king will be jumped FROM is returned.
        ///
        /// TODO -- should we return paths instead?
        /// </summary>
        /// <returns>The check tiles.</returns>
        /// <param name="color">Color of the player.</param>
        /// <param name="exitEarly">If set to <c>true</c> exit when the first tile
        /// is found.</param>
        List<Tile> CalculateCheckTiles (ColorEnum color, bool exitEarly) {
            // -- DIRECT ATTACK
            // Check that the king cannot be attacked directly.

            List<Tile> result = new List<Tile>();

            Chessman kingChessman = (color == ColorEnum.WHITE) ?
                board.GetWhiteKing() :
                board.GetBlackKing();

            List<Tile> captureResult = board.CanChessmanBeCaptured(kingChessman).ToList();
            result = result.Concat(captureResult).ToList();
            if (captureResult.Count > 0) {
                //Match.Log($"Can be captured from: {string.Join(", ", captureResult.Select((t) => t.id))}");
            }
            if (exitEarly && result.Count > 0) {
                return result;
            }

            List<Tile> jumpResult = board.CanChessmanBeJumped(kingChessman).ToList();
            result = result.Concat(jumpResult).ToList();
            if (jumpResult.Count > 0) {
                //Match.Log($"Can be captured from: {string.Join(", ", jumpResult.Select((t) => t.id))}");
            }
            if (exitEarly && result.Count > 0) {
                return result;
            }

            // -- Validate jumping (multijumping AND capture-jumping)
            Tile kingTile = kingChessman.GetUnderlyingTile();

            List<Tile> diagTilesOfKing = board.GetDiagonallyAdjacentTiles(kingTile);

            foreach (var diagTile in diagTilesOfKing) {
                // A tile diagonally adjacent from the king could represent one
                // of two things:
                //
                //    1. the tile that a piece lands on BEFORE capturing the king
                //    2. same, but AFTER capturing the king
                //
                // `diagTile` will be #1.

                int diagRow = board.GetRow(diagTile);
                int diagCol = board.GetColumn(diagTile);

                // Get the tile that is 2 diagonal steps away from this tile,
                // in the direction of the king.
                int rowDiff = board.GetRow(kingTile) - diagRow;
                int colDiff = board.GetColumn(kingTile) - diagCol;

                Tile tileToLandOn = board.GetTileByRowColumn(diagRow + (2 * rowDiff), diagCol + (2 * colDiff));

                if (tileToLandOn == null) {
                    // out of bounds
                    continue;
                } else if (tileToLandOn.IsOccupied()) {
                    // Can't jump onto an occupied tile!
                    continue;
                }

                //Match.Log($"Starting at: {diagTile.id}");
                // Ignore the tile to land on because it was including it as part
                // of a potential path, which is encapsulated by a different path
                var crazyResult = RecursivelyFuckMeUp(
                    diagTile,
                    color,
                    new HashSet<int> { tileToLandOn.id }
                );

                foreach (List<int> _path in crazyResult) {
                    List<int> path = new List<int>(_path);
                    path.Reverse();
                    path.Add(diagTile.id);
                    path.Add(tileToLandOn.id);

                    // `path` represents a full set of moves that would win the
                    // game for the attacking player. We have to determine if
                    // excuting these moves would put them in check.
                    //Match.Log(string.Join(", ", path));

                    bool isGood = true;
                    Board boardCopy = board.CreateCopy();
                    for (int i = 0; i < path.Count - 1; i++) {
                        Tile startTile = boardCopy.GetTile(path[i]);
                        Tile endTile = boardCopy.GetTile(path[i + 1]);
                        Chessman movingPiece = startTile.GetPiece();

                        MoveResult _moveResult = boardCopy.MoveChessman(new MoveAttempt {
                            pieceId = movingPiece.id,
                            tileId = endTile.id
                        });

                        if (_moveResult == null || !_moveResult.valid) {
                            isGood = false;
                            break;
                        }
                    }

                    if (isGood) {
                        result.Add(diagTile);
                        if (exitEarly) {
                            return result;
                        }
                    }
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
            List<Tile> result = (CalculateCheckTiles(chessman.color, exitEarly: true));
            return result.Count > 0;
        }

        /*

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

            int rowDelta = toRow - fromRow;
            int colDelta = toColumn - fromColumn;
            moveResult.valid = true;

            if (chessman.IsChecker()) {
                // This was a valid movement - if the piece moved +/- 2 rows/columns, then
                // a piece was jumped.
                if (
                    (Math.Abs(rowDelta) == 2) ||
                    (Math.Abs(colDelta) == 2)
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

            // If this was a castling, the king was the one to move; now move the rook.
            if (
                chessman.IsKing() &&
                (rowDelta == 0) &&
                (Math.Abs(colDelta) == 2)
            ) {
                moveResult.isCastle = true;
                Tile rookFromTile = null;
                Tile rookToTile = null;

                if (colDelta > 0) {
                    rookFromTile = board.GetRightmostTileOfRow(toRow);
                    rookToTile = board.GetTileIfExists(toRow, toColumn - 1);
                } else {
                    rookFromTile = board.GetLeftmostTileOfRow(toRow);
                    rookToTile = board.GetTileIfExists(toRow, toColumn + 1);
                }

                Chessman rookChessman = rookFromTile.GetPiece();
                //Match.Log($"{board.GetRowColumn(rookFromTile)} | {board.GetRowColumn(rookToTile)} | {rookChessman != null}");
                if (rookChessman != null) {
                    rookFromTile.RemovePiece();
                    rookToTile.SetPiece(rookChessman);
                    rookChessman.SetUnderlyingTile(rookToTile);
                }
            }

            if (IsMovingPlayerInCheck()) {
                moveResult.valid = false;
                //Match.Log("Moving player is in check.");
                return;
            }

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

        /// <summary>
        /// Gets the pseudo legal move result.
        /// </summary>
        /// <returns>The pseudo legal move result.</returns>
        MoveResult GetPseudoLegalMoveResult () {
            // -- Skip all piece-specific validation.

            chessman.SetHasMoved(true);
            fromTile.RemovePiece();
            toTile.SetPiece(chessman);
            chessman.SetUnderlyingTile(toTile);

            if (IsMovingPlayerInCheck()) {
                Match.Log("            Attacking player is in check.");
                return null;
            }

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

            // Most moves result in a change of whose turn it is, EXCEPT for jumping (leading to a
            // potential multijump) or for capture-jumping
            bool shouldChangeTurns = true;

            if (
                chessman.IsChecker() && (
                    moveResult.WasPieceJumped() || (
                        moveResult.WasPieceCaptured() &&
                        moveResult.polarityChanged
                    )
                )
            ) {
                // Determine if the piece that moved can now jump another piece (either through regular
                // multijumping or through capture-jumping). In either case the moved piece must still
                // be a checker.
                //
                // It can't just be any potential move after the previous move, it needs to be a jump!
                List<Tile> nextMovePotentialTiles = board.GetPotentialTilesForMovement(chessman);
                foreach (Tile nextTile in nextMovePotentialTiles) {
                    (int nextRowDelta, int nextColDelta) = board.CalculateRowColumnDelta(nextTile, toTile);

                    if (Math.Abs(nextRowDelta) == 2 && Math.Abs(nextColDelta) == 2) {
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
