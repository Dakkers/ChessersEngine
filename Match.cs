﻿using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ChessersEngine {
    public class MatchData {
        public int blackPlayerId;

        /// <summary>
        /// The ID of the user whose turn it is. (One of blackPlayerId, whitePlayerId)
        /// </summary>
        public ChessersEngine.ColorEnum currentTurn;

        public bool isDraw = false;
        public bool isResignation = false;

        public int matchId;
        public string matchGuid;

        public List<string> moves;
        public List<ChessmanSchema> pieces;
        public List<ChessmanSchemaMinified> piecesMinified;
        public int whitePlayerId;
        public int winningPlayerId = -1;
    }

    public struct MatchCloningResult {
        public Dictionary<int, Chessman> chessmenById;
        public Dictionary<int, Tile> tilesById;
    }

    public class Match {
        int id = -1;

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

        // Each element is a different turn; if multiple moves are made in a single turn, the element
        // will be separated by commas
        List<string> moves = new List<string>();

        public int whitePlayerId = -1;
        public int blackPlayerId = -1;
        int winningPlayerId = -1;
        bool isDraw = false;
        bool isResignation = false;
        readonly System.Random rng = new System.Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ChessersEngine.Match"/> class.
        /// </summary>
        /// <param name="data">The data to initialize the match with. If null, a new match is created.</param>
        public Match (MatchData data) {
            List<ChessmanSchema> pieces = null;
            if (data == null) {
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID;
                turnColor = ColorEnum.WHITE;
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID;
            } else {
                blackPlayerId = data.blackPlayerId;
                id = data.matchId;
                isDraw = data.isDraw;
                isResignation = data.isResignation;
                moves = data.moves ?? new List<string>();
                pieces = data.pieces;
                turnColor = data.currentTurn;
                whitePlayerId = data.whitePlayerId;
            }

            committedTurnColor = turnColor;

            pendingBoard = new Board(pieces);
            committedBoard = new Board(pieces);
        }

        void SetTurnColorFromPlayerId (int playerId) {
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

            List<string> movesForTurn = new List<string>();
            foreach (MoveResult moveResult in pendingMoveResults) {
                movesForTurn.Add(moveResult.CreateNotation());
            }

            moves.Add(string.Join(",", movesForTurn));

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
            turnColor = Helpers.GetOppositeColor(turnColor);
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
        public int GetCommittedTurnPlayerId () {
            if (committedTurnColor == ColorEnum.WHITE) {
                return whitePlayerId;
            } else {
                return blackPlayerId;
            }
        }

        public ColorEnum GetColorOfPlayer (int playerId) {
            return (playerId == blackPlayerId) ? ColorEnum.BLACK : ColorEnum.WHITE;
        }

        #endregion

        #region Move-related

        public MoveResult MoveChessman (MoveAttempt moveAttempt) {
            // -- Base validation
            if (turnColor == ColorEnum.WHITE) {
                if (moveAttempt.playerId == blackPlayerId) {
                    //Match.Log("Invalid turn. (is WHITE)");
                    // White's turn, black is trying to move --> no!
                    return null;
                }
            }

            if (turnColor == ColorEnum.BLACK) {
                if (moveAttempt.playerId == whitePlayerId) {
                    //Match.Log("Invalid turn. (is BLACK)");
                    // Black's turn, white is trying to move --> no!
                    return null;
                }
            }

            Chessman chessman = GetPendingChessman(moveAttempt.pieceId);
            if (chessman.color != turnColor) {
                //Match.Log("Invalid permission.");
                // Make sure the moving piece belongs to the player.
                return null;
            }

            Tile toTile = GetPendingTile(moveAttempt.tileId);

            Chessman targetChessman = toTile.occupant;
            //Log($"{moveAttempt.pieceId} - {toTile.id} - {targetChessman?.id}");
            if (targetChessman != null && targetChessman.color == chessman.color) {
                //Match.Log("Trying to move to an occupied tile (by yourself)");
                // Can't move to a tile occupied by the moving player
                return null;
            }

            MoveResult moveResult = pendingBoard.MoveChessman(moveAttempt, jumpsOnly: (pendingMoveResults.Count > 0));
            if (moveResult == null || !moveResult.valid) {
                return null;
            }

            moveResult.playerId = moveAttempt.playerId;
            if (moveAttempt.promotionRank >= 0) {
                moveResult.promotionOccurred = true;
                moveResult.promotionRank = (ChessmanKindEnum) moveAttempt.promotionRank;
                Promote(moveResult);
            }

            if (moveResult.isWinningMove) {
                winningPlayerId = (turnColor == ColorEnum.WHITE) ? whitePlayerId : blackPlayerId;
            }

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
            pendingBoard.Promote(moveResult.pieceId, (ChessmanKindEnum) moveResult.promotionRank);
        }

        public List<MoveResult> GetMovesForLastTurn () {
            //Match.Log($"moves.Count = {moves.Count} | {string.Join(" ", moves)}");
            if (moves.Count == 0) {
                return null;
            }

            List<MoveResult> result = new List<MoveResult>();
            string[] movesOfLastTurn = moves[moves.Count - 1].Split(',');
            bool isMultipleMoves = (movesOfLastTurn.Length > 0);

            // Go in reverse order because the last move will have the piece on the tile
            for (int i = movesOfLastTurn.Length - 1; i >= 0; i--) {
                string moveNotation = movesOfLastTurn[i];
                MoveResult partialResult = MoveResult.CreatePartialMoveResultFromNotation(moveNotation);
                if (partialResult.isCastle) {
                    ColorEnum c = Helpers.GetOppositeColor(GetCommittedTurnColor());
                    partialResult.toRow = (c == ColorEnum.BLACK) ? (committedBoard.GetNumberOfRows() - 1) : 0;
                    partialResult.fromRow = partialResult.toRow;
                }

                Tile fromTile = committedBoard.GetTileIfExists(partialResult.fromRow, partialResult.fromColumn);
                Tile toTile = committedBoard.GetTileIfExists(partialResult.toRow, partialResult.toColumn);

                if (i == (movesOfLastTurn.Length - 1)) {
                    Chessman chessmanThatMoved = toTile.GetPiece();
                    partialResult.pieceId = chessmanThatMoved.id;
                } else {
                    partialResult.pieceId = result[0].pieceId;
                }

                partialResult.tileId = toTile.id;
                partialResult.fromTileId = fromTile.id;
                result.Add(partialResult);
            }

            result.Reverse();

            return result;
        }

        #endregion

        #region Move generation

        class MoveOptimizationConfig {
            public bool allowMultijumps;
            public bool allowCapturejumps;
            public int maxDepth;
        }

        /// <summary>
        /// Minimax!
        /// </summary>
        /// <param name="b">Board.</param>
        /// <param name="currentDepth">Depth.</param>
        /// <param name="isMaximizingPlayer">TRUE = WHITE, FALSE = BLACK</param>
        (List<MoveAttempt>, int) MinimaxHelper (
            Board board,
            MoveOptimizationConfig config,
            int currentDepth,
            int alpha,
            int beta,
            bool isMaximizingPlayer,
            Chessman movingChessman = null
        ) {
            bool isMultipleMoves = (movingChessman != null);

            if (currentDepth == config.maxDepth || board.IsGameOver()) {
                return (null, board.CalculateBoardValue(moves.Count + currentDepth));
            }

            ColorEnum color = isMaximizingPlayer ? ColorEnum.WHITE : ColorEnum.BLACK;
            // This is a LIST of best moves because in Chessers, you can have multiple moves in a single turn.
            // (If this was Chess, it would not be a list, but a single value.)
            List<MoveAttempt> bestMoves = new List<MoveAttempt>();
            // Even though we have a list of best moves, the best score will still be a single value; it could
            // be either from doing multiple moves in a turn, or by ending the turn early.
            int bestValueSoFar = isMaximizingPlayer ? int.MinValue : int.MaxValue;

            MoveAttempt fallbackMove = null;

            List<Chessman> availableChessmen = isMultipleMoves ?
                new List<Chessman> { movingChessman } :
                board.GetActiveChessmenOfColor(color);

            Helpers.Shuffle(rng, availableChessmen);

            foreach (var chessman in availableChessmen) {
                List<Tile> potentialTiles = board.GetPotentialTilesForMovement(chessman, jumpsOnly: isMultipleMoves);
                Helpers.Shuffle(rng, potentialTiles);
                //Match.Log($"Looking @ chessman for tile {committedBoard.GetRowColumn(chessman.GetUnderlyingTile())}. Found:" +
                //$"{potentialTiles.Count} potential moves.");

                bool exitEarly = false;
                bool startedAsChecker = chessman.IsChecker();

                foreach (var tile in potentialTiles) {
                    MoveAttempt moveAttempt = new MoveAttempt {
                        pieceGuid = chessman.guid,
                        pieceId = chessman.id,
                        playerId = isMaximizingPlayer ? whitePlayerId : blackPlayerId,
                        tileId = tile.id
                    };

                    if (Helpers.CanBePromoted(chessman, tile)) {
                        moveAttempt.promotionRank = (int) ChessmanKindEnum.QUEEN;
                    }

                    MoveResult moveResult = board.MoveChessman(moveAttempt);
                    if (moveResult == null || !moveResult.valid) {
                        // This should not occur
                        continue;
                    }

                    if (fallbackMove == null) {
                        fallbackMove = moveAttempt;
                    }

                    // This represents the scenario where the turn is ended after the move has been
                    // made (either by choice or because there are no other options)
                    (List<MoveAttempt> _, int valueEndTurn) = MinimaxHelper(
                        board,
                        config,
                        currentDepth + 1,
                        alpha,
                        beta,
                        !isMaximizingPlayer
                    );

                    int value = valueEndTurn;
                    List<MoveAttempt> movesToExecute = new List<MoveAttempt> { moveAttempt };

                    if (
                        !moveResult.turnChanged &&
                        (startedAsChecker ? config.allowMultijumps : config.allowCapturejumps)
                    ) {
                        // Multijump/capturejump available - these strategies represent multiple moves
                        // in a single turn. As such, keep the `depth` the same.
                        (List<MoveAttempt> additionalMoves, int valueContinueTurn) = MinimaxHelper(
                            board,
                            config,
                            currentDepth,
                            alpha,
                            beta,
                            isMaximizingPlayer,
                            chessman
                        );

                        //Match.Log($"{valueEndTurn} | {valueContinueTurn}");

                        bool swapValues = isMaximizingPlayer ?
                            (valueContinueTurn > valueEndTurn) :
                            (valueContinueTurn < valueEndTurn);

                        if (swapValues) {
                            value = valueContinueTurn;
                            movesToExecute.AddRange(additionalMoves);
                        }
                    }

                    if (isMaximizingPlayer) {
                        if (value > bestValueSoFar) {
                            bestValueSoFar = value;
                            bestMoves = movesToExecute;
                        }

                        alpha = System.Math.Max(alpha, value);
                    } else {
                        if (value < bestValueSoFar) {
                            //Match.Log($"  Overriding best choice: {value} {moves?.Count}");
                            bestValueSoFar = value;
                            bestMoves = movesToExecute;
                        }

                        beta = System.Math.Min(beta, value);
                    }

                    board.UndoMove(moveResult);

                    if (beta <= alpha) {
                        exitEarly = true;
                        break;
                    }
                }

                if (exitEarly) {
                    break;
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
        public List<MoveAttempt> CalculateBestMove (int level = 0) {
            Board boardClone = new Board(committedBoard.GetChessmanSchemas());
            boardClone.CopyState(committedBoard);

            MoveOptimizationConfig config = new MoveOptimizationConfig {
                allowMultijumps = false,
                allowCapturejumps = false,
                maxDepth = 2,
            };

            switch (level) {
                case 1:
                    config.allowMultijumps = true;
                    break;
                case 2:
                    config.allowMultijumps = true;
                    config.allowCapturejumps = true;
                    config.maxDepth = 4;
                    break;
            }

            (List<MoveAttempt> moves, int value) = MinimaxHelper(
                boardClone,
                config,
                0,
                int.MinValue,
                int.MaxValue,
                isMaximizingPlayer: turnColor == ColorEnum.WHITE
            );

            if (moves.Count == 0 || (moves.Count == 1 && moves[0] == null)) {
                return null;
            }

            //Log($"BEST CALCULATION: {value} NUM MOVES TO MAKE: {moves?.Count}");
            //if (moves?.Count > 0) {
            //    foreach (var m in moves) {
            //        Log(m);
            //    }
            //}

            return moves;
        }

        public List<MoveResult> DoBestMovesForCurrentPlayer (int level = 0) {
            List<MoveAttempt> moveAttempts = CalculateBestMove(level);
            if (moveAttempts == null) {
                return null;
            }

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

        #region Match state

        public void Resign (ColorEnum color) {
            isResignation = true;
            if (color == ColorEnum.WHITE) {
                winningPlayerId = blackPlayerId;
            } else {
                winningPlayerId = whitePlayerId;
            }
        }

        public void Resign (int playerId) {
            if (playerId == blackPlayerId) {
                Resign(ColorEnum.BLACK);
            } else if (playerId == whitePlayerId) {
                Resign(ColorEnum.WHITE);
            }
        }

        /// <summary>
        /// Resign as the current player.
        /// </summary>
        public void Resign () {
            Resign(committedTurnColor);
        }

        public ColorEnum GetWinnerColor () {
            return GetColorOfPlayer(winningPlayerId);
        }

        public bool HasWinner () {
            return winningPlayerId >= 0;
        }

        public int GetWinner () {
            return winningPlayerId;
        }

        public void SetPlayerIds (int whiteId, int blackId) {
            whitePlayerId = whiteId;
            blackPlayerId = blackId;
        }

        #endregion

        public void UpdateMatch (MatchData newMatchData) {
            foreach (var pair in GetAllCommittedChessmen()) {
                if (pair.Value.GetUnderlyingTile() != null) {
                    pair.Value.GetUnderlyingTile().RemovePiece();
                }
                pair.Value.RemoveUnderlyingTileReference();
            }

            foreach (ChessmanSchema cs in newMatchData.pieces) {
                Chessman committedChessman = GetCommittedChessman(cs.id);
                //int currentloc = committedChessman.GetUnderlyingTile()?.id ?? -1;
                //if (cs.isActive && (currentloc != cs.location)) {
                //    Match.Log($"Moving {cs.id} from {currentloc} to {cs.location}");
                //}

                committedChessman.CopyFrom(cs);

                if (committedChessman.isActive) {
                    committedChessman.SetUnderlyingTile(GetCommittedTile(cs.location));
                    committedChessman.GetUnderlyingTile().SetPiece(committedChessman);
                }
            }

            turnColor = newMatchData.currentTurn;
            committedTurnColor = turnColor;
            moves = newMatchData.moves;

            ResetMatchState();
        }

        public MatchData CreateMatchData () {
            return new MatchData {
                blackPlayerId = blackPlayerId,
                currentTurn = turnColor,
                isDraw = isDraw,
                isResignation = isResignation,
                matchId = id,
                moves = moves,
                pieces = committedBoard.GetChessmanSchemas(),
                whitePlayerId = whitePlayerId,
                winningPlayerId = winningPlayerId,
            };
        }

        #region Utils

        public List<Tile> GetPotentialTilesForMovement (Chessman c) {
            return pendingBoard.GetPotentialTilesForMovement(c);
        }

        public Board _GetPendingBoard () {
            return pendingBoard;
        }
        public Board _GetCommittedBoard () {
            return committedBoard;
        }

        public static void Log (object s) {
#if UNITY_EDITOR
            Debug.Log(s.ToString());
#else
            System.Console.WriteLine(s.ToString());
#endif
        }

        public static void Log (object s, int indent) {
            Log($"{string.Concat(System.Linq.Enumerable.Repeat("    ", indent))}{s}");
        }

        #endregion Utils
    }
}
