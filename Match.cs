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

        public bool isDraw = false;
        public bool isResignation = false;

        public long matchId;

        public List<ChessmanSchema> pieces;
        public long whitePlayerId;
        public long winningPlayerId = -1;
    }

    public struct MatchCloningResult {
        public Dictionary<int, Tile> tilesById;
        public Dictionary<int, Chessman> chessmenById;
    }

    public class Match {
        long id = -1;

        /// <summary>
        /// The "effective" turn color. When a player's turn begins and they make a move
        /// ends their turn (i.e. a move where they cannot do a jump), this value switches.
        /// The value must be switched while moves are being made instead of at the "commit"
        /// step because otherwise the engine would never say the move is invalid based
        /// solely on this value.
        /// </summary>
        ColorEnum turnColor;

        /// <summary>
        /// The color of the player currently making moves. Switches values when the "commit"
        /// step occurs.
        /// </summary>
        ColorEnum committedTurnColor;

        /// <summary>
        /// The list of moves that have been executed in `pendingBoard` but not
        /// executed in `committedBoard`.
        /// </summary>
        List<MoveResult> pendingMoveResults = new List<MoveResult>();

        Board pendingBoard;
        Board committedBoard;

        List<string> moves = new List<string>();

        public long whitePlayerId = -1;
        public long blackPlayerId = -1;
        long winningPlayerId = -1;
        bool isDraw = false;
        bool isResignation = false;
        System.Random rng = new System.Random();

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
                id = data.matchId;
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
                turnColor = ColorEnum.WHITE;
            } else if (playerId == blackPlayerId) {
                turnColor = ColorEnum.BLACK;
            }
        }

        void CommitMatchState () {
            // It's possible that the final pending move was a checker jumping over
            // a piece, which does NOT automatically change the turn colour.
            if (committedTurnColor == turnColor) {
                ChangeTurn();
            }

            committedTurnColor = turnColor;

            committedBoard.CopyState(pendingBoard);

            if (!committedBoard.GetBlackKing().isActive) {
                winningPlayerId = whitePlayerId;
            } else if (!committedBoard.GetWhiteKing().isActive) {
                winningPlayerId = blackPlayerId;
            }

            foreach (MoveResult moveResult in pendingMoveResults) {
                moves.Add(moveResult.CreateNotation());
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

        public ColorEnum GetCommittedTurnColor () {
            return committedTurnColor;
        }

        public ColorEnum GetTurn () {
            return turnColor;
        }

        public void ChangeTurn () {
            if (turnColor == Constants.ID_WHITE) {
                turnColor = ColorEnum.BLACK;
            } else {
                turnColor = ColorEnum.WHITE;
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

        public List<string> GetMoves () {
            return moves;
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

        public long GetWinner () {
            return winningPlayerId;
        }

        public bool HasWinner () {
            return winningPlayerId >= 0;
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

            if (turnColor == ColorEnum.BLACK) {
                if (moveAttempt.playerId == whitePlayerId) {
                    // Black's turn, white is trying to move --> no!
                    return null;
                }
            }

            Chessman chessman = GetPendingChessman(moveAttempt.pieceId);
            if (chessman.color != turnColor) {
                // Make sure the moving piece belongs to the player.
                return null;
            }

            Tile toTile = GetPendingTile(moveAttempt.tileId);

            Chessman targetChessman = toTile.occupant;
            if (targetChessman?.color == chessman.color) {
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

        /// <summary>
        /// Promote a piece.
        /// </summary>
        /// <param name="moveResult">Move result.</param>
        public void Promote (MoveResult moveResult) {
            Chessman c = pendingBoard.GetChessman(moveResult.pieceId);
            c.kind = moveResult.promotionRank;
        }

        #endregion

        #region Move generation?

        /// <summary>
        /// Minimax!
        /// </summary>
        /// <param name="b">Board.</param>
        /// <param name="depth">Depth.</param>
        /// <param name="isMaximizingPlayer">TRUE = WHITE, FALSE = BLACK</param>
        (List<MoveAttempt>, int) MinimaxHelper (
            Board board,
            int depth,
            bool isMaximizingPlayer
        ) {
            if (depth == 0 || board.IsGameOver()) {
                return (null, board.CalculateBoardValue());
            }

            ColorEnum color = isMaximizingPlayer ? ColorEnum.WHITE : ColorEnum.BLACK;
            // This is a LIST of best moves because in Chessers, you can have multiple moves in a single turn.
            // (If this was Chess, it would not be a list, but a single value.)
            List<MoveAttempt> bestMoves = new List<MoveAttempt>();
            // Even though we have a list of best moves, the best score will still be a single value; it could
            // be either from doing multiple moves in a turn, or by ending the turn early.
            int bestValueSoFar = isMaximizingPlayer ? int.MinValue : int.MaxValue;

            MoveAttempt fallbackMove = null;

            List<Chessman> availableChessmen = board.GetActiveChessmenOfColor(color);
            Helpers.Shuffle(rng, availableChessmen);

            foreach (var chessman in availableChessmen) {
                List<Tile> potentialTiles = board.GetPotentialTilesForMovement(chessman);
                Helpers.Shuffle(rng, potentialTiles);
                //Match.Log($"Looking @ chessman for tile {committedBoard.GetRowColumn(chessman.GetUnderlyingTile())}. Found:" +
                //$"{potentialTiles.Count} potential moves.");

                foreach (var tile in potentialTiles) {
                    MoveAttempt moveAttempt = new MoveAttempt {
                        pieceGuid = chessman.guid,
                        pieceId = chessman.id,
                        playerId = isMaximizingPlayer ? whitePlayerId : blackPlayerId,
                        tileId = tile.id
                    };

                    MoveResult moveResult = board.MoveChessman(moveAttempt);
                    if (moveResult == null || !moveResult.valid) {
                        // This should not occur
                        continue;
                    }

                    if (fallbackMove == null) {
                        fallbackMove = moveAttempt;
                    }

                    // Because the move might not have changed the turn, we need to determine if it's best
                    // that the player stops here, or continues moving.
                    // TODO! -- this should be doable by just passing in `isMaximizingPlayer` is the same value...

                    (List<MoveAttempt> moves, int value) = MinimaxHelper(
                        board,
                        depth - 1,
                        !isMaximizingPlayer
                    );

                    moves = moves ?? new List<MoveAttempt>();
                    moves.Add(moveAttempt);

                    if (isMaximizingPlayer) {
                        if (value > bestValueSoFar) {
                            bestValueSoFar = value;
                            bestMoves = moves;
                        }
                    } else {
                        if (value < bestValueSoFar) {
                            //Match.Log($"  Overriding best choice: {value} {moves?.Count}");
                            bestValueSoFar = value;
                            bestMoves = moves;
                        }
                    }

                    board.UndoMove(moveResult);
                }
            }

            return (
                (bestMoves?.Count ?? 0) == 0 ? new List<MoveAttempt> { fallbackMove } : bestMoves,
                bestValueSoFar
            );
        }

        /// <summary>
        /// Calculates the best move for the current player.
        /// </summary>
        public List<MoveAttempt> CalculateBestMove () {
            Board boardClone = new Board(committedBoard.GetChessmanSchemas());
            boardClone.CopyState(committedBoard);

            (List<MoveAttempt> moves, int value) = MinimaxHelper(
                boardClone,
                1,
                isMaximizingPlayer: turnColor == ColorEnum.WHITE
            );

            //Log($"BEST CALCULATION: {value} NUM MOVES TO MAKE: {moves?.Count}");
            //if (moves?.Count > 0) {
            //    Log(moves[0]);
            //}

            return moves;
        }

        public List<MoveResult> DoBestMovesForCurrentPlayer () {
            List<MoveAttempt> moveAttempts = CalculateBestMove();
            List<MoveResult> moveResults = new List<MoveResult>();

            foreach (var attempt in moveAttempts) {
                MoveResult moveResult = MoveChessman(attempt);
                //Match.Log(moveResult);

                if (moveResult == null || !moveResult.valid) {
                    break;
                }

                moveResults.Add(moveResult);
            }

            return moveResults;
        }

        public void UndoMoves () {
            for (int i = pendingMoveResults.Count - 1; i >= 0; i--) {
                pendingBoard.UndoMove(pendingMoveResults[i]);
            }
            pendingMoveResults.Clear();
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

        public MatchData CreateMatchData () {
            return new MatchData {
                pieces = committedBoard.GetChessmanSchemas(),
                blackPlayerId = blackPlayerId,
                currentTurn = Helpers.ConvertColorEnumToInt(turnColor),
                isDraw = isDraw,
                isResignation = isResignation,
                matchId = id,
                whitePlayerId = whitePlayerId,
                winningPlayerId = winningPlayerId,
            };
        }

        public static void Log (object s) {
#if UNITY_EDITOR
            Debug.Log(s.ToString());
#endif
        }
    }
}
