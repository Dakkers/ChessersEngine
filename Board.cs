using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessersEngine {
    public class Board {
        public readonly long id;
        readonly List<ChessmanSchema> pieces;
        Dictionary<int, Tile> tilesById;
        Dictionary<int, Chessman> chessmenById;

        readonly int numColumns = 8;
        readonly int numRows = 8;
        readonly int rightDiagonalDelta;
        readonly int leftDiagonalDelta;

        public Board (List<ChessmanSchema> _pieces) {
            pieces = _pieces ?? CreateDefaultChessmen();

            id = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            tilesById = new Dictionary<int, Tile>();
            chessmenById = new Dictionary<int, Chessman>();

            rightDiagonalDelta = numColumns - 1;
            leftDiagonalDelta = numColumns + 1;

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

        public List<Chessman> GetActiveChessmen () {
            return chessmenById.Values.Where((c) => c.isActive).ToList();
        }

        public List<Chessman> GetActiveChessmenOfColor (ColorEnum color) {
            return GetActiveChessmen().Where((c) => c.color == color).ToList();
        }

        public Chessman GetWhiteKing () {
            return GetKingOfColor(ColorEnum.WHITE);
        }

        public Chessman GetBlackKing () {
            return GetKingOfColor(ColorEnum.BLACK);
        }

        public bool IsGameOver () {
            return (!GetWhiteKing().isActive || !GetBlackKing().isActive);
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
            if (row < 0 || row >= GetNumberOfRows() || col < 0 || col >= GetNumberOfColumns()) {
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

        public (int, int) GetRowColumn (Tile tile) {
            return (
                GetRow(tile),
                GetColumn(tile)
            );
        }

        public Tile GetTopTileOfColumn (int column) {
            return GetTile(column + ((numColumns - 1) * numRows));
        }

        public Tile GetTopTileOfColumn (int row, int column) {
            return GetTopTileOfColumn(column);
        }

        public Tile GetBottomTileOfColumn (int column) {
            return GetTile(column);
        }

        public Tile GetBottomTileOfColumn (int row, int column) {
            return GetBottomTileOfColumn(column);
        }

        public Tile GetLeftmostTileOfRow (int row) {
            return GetTile(row * numColumns);
        }

        public Tile GetLeftmostTileOfRow (int row, int col) {
            return GetLeftmostTileOfRow(row);
        }

        public Tile GetRightmostTileOfRow (int row) {
            return GetTile((row * numColumns) + (numColumns - 1));
        }

        public Tile GetRightmostTileOfRow (int row, int col) {
            return GetRightmostTileOfRow(row);
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

        public List<Tile> GetTilesInDirection (
            Tile baseTile,
            Directionality dir
        ) {
            List<Tile> result = new List<Tile>();
            (int row, int col) = GetRowColumn(baseTile);
            int rowStart = row, colStart = col;

            Func<int, int, Tile> f = null;

            if (dir == Directionality.VERTICAL_ABOVE) {
                rowStart = row + 1;
                f = GetTopTileOfColumn;
            } else if (dir == Directionality.VERTICAL_BELOW) {
                rowStart = row - 1;
                f = GetBottomTileOfColumn;
            } else if (dir == Directionality.HORIZONTAL_LEFT) {
                colStart = col - 1;
                f = GetLeftmostTileOfRow;
            } else if (dir == Directionality.HORIZONTAL_RIGHT) {
                colStart = col + 1;
                f = GetRightmostTileOfRow;
            } else if (dir == Directionality.POSITIVE_DIAGONAL_ABOVE) {
                colStart = col + 1;
                rowStart = col + 1;
                f = GetTopRightmostTileOfDiagonal;
            } else if (dir == Directionality.POSITIVE_DIAGONAL_BELOW) {
                colStart = col - 1;
                rowStart = col - 1;
                f = GetBottomLeftmostTileOfDiagonal;
            } else if (dir == Directionality.NEGATIVE_DIAGONAL_ABOVE) {
                colStart = col - 1;
                rowStart = col + 1;
                f = GetTopLeftmostTileOfDiagonal;
            } else if (dir == Directionality.NEGATIVE_DIAGONAL_BELOW) {
                colStart = col + 1;
                rowStart = col - 1;
                f = GetBottomRightmostTileOfDiagonal;
            }

            //Match.Log(dir);

            Tile startTile = GetTileIfExists(rowStart, colStart);
            if (startTile == null) {
                Match.Log("  Exiting early");
                return result;
            }

            Tile endTile = f(rowStart, colStart);
            (int rowEnd, int colEnd) = GetRowColumn(endTile);

            int rowStepSize = Math.Sign(rowEnd - rowStart);
            int colStepSize = Math.Sign(colEnd - colStart);

            if (rowStepSize == 0 && colStepSize == 0) {
                return result;
            }

            Tile tileIter = startTile;
            //Match.Log($"    {rowStepSize} {colStepSize} | {rowStart},{colStart} | {rowEnd},{colEnd}");

            while (tileIter != null) {
                result.Add(tileIter);
                tileIter = GetTileIfExists(
                    rowStart + (result.Count * rowStepSize),
                    colStart + (result.Count * colStepSize)
                );
            }

            return result;
        }

        /// <summary>
        /// Gets a list of valid tiles for knight movement. Ignores occupation
        /// of any kind.
        /// </summary>
        /// <returns>The tiles for knight movement.</returns>
        /// <param name="baseTile">Base tile.</param>
        public List<Tile> GetTilesForKnightMovement (Tile baseTile) {
            (int row, int col) = GetRowColumn(baseTile);

            List<Tile> potentialTiles = new List<Tile> {
                GetTileByRowColumn(row - 2, col - 1),
                GetTileByRowColumn(row + 2, col - 1),
                GetTileByRowColumn(row - 2, col + 1),
                GetTileByRowColumn(row + 2, col + 1),

                GetTileByRowColumn(row - 1, col - 2),
                GetTileByRowColumn(row + 1, col - 2),
                GetTileByRowColumn(row - 1, col + 2),
                GetTileByRowColumn(row + 1, col + 2),
            };

            return potentialTiles.Where((t) => t != null).ToList();
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

        /// <summary>
        /// Determine which tile a given chessman can be captured from if it were
        /// to be attacked in a straight line from a given line of tiles. E.g. if
        /// there was an opponent chessman within the same column. This does NOT
        /// include jumping.
        ///
        /// Note that this is "generic" in that we aren't checking what type of
        /// opponent chessman it is; so if we were checking tiles of the same ROW
        /// and found an opponent KNIGHT then this would return true.
        ///
        /// It only returns a single tile, which is the closest tile to the target
        /// chessman that has an opponent piece. No other tiles are needed because
        /// the path would be blocked by the first opponent chessman that appears.
        /// </summary>
        /// <returns>The chessman be captured from direction subset2.</returns>
        /// <param name="chessman">Chessman.</param>
        /// <param name="tilesInDirection">Tiles in direction.</param>
        Tile CanChessmanBeCapturedFromDirectionSubset2 (
            Chessman chessman,
            List<Tile> tilesInDirection
        ) {
            for (int i = 0; i < tilesInDirection.Count; i++) {
                Tile t = tilesInDirection[i];
                if (!t.IsOccupied()) {
                    continue;
                }

                Chessman otherPiece = t.GetPiece();
                if (otherPiece.IsSameColor(chessman)) {
                    return null;
                } else if (otherPiece.IsChecker()) {
                    return null;
                }

                return t;
            }

            return null;
        }

        List<Tile> CanChessmanBeCapturedFromDirection2 (
            Chessman chessman,
            Directionality directionality
        ) {
            Tile baseTile = chessman.GetUnderlyingTile();
            List<Tile> tilesToCheck = new List<Tile> { };

            List<Directionality> directionalities = new List<Directionality>();
            Func<Tile, bool> additionalValidator;

            if (directionality == Directionality.HORIZONTAL) {
                directionalities.Add(Directionality.HORIZONTAL_LEFT);
                directionalities.Add(Directionality.HORIZONTAL_RIGHT);
            } else if (directionality == Directionality.VERTICAL) {
                directionalities.Add(Directionality.VERTICAL_ABOVE);
                directionalities.Add(Directionality.VERTICAL_BELOW);
            } else if (directionality == Directionality.POSITIVE_DIAGONAL) {
                directionalities.Add(Directionality.POSITIVE_DIAGONAL_ABOVE);
                directionalities.Add(Directionality.POSITIVE_DIAGONAL_BELOW);
            } else if (directionality == Directionality.NEGATIVE_DIAGONAL) {
                directionalities.Add(Directionality.NEGATIVE_DIAGONAL_ABOVE);
                directionalities.Add(Directionality.NEGATIVE_DIAGONAL_BELOW);
            }

            if (
                directionality == Directionality.POSITIVE_DIAGONAL ||
                directionality == Directionality.NEGATIVE_DIAGONAL
            ) {
                additionalValidator = (t) => {
                    (int rowDelta, int colDelta) = CalculateRowColumnDelta(baseTile, t);
                    Chessman otherChessman = t.GetPiece();

                    if (otherChessman.IsPawn()) {
                        return (
                            (otherChessman.IsBlack() ? (rowDelta == -1) : (rowDelta == 1)) &&
                            (Math.Abs(colDelta) == 1)
                        );
                    }

                    return otherChessman.IsBishop() || otherChessman.IsQueen();
                };
            } else {
                additionalValidator = (t) => t.GetPiece().IsQueen() || t.GetPiece().IsRook();
            }

            foreach (Directionality partialDir in directionalities) {
                tilesToCheck.Add(
                    CanChessmanBeCapturedFromDirectionSubset2(
                        chessman,
                        GetTilesInDirection(baseTile, partialDir)
                    )
                );
            }

            return tilesToCheck.Where((Tile otherTile) => {
                return (
                    otherTile != null &&
                    additionalValidator(otherTile)
                );
            }).ToList();
        }

        /*
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
                if (occupant.color == chessman.color || occupant.IsChecker()) {
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
            Directionality dir,
            bool exitEarly
        ) {
            Tile tile = chessman.GetUnderlyingTile();
            Func<Chessman, bool> additionalValidator = null;

            List<Tile> capturableTiles = new List<Tile>();

            if (dir == Directionality.HORIZONTAL) {
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
            } else if (dir == Directionality.VERTICAL) {
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
            } else if (dir == Directionality.POSITIVE_DIAGONAL) {
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
            } else if (dir == Directionality.NEGATIVE_DIAGONAL) {
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

            if (dir == Directionality.NEGATIVE_DIAGONAL || dir == Directionality.POSITIVE_DIAGONAL) {
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
        */

        public List<Tile> CanChessmanBeCapturedVertically (Chessman chessman) {
            return CanChessmanBeCapturedFromDirection2(
                chessman,
                Directionality.VERTICAL
            );
        }

        public List<Tile> CanChessmanBeCapturedHorizontally (Chessman chessman) {
            return CanChessmanBeCapturedFromDirection2(
                chessman,
                Directionality.HORIZONTAL
            );
        }

        public List<Tile> CanChessmanBeCapturedDiagonally (Chessman chessman) {
            return CanChessmanBeCapturedFromDirection2(
                chessman,
                Directionality.HORIZONTAL
            );
        }

        public List<Tile> CanChessmanBeCaptured (Chessman chessman) {
            return (
                CanChessmanBeCapturedVertically(chessman)
                .Concat(CanChessmanBeCapturedHorizontally(chessman))
                .Concat(CanChessmanBeCapturedDiagonally(chessman))
                .Concat(
                    GetTilesForKnightMovement(chessman.GetUnderlyingTile())
                    .Where((t) => (t.IsOccupied() && !t.GetPiece().IsSameColor(chessman)))
                )
                .ToList()
            );
        }

        #endregion

        #region Can jump?

        bool CanChessmanBeJumpedFromTile (Chessman targetChessman, Tile jumpFromTile) {
            if (jumpFromTile == null || !jumpFromTile.IsOccupied()) {
                return false;
            }

            Tile tileToJumpOver = targetChessman.GetUnderlyingTile();

            // -- Validate the position of the specified tile.
            // Ths includes it being 1 row, 1 column away, and that there is a
            // tile to jump onto (i.e. the chessman is not along a board edge)

            if (
                (CalculateRowDelta(tileToJumpOver, jumpFromTile) != 1) ||
                (CalculateColumnDelta(tileToJumpOver, jumpFromTile) != 1)
            ) {
                return false;
            }

            int delta = jumpFromTile.id - tileToJumpOver.id;

            // The distance from the jumping tile to the tile being jumped
            // over is the same distance as the tile being jumped over to the
            // tile being jumped onto.
            int tileJumpedOntoLocation = tileToJumpOver.id - delta;
            if (tileJumpedOntoLocation < 0 || tileJumpedOntoLocation > GetMaxTileNumber()) {
                return false;
            }

            Tile tileJumpedOnto = GetTile(tileJumpedOntoLocation);
            if (
                (CalculateRowDelta(tileToJumpOver, tileJumpedOnto) != 1) ||
                (CalculateColumnDelta(tileToJumpOver, tileJumpedOnto) != 1)
            ) {
                return false;
            }

            // -- Validate the attrs of the occupant doing the jump
            // (They have to be a different colour, and currently a checker)
            Chessman jumpingChessman = jumpFromTile.GetPiece();
            if (jumpingChessman.color == targetChessman.color || !jumpingChessman.IsChecker()) {
                return false;
            }

            // -- Validate that the tile being jumped onto is NOT occupied
            if (tileJumpedOnto.IsOccupied()) {
                return false;
            }

            // -- All checks passed. Finally check that the movement itself is legal, depending
            // on the kinged status of the jumping piece.
            if (jumpingChessman.isKinged) {
                return true;
            }

            if (targetChessman.IsWhite()) {
                // The chkecer doing the jump can only jump from top towards bottom
                return (GetRow(jumpFromTile) > GetRow(tileToJumpOver));
            } else {
                // Bottom towards top
                return GetRow(jumpFromTile) < GetRow(tileToJumpOver);
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
            (int row, int col) = GetRowColumn(tile);

            return new List<Tile>() {
                GetTileByRowColumn(row + 1, col + 1),
                GetTileByRowColumn(row + 1, col - 1),
                GetTileByRowColumn(row - 1, col + 1),
                GetTileByRowColumn(row - 1, col - 1),
            }.Where((t) => t != null).ToList();
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

            if (directionality == (int) Directionality.HORIZONTAL) {
                stepSize = 1;
            } else if (directionality == (int) Directionality.VERTICAL) {
                stepSize = 8;
            } else if (directionality == (int) Directionality.POSITIVE_DIAGONAL) {
                stepSize = 9;
            } else if (directionality == (int) Directionality.NEGATIVE_DIAGONAL) {
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

        //TODO -- this code fucking sucks. Maybe use an iterator (yield) instead or something??

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

        List<Tile> PotentialTilesForHorizontalMovement (Chessman chessman, int limit = int.MaxValue) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            (int row, int col) = GetRowColumn(tile);

            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: col + 1,
                loopConditional: (iter) => iter < GetNumberOfColumns(),
                currentTileGetter: (iter) => GetTileByRowColumn(row, iter),
                limit: limit
            ));
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: col - 1,
                loopConditional: (iter) => iter >= 0,
                currentTileGetter: (iter) => GetTileByRowColumn(row, iter),
                increasing: false,
                limit: limit
            ));
            return potentialTiles;
        }

        List<Tile> PotentialTilesForVerticalMovement (Chessman chessman, int limit = int.MaxValue) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            (int row, int col) = GetRowColumn(tile);

            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: row + 1,
                loopConditional: (iter) => iter < GetNumberOfRows(),
                currentTileGetter: (iter) => GetTileByRowColumn(iter, col),
                limit: limit
            ));
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: row - 1,
                loopConditional: (iter) => iter >= 0,
                currentTileGetter: (iter) => GetTileByRowColumn(iter, col),
                increasing: false,
                limit: limit
            ));
            return potentialTiles;
        }

        List<Tile> PotentialTilesForDiagonalMovement (Chessman chessman, int limit = int.MaxValue) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            (int row, int col) = GetRowColumn(tile);

            int i;

            // Positive diagonal: up-and-right
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row + iter) < GetNumberOfRows()) && ((col + iter) < GetNumberOfColumns());
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row + iter, col + iter),
                limit: limit
            ));

            // Positive diagonal: down-and-left
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row - iter) >= 0) && ((col - iter) >= 0);
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row - iter, col - iter),
                limit: limit
            ));

            // negative diagonal: down-and-right
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row - iter) >= 0) && ((col + iter) < GetNumberOfColumns());
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row - iter, col + iter),
                limit: limit
            ));

            // negative diagonal: up-and-left
            i = 1;
            potentialTiles.AddRange(PotentialTileIterator(
                chessman,
                iter: i,
                loopConditional: (iter) => {
                    return ((row + iter) < GetNumberOfRows()) && ((col - iter) >= 0);
                },
                currentTileGetter: (iter) => GetTileByRowColumn(row + iter, col - iter),
                limit: limit
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
            int row = GetRow(tile);
            int col = GetColumn(tile);

            int modifier = (chessman.IsBlack()) ? -1 : 1;

            Tile tileForRegularMovement = GetTile(tile.id + modifier * GetNumberOfColumns());
            if (!tileForRegularMovement.IsOccupied()) {
                potentialTiles.Add(tileForRegularMovement);
            }

            Tile tileForLongMovement = GetTile(tile.id + 2 * modifier * GetNumberOfColumns());
            if (
                !chessman.hasMoved &&
                !tileForLongMovement.IsOccupied() &&
                !tileForRegularMovement.IsOccupied()
            ) {
                potentialTiles.Add(tileForLongMovement);
            }

            //int? captureDelta1 = null, captureDelta2 = null;

            //if (chessman.IsBlack()) {
            //    // The rightmost column is actually the left side for black...
            //    if (!IsTileInRightmostColumn(tile)) {
            //        captureDelta1 = -1 * GetNegativeDiagonalDelta();
            //    }
            //    if (!IsTileInLeftmostColumn(tile)) {
            //        captureDelta2 = -1 * GetPositiveDiagonalDelta();
            //    }
            //} else {
            //    if (!IsTileInRightmostColumn(tile)) {
            //        captureDelta1 = GetPositiveDiagonalDelta();
            //    }
            //    if (!IsTileInLeftmostColumn(tile)) {
            //        captureDelta2 = GetNegativeDiagonalDelta();
            //    }
            //}

            Tile captureTile1 = GetTileByRowColumn(row + modifier, col + 1);
            Tile captureTile2 = GetTileByRowColumn(row + modifier, col - 1);

            foreach (Tile captureTile in new Tile[] { captureTile1, captureTile2 }) {
                if (captureTile == null || !captureTile.IsOccupied() || captureTile.GetPiece().IsSameColor(chessman)) {
                    continue;
                }

                (int rowDelta, int colDelta) = CalculateRowColumnDelta(tile, captureTile);
                if (rowDelta != 1 || colDelta != 1) {
                    continue;
                }

                potentialTiles.Add(captureTile);
            }
            return potentialTiles;
        }

        List<Tile> GetPotentialTilesForKnightMovement (Chessman chessman) {
            return GetTilesForKnightMovement(chessman.GetUnderlyingTile())
                .Where((t) => !t.IsOccupied() || (!t.GetPiece().IsSameColor(chessman.color)))
                .ToList();
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
        /// Kings can move only by 1 tile at a time.
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

            /**
            // OLD LOGIC: The king used to only be above to move forward/forward diagonal.

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

            */

            potentialTiles.AddRange(PotentialTilesForVerticalMovement(chessman, limit: 1));
            potentialTiles.AddRange(PotentialTilesForHorizontalMovement(chessman, limit: 1));
            potentialTiles.AddRange(PotentialTilesForDiagonalMovement(chessman, limit: 1));

            return potentialTiles;
        }

        List<Tile> GetPotentialTilesForCheckerMovement (Chessman chessman) {
            List<Tile> potentialTiles = new List<Tile>();
            Tile tile = chessman.GetUnderlyingTile();
            int modifier = (chessman.IsBlack()) ? -1 : 1;

            List<int> regularMovementDeltas = new List<int>() {
                modifier * rightDiagonalDelta,
                modifier * leftDiagonalDelta,
            };

            List<int> jumpMovementDeltas = new List<int>() {
                2 * modifier * rightDiagonalDelta,
                2 * modifier * leftDiagonalDelta,
            };

            if (chessman.isKinged) {
                regularMovementDeltas.AddRange(regularMovementDeltas.Select((int d) => -1 * d).ToList());
                jumpMovementDeltas.AddRange(jumpMovementDeltas.Select((int d) => -1 * d).ToList());
            }

            foreach (int d in regularMovementDeltas) {
                Tile potentialTile = GetTileIfExists(tile.id + d);
                if (potentialTile == null) { continue; }

                (int rowDelta, int colDelta) = CalculateRowColumnDelta(tile, potentialTile);
                if (
                    !potentialTile.IsOccupied() &&
                    rowDelta == 1 &&
                    colDelta == 1
                ) {
                    potentialTiles.Add(potentialTile);
                }
            }

            foreach (int d in jumpMovementDeltas) {
                Tile potentialTile = GetTileIfExists(tile.id + d);
                Tile jumpOverTile = GetTileIfExists(tile.id + (d / 2));
                if (potentialTile == null || jumpOverTile == null) { continue; }

                (int farRowDelta, int farColDelta) = CalculateRowColumnDelta(tile, potentialTile);
                (int shortRowDelta, int shortColDelta) = CalculateRowColumnDelta(tile, jumpOverTile);

                if (
                    potentialTile.IsOccupied() ||
                    !jumpOverTile.IsOccupied() ||
                    jumpOverTile.GetPiece().IsSameColor(chessman) ||
                    farRowDelta != 2 ||
                    farColDelta != 2 ||
                    shortRowDelta != 1 ||
                    shortColDelta != 1
                ) {
                    continue;
                }

                // The tile that the piece is jumping OVER has a piece of the opposite color
                potentialTiles.Add(potentialTile);
            }

            return potentialTiles;
        }

        public List<Tile> GetPotentialTilesForMovement (Chessman chessman) {
            Func<Chessman, List<Tile>> potentialTilesGetter = null;

            if (chessman.IsChecker()) {
                potentialTilesGetter = GetPotentialTilesForCheckerMovement;
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

        public (int, int) CalculateRowColumnDelta (Tile tile1, Tile tile2) {
            (int row1, int col1) = GetRowColumn(tile1);
            (int row2, int col2) = GetRowColumn(tile2);

            return (
                Math.Abs(row1 - row2),
                Math.Abs(col1 - col2)
            );
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

        public void UndoMove (MoveResult moveResult) {
            Chessman movedChessman = GetChessman(moveResult.pieceId);
            Tile fromTile = GetTile(moveResult.fromTileId);
            Tile toTile = GetTile(moveResult.tileId);

            toTile.RemovePiece();

            fromTile.SetPiece(movedChessman);
            movedChessman.SetUnderlyingTile(fromTile);

            if (moveResult.promotionOccurred) {
                movedChessman.kind = moveResult.chessmanKind;
            }

            if (moveResult.kinged) {
                movedChessman.isKinged = false;
            }

            if (moveResult.polarityChanged) {
                movedChessman.isChecker = !movedChessman.isChecker;
            }

            if (moveResult.wasFirstMoveForPiece) {
                movedChessman.hasMoved = false;
            }

            if (moveResult.WasPieceJumped()) {
                Chessman jumpedChessman = GetChessman(moveResult.jumpedPieceId);
                Tile jumpedOverTile = GetTile(moveResult.jumpedTileId);
                jumpedChessman.isActive = true;

                jumpedOverTile.SetPiece(jumpedChessman);
                jumpedChessman.SetUnderlyingTile(jumpedOverTile);
            }

            if (moveResult.WasPieceCaptured()) {
                Chessman capturedChessman = GetChessman(moveResult.capturedPieceId);
                capturedChessman.isActive = true;

                toTile.SetPiece(capturedChessman);
                capturedChessman.SetUnderlyingTile(toTile);
            }
        }

        #endregion

        const int VALUE_PAWN = 100;
        const int VALUE_KNIGHT = 350;
        const int VALUE_BISHOP = 350;
        const int VALUE_ROOK = 525;
        const int VALUE_QUEEN = 1000;
        const int VALUE_KING = 10000;

        public int CalculateBoardValue () {
            if (!GetWhiteKing().isActive) {
                return int.MinValue;
            } else if (!GetBlackKing().isActive) {
                return int.MaxValue;
            }

            int result = 0;

            foreach (Chessman c in GetActiveChessmen()) {
                int temp = 0;
                if (c.IsPawn()) {
                    temp = VALUE_PAWN;
                } else if (c.IsKnight()) {
                    temp = VALUE_KNIGHT;
                } else if (c.IsBishop()) {
                    temp = VALUE_BISHOP;
                } else if (c.IsRook()) {
                    temp = VALUE_ROOK;
                } else if (c.IsQueen()) {
                    temp = VALUE_QUEEN;
                } else if (c.IsKing()) {
                    temp = VALUE_KING;
                } else if (c.IsChecker()) {
                    //TODO
                }

                if (c.IsBlack()) {
                    temp = -1 * temp;
                }

                result += temp;
            }

            return result;
        }
    }
}
