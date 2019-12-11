using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessersEngine {
    public class Board {
        public readonly long id;
        List<ChessmanSchema> pieces;
        Dictionary<int, Tile> tilesById;
        Dictionary<int, Chessman> chessmenById;

        readonly List<string> moves = new List<string>();

        readonly List<MoveResult> pendingMoveResults = new List<MoveResult>();

        readonly int numColumns = 8;
        readonly int numRows = 8;

        public Board (List<ChessmanSchema> _pieces) {
            pieces = _pieces ?? CreateDefaultChessmen();

            id = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            tilesById = new Dictionary<int, Tile>();
            chessmenById = new Dictionary<int, Chessman>();

            for (int i = 0; i < 64; i++) {
                tilesById[i] = new Tile {
                    id = i
                };
            }

            foreach (ChessmanSchema cs in pieces) {
                Chessman newChessman = Chessman.CreateFromSchema(cs);
                Tile underlyingTile = GetTile(cs.location);

                newChessman.SetUnderlyingTile(underlyingTile);
                underlyingTile.SetPiece(newChessman);

                chessmenById[newChessman.id] = newChessman;
            }
        }

        public List<ChessmanSchema> GetChessmanSchemas () {
            return chessmenById.Values.Select((c) => c.CreateSchema()).ToList();
        }

        public Chessman GetChessman (int id) {
            return chessmenById[id];
        }

        List<ChessmanSchema> CreateDefaultChessmen () {
            List<ChessmanSchema> chessmanSchemas = new List<ChessmanSchema> {
                new ChessmanSchema () {
                    location = 0,
                    id = Constants.ID_WHITE_ROOK_1
                },
                new ChessmanSchema () {
                    location = 1,
                    id = Constants.ID_WHITE_KNIGHT_1
                },
                new ChessmanSchema () {
                    location = 2,
                    id = Constants.ID_WHITE_BISHOP_1
                },
                new ChessmanSchema () {
                    location = 3,
                    id = Constants.ID_WHITE_QUEEN
                },
                new ChessmanSchema () {
                    location = 4,
                    id = Constants.ID_WHITE_KING
                },
                new ChessmanSchema () {
                    location = 5,
                    id = Constants.ID_WHITE_BISHOP_2
                },
                new ChessmanSchema () {
                    location = 6,
                    id = Constants.ID_WHITE_KNIGHT_2
                },
                new ChessmanSchema () {
                    location = 7,
                    id = Constants.ID_WHITE_ROOK_2
                },
                new ChessmanSchema () {
                    location = 8,
                    id = Constants.ID_WHITE_PAWN_1
                },
                new ChessmanSchema () {
                    location = 9,
                    id = Constants.ID_WHITE_PAWN_2
                },
                new ChessmanSchema () {
                    location = 10,
                    id = Constants.ID_WHITE_PAWN_3
                },
                new ChessmanSchema () {
                    location = 11,
                    id = Constants.ID_WHITE_PAWN_4
                },
                new ChessmanSchema () {
                    location = 12,
                    id = Constants.ID_WHITE_PAWN_5
                },
                new ChessmanSchema () {
                    location = 13,
                    id = Constants.ID_WHITE_PAWN_6
                },
                new ChessmanSchema () {
                    location = 14,
                    id = Constants.ID_WHITE_PAWN_7
                },
                new ChessmanSchema () {
                    location = 15,
                    id = Constants.ID_WHITE_PAWN_8
                },
                new ChessmanSchema () {
                    location = 48,
                    id = Constants.ID_BLACK_PAWN_1
                },
                new ChessmanSchema () {
                    location = 49,
                    id = Constants.ID_BLACK_PAWN_2
                },
                new ChessmanSchema () {
                    location = 50,
                    id = Constants.ID_BLACK_PAWN_3
                },
                new ChessmanSchema () {
                    location = 51,
                    id = Constants.ID_BLACK_PAWN_4
                },
                new ChessmanSchema () {
                    location = 52,
                    id = Constants.ID_BLACK_PAWN_5
                },
                new ChessmanSchema () {
                    location = 53,
                    id = Constants.ID_BLACK_PAWN_6
                },
                new ChessmanSchema () {
                    location = 54,
                    id = Constants.ID_BLACK_PAWN_7
                },
                new ChessmanSchema () {
                    location = 55,
                    id = Constants.ID_BLACK_PAWN_8
                },
                new ChessmanSchema () {
                    location = 56,
                    id = Constants.ID_BLACK_ROOK_1
                },
                new ChessmanSchema () {
                    location = 57,
                    id = Constants.ID_BLACK_KNIGHT_1
                },
                new ChessmanSchema () {
                    location = 58,
                    id = Constants.ID_BLACK_BISHOP_1
                },
                new ChessmanSchema () {
                    location = 59,
                    id = Constants.ID_BLACK_QUEEN
                },
                new ChessmanSchema () {
                    location = 60,
                    id = Constants.ID_BLACK_KING
                },
                new ChessmanSchema () {
                    location = 61,
                    id = Constants.ID_BLACK_BISHOP_2
                },
                new ChessmanSchema () {
                    location = 62,
                    id = Constants.ID_BLACK_KNIGHT_2
                },
                new ChessmanSchema () {
                    location = 63,
                    id = Constants.ID_BLACK_ROOK_2
                },
            };

            foreach (ChessmanSchema cs in chessmanSchemas) {
                cs.kind = Helpers.GetKind(cs.id);
            }

            return chessmanSchemas;
        }

        public void CopyState (Board otherBoard) {
            //Match.Log($"Copying {otherBoard.id} to {this.id}");
            // Update the states of the Chessmen
            foreach (KeyValuePair<int, Chessman> pair in chessmenById) {
                int chessmanId = pair.Key;
                Chessman otherChessman = otherBoard.GetChessman(chessmanId);
                Chessman chessman = GetChessman(chessmanId);

                chessman.CopyFrom(otherChessman);
            }

            // Update the states of the Tiles, and which Chessmen they reference
            foreach (KeyValuePair<int, Tile> pair in tilesById) {
                int tileId = pair.Key;
                Tile otherTile = otherBoard.GetTile(tileId);
                Tile tile = GetTile(tileId);

                tile.CopyFrom(otherTile);

                if (otherTile.IsOccupied()) {
                    // If the tile became occupied, update the Chessman reference
                    Chessman newlyCommittedChessman = GetChessman(otherTile.GetPiece().id);

                    tile.SetPiece(newlyCommittedChessman);
                } else {
                    // If the tile is no longer occupied, remove the Chessman reference
                    tile.RemovePiece();
                }
            }

            // Update the Tile references for the Chessmen
            foreach (KeyValuePair<int, Chessman> pair in chessmenById) {
                int chessmanId = pair.Key;
                Chessman otherChessman = otherBoard.GetChessman(chessmanId);
                Chessman chessman = GetChessman(chessmanId);

                if (otherChessman.GetUnderlyingTile() == null) {
                    chessman.RemoveUnderlyingTileReference();
                } else {
                    Tile tile = GetTile(otherChessman.GetUnderlyingTile().id);
                    chessman.SetUnderlyingTile(tile);
                }
            }
        }

        #region Chessman getters

        public Dictionary<int, Chessman> GetAllChessmen () {
            return chessmenById;
        }

        Chessman GetKingOfColor (ColorEnum color) {
            return color == ColorEnum.BLACK ? GetChessman(Constants.ID_BLACK_KING) : GetChessman(Constants.ID_WHITE_KING);
        }

        public List<Chessman> GetActiveChessmenOfColor (ColorEnum color) {
            List<Chessman> val = new List<Chessman>();
            foreach (var pair in chessmenById) {
                Chessman c = pair.Value;
                if (c.isActive && c.IsSameColor(color)) {
                    val.Add(c);
                }
            }
            return val;
        }

        public Chessman GetWhiteKing () {
            return GetKingOfColor(ColorEnum.WHITE);
        }

        public Chessman GetBlackKing () {
            return GetKingOfColor(ColorEnum.BLACK);
        }

        #endregion

        #region Tile-getters

        public Tile GetTile (int id) {
            return tilesById[id];
        }

        public Tile GetTileIfExists (int id) {
            if (tilesById.ContainsKey(id)) {
                return GetTile(id);
            }
            return null;
        }

        public Tile GetTileIfExists (int row, int col) {
            return GetTileIfExists(GetTileNumberFromRowColumn(row, col));
        }

        /// <summary>
        /// Gets the tile by row+column combination. If either value is out of
        /// bounds, null is returned.
        /// </summary>
        /// <returns>Tile at (col, row).</returns>
        /// <param name="row">Row.</param>
        /// <param name="col">Col.</param>
        public Tile GetTileByRowColumn (int row, int col) {
            if (row < 0 || row > GetNumberOfRows() || col < 0 || col > GetNumberOfColumns()) {
                return null;
            }

            return GetTileIfExists(row, col);
        }

        public int GetColumn (int tileId) {
            return tileId % numColumns;
        }

        public int GetColumn (Tile tile) {
            return GetColumn(tile.id);
        }

        public int GetRow (int tileId) {
            return tileId / numColumns;
        }

        public int GetRow (Tile tile) {
            return GetRow(tile.id);
        }

        public Tile GetTopTileOfColumn (int column) {
            return GetTile(column + ((numColumns - 1) * numRows));
        }

        public Tile GetBottomTileOfColumn (int column) {
            return GetTile(column);
        }

        public Tile GetLeftmostTileOfRow (int row) {
            return GetTile(row * numColumns);
        }

        public Tile GetRightmostTileOfRow (int row) {
            return GetTile((row * numColumns) + (numColumns - 1));
        }

        Tile GetEndTileOfDiagonal (int row, int col, int rowStep, int colStep) {
            int currentRow = row;
            int currentCol = col;

            Tile result = null;

            while (true) {
                Tile temp = GetTileByRowColumn(row, col);
                if (temp == null) {
                    return result;
                } else {
                    result = temp;
                }
                row += rowStep;
                col += colStep;
            }
        }

        public Tile GetBottomLeftmostTileOfDiagonal (int row, int col) {
            return GetEndTileOfDiagonal(row, col, -1, -1);
        }

        public Tile GetTopLeftmostTileOfDiagonal (int row, int col) {
            return GetEndTileOfDiagonal(row, col, 1, -1);
        }

        public Tile GetBottomRightmostTileOfDiagonal (int row, int col) {
            return GetEndTileOfDiagonal(row, col, -1, 1);
        }

        public Tile GetTopRightmostTileOfDiagonal (int row, int col) {
            return GetEndTileOfDiagonal(row, col, 1, 1);
        }

        #endregion

        public int GetTileNumberFromRowColumn (int row, int col) {
            return (numRows * row) + col;
        }

        public int GetNumberOfColumns () {
            return numColumns;
        }

        public int GetNumberOfRows () {
            return numRows;
        }

        /// <summary>
        /// Get the delta value for a positive diagonal step. That is, the distance
        /// between a tile and the tile that is adjacent to it in the upper-right
        /// direction.
        /// </summary>
        /// <returns>The positive diagonal delta.</returns>
        public int GetPositiveDiagonalDelta () {
            return (numColumns + 1);
        }

        /// <summary>
        /// Get the delta value for a negative diagonal step. That is, the distance
        /// between a tile and the tile that is adjacent to it in the upper-left
        /// direction.
        /// </summary>
        /// <returns>The positive diagonal delta.</returns>
        public int GetNegativeDiagonalDelta () {
            return (numColumns - 1);
        }

        public bool IsTileInLeftmostColumn (Tile tile) {
            return GetColumn(tile) == 0;
        }

        public bool IsTileInBottomRow (Tile tile) {
            return GetRow(tile) == 0;
        }

        public bool IsTileInTopRow (Tile tile) {
            return GetRow(tile) == (numRows - 1);
        }

        public bool IsTileInRightmostColumn (Tile tile) {
            return GetColumn(tile) == (numColumns - 1);
        }

        public int CalculateRowDelta (Tile tile1, Tile tile2) {
            return Math.Abs(GetRow(tile1) - GetRow(tile2));
        }

        public int CalculateColumnDelta (Tile tile1, Tile tile2) {
            return Math.Abs(GetColumn(tile1) - GetColumn(tile2));
        }

        public int GetMaxTileNumber () {
            return (GetNumberOfRows() * GetNumberOfColumns()) - 1;
        }

        #region Can capture?

        List<Tile> CanChessmanBeCapturedFromDirectionSubset (
            Chessman chessman,
            Tile startTile,
            Tile endTile,
            int stepSize,
            Func<Chessman, bool> additionalValidator,
            bool exitEarly
        ) {
            List<Tile> capturableTiles = new List<Tile>();

            int sign = (stepSize > 0) ? 1 : -1;

            for (int i = (sign * startTile.id); i <= (sign * endTile.id); i += Math.Abs(stepSize)) {
                int tileId = Math.Abs(i);

                if (tileId == startTile.id) {
                    continue;
                }

                Tile tileToCheck = GetTile(tileId);
                if (!tileToCheck.IsOccupied()) {
                    continue;
                }

                Chessman occupant = tileToCheck.GetPiece();
                if (occupant.colorId == chessman.colorId || occupant.IsChecker()) {
                    break;
                }

                bool additionalValidationResult = true;
                if (additionalValidator != null) {
                    additionalValidationResult = additionalValidator(occupant);
                }

                if (!additionalValidationResult) {
                    capturableTiles.Add(tileToCheck);
                    if (exitEarly) {
                        break;
                    }
                }
            }

            return capturableTiles;
        }

        List<Tile> CanChessmanBeCapturedFromDirection (
            Chessman chessman,
            Directionalities dir,
            bool exitEarly
        ) {
            Tile tile = chessman.GetUnderlyingTile();
            Func<Chessman, bool> additionalValidator = null;

            List<Tile> capturableTiles = new List<Tile>();

            if (dir == Directionalities.HORIZONTAL) {
                additionalValidator = (Chessman occupant) => {
                    return !occupant.IsQueen() && !occupant.IsRook();
                };

                capturableTiles = capturableTiles
                        // Tile to left side
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetLeftmostTileOfRow(GetRow(tile)),
                            stepSize: -1,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        // Tile to right side
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetRightmostTileOfRow(GetRow(tile)),
                            stepSize: 1,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        .ToList();
            } else if (dir == Directionalities.VERTICAL) {
                additionalValidator = (Chessman occupant) => {
                    return !occupant.IsQueen() && !occupant.IsRook();
                };

                capturableTiles = capturableTiles
                        // Tile to bottom
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetBottomTileOfColumn(GetColumn(tile)),
                            stepSize: -1 * numColumns,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        // Tile to top
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetTopTileOfColumn(GetColumn(tile)),
                            stepSize: numColumns,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        .ToList();
            } else if (dir == Directionalities.POSITIVE_DIAGONAL) {
                int stepSize = GetPositiveDiagonalDelta();
                additionalValidator = (Chessman occupant) => {
                    return !occupant.IsQueen() && !occupant.IsBishop();
                };

                capturableTiles = capturableTiles
                        // Tile to bottom-left
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetBottomLeftmostTileOfDiagonal(GetRow(tile), GetColumn(tile)),
                            stepSize: -1 * stepSize,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        // Tile to top-right
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetTopRightmostTileOfDiagonal(GetRow(tile), GetColumn(tile)),
                            stepSize: stepSize,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        .ToList();
            } else if (dir == Directionalities.NEGATIVE_DIAGONAL) {
                int stepSize = GetNegativeDiagonalDelta();
                additionalValidator = (Chessman occupant) => {
                    return !occupant.IsQueen() && !occupant.IsBishop();
                };

                capturableTiles = capturableTiles
                        // Tile to top-left
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetTopLeftmostTileOfDiagonal(GetRow(tile), GetColumn(tile)),
                            stepSize: -1 * stepSize,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        // Tile to bottom-right
                        .Concat(CanChessmanBeCapturedFromDirectionSubset(
                            chessman,
                            startTile: tile,
                            endTile: GetBottomRightmostTileOfDiagonal(GetRow(tile), GetColumn(tile)),
                            stepSize: stepSize,
                            additionalValidator: additionalValidator,
                            exitEarly: exitEarly
                        ))
                        .ToList();
            } else {
                throw new Exception($"Invalid directionality: {dir}");
            }

            if (dir == Directionalities.NEGATIVE_DIAGONAL || dir == Directionalities.POSITIVE_DIAGONAL) {
                List<Tile> pawnTilesToCheck = new List<Tile>();
                if (chessman.IsWhite()) {
                    pawnTilesToCheck.Add(GetTileIfExists(GetRow(tile) + 1, GetColumn(tile) - 1));
                    pawnTilesToCheck.Add(GetTileIfExists(GetRow(tile) + 1, GetColumn(tile) + 1));
                } else {
                    pawnTilesToCheck.Add(GetTileIfExists(GetRow(tile) - 1, GetColumn(tile) - 1));
                    pawnTilesToCheck.Add(GetTileIfExists(GetRow(tile) - 1, GetColumn(tile) + 1));
                }

                foreach (var pawnTile in pawnTilesToCheck) {
                    if (pawnTile == null || !pawnTile.IsOccupied()) {
                        continue;
                    }
                    var occ = pawnTile.GetPiece();
                    if (!occ.IsSameColor(chessman) && !occ.IsChecker() && occ.IsPawn()) {
                        capturableTiles.Add(pawnTile);
                    }
                }
            }

            //TODO -- for diagonal, way to check for pawns is to allow the additional validator
            // to take in the current distance from the target tile?

            return capturableTiles;
        }

        public List<Tile> CanChessmanBeCapturedVertically (Chessman chessman, bool exitEarly = true) {
            return CanChessmanBeCapturedFromDirection(
                chessman,
                Directionalities.VERTICAL,
                exitEarly
            );
        }

        public List<Tile> CanChessmanBeCapturedHorizontally (Chessman chessman, bool exitEarly = true) {
            return CanChessmanBeCapturedFromDirection(
                chessman,
                Directionalities.HORIZONTAL,
                exitEarly
            );
        }

        public List<Tile> CanChessmanBeCaptured (Chessman chessman, bool exitEarly = true) {
            return (
                CanChessmanBeCapturedVertically(chessman, exitEarly)
                .Concat(
                    CanChessmanBeCapturedHorizontally(chessman, exitEarly)
                )
                .ToList()
            // TODO -- diagonal... -_-
            );
        }

        #endregion

        #region Can jump?

        bool CanChessmanBeJumpedFromTile (Chessman targetChessman, Tile attackingTile) {
            if (attackingTile == null) {
                return false;
            }

            Tile tile = targetChessman.GetUnderlyingTile();

            if (attackingTile == null || !attackingTile.IsOccupied()) {
                return false;
            }

            // NOTE -- may be worse to do it this way, instead of the way below.
            //List<int> validDeltas = new List<int> {
            //    GetNegativeDiagonalDelta(),
            //    GetPositiveDiagonalDelta(),
            //    -(GetNegativeDiagonalDelta()),
            //    -(GetPositiveDiagonalDelta()),
            //};
            //int delta = attackingTile.id - tile.id;

            //if (!validDeltas.Contains(delta)) {
            //    return false;
            //}

            // -- Validate the position of the specified tile.
            // Ths includes it being 1 row, 1 column away, and that there is a
            // tile to jump onto (i.e. the chessman is not along a board edge)

            if (
                (CalculateRowDelta(tile, attackingTile) != 1) ||
                (CalculateColumnDelta(tile, attackingTile) != 1)
            ) {
                return false;
            }

            int delta = attackingTile.id - tile.id;

            // The distance from the jumping tile to the tile being jumped
            // over is the same distance as the tile being jumped over to the
            // tile being jumped onto.
            int tileJumpedOntoLocation = tile.id - delta;
            if (tileJumpedOntoLocation < 0 || tileJumpedOntoLocation > GetMaxTileNumber()) {
                return false;
            }

            Tile tileJumpedOnto = GetTile(tileJumpedOntoLocation);
            if (
                (CalculateRowDelta(tile, attackingTile) != 1) ||
                (CalculateColumnDelta(tile, attackingTile) != 1)
            ) {
                return false;
            }

            // -- Validate the attrs of the occupant doing the jump
            // (They have to be a different colour, and currently a checker)
            Chessman jumpingChessman = attackingTile.GetPiece();
            if (jumpingChessman.colorId == targetChessman.colorId || !jumpingChessman.IsChecker()) {
                return false;
            }

            // -- Validate that the tile being jumped onto is NOT occupied
            if (tileJumpedOnto.IsOccupied()) {
                return false;
            }

            if (jumpingChessman.isKinged) {
                return true;
            }

            // -- Validate that the checker doing the jump can jump in that direction
            if (targetChessman.IsWhite()) {
                // The chkecer doing the jump can only jump from top towards bottom
                return (GetRow(attackingTile) > GetRow(tile));
            } else {
                // Bottom towards top
                return GetRow(attackingTile) < GetRow(tile);
            }
        }

        public List<Tile> CanChessmanBeJumped (Chessman chessman, bool exitEarly = true) {
            List<Tile> result = new List<Tile>();

            Tile tile = chessman.GetUnderlyingTile();

            List<Tile> diagonallyAdjacentTiles = GetDiagonallyAdjacentTiles(tile);
            foreach (var diagonallyAdjacentTile in diagonallyAdjacentTiles) {
                if (CanChessmanBeJumpedFromTile(chessman, diagonallyAdjacentTile)) {
                    result.Add(diagonallyAdjacentTile);
                    if (exitEarly) {
                        break;
                    }
                }
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Given a tile, gets all of the diagonally adjacent tiles.
        /// </summary>
        /// <returns>The diagonally adjacent tiles.</returns>
        /// <param name="tile">Tile.</param>
        public List<Tile> GetDiagonallyAdjacentTiles (Tile tile) {
            List<Tile> tiles = new List<Tile>();

            if (!IsTileInLeftmostColumn(tile)) {
                if (!IsTileInTopRow(tile)) {
                    tiles.Add(GetTile(tile.id + GetNegativeDiagonalDelta()));
                }
                if (!IsTileInBottomRow(tile)) {
                    tiles.Add(GetTile(tile.id - GetPositiveDiagonalDelta()));
                }
            }

            if (!IsTileInRightmostColumn(tile)) {
                if (!IsTileInTopRow(tile)) {
                    tiles.Add(GetTile(tile.id + GetPositiveDiagonalDelta()));
                }
                if (!IsTileInBottomRow(tile)) {
                    tiles.Add(GetTile(tile.id - GetNegativeDiagonalDelta()));
                }
            }

            return tiles;
        }

        public bool AreTilesDiagonallyAdjacent (Tile tile1, Tile tile2) {
            return CalculateRowDelta(tile1, tile2) == 1 &&
                CalculateColumnDelta(tile1, tile2) == 1;
        }

        /// <summary>
        /// Given a tile, get the tiles that could potentially have a piece jump
        /// to the first tile from. "Potentially" means that if a piece WERE to
        /// be on this distant tile, could it jump?
        ///
        /// A tile can be jumped to from another tile that is 2 columns and 2 rows
        /// away, and for the tile in between, it must be occupied.
        ///
        /// We do NOT check that the distant tile (2 and 2 away) is occupied or
        /// not. (Again, IF there is a piece here.)
        /// </summary>
        /// <returns>Tiles that could have pieces jump to the supplied tile.</returns>
        /// <param name="tile">Tile.</param>
        public List<Tile> GetPotentialJumpLocationsForTile (Tile tile) {
            List<Tile> tiles = new List<Tile>();

            List<int> deltas = new List<int> {
                GetNegativeDiagonalDelta(),
                -GetNegativeDiagonalDelta(),
                GetPositiveDiagonalDelta(),
                -GetPositiveDiagonalDelta()
            };

            foreach (int delta in deltas) {
                Tile jumpingOverTile = GetTileIfExists(tile.id + delta);
                if (
                    jumpingOverTile == null ||
                    !AreTilesDiagonallyAdjacent(tile, jumpingOverTile) ||
                    !jumpingOverTile.IsOccupied()
                ) {
                    continue;
                }

                Tile jumpingFromTile = GetTileIfExists(tile.id + (2 * delta));
                if (
                    jumpingFromTile == null ||
                    !AreTilesDiagonallyAdjacent(jumpingOverTile, jumpingFromTile)
                ) {
                    continue;
                }

                tiles.Add(jumpingFromTile);
            }

            return tiles;
        }

        #region Potential moves

        bool IsPathBlockedByChessman (Tile fromTile, Tile toTile, int delta, int directionality) {
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
                if (GetTile(tileId).IsOccupied()) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///
        /// Gets a list of tiles the chessman could potentially move to, in a single line.
        /// For example, a rook can move anywhere horizontally and vertically, but they can
        /// only move to the tiles that are not blocked by other chessmen. If the chessman
        /// blocking the rook's path is of the opposite color, then the rook can move at most
        /// to that tile. (If the blocking chessman is of the same color, the rook can move to
        /// at most the tile before.)
        ///
        /// </summary>
        ///
        /// <returns>The tile iterator.</returns>
        /// <param name="chessman">Chessman.</param>
        /// <param name="iter">Iter.</param>
        /// <param name="loopConditional">Loop conditional.</param>
        /// <param name="currentTileGetter">Current tile getter.</param>
        /// <param name="increasing">If set to <c>true</c> increasing.</param>
        List<Tile> PotentialTileIterator (
            Chessman chessman,
            int iter,
            Func<int, bool> loopConditional,
            Func<int, Tile> currentTileGetter,
            bool increasing = true,
            int limit = int.MaxValue
        ) {
            List<Tile> potentialTiles = new List<Tile>();
            int loopLimiter = 0;

            while (loopConditional(iter) && loopLimiter < limit) {
                Tile nextTile = currentTileGetter(iter);
                if (increasing) {
                    iter++;
                } else {
                    iter--;
                }

                if (nextTile.IsOccupied()) {
                    if (!nextTile.GetPiece().IsSameColor(chessman)) {
                        potentialTiles.Add(nextTile);
                    }

                    break;
                }

                potentialTiles.Add(nextTile);
                loopLimiter++;
            }
            return potentialTiles;
        }

        List<Tile> PotentialTilesForHorizontalMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            int col = GetColumn(tile);
            int row = GetRow(tile);

            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: col + 1,
                loopConditional: (iter) => iter < GetNumberOfColumns(),
                currentTileGetter: (iter) => GetTileByRowColumn(row, iter)
            ));
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: col - 1,
                loopConditional: (iter) => iter >= 0,
                currentTileGetter: (iter) => GetTileByRowColumn(row, iter),
                increasing: false
            ));
            return potentialTiles;
        }

        List<Tile> PotentialTilesForVerticalMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            int col = GetColumn(tile);
            int row = GetRow(tile);

            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: row + 1,
                loopConditional: (iter) => iter <= GetNumberOfRows(),
                currentTileGetter: (iter) => GetTileByRowColumn(iter, col)
            ));
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: row - 1,
                loopConditional: (iter) => iter >= 0,
                currentTileGetter: (iter) => GetTileByRowColumn(iter, col),
                increasing: false
            ));
            return potentialTiles;
        }

        List<Tile> PotentialTilesForDiagonalMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            int col = GetColumn(tile);
            int row = GetRow(tile);

            int i;

            // Positive diagonal: up-and-right
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row + iter) < GetNumberOfRows()) && ((col + iter) < GetNumberOfColumns());
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row + iter, col + iter)
            ));

            // Positive diagonal: down-and-left
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row - iter) >= 0) && ((col - iter) >= 0);
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row - iter, col - iter)
            ));

            // negative diagonal: down-and-right
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row - iter) >= 0) && ((col + iter) < GetNumberOfColumns());
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row - iter, col + iter)
            ));

            // negative diagonal: up-and-left
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row + iter) < GetNumberOfRows()) && ((col - iter) >= 0);
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row + iter, col - iter)
            ));

            return potentialTiles;
        }

        /// <summary>
        /// Gets the potential tiles for pawn movement.
        /// </summary>
        /// <returns>The potential tiles for pawn movement.</returns>
        /// <param name="chessman">Chessman.</param>
        List<Tile> GetPotentialTilesForPawnMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            int modifier = (chessman.IsBlack()) ? -1 : 1;

            Tile tileForRegularMovement = GetTile(modifier * GetNumberOfColumns());
            if (!tileForRegularMovement.IsOccupied()) {
                potentialTiles.Add(tileForRegularMovement);
            }

            Tile tileForLongMovement = GetTile(2 * modifier * GetNumberOfColumns());
            if (!tileForLongMovement.IsOccupied() && !tileForRegularMovement.IsOccupied()) {
                potentialTiles.Add(tileForLongMovement);
            }

            Tile captureTile1 = null, captureTile2 = null;

            if (!IsTileInLeftmostColumn(tile)) {
                captureTile1 = GetTile(modifier * GetPositiveDiagonalDelta());
            }
            if (!IsTileInRightmostColumn(tile)) {
                captureTile2 = GetTile(modifier * GetNegativeDiagonalDelta());
            }

            foreach (var t in new Tile[] { captureTile1, captureTile2 }) {
                if (t != null && t.IsOccupied() && !t.GetPiece().IsSameColor(chessman)) {
                    potentialTiles.Add(t);
                }
            }
            return potentialTiles;
        }

        List<Tile> GetPotentialTilesForKnightMovement (Chessman chessman) {
            Tile tile = chessman.GetUnderlyingTile();
            int col = GetColumn(tile);
            int row = GetRow(tile);

            List<Tile> potentialTiles = new List<Tile> {
                GetTileByRowColumn(col - 2, row - 1),
                GetTileByRowColumn(col + 2, row - 1),
                GetTileByRowColumn(col - 2, row + 1),
                GetTileByRowColumn(col + 2, row + 1),

                GetTileByRowColumn(col - 1, row - 2),
                GetTileByRowColumn(col + 1, row - 2),
                GetTileByRowColumn(col - 1, row + 2),
                GetTileByRowColumn(col + 1, row + 2),
            };

            return potentialTiles.Where((t) => {
                return (
                    t != null &&
                    (!t.IsOccupied() || t.GetPiece().colorId != chessman.colorId)
                );
            }).ToList();
        }

        List<Tile> GetPotentialTilesForRookMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();

            potentialTiles.AddRange(PotentialTilesForVerticalMovement(chessman));
            potentialTiles.AddRange(PotentialTilesForHorizontalMovement(chessman));

            return potentialTiles;
        }

        List<Tile> GetPotentialTilesForBishopMovement (Chessman chessman) {
            return PotentialTilesForDiagonalMovement(chessman);
        }

        List<Tile> GetPotentialTilesForQueenMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();

            potentialTiles.AddRange(PotentialTilesForVerticalMovement(chessman));
            potentialTiles.AddRange(PotentialTilesForHorizontalMovement(chessman));
            potentialTiles.AddRange(PotentialTilesForDiagonalMovement(chessman));

            return potentialTiles;
        }

        /// <summary>
        /// Gets the potential tiles for king movement.
        ///
        /// Kings can move only by 1 tile, and only up, up-left, up-right (from
        /// their point of reference; i.e. for black it's down, down-left, down-right)
        /// </summary>
        /// <returns>The potential tiles for king movement.</returns>
        /// <param name="chessman">Chessman.</param>
        List<Tile> GetPotentialTilesForKingMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();

            Tile tile = chessman.GetUnderlyingTile();
            int col = GetColumn(tile);
            int row = GetRow(tile);
            bool upwards = chessman.IsWhite();
            int modifier = (upwards) ? 1 : -1;

            // Up/down
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: row + modifier,
                loopConditional: (iter) => {
                    if (upwards) {
                        return iter < GetNumberOfRows();
                    } else {
                        return iter >= 0;
                    }
                },
                currentTileGetter: (iter) => GetTileByRowColumn(iter, col),
                increasing: true,
                limit: 1
            ));

            // Up-right/down-right
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: 1,
                loopConditional: (iter) => {
                    if (upwards) {
                        return ((row + iter) < GetNumberOfRows()) && ((col + iter) < GetNumberOfColumns());
                    } else {
                        return ((row - iter) >= 0) && ((col + iter) < GetNumberOfColumns());
                    }
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row + iter, col + iter),
                increasing: true,
                limit: 1
            ));

            // Up-left/down-left
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: 1,
                loopConditional: (iter) => {
                    if (upwards) {
                        return ((row + iter) < GetNumberOfRows()) && ((col - iter) >= 0);
                    } else {
                        return ((row - iter) >= 0) && ((col - iter) >= 0);
                    }
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row + iter, col - iter),
                increasing: true,
                limit: 1
            ));

            return potentialTiles;
        }


        public List<Tile> GetPotentialTilesForMovement (Chessman chessman) {
            Func<Chessman, List<Tile>> potentialTilesGetter = null;

            if (chessman.IsChecker()) {
                // TODO...
            } else if (chessman.IsPawn()) {
                potentialTilesGetter = GetPotentialTilesForPawnMovement;
            } else if (chessman.IsKnight()) {
                potentialTilesGetter = GetPotentialTilesForKnightMovement;
            } else if (chessman.IsRook()) {
                potentialTilesGetter = GetPotentialTilesForRookMovement;
            } else if (chessman.IsBishop()) {
                potentialTilesGetter = GetPotentialTilesForBishopMovement;
            } else if (chessman.IsQueen()) {
                potentialTilesGetter = GetPotentialTilesForQueenMovement;
            } else if (chessman.IsKing()) {
                potentialTilesGetter = GetPotentialTilesForKingMovement;
            }

            if (potentialTilesGetter != null) {
                return potentialTilesGetter(chessman);
            }

            return new List<Tile>();
        }

        #endregion

        #region Movements

        public MoveResult MoveChessman (MoveAttempt moveAttempt) {
            Board boardCopy = new Board(this.GetChessmanSchemas());
            boardCopy.CopyState(this);

            Move move = new Move(boardCopy, moveAttempt);
            MovementValidationEndResult result = move.ExecuteMove();

            if (result.moveResult == null || !result.moveResult.valid) {
                return null;
            }

            result.moveResult.playerId = moveAttempt.playerId;
            this.CopyState(result.modifiedBoard);

            return result.moveResult;
        }

        #endregion
    }
}
