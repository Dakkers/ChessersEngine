using System;
using System.Collections.Generic;

namespace ChessersEngine {
    public class Board {
        private List<ChessmanSchema> pieces;
        private Dictionary<int, Tile> tilesById;
        private Dictionary<int, Chessman> chessmenById;

        private List<string> moves = new List<string>();

        private List<MoveResult> pendingMoveResults = new List<MoveResult>();

        private int numColumns = 8;
        private int numRows = 8;

        public Board (List<ChessmanSchema> pieces2) {
            pieces = pieces2;
            tilesById = new Dictionary<int, Tile>();
            chessmenById = new Dictionary<int, Chessman>();

            for (int i = 0; i < 64; i++) {
                tilesById[i] = new Tile {
                    id = i
                };
            }

            if (pieces == null) {
                pieces = CreateDefaultChessmen();
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
            return pieces;
        }

        public Chessman GetChessman (int id) {
            return chessmenById[id];
        }

        public Tile GetTile (int id) {
            return tilesById[id];
        }

        public Tile GetTileIfExists (int id) {
            if (tilesById.ContainsKey(id)) {
                return tilesById[id];
            }
            return null;
        }

        private List<ChessmanSchema> CreateDefaultChessmen () {
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

                Tile tile = GetTile(otherChessman.GetUnderlyingTile().id);
                chessman.SetUnderlyingTile(tile);
            }
        }

        public Dictionary<int, Chessman> GetAllChessmen () {
            return chessmenById;
        }

        private Chessman GetKingOfColor (ColorEnum color) {
            if (color == ColorEnum.BLACK) {
                return GetChessman(Constants.ID_BLACK_KING);
            } else {
                return GetChessman(Constants.ID_WHITE_KING);
            }
        }

        public Chessman GetWhiteKing () {
            return GetKingOfColor(ColorEnum.WHITE);
        }

        public Chessman GetBlackKing () {
            return GetKingOfColor(ColorEnum.BLACK);
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

        private bool CanChessmanBeCapturedFromDirection (
            Chessman chessman,
            Directionalities dir
        ) {
            Tile tile = chessman.GetUnderlyingTile();
            Tile startTile, endTile;

            Func<Chessman, bool> additionalValidator = null;

            int stepSize;
            if (dir == Directionalities.HORIZONTAL) {
                stepSize = 1;
                startTile = GetLeftmostTileOfRow(GetRow(tile));
                endTile = GetRightmostTileOfRow(GetRow(tile));

                additionalValidator = (Chessman occupant) => {
                    return !occupant.IsQueen() && !occupant.IsRook();
                };
            } else if (dir == Directionalities.VERTICAL) {
                stepSize = numColumns;
                startTile = GetBottomTileOfColumn(GetColumn(tile));
                endTile = GetTopTileOfColumn(GetColumn(tile));
                //} else if (dir == Directionalities.POSITIVE_DIAGONAL) {
                //    stepSize = GetPositiveDiagonalDelta();
                //} else if (dir == Directionalities.NEGATIVE_DIAGONAL) {
                //stepSize = GetNegativeDiagonalDelta();

                additionalValidator = (Chessman occupant) => {
                    return !occupant.IsQueen() && !occupant.IsRook();
                };
            } else {
                throw new Exception($"Invalid directionality: {dir}");
            }

            for (int i = startTile.id; i <= endTile.id; i += numColumns) {
                if (i == tile.id) {
                    continue;
                }

                Tile tileToCheck = GetTile(i);
                if (!tileToCheck.IsOccupied()) {
                    continue;
                }

                Chessman occupant = tileToCheck.GetPiece();
                if (occupant.colorId == chessman.colorId || occupant.IsChecker()) {
                    break;
                }

                bool additionalValidationResult = additionalValidator(occupant);

                if (!additionalValidationResult) {
                    return true;
                }
            }

            return false;
        }

        public bool CanChessmanBeCapturedVertically (Chessman chessman) {
            return CanChessmanBeCapturedFromDirection(
                chessman,
                Directionalities.VERTICAL
            );
        }

        public bool CanChessmanBeCapturedHorizontally (Chessman chessman) {
            return CanChessmanBeCapturedFromDirection(
                chessman,
                Directionalities.HORIZONTAL
            );
        }

        public bool CanChessmanBeCaptured (Chessman chessman) {
            return (
                CanChessmanBeCapturedVertically(chessman) ||
                CanChessmanBeCapturedHorizontally(chessman)
            );
        }

        private bool CanChessmanBeJumpedFromTile (Chessman targetChessman, Tile attackingTile) {
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

        public bool CanChessmanBeJumped (Chessman chessman) {
            Tile tile = chessman.GetUnderlyingTile();

            List<Tile> diagonallyAdjacentTiles = GetDiagonallyAdjacentTiles(tile);
            foreach (var diagonallyAdjacentTile in diagonallyAdjacentTiles) {
                if (CanChessmanBeJumpedFromTile(chessman, diagonallyAdjacentTile)) {
                    return true;
                }
            }

            return false;
        }

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
    }
}