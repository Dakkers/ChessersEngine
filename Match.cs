using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ChessersEngine {
    public class MatchData {
        public long blackPlayerId;

        /// <summary>
        /// The ID of the user whose turn it is. (One of blackPlayerId, whitePlayerId)
        /// </summary>
        public long currentTurn;

        public long matchId;

        public List<ChessmanSchema> pieces;
        public long whitePlayerId;

        // TODO -- specify check status for players
    }

    public class Match {
        private int turn;

        public Dictionary<int, Tile> tilesById;
        public Dictionary<int, Chessman> chessmenById;

        public Dictionary<int, Tile> pendingTilesById;
        public Dictionary<int, Chessman> pendingChessmenById;

        public List<string> moves = new List<string>();

        public long whitePlayerId = -1;
        public long blackPlayerId = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ChessersEngine.Match"/> class.
        /// </summary>
        /// <param name="data">The data to initialize the match with. If null, a new match is created.</param>
        public Match (MatchData data) {
            tilesById = new Dictionary<int, Tile>();
            chessmenById = new Dictionary<int, Chessman>();

            pendingTilesById = new Dictionary<int, Tile>();
            pendingChessmenById = new Dictionary<int, Chessman>();

            for (int i = 0; i < 64; i++) {
                tilesById[i] = new Tile {
                    id = i
                };

                pendingTilesById[i] = tilesById[i].Clone();
            }

            List<ChessmanSchema> pieces;
            if (data == null) {
                pieces = CreateDefaultChessmen();
            } else {
                pieces = data.pieces;
                whitePlayerId = data.whitePlayerId;
                blackPlayerId = data.blackPlayerId;
                if (data.currentTurn == whitePlayerId) {
                    turn = Constants.ID_WHITE;
                } else if (data.currentTurn == blackPlayerId) {
                    turn = Constants.ID_BLACK;
                }
            }

            foreach (ChessmanSchema cs in pieces) {
                Chessman newChessman = Chessman.CreateFromSchema(cs);
                Tile underlyingTile = GetTile(cs.location);

                newChessman.SetUnderlyingTile(underlyingTile);
                underlyingTile.SetPiece(newChessman);

                chessmenById[newChessman.id] = newChessman;
            }

            foreach (ChessmanSchema cs in pieces) {
                Chessman newChessman = Chessman.CreateFromSchema(cs);
                Tile underlyingTile = pendingTilesById[cs.location];

                newChessman.SetUnderlyingTile(underlyingTile);
                underlyingTile.SetPiece(newChessman);

                pendingChessmenById[newChessman.id] = newChessman;
            }
        }

        // TODO
        public void Save () {
        }

        private List<ChessmanSchema> CreateDefaultChessmen () {
            List<ChessmanSchema> chessmanSchemas = new List<ChessmanSchema>() {
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

        private void CommitMatchState () {
            // Update the states of the Chessmen
            foreach (KeyValuePair<int, Chessman> pair in chessmenById) {
                int chessmanId = pair.Key;
                Chessman pendingChessman = pendingChessmenById[chessmanId];
                Chessman committedChessman = chessmenById[chessmanId];

                committedChessman.CopyFrom(pendingChessman);
            }

            // Update the states of the Tiles, and which Chessmen they reference
            foreach (KeyValuePair<int, Tile> pair in tilesById) {
                int tileId = pair.Key;
                Tile pendingTile = pendingTilesById[tileId];
                Tile committedTile = tilesById[tileId];

                committedTile.CopyFrom(pendingTile);

                if (pendingTile.IsOccupied()) {
                    // If the tile became occupied, update the Chessman reference
                    Chessman newlyCommittedChessman = chessmenById[pendingTile.GetPiece().id];

                    committedTile.SetPiece(newlyCommittedChessman);
                } else {
                    // If the tile is no longer occupied, remove the Chessman reference
                    committedTile.RemovePiece();
                }
            }

            // Update the Tile references for the Chessmen
            foreach (KeyValuePair<int, Chessman> pair in chessmenById) {
                int chessmanId = pair.Key;
                Chessman pendingChessman = pendingChessmenById[chessmanId];
                Chessman committedChessman = chessmenById[chessmanId];

                Tile committedTile = tilesById[pendingChessman.GetUnderlyingTile().id];
                committedChessman.SetUnderlyingTile(committedTile);
            }
        }

        /// <summary>
        /// Resets the state of the match (chessmen, tiles) to what they were at the beginning
        /// of the turn. Identical to `CommitMatchState`, but the pending objects copy FROM the
        /// committed objects, instead of vice versa.
        /// </summary>
        private void ResetMatchState () {
            foreach (KeyValuePair<int, Chessman> pair in chessmenById) {
                int chessmanId = pair.Key;
                Chessman pendingChessman = pendingChessmenById[chessmanId];
                Chessman committedChessman = chessmenById[chessmanId];

                pendingChessman.CopyFrom(committedChessman);
            }

            foreach (KeyValuePair<int, Tile> pair in tilesById) {
                int tileId = pair.Key;
                Tile pendingTile = pendingTilesById[tileId];
                Tile committedTile = tilesById[tileId];

                pendingTile.CopyFrom(committedTile);

                if (committedTile.IsOccupied()) {
                    Chessman committedChessman = chessmenById[committedTile.GetPiece().id];

                    pendingTile.SetPiece(committedChessman);
                } else {
                    // If the tile is no longer occupied, remove the Chessman reference
                    pendingTile.RemovePiece();
                }
            }

            foreach (KeyValuePair<int, Chessman> pair in chessmenById) {
                int chessmanId = pair.Key;
                Chessman pendingChessman = pendingChessmenById[chessmanId];
                Chessman committedChessman = chessmenById[chessmanId];

                Tile committedTile = tilesById[committedChessman.GetUnderlyingTile().id];
                pendingChessman.SetUnderlyingTile(committedTile);
            }
        }

        public void CommitTurn () {

        }

        public Chessman GetChessman (int id) {
            return chessmenById[id];
        }

        public Tile GetTile (int id) {
            return tilesById[id];
        }

        public int GetTurn () {
            return turn;
        }

        public void ChangeTurn () {
            if (turn == Constants.ID_WHITE) {
                turn = Constants.ID_BLACK;
            } else {
                turn = Constants.ID_WHITE;
            }
        }

        public bool IsWhitePlayerSet () {
            return whitePlayerId != -1;
        }

        public bool IsBlackPlayerSet () {
            return blackPlayerId != -1;
        }

        #region Move-related

        public MoveResult GetMoveResult (Chessman chessman, Tile toTile) {
            Move move = new Move(this, chessman, toTile);
            return move.GetMoveResult();
        }

        public MoveResult MoveChessman (long playerId, Chessman chessman, Tile toTile) {
            // -- Base validation
            if (turn == Constants.ID_WHITE) {
                if (playerId == blackPlayerId) {
                    return null;
                }
            }

            if (turn == Constants.ID_BLACK) {
                if (playerId == whitePlayerId) {
                    return null;
                }
            }

            if (chessman.colorId != turn) {
                return null;
            }

            Chessman targetChessman = toTile.occupant;
            if (targetChessman?.colorId == chessman.colorId) {
                return null;
            }

            // -- Check to see if this move is legal
            MoveResult moveResult = GetMoveResult(chessman, toTile);

            UpdateFromMoveResult(moveResult);

            moveResult.playerId = playerId;

            return moveResult;
        }

        public MoveResult MoveChessman (long playerId, int chessmanId, int toTileId) {
            return MoveChessman(playerId, GetChessman(chessmanId), GetTile(toTileId));
        }

        public MoveResult MoveChessman (MoveAttempt moveAttempt) {
            return MoveChessman(moveAttempt.playerId, moveAttempt.pieceId, moveAttempt.tileId);
        }

        public void HandleJumpAndCapture (MoveResult moveResult) {
            Chessman pieceToRemove = null;

            if (moveResult.WasPieceJumped()) {
                pieceToRemove = GetChessman(moveResult.jumpedPieceId);
            } else if (moveResult.WasPieceCaptured()) {
                pieceToRemove = GetChessman(moveResult.capturedPieceId);
            }

            if (pieceToRemove != null) {
                pieceToRemove.SetActive(false);
                Tile tileWithRemovedPiece = pieceToRemove.GetUnderlyingTile();
                tileWithRemovedPiece?.RemovePiece();
            }
        }

        public void UpdateFromMoveResult (MoveResult moveResult) {
            Chessman chessman = GetChessman(moveResult.pieceId);
            Tile fromTile = chessman.GetUnderlyingTile();
            Tile toTile = GetTile(moveResult.tileId);

            HandleJumpAndCapture(moveResult);

            // Valid move; remove piece from current tile, move it to new tile
            chessman.SetHasMoved(true);
            fromTile.RemovePiece();
            toTile.SetPiece(chessman);
            chessman.SetUnderlyingTile(toTile);

            // Most moves result in a change of whose turn it is, EXCEPT for jumping
            bool shouldChangeTurns = !moveResult.WasPieceJumped();

            if (shouldChangeTurns) {
                moveResult.turnChanged = true;
                ChangeTurn();
            }

            // If the moveResult contains information about what the piece was promoted to,
            // handle the promotion here
            if (moveResult.promotionOccurred && Helpers.IsValidPromotion(moveResult.promotionRank)) {
                PromoteChessman(chessman.id, moveResult.promotionRank);
            }
        }

        #endregion

        public void PromoteChessman (int chessmanId, ChessmanKindEnum promotionValue) {
            Chessman chessman = GetChessman(chessmanId);
            chessman.Promote(promotionValue);
        }

        public static void Log (object s) {
#if UNITY_EDITOR
            Debug.Log(s.ToString());
#endif
        }
    }
}
