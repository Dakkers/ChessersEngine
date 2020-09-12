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

        public Move (Board _board, MoveAttempt moveAttempt, bool jumpsOnly = false) {
            board = _board;

            chessman = board.GetChessman(moveAttempt.pieceId);
            toTile = board.GetTile(moveAttempt.tileId);
            fromTile = chessman.GetUnderlyingTile();

            delta = toTile.id - fromTile.id;

            (fromRow, fromColumn) = board.GetRowColumn(fromTile);
            (toRow, toColumn) = board.GetRowColumn(toTile);

            ChessmanKindEnum? promotionRank = null;
            if (moveAttempt.promotionRank >= 0 && Enum.IsDefined(typeof(ChessmanKindEnum), moveAttempt.promotionRank)) {
                promotionRank = (ChessmanKindEnum?) moveAttempt.promotionRank;
            }

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
                promotionRank = promotionRank,
            };

            potentialTilesForMovement = board.GetPotentialTilesForMovement(chessman, jumpsOnly: jumpsOnly);
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

        void _CalculateJumpPaths (
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
                    //Match.Log("Found a potential multijump...", depth + 1);
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

            // Generate tiles that are 2 diagonal spaces away - tiles that the piece would land on
            (int row, int col) = board.GetRowColumn(tile);

            List<Tile> tilesToLandOn = new List<Tile> {
                board.GetTileByRowColumn(row + 2, col + 2),
                board.GetTileByRowColumn(row + 2, col - 2),
                board.GetTileByRowColumn(row - 2, col + 2),
                board.GetTileByRowColumn(row - 2, col - 2),
            };

            // NOTE: >= 32 is black... 4 * row length
            int halfwayPointThreshold = (board.GetNumberOfRows() / 2) * board.GetNumberOfColumns();
            //Match.Log($"{row},{col} -- {string.Join(" | ", diagTiles.Select((t) => $"{board.GetRow(t)},{board.GetColumn(t)}"))}");

            foreach (Tile tileToLandOn in tilesToLandOn) {
                if (tileToLandOn == null || tilesToIgnore.Contains(tileToLandOn.id)) {
                    continue;
                }

                (int landingRow, int landingCol) = board.GetRowColumn(tileToLandOn);
                Tile tileToJumpOver = board.GetTileByRowColumn(
                    row + (landingRow - row) / 2,
                    col + (landingCol - col) / 2
                );

                if (tileToJumpOver == null || !tileToJumpOver.IsOccupied()) {
                    continue;
                }

                // The tile needs to be on the opposite side of the attacker in order for the
                // attacker's pieces to actually jump
                if (
                    ((targetColor == ColorEnum.BLACK) && (tileToLandOn.id < halfwayPointThreshold)) ||
                    ((targetColor == ColorEnum.WHITE) && (tileToLandOn.id >= halfwayPointThreshold))
                ) {
                    continue;
                }

                currentPath.Add(tileToLandOn.id);

                _CalculateJumpPaths(
                    result,
                    currentPath,
                    targetColor,
                    tileToLandOn,
                    new HashSet<int>(tilesToIgnore) { tileToLandOn.id },
                    depth: depth + 1
                );

                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }

        List<List<int>> CalculateJumpPaths (
            Tile baseTile,
            ColorEnum targetColor,
            HashSet<int> tilesToIgnore
        ) {
            //Match.Log($"CalculateJumpPaths() {baseTile.id}");
            // Each nested list is a path that the attacking player would take to (capture-)multijump
            List<List<int>> result = new List<List<int>>();

            _CalculateJumpPaths(
                result,
                currentPath: new List<int>(),
                targetColor: targetColor,
                tile: baseTile,
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

                // Ignore the tile to land on because it was including it as part
                // of a potential path, which is encapsulated by a different path
                List<List<int>> paths = CalculateJumpPaths(
                    diagTile,
                    color,
                    new HashSet<int> { tileToLandOn.id }
                );

                foreach (List<int> _path in paths) {
                    List<int> path = new List<int>(_path);
                    path.Reverse();
                    path.Add(diagTile.id);
                    path.Add(tileToLandOn.id);

                    // `path` represents a full set of moves that would win the game for the attacking
                    // player. We have to determine if executing these moves would put them in check.

                    bool isGood = true;
                    Board boardCopy = board.CreateCopy();
                    for (int i = 0; i < path.Count - 1; i++) {
                        Tile startTile = boardCopy.GetTile(path[i]);
                        Tile endTile = boardCopy.GetTile(path[i + 1]);
                        Chessman movingChessman = startTile.GetPiece();

                        // Ok so this took me a while to figure out but here's the gist:
                        //      - if it's player A's turn, then player B is not in check
                        //      - you can place pieces to check the opponent such that if you were to
                        //          actually move the piece, you yourself would be in check
                        //          (see: https://chess.stackexchange.com/a/2350 )
                        //
                        // We want to determine here if our king can be jumped. If we're WHITE then
                        // we will check for all the movements BLACK can make and we DON'T NEED TO VERIFY
                        // if BLACK will actually be in check or not by any of these moves.
                        //
                        //  This was causing a stack overflow error that was like:
                        //      - ExecuteLegalMove()
                        //      - IsMovingPlayerInCheck()
                        //      - DetermineCheckTiles()
                        //      - GetPseudoLegalMoveResult()
                        //      - IsMovingPlayerInCheck()
                        //      - ...

                        var move = new Move(boardCopy, new MoveAttempt {
                            pieceId = movingChessman.id,
                            tileId = endTile.id
                        });

                        var otherMoveResult = move.DoTheCrazyMove();

                        //MoveResult otherMoveResult = move.GetPseudoLegalMoveResult();
                        if (otherMoveResult != null && otherMoveResult.valid) {
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

        bool IsOpposingPlayerCheckmated (List<Tile> tilesThatCheckOpposingPlayer) {
            List<Chessman> chessmenForOpposingPlayer = board.GetActiveChessmenOfColor(Helpers.GetOppositeColor(chessman.color));
            Board boardCopy = board.CreateCopy();

            // Sort by King, Queen, Rook, Bishop, Knight, Pawn.

            chessmenForOpposingPlayer.Sort((c1, c2) => ConvertChessmanToSortScore(c1) - ConvertChessmanToSortScore(c2));

            foreach (Chessman otherChessman in chessmenForOpposingPlayer) {
                List<Tile> potentialTiles = boardCopy.GetPotentialTilesForMovement(otherChessman);

                foreach (Tile tile in potentialTiles) {
                    // Use `new Move` instead of `boardClone.MoveChessman` so we can use the
                    // `GetPseudoLegalMoveResult` method

                    Move move = new Move(
                        boardCopy,
                        new MoveAttempt {
                            pieceId = otherChessman.id,
                            tileId = tile.id
                        }
                    );

                    MoveResult otherMoveResult = move.GetPseudoLegalMoveResult();
                    if (otherMoveResult != null && otherMoveResult.valid) {
                        return false;
                    }

                    // This is something important I realized: when a player has a potential
                    // multijump or capturejump, they need to be out of check on the initial move to
                    // be able to continue the moves. As such we don't need to do anything fancy with
                    // recursing or anything like that.

                    // No need to undo the move because if the move was legal, then the player is not
                    // checkmated.
                }
            }

            return true;
        }

        #endregion

        /// <summary>
        /// This function DOES:
        ///     - Moves the piece
        ///     - Removes jumped/captured pieces
        ///     - Sets promotion, polarity, kinging
        ///
        /// This function does NOT:
        ///     - ensure the movement itself is legal
        ///     - validate for "check" or "checkmate"
        /// </summary>
        void ExecuteBaseMove () {
            //Match.Log($"{chessman.kind} | {string.Join(",", potentialTilesForMovement.Select((t) => t.id))}");
            if (!chessman.hasMoved) {
                moveResult.wasFirstMoveForPiece = true;
                chessman.SetHasMoved(true);
            }

            int rowDelta = toRow - fromRow;
            int colDelta = toColumn - fromColumn;

            // If this was a castling, the king was the one to move; now move the rook.
            if (
                chessman.IsKing() &&
                (rowDelta == 0) &&
                (Math.Abs(colDelta) == 2) &&
                !chessman.IsChecker()
            ) {
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
                if (rookChessman != null) {
                    rookFromTile.RemovePiece();
                    rookToTile.SetPiece(rookChessman);
                    rookChessman.SetUnderlyingTile(rookToTile);
                    rookChessman.SetHasMoved(true);

                    moveResult.isCastle = true;
                }
            }

            // If a piece was jumped or captured, mark that down before moving the piece
            if (chessman.IsChecker()) {
                // This was a valid movement - if the piece moved +/- 2 rows/columns, then
                // a piece was jumped.
                if (
                    (Math.Abs(rowDelta) == 2) &&
                    (Math.Abs(colDelta) == 2)
                ) {
                    // `delta` will always be even in this case; this value will represent
                    // the movement amount from `fromTile` to the tile that the jumped piece
                    // was occupying
                    int halfDelta = delta / 2;
                    Tile jumpedTile = board.GetTile(fromTile.id + halfDelta);
                    //Match.Log($"{chessman.kind} | {fromTile.id} -> {toTile.id} | {jumpedTile.id} | delta={delta} | {Helpers.FormatTiles(potentialTilesForMovement)}");

                    moveResult.wasPieceJumped = true;
                    moveResult.jumpedPieceId = jumpedTile.GetPiece().id;
                    moveResult.jumpedTileId = jumpedTile.id;
                }
            } else {
                if (toTile.IsOccupied()) {
                    moveResult.wasPieceCaptured = true;
                    moveResult.capturedPieceId = toTile.GetPiece().id;
                }
            }

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
            Chessman chessmanToRemove = null;

            if (moveResult.WasPieceJumped()) {
                chessmanToRemove = board.GetChessman(moveResult.jumpedPieceId);
            } else if (moveResult.WasPieceCaptured()) {
                chessmanToRemove = board.GetChessman(moveResult.capturedPieceId);
            }

            if (chessmanToRemove != null) {
                chessmanToRemove.Deactivate();
                if (chessmanToRemove.IsKing()) {
                    moveResult.isWinningMove = true;
                }

                if (moveResult.WasPieceJumped()) {
                    board.GetTile(moveResult.jumpedTileId).RemovePiece();
                }
            }

            // -- Actually move piece
            fromTile.RemovePiece();
            toTile.SetPiece(chessman);
            chessman.SetUnderlyingTile(toTile);

            // -- Validate promotion
            if (
                moveResult.promotionRank == null ||
                !Helpers.CanBePromoted(chessman, toTile) ||
                moveResult.promotionRank == ChessmanKindEnum.KING ||
                moveResult.promotionRank == ChessmanKindEnum.PAWN
            ) {
                moveResult.promotionRank = null;
            }

            if (moveResult.promotionRank != null) {
                chessman.Promote((ChessmanKindEnum) moveResult.promotionRank);
            }
        }

        /// <summary>
        /// Does `ExecuteBaseMove` but also validates for check (but NOT checkmate)
        /// </summary>
        MoveResult ExecuteBaseMoveWithCheckValidation () {
            if (!(potentialTilesForMovement.Exists((t) => t.id == toTile.id))) {
                // Not a legal move to the specified tile.
                //Match.Log("Not a legal move.");
                return null;
            }

            ExecuteBaseMove();

            if (IsMovingPlayerInCheck()) {
                //Match.Log("Moving player is in check.");
                return null;
            }

            PostValidationHandler();
            return moveResult;
        }

        /// <summary>
        /// `ExecuteBaseMove` + `PostValidationHandler`. To be used when a move needs to be made but
        /// we don't care about the check/checkmate validations.
        /// </summary>
        /// <returns>The the crazy move.</returns>
        public MoveResult DoTheCrazyMove () {
            ExecuteBaseMove();
            PostValidationHandler();
            return moveResult;
        }

        /// <summary>
        /// Gets the pseudo legal move result. Skips the piece-specific validation - only checks that
        /// the player is not in  check.
        /// </summary>
        /// <returns>The pseudo legal move result.</returns>
        public MoveResult GetPseudoLegalMoveResult () {
            ExecuteBaseMoveWithCheckValidation();
            return moveResult;
        }

        public MovementValidationEndResult ExecuteMove () {
            ExecuteBaseMoveWithCheckValidation();

            if (!moveResult.isWinningMove) {
                // Redundant check here as an attempt to prevent stack overflow :'(
                // (Probably not a solution to the issue but whatever)

                List<Tile> tilesThatCheckOpposingPlayer = CalculateCheckTiles(Helpers.GetOppositeColor(chessman.color), exitEarly: false);

                // -- Now see if this player has won.
                if (tilesThatCheckOpposingPlayer.Count > 0) {
                    if (IsOpposingPlayerCheckmated(tilesThatCheckOpposingPlayer)) {
                        moveResult.isWinningMove = true;
                    }
                }
            }

            return new MovementValidationEndResult(board, moveResult);
        }

        /// <summary>
        /// Modifies the MoveResult in-place. Common between pseudo-legal and legal
        /// movement validations. Called after validation succeeds.
        /// </summary>
        void PostValidationHandler () {
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
                List<Tile> nextMovePotentialTiles = board.GetPotentialTilesForMovement(chessman, jumpsOnly: true);
                shouldChangeTurns = (nextMovePotentialTiles.Count == 0);
            }

            if (shouldChangeTurns) {
                moveResult.turnChanged = true;
            }

            moveResult.valid = true;
        }
    }
}
