using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ChessersEngine;

namespace ChessersEngine.Cli {
    enum PlayerKind { Human, Ai }

    enum TurnOutcome { Moved, NoLegalMoves, Resigned, Quit }

    /// <summary>Parsed command-line options for a run.</summary>
    class Options {
        public PlayerKind White = PlayerKind.Human;
        public PlayerKind Black = PlayerKind.Ai;
        public int Level = 2;
        public int? Seed = null;
        public DeathjumpSetting Deathjump = DeathjumpSetting.OFF;
        public int Games = 1;
        public int MaxPlies = 400;
        public int DelayMs = 0;
        public bool NoColor = false;
        public bool Quiet = false; // suppress board rendering (for bulk AI-vs-AI corpus runs)
    }

    static class Program {
        static int Main (string[] args) {
            Options opts;
            try {
                opts = ParseArgs(args);
            } catch (ArgException ex) {
                Console.Error.WriteLine("error: " + ex.Message);
                Console.Error.WriteLine();
                PrintUsage(Console.Error);
                return 2;
            }

            if (opts == null) {
                // --help
                PrintUsage(Console.Out);
                return 0;
            }

            BoardRenderer.UseColor = !opts.NoColor && !Console.IsOutputRedirected;

            for (int g = 0; g < opts.Games; g++) {
                int? seed = opts.Seed.HasValue ? opts.Seed.Value + g : (int?) null;

                bool interactive = opts.White == PlayerKind.Human || opts.Black == PlayerKind.Human;
                if (opts.Games > 1) {
                    Console.WriteLine($"\n=== Game {g + 1} of {opts.Games} (seed={FormatSeed(seed)}) ===");
                }

                bool keepPlaying = PlayGame(opts, seed);

                if (!keepPlaying && interactive) {
                    // Human asked to quit the whole run.
                    break;
                }
            }

            return 0;
        }

        /// <summary>Plays a single game to completion. Returns false if the human chose to quit the run.</summary>
        static bool PlayGame (Options opts, int? seed) {
            var config = new MatchConfig { deathjumpSetting = opts.Deathjump };
            var match = new Match(null, config, seed);

            int whiteId = match.whitePlayerId;
            int blackId = match.blackPlayerId;

            bool showBoards = !opts.Quiet;
            if (showBoards) {
                Console.WriteLine(Banner(opts, seed, whiteId, blackId));
            }

            int plyIndex = 0;
            string endReason = null;
            bool humanQuit = false;

            while (!match.IsGameOver() && plyIndex < opts.MaxPlies) {
                ColorEnum moverColor = match.GetCommittedTurnColor();
                int moverPlayerId = moverColor == ColorEnum.WHITE ? whiteId : blackId;
                PlayerKind actor = moverColor == ColorEnum.WHITE ? opts.White : opts.Black;

                if (showBoards) {
                    Console.WriteLine(BoardRenderer.Render(match._GetPendingBoard()));
                    Console.WriteLine(BoardRenderer.Legend());
                    Console.WriteLine($"\nTurn {plyIndex + 1}: {moverColor} to move ({actor}).");
                }

                TurnOutcome outcome = actor == PlayerKind.Human
                    ? PlayHumanTurn(match, moverColor, moverPlayerId, opts.Level, showBoards)
                    : PlayAiTurn(match, moverColor, moverPlayerId, opts.Level, opts.DelayMs, showBoards);

                if (outcome == TurnOutcome.Quit) {
                    match.ResetTurn();
                    endReason = "quit";
                    humanQuit = true;
                    break;
                }

                if (outcome == TurnOutcome.Resigned) {
                    match.ResetTurn();
                    match.Resign(moverPlayerId);
                    endReason = "resignation";
                    break;
                }

                if (outcome == TurnOutcome.NoLegalMoves) {
                    // The engine flags mate/stalemate as part of the *opponent's* move, so reaching
                    // here means an unflagged dead position; stop and adjudicate.
                    endReason = "no-legal-moves";
                    break;
                }

                match.CommitTurn();
                plyIndex++;

                if (showBoards) {
                    if (match.HasWinner()) Console.WriteLine("\n** Checkmate! **");
                    else if (match.IsDraw()) Console.WriteLine("\n** Stalemate. **");
                }
            }

            if (endReason == null) {
                if (match.HasWinner()) endReason = "checkmate";
                else if (match.IsDraw()) endReason = "stalemate";
                else if (plyIndex >= opts.MaxPlies) endReason = "move-limit";
            }

            // Final board + result summary.
            if (showBoards) {
                Console.WriteLine(BoardRenderer.Render(match._GetCommittedBoard()));
            }
            Console.WriteLine(ResultLine(match, endReason, whiteId));

            return !humanQuit;
        }

        #region Turn logic

        static TurnOutcome PlayAiTurn (
            Match match,
            ColorEnum moverColor,
            int moverPlayerId,
            int level,
            int delayMs,
            bool showBoards
        ) {
            if (showBoards) {
                Console.WriteLine($"{moverColor} (AI, level {level}) is thinking...");
            }

            List<MoveAttempt> attempts = match.CalculateBestMove(level);
            if (attempts == null || attempts.Count == 0 || attempts[0] == null) {
                return TurnOutcome.NoLegalMoves;
            }

            bool moved = false;
            foreach (MoveAttempt attempt in attempts) {
                attempt.playerId = moverPlayerId;
                MoveResult result = match.MoveChessman(attempt);
                if (result == null || !result.valid) {
                    // Should not happen -- the search only returns legal moves.
                    break;
                }
                moved = true;
                if (showBoards) {
                    Console.WriteLine("  " + moverColor + ": " + BoardRenderer.DescribeMove(result));
                }
            }

            if (delayMs > 0) {
                Thread.Sleep(delayMs);
            }
            return moved ? TurnOutcome.Moved : TurnOutcome.NoLegalMoves;
        }

        static TurnOutcome PlayHumanTurn (
            Match match,
            ColorEnum moverColor,
            int moverPlayerId,
            int aiLevel,
            bool showBoards
        ) {
            // The id of the piece that must continue a multi-jump (once one is in progress), else -1.
            int continuationPieceId = -1;

            while (true) {
                Board board = match._GetPendingBoard();

                if (continuationPieceId >= 0) {
                    Chessman cont = match.GetPendingChessman(continuationPieceId);
                    var jumps = board.GetValidTilesForMovement(cont, jumpsOnly: true);
                    Console.WriteLine($"  Multi-jump in progress with {BoardRenderer.TileToSquare(cont.GetUnderlyingTile().id)} " +
                        $"-> continue to: {FormatTargets(jumps)}");
                }

                Console.Write(continuationPieceId >= 0 ? "  continue> " : "move> ");
                string line = Console.ReadLine();

                if (line == null) {
                    // EOF (piped input exhausted / Ctrl-D): treat as quit.
                    return TurnOutcome.Quit;
                }

                line = line.Trim();
                if (line.Length == 0) {
                    continue;
                }

                string lower = line.ToLowerInvariant();
                string[] parts = Regex.Split(lower, @"[\s\-]+").Where(p => p.Length > 0).ToArray();
                string cmd = parts[0];

                // -- Non-move commands
                if (cmd == "help" || cmd == "?") { PrintInGameHelp(); continue; }
                if (cmd == "board") { Console.WriteLine(BoardRenderer.Render(board)); continue; }
                if (cmd == "legend") { Console.WriteLine(BoardRenderer.Legend()); continue; }
                if (cmd == "quit" || cmd == "exit") { return TurnOutcome.Quit; }
                if (cmd == "resign") {
                    return TurnOutcome.Resigned;
                }
                if (cmd == "moves") {
                    ShowMovesFor(match, moverColor, parts.Length > 1 ? parts[1] : null, continuationPieceId);
                    continue;
                }
                if (cmd == "hint") { ShowHint(match, moverColor, aiLevel); continue; }

                // -- Otherwise, parse as a move
                if (!TryParseMove(parts, out int fromTile, out int toTile, out int explicitPromo, out string parseErr)) {
                    Console.WriteLine("  " + parseErr + "  (type 'help' for input formats)");
                    continue;
                }

                Chessman piece = board.GetTileIfExists(fromTile)?.GetPiece();
                if (piece == null) {
                    Console.WriteLine($"  No piece on {BoardRenderer.TileToSquare(fromTile)}.");
                    continue;
                }
                if (piece.color != moverColor) {
                    Console.WriteLine($"  {BoardRenderer.TileToSquare(fromTile)} holds a {piece.color} piece; it's {moverColor}'s turn.");
                    continue;
                }
                if (continuationPieceId >= 0 && piece.id != continuationPieceId) {
                    Console.WriteLine("  You must continue the multi-jump with the same piece.");
                    continue;
                }

                int promo = ResolvePromotion(piece, moverColor, toTile, explicitPromo);

                var attempt = new MoveAttempt {
                    playerId = moverPlayerId,
                    pieceId = piece.id,
                    pieceGuid = piece.guid,
                    tileId = toTile,
                    promotionRank = promo,
                };

                MoveResult result = match.MoveChessman(attempt);
                if (result == null) {
                    Console.WriteLine("  Rejected (not your turn / target occupied by your own piece).");
                    continue;
                }
                if (!result.valid) {
                    Console.WriteLine($"  Illegal move for that piece. Try 'moves {BoardRenderer.TileToSquare(fromTile)}' to list legal targets.");
                    continue;
                }

                if (showBoards) {
                    Console.WriteLine("  " + moverColor + ": " + BoardRenderer.DescribeMove(result));
                }

                if (result.turnChanged || result.isWinningMove || result.isStalemate) {
                    return TurnOutcome.Moved;
                }

                // A checker jump that leaves a legal continuation: the same piece must jump again.
                continuationPieceId = result.pieceId;
                if (showBoards) {
                    Console.WriteLine(BoardRenderer.Render(match._GetPendingBoard()));
                }
            }
        }

        #endregion

        #region Move helpers

        /// <summary>
        /// Decide the promotion rank to attach to a move attempt. Mirrors the engine's own
        /// auto-queen (Helpers.CanBePromoted) but lets a human under-promote by typing a suffix.
        /// </summary>
        static int ResolvePromotion (Chessman piece, ColorEnum moverColor, int toTile, int explicitPromo) {
            if (toTile < 0 || !piece.IsPawn() || piece.isPromoted) {
                return -1;
            }
            int toRow = toTile / 8;
            bool backRank = (moverColor == ColorEnum.WHITE && toRow == 7) ||
                            (moverColor == ColorEnum.BLACK && toRow == 0);
            if (!backRank) {
                return -1;
            }
            return explicitPromo >= 0 ? explicitPromo : (int) ChessmanKindEnum.QUEEN;
        }

        static bool TryParseMove (string[] parts, out int fromTile, out int toTile, out int promo, out string error) {
            fromTile = toTile = int.MinValue;
            promo = -1;
            error = null;

            string fromTok, toTok, promoTok = null;

            // Compact "e2e4" / "e7e8q" form (pure algebraic, no #/d tokens).
            if (parts.Length == 1 && Regex.IsMatch(parts[0], "^[a-h][1-8][a-h][1-8][qrbn]?$")) {
                fromTok = parts[0].Substring(0, 2);
                toTok = parts[0].Substring(2, 2);
                if (parts[0].Length == 5) promoTok = parts[0].Substring(4, 1);
            } else if (parts.Length >= 2) {
                fromTok = parts[0];
                toTok = parts[1];
                if (parts.Length >= 3) promoTok = parts[2];
            } else {
                error = "Could not read a move.";
                return false;
            }

            if (!BoardRenderer.TryParseTile(fromTok, out fromTile)) {
                error = $"'{fromTok}' is not a valid square.";
                return false;
            }
            if (!BoardRenderer.TryParseTile(toTok, out toTile)) {
                error = $"'{toTok}' is not a valid square.";
                return false;
            }
            if (promoTok != null && !TryParsePromo(promoTok, out promo)) {
                error = $"'{promoTok}' is not a promotion piece (use q, r, b, or n).";
                return false;
            }
            return true;
        }

        static bool TryParsePromo (string tok, out int rank) {
            rank = -1;
            switch (tok.Trim().TrimStart('=').ToLowerInvariant()) {
                case "q": rank = (int) ChessmanKindEnum.QUEEN; return true;
                case "r": rank = (int) ChessmanKindEnum.ROOK; return true;
                case "b": rank = (int) ChessmanKindEnum.BISHOP; return true;
                case "n": rank = (int) ChessmanKindEnum.KNIGHT; return true;
                default: return false;
            }
        }

        static string FormatTargets (List<Tile> tiles) {
            if (tiles == null || tiles.Count == 0) return "(none)";
            return string.Join(" ", tiles.Select(t => BoardRenderer.TileToSquare(t.id)));
        }

        static void ShowMovesFor (Match match, ColorEnum moverColor, string squareTok, int continuationPieceId) {
            Board board = match._GetPendingBoard();

            if (squareTok == null) {
                Console.WriteLine("  usage: moves <square>   e.g. 'moves e2'");
                return;
            }
            if (!BoardRenderer.TryParseTile(squareTok, out int tileId)) {
                Console.WriteLine($"  '{squareTok}' is not a valid square.");
                return;
            }
            Chessman piece = board.GetTileIfExists(tileId)?.GetPiece();
            if (piece == null) {
                Console.WriteLine($"  No piece on {BoardRenderer.TileToSquare(tileId)}.");
                return;
            }
            if (piece.color != moverColor) {
                Console.WriteLine($"  {BoardRenderer.TileToSquare(tileId)} holds a {piece.color} piece.");
                return;
            }
            bool jumpsOnly = continuationPieceId >= 0;
            var targets = board.GetValidTilesForMovement(piece, jumpsOnly);
            Console.WriteLine($"  Legal targets for {BoardRenderer.TileToSquare(tileId)}: {FormatTargets(targets)}");
        }

        static void ShowHint (Match match, ColorEnum moverColor, int level) {
            List<MoveAttempt> best = match.CalculateBestMove(level);
            if (best == null || best.Count == 0 || best[0] == null) {
                Console.WriteLine("  No suggestion available.");
                return;
            }
            var parts = best.Select(a => {
                Chessman c = match.GetCommittedChessman(a.pieceId);
                string from = c?.GetUnderlyingTile() != null ? BoardRenderer.TileToSquare(c.GetUnderlyingTile().id) : "?";
                return $"{from}{BoardRenderer.TileToSquare(a.tileId)}";
            });
            Console.WriteLine("  Suggestion: " + string.Join(", ", parts));
        }

        #endregion

        #region Outcome / banners

        static string ResultLine (Match match, string reason, int whiteId) {
            if (match.HasWinner()) {
                ColorEnum wc = match.GetWinnerColor();
                return $"Result: {wc} wins ({reason}).";
            }
            if (match.IsDraw()) {
                return $"Result: draw ({reason}).";
            }
            return $"Result: game ended ({reason}); no winner recorded.";
        }

        static string Banner (Options opts, int? seed, int whiteId, int blackId) {
            return
                "\n================ CHESSERS (terminal) ================\n" +
                $"  White: {opts.White}  (player {whiteId})\n" +
                $"  Black: {opts.Black}  (player {blackId})\n" +
                $"  AI level: {opts.Level}   Deathjump: {opts.Deathjump}   Seed: {FormatSeed(seed)}\n" +
                "  Type 'help' at the prompt for commands.\n" +
                "====================================================";
        }

        static string FormatSeed (int? seed) => seed.HasValue ? seed.Value.ToString() : "random";

        static void PrintInGameHelp () {
            Console.WriteLine(
                "\n  Move input:\n" +
                "    e2e4        move from e2 to e4 (also 'e2 e4' or 'e2-e4')\n" +
                "    e7e8q       promote to queen (q/r/b/n; auto-queens if omitted)\n" +
                "    a3 #-14     '#-14' targets deathjump tile -14 (raw tile id)\n" +
                "                (deathjump targets are shown as #<id> by 'moves')\n" +
                "  Commands:\n" +
                "    moves <sq>  list legal targets for the piece on <sq>\n" +
                "    hint        suggest a move (runs the AI for your side)\n" +
                "    board       redraw the board\n" +
                "    legend      explain the piece symbols\n" +
                "    resign      resign the game\n" +
                "    quit        stop without resigning\n");
        }

        #endregion

        #region Argument parsing

        class ArgException : Exception {
            public ArgException (string message) : base(message) { }
        }

        static Options ParseArgs (string[] args) {
            var o = new Options();
            for (int i = 0; i < args.Length; i++) {
                string a = args[i];
                string Next (string name) {
                    if (i + 1 >= args.Length) throw new ArgException($"{name} requires a value");
                    return args[++i];
                }

                switch (a) {
                    case "-h":
                    case "--help":
                        return null;
                    case "--white": o.White = ParseKind(Next(a)); break;
                    case "--black": o.Black = ParseKind(Next(a)); break;
                    case "--level": o.Level = ParseIntRange(Next(a), 0, 2, a); break;
                    case "--seed": o.Seed = ParseInt(Next(a), a); break;
                    case "--deathjump": o.Deathjump = ParseDeathjump(Next(a)); break;
                    case "--games": o.Games = ParseIntRange(Next(a), 1, 100000, a); break;
                    case "--max-plies": o.MaxPlies = ParseIntRange(Next(a), 1, 100000, a); break;
                    case "--delay": o.DelayMs = ParseIntRange(Next(a), 0, 60000, a); break;
                    case "--no-color": o.NoColor = true; break;
                    case "--quiet": o.Quiet = true; break;
                    // Convenience presets
                    case "--ai-vs-ai": o.White = PlayerKind.Ai; o.Black = PlayerKind.Ai; break;
                    case "--hotseat": o.White = PlayerKind.Human; o.Black = PlayerKind.Human; break;
                    default:
                        throw new ArgException($"unknown option '{a}'");
                }
            }
            return o;
        }

        static PlayerKind ParseKind (string s) {
            switch (s.ToLowerInvariant()) {
                case "human":
                case "h": return PlayerKind.Human;
                case "ai":
                case "cpu": return PlayerKind.Ai;
                default: throw new ArgException($"player must be 'human' or 'ai', got '{s}'");
            }
        }

        static DeathjumpSetting ParseDeathjump (string s) {
            switch (s.ToLowerInvariant()) {
                case "off": return DeathjumpSetting.OFF;
                case "sides": return DeathjumpSetting.SIDES;
                case "back": return DeathjumpSetting.BACK;
                case "all": return DeathjumpSetting.ALL;
                default: throw new ArgException($"deathjump must be off|sides|back|all, got '{s}'");
            }
        }

        static int ParseInt (string s, string name) {
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) {
                throw new ArgException($"{name} expects an integer, got '{s}'");
            }
            return v;
        }

        static int ParseIntRange (string s, int min, int max, string name) {
            int v = ParseInt(s, name);
            if (v < min || v > max) {
                throw new ArgException($"{name} must be between {min} and {max}, got {v}");
            }
            return v;
        }

        static void PrintUsage (System.IO.TextWriter w) {
            w.WriteLine(
                "chessers - terminal Chessers game\n\n" +
                "USAGE:\n" +
                "  dotnet run --project ChessersEngine.Cli -- [options]\n\n" +
                "OPTIONS:\n" +
                "  --white human|ai     who controls white   (default: human)\n" +
                "  --black human|ai     who controls black   (default: ai)\n" +
                "  --ai-vs-ai           shorthand for --white ai --black ai\n" +
                "  --hotseat            shorthand for two humans\n" +
                "  --level 0|1|2        AI strength/search depth (default: 2)\n" +
                "  --seed <int>         RNG seed for reproducible AI (per game: seed+index)\n" +
                "  --deathjump MODE     off|sides|back|all   (default: off)\n" +
                "  --games <n>          play n games in a row (default: 1)\n" +
                "  --max-plies <n>      adjudicate a draw after n plies (default: 400)\n" +
                "  --delay <ms>         pause after each AI turn, for watching (default: 0)\n" +
                "  --quiet              don't render boards (bulk AI-vs-AI runs)\n" +
                "  --no-color           disable ANSI colors\n" +
                "  -h, --help           show this help\n\n" +
                "EXAMPLES:\n" +
                "  # Play white against the AI, reproducible via a seed:\n" +
                "  dotnet run --project ChessersEngine.Cli -- --white human --black ai --seed 42\n\n" +
                "  # Watch 5 AI-vs-AI games:\n" +
                "  dotnet run --project ChessersEngine.Cli -- --ai-vs-ai --games 5 --seed 1 --delay 300\n");
        }

        #endregion
    }
}
