using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ChessersEngine {
    public enum DeathjumpSetting {
        OFF,
        SIDES,
        BACK,
        ALL,
    };

    public enum MovejumpSetting {
        OFF,
        ON,
    }

    public class MatchData {
        public int blackPlayerId;
        /// <summary>
        /// The ID of the user whose turn it is. (One of blackPlayerId, whitePlayerId)
        /// </summary>
        public ColorEnum currentTurn;
        public int deathjumpSetting;
        public bool isDraw = false;
        public bool isResignation = false;
        public string matchGuid;
        public int matchId;
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

    public struct MatchConfig {
        public DeathjumpSetting deathjumpSetting;
    }

    public class Match {
        int id = -1;
        readonly MatchConfig config;

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
        public Match (MatchData data, MatchConfig? _config = null) {
            List<ChessmanSchema> pieces = null;

            if (data == null) {
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID;
                config = _config ?? new MatchConfig {
                    deathjumpSetting = DeathjumpSetting.OFF,
                };
                turnColor = ColorEnum.WHITE;
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID;
            } else {
                blackPlayerId = data.blackPlayerId;
                config = _config ?? new MatchConfig {
                    deathjumpSetting = (DeathjumpSetting) data.deathjumpSetting,
                };
                id = data.matchId;
                isDraw = data.isDraw;
                isResignation = data.isResignation;
                moves = data.moves ?? new List<string>();
                pieces = data.pieces;
                turnColor = data.currentTurn;
                whitePlayerId = data.whitePlayerId;
            }

            committedTurnColor = turnColor;

            pendingBoard = new Board(pieces, config);
            committedBoard = new Board(pieces, config);
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

            Chessman targetChessman = toTile.GetPiece();
            //Log($"{moveAttempt.pieceId} - {toTile.id} - {targetChessman?.id} - {toTile.IsDeathjumpTile()}");
            if (
                !toTile.IsDeathjumpTile() &&
                (targetChessman != null) &&
                (targetChessman.color == chessman.color)
            ) {
                // A piece cannot move to a tile that is occupied by the same color as itself, except
                // for deathjump tiles because they're still occupied by the last piece that moved onto
                // them.
                //Match.Log("Trying to move to an occupied tile (by yourself)");
                return null;
            }

            MoveResult moveResult = pendingBoard.MoveChessman(moveAttempt, jumpsOnly: (pendingMoveResults.Count > 0));
            if (!moveResult.valid) {
                return moveResult;
            }

            moveResult.playerId = moveAttempt.playerId;
            if (moveAttempt.promotionRank >= 0) {
                moveResult.promotionOccurred = true;
                moveResult.promotionRank = (ChessmanKindEnum) moveAttempt.promotionRank;
                Promote(moveResult);
            }

            if (moveResult.isWinningMove) {
                winningPlayerId = (turnColor == ColorEnum.WHITE) ? whitePlayerId : blackPlayerId;
            } else if (moveResult.isStalemate) {
                isDraw = true;
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
            if (moves.Count == 0) {
                return null;
            }

            List<MoveResult> result = new List<MoveResult>();
            string[] movesOfLastTurn = moves[moves.Count - 1].Split(',');

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

                partialResult.tileId = toTile.id;
                partialResult.fromTileId = fromTile.id;
                result.Add(partialResult);
            }

            result.Reverse();

            return result;
        }

        public string GetLastMove () => (moves.Count == 0) ? null : moves[moves.Count - 1];

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
            MoveOptimizationConfig optimizationConfig,
            int currentDepth,
            int alpha,
            int beta,
            bool isMaximizingPlayer,
            Chessman movingChessman = null
        ) {
            bool isMultipleMoves = (movingChessman != null);

            if (currentDepth == optimizationConfig.maxDepth || board.IsGameOver()) {
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

                bool exitEarly = false;
                bool startedAsChecker = chessman.IsChecker();
                //Log($"{chessman.kind} @ {chessman.GetUnderlyingTile().id} | {Helpers.FormatTiles(potentialTiles)}");

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
                    if (!moveResult.valid) {
                        continue;
                    } else if (moveResult.isStalemate) {
                        continue;
                    }

                    //Match.Log(moveAttempt);
                    if (fallbackMove == null) {
                        fallbackMove = moveAttempt;
                    }

                    // This represents the scenario where the turn is ended after the move has been
                    // made (either by choice or because there are no other options)
                    (List<MoveAttempt> _, int valueEndTurn) = MinimaxHelper(
                        board,
                        optimizationConfig,
                        currentDepth + 1,
                        alpha,
                        beta,
                        !isMaximizingPlayer
                    );

                    int value = valueEndTurn;
                    List<MoveAttempt> movesToExecute = new List<MoveAttempt> { moveAttempt };

                    if (
                        !moveResult.turnChanged &&
                        (startedAsChecker ? optimizationConfig.allowMultijumps : optimizationConfig.allowCapturejumps)
                    ) {
                        // Multijump/capturejump available - these strategies represent multiple moves
                        // in a single turn. As such, keep the `depth` the same.
                        (List<MoveAttempt> additionalMoves, int valueContinueTurn) = MinimaxHelper(
                            board,
                            optimizationConfig,
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
            Board boardClone = committedBoard.CreateCopy();
            boardClone.CopyState(committedBoard);

            MoveOptimizationConfig optimizationConfig = new MoveOptimizationConfig {
                allowMultijumps = false,
                allowCapturejumps = false,
                maxDepth = 2,
            };

            switch (level) {
                case 1:
                    optimizationConfig.allowMultijumps = true;
                    break;
                case 2:
                    optimizationConfig.allowMultijumps = true;
                    optimizationConfig.allowCapturejumps = true;
                    optimizationConfig.maxDepth = 4;
                    break;
            }

            (List<MoveAttempt> moves, int value) = MinimaxHelper(
                boardClone,
                optimizationConfig,
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
                if (!moveResult.valid) {
                    break;
                }

                moveResults.Add(moveResult);
            }

            return moveResults;
        }

        #endregion

        #region Match state

        public int GetWinner () {
            return winningPlayerId;
        }

        public ColorEnum GetWinnerColor () {
            return GetColorOfPlayer(winningPlayerId);
        }

        public bool HasWinner () {
            return winningPlayerId >= 0;
        }

        public bool IsDraw () {
            return isDraw;
        }

        public bool IsGameOver () {
            return IsDraw() || HasWinner();
        }

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

            isDraw = newMatchData.isDraw;
            moves = newMatchData.moves;
            winningPlayerId = newMatchData.winningPlayerId;

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

        public MatchConfig GetConfig () {
            return config;
        }

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
