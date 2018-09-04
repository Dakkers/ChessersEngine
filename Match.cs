using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ChessersEngine {
    public class MatchData {
        public long blackPlayerId;
        public int currentTurn;
        public List<ChessmanSchema> pieces;
        public long whitePlayerId;

        // TODO -- specify check status for players
    }

    public class Match {
        private int turn;

        public Dictionary<int, Tile> tilesById;
        public Dictionary<long, Chessman> chessmenById;

        public List<string> moves = new List<string>();

        public long whitePlayerId = -1;
        public long blackPlayerId = -1;

        public Match (MatchData data) {
            tilesById = new Dictionary<int, Tile>();
            chessmenById = new Dictionary<long, Chessman>();

            for (int i = 0; i < 64; i++) {
                tilesById[i] = new Tile {
                    id = i
                };
            }

            List<ChessmanSchema> pieces;
            if (data == null) {
                pieces = CreateDefaultChessmen();
            } else {
                pieces = data.pieces;
                whitePlayerId = data.whitePlayerId;
                blackPlayerId = data.blackPlayerId;
            }

            foreach (ChessmanSchema cs in pieces) {
                Chessman c = Chessman.CreateFromSchema(cs);
                Tile underlyingTile = GetTile(cs.location);

                c.underlyingTile = underlyingTile;
                underlyingTile.SetPiece(c);

                chessmenById[c.id] = c;
            }
        }

        // TODO
        public void Save () {
            List<ChessmanSchema> chessmanSchemas = new List<ChessmanSchema>();

            foreach (KeyValuePair<long, Chessman> pair in chessmenById) {
                Chessman chessman = pair.Value;

                chessmanSchemas.Add(chessman.CreateSchema());
            }
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

        private void CreateChessman (ChessmanSchema cs) {
            //Chessman c = new Chessman { id = id };
            //chessmenById[id] = c;
        }

        public Chessman GetChessman (long id) {
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

        public MoveResult MoveChessman (long playerId, long chessmanId, int toTileId) {
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
                Tile tileWithRemovedPiece = pieceToRemove.underlyingTile;
                tileWithRemovedPiece?.RemovePiece();
            }
        }

        public void UpdateFromMoveResult (MoveResult moveResult) {
            Chessman chessman = GetChessman(moveResult.pieceId);
            Tile toTile = GetTile(moveResult.tileId);

            HandleJumpAndCapture(moveResult);

            // Valid move; remove piece from current tile, move it to new tile
            chessman.SetHasMoved(true);
            chessman.underlyingTile.RemovePiece();
            toTile.SetPiece(chessman);

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
