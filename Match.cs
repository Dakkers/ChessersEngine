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
    }

    public struct MatchCloningResult {
        public Dictionary<int, Tile> tilesById;
        public Dictionary<int, Chessman> chessmenById;
    }

    public class Match {
        /// <summary>
        /// The "effective" turn color. When a player's turn begins and they make a move
        /// ends their turn (i.e. a move where they cannot do a jump), this value switches.
        /// The value must be switched while moves are being made instead of at the "commit"
        /// step because otherwise the engine would never say the move is invalid based
        /// solely on this value.
        /// </summary>
        int turnColor;

        /// <summary>
        /// The color of the player currently making moves. Switches values when the "commit"
        /// step occurs.
        /// </summary>
        int committedTurnColor;

        /// <summary>
        /// The list of moves that have been executed in `pendingBoard` but not
        /// executed in `committedBoard`.
        /// </summary>
        List<MoveResult> pendingMoveResults = new List<MoveResult>();

        Board pendingBoard;
        Board committedBoard;

        public List<string> moves = new List<string>();

        public long whitePlayerId = -1;
        public long blackPlayerId = -1;
        long winningPlayer = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ChessersEngine.Match"/> class.
        /// </summary>
        /// <param name="data">The data to initialize the match with. If null, a new match is created.</param>
        public Match (MatchData data) {
            List<ChessmanSchema> pieces = null;
            if (data == null) {
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID;
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID;
                turnColor = Constants.ID_WHITE;
            } else {
                pieces = data.pieces;
                whitePlayerId = data.whitePlayerId;
                blackPlayerId = data.blackPlayerId;
                SetTurnColorFromPlayerId(data.currentTurn);
            }

            committedTurnColor = turnColor;

            pendingBoard = new Board(pieces);
            committedBoard = new Board(pieces);
        }

        // TODO
        public void Save () {
        }

        void SetTurnColorFromPlayerId (long playerId) {
            if (playerId == whitePlayerId) {
                turnColor = Constants.ID_WHITE;
            } else if (playerId == blackPlayerId) {
                turnColor = Constants.ID_BLACK;
            }
        }

        void CommitMatchState () {
            committedTurnColor = turnColor;

            committedBoard.CopyState(pendingBoard);

            if (!committedBoard.GetBlackKing().isActive) {
                winningPlayer = whitePlayerId;
            } else if (!committedBoard.GetWhiteKing().isActive) {
                winningPlayer = blackPlayerId;
            }

            pendingMoveResults.Clear();
        }

        /// <summary>
        /// Resets the state of the match (chessmen, tiles) to what they were at the beginning
        /// of the turn. Identical to `CommitMatchState`, but the pending objects copy FROM the
        /// committed objects, instead of vice versa.
        /// </summary>
        void ResetMatchState () {
            turnColor = committedTurnColor;

            pendingBoard.CopyState(committedBoard);

            pendingMoveResults.Clear();
        }

        public void CommitTurn () {
            CommitMatchState();
        }

        public void ResetTurn () {
            ResetMatchState();
        }

        public Board CopyPendingBoard () {
            Board pendingBoardCopy = new Board(pendingBoard.GetChessmanSchemas());
            pendingBoardCopy.CopyState(pendingBoard);

            return pendingBoardCopy;
        }

        #region Getters / Setters

        public Chessman GetPendingChessman (int id) {
            return pendingBoard.GetChessman(id);
        }

        public Chessman GetCommittedChessman (int id) {
            return committedBoard.GetChessman(id);
        }

        public Dictionary<int, Chessman> GetAllCommittedChessmen () {
            return committedBoard.GetAllChessmen();
        }

        public Dictionary<int, Chessman> GetAllPendingChessmen () {
            return pendingBoard.GetAllChessmen();
        }

        public Tile GetPendingTile (int id) {
            return pendingBoard.GetTile(id);
        }

        public Tile GetCommittedTile (int id) {
            return committedBoard.GetTile(id);
        }

        public int GetCommittedTurnColor () {
            return committedTurnColor;
        }

        public int GetTurn () {
            return turnColor;
        }

        public void ChangeTurn () {
            if (turnColor == Constants.ID_WHITE) {
                turnColor = Constants.ID_BLACK;
            } else {
                turnColor = Constants.ID_WHITE;
            }
        }

        public bool IsWhitePlayerSet () {
            return whitePlayerId != -1;
        }

        public bool IsBlackPlayerSet () {
            return blackPlayerId != -1;
        }

        public List<MoveResult> GetPendingMoveResults () {
            return pendingMoveResults;
        }

        /// <summary>
        /// Gets the ID of the player whose turn it is.
        /// </summary>
        /// <returns>The turn player identifier.</returns>
        public long GetCommittedTurnPlayerId () {
            if (committedTurnColor == Constants.ID_WHITE) {
                return whitePlayerId;
            } else {
                return blackPlayerId;
            }
        }

        #endregion

        #region Move-related

        public MoveResult MoveChessman (MoveAttempt moveAttempt) {
            // -- Base validation
            if (turnColor == Constants.ID_WHITE) {
                if (moveAttempt.playerId == blackPlayerId) {
                    // White's turn, black is trying to move --> no!
                    return null;
                }
            }

            if (turnColor == Constants.ID_BLACK) {
                if (moveAttempt.playerId == whitePlayerId) {
                    // Black's turn, white is trying to move --> no!
                    return null;
                }
            }

            Chessman chessman = GetPendingChessman(moveAttempt.pieceId);
            if (chessman.colorId != turnColor) {
                // Make sure the moving piece belongs to the player.
                return null;
            }

            Tile toTile = GetPendingTile(moveAttempt.tileId);

            Chessman targetChessman = toTile.occupant;
            if (targetChessman?.colorId == chessman.colorId) {
                // Can't move to a tile occupied by the moving player
                return null;
            }

            MoveResult moveResult = pendingBoard.MoveChessman(moveAttempt);
            if (moveResult == null || !moveResult.valid) {
                return null;
            }

            moveResult.playerId = moveAttempt.playerId;

            pendingMoveResults.Add(moveResult);

            if (moveResult.turnChanged) {
                ChangeTurn();
            }

            return moveResult;
        }

        #endregion

        public void UpdateMatch (MatchData newMatchData) {
            foreach (ChessmanSchema cs in newMatchData.pieces) {
                Chessman committedChessman = GetCommittedChessman(cs.id);

                committedChessman.CopyFrom(cs);

                if (!committedChessman.isActive) {
                    if (committedChessman.GetUnderlyingTile() != null) {
                        committedChessman.GetUnderlyingTile().RemovePiece();
                    }

                    committedChessman.RemoveUnderlyingTileReference();
                } else {
                    committedChessman.SetUnderlyingTile(GetCommittedTile(cs.location));
                }
            }

            SetTurnColorFromPlayerId(newMatchData.currentTurn);
            committedTurnColor = turnColor;

            ResetMatchState();
        }

        public static void Log (object s) {
#if UNITY_EDITOR
            Debug.Log(s.ToString());
#endif
        }
    }
}
