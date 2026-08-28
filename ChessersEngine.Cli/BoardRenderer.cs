using System;
using System.Collections.Generic;
using System.Text;
using ChessersEngine;

namespace ChessersEngine.Cli {
    /// <summary>
    /// ASCII board rendering plus the square/coordinate <-> tile-id conversions the CLI
    /// needs to translate between what a human types ("e2e4") and what the engine expects
    /// (piece id + tile id).
    /// </summary>
    static class BoardRenderer {
        public static bool UseColor = true;

        public enum UiTheme { Dark, Light }

        /// <summary>Which palette to render with; set from terminal detection at startup.</summary>
        public static UiTheme Theme = UiTheme.Dark;

        const string Reset = "\u001b[0m";

        // Piece colours. The two sides are separated by HUE, not just brightness, so they stay
        // distinguishable under colour-vision deficiencies -- and the UPPER/lower case of the
        // letters carries the very same information with no colour at all.
        //   Light theme: white pieces render BLACK (bright white would vanish on a light bg);
        //                black pieces render blue, clearly distinct from the black white-pieces.
        //   Dark theme:  white pieces bright white; black pieces bright yellow.
        static string WhitePieceColor => Theme == UiTheme.Light ? "\u001b[1;30m" : "\u001b[1;97m";
        static string BlackPieceColor => Theme == UiTheme.Light ? "\u001b[1;34m" : "\u001b[1;93m";

        // The board frame (grid lines + coordinate labels): a wooden brown, shaded per theme so it
        // keeps contrast against either background.
        static string LineColor => Theme == UiTheme.Light
            ? "\u001b[38;5;94m"    // dark brown on a light background
            : "\u001b[38;5;137m";  // tan/wood brown on a dark background

        // Faint centre dot on empty dark squares (shows the checkerboard pattern).
        static string DotColor => Theme == UiTheme.Light
            ? "\u001b[38;5;245m"   // medium grey, visible on white
            : "\u001b[38;5;240m";  // dim grey, subtle on black

        static string Color (string body, string code) => UseColor ? (code + body + Reset) : body;
        static string Line (string body) => Color(body, LineColor);

        /// <summary>Single letter for a chessman kind (P N B R Q K).</summary>
        static char KindLetter (ChessmanKindEnum kind) {
            switch (kind) {
                case ChessmanKindEnum.PAWN: return 'P';
                case ChessmanKindEnum.KNIGHT: return 'N';
                case ChessmanKindEnum.BISHOP: return 'B';
                case ChessmanKindEnum.ROOK: return 'R';
                case ChessmanKindEnum.QUEEN: return 'Q';
                case ChessmanKindEnum.KING: return 'K';
                default: return '?';
            }
        }

        /// <summary>
        /// A 3-wide cell for a piece:
        ///   " P "  chess piece      (spaces around the letter)
        ///   "(P)"  checker          (parentheses)
        ///   "[P]"  kinged checker   (brackets)
        /// White pieces are UPPERCASE, black pieces lowercase.
        /// </summary>
        static string PieceCell (Chessman c) {
            char letter = KindLetter(c.kind);
            if (c.color == ColorEnum.BLACK) {
                letter = char.ToLowerInvariant(letter);
            }

            string body;
            if (c.isKinged) {
                body = "[" + letter + "]";
            } else if (c.isChecker) {
                body = "(" + letter + ")";
            } else {
                body = " " + letter + " ";
            }

            return Color(body, c.color == ColorEnum.WHITE ? WhitePieceColor : BlackPieceColor);
        }

        static string EmptyCell (int row, int col) {
            // Dark squares (a1 is dark: row+col even) get a faint centre dot so the
            // checkerboard pattern -- which matters for checker movement -- stays visible.
            bool dark = ((row + col) % 2) == 0;
            return dark ? Color(" . ", DotColor) : "   ";
        }

        /// <summary>
        /// Render the board with rank 8 (row 7) at the top and rank 1 (row 0) at the bottom,
        /// files a-h left to right -- i.e. the conventional white-at-the-bottom orientation.
        /// </summary>
        public static string Render (Board board) {
            var sb = new StringBuilder();
            string filesHeader = Line("      a   b   c   d   e   f   g   h");
            string sep = Line("    +---+---+---+---+---+---+---+---+");
            string bar = Line("|");

            sb.Append('\n').Append(filesHeader).Append('\n');
            sb.Append(sep).Append('\n');

            for (int row = 7; row >= 0; row--) {
                int rank = row + 1;
                sb.Append(Line($" {rank}  ")).Append(bar);
                for (int col = 0; col < 8; col++) {
                    Tile tile = board.GetTile((row * 8) + col);
                    Chessman piece = tile?.GetPiece();
                    sb.Append(piece != null ? PieceCell(piece) : EmptyCell(row, col));
                    sb.Append(bar);
                }
                sb.Append(Line($"  {rank}")).Append('\n');
                sb.Append(sep).Append('\n');
            }

            sb.Append(filesHeader).Append('\n');
            return sb.ToString();
        }

        public static string Legend () =>
            "  UPPER=white  lower=black   \" X \"=chess  \"(X)\"=checker  \"[X]\"=kinged checker";

        #region Square <-> tile-id conversion

        /// <summary>
        /// Convert a tile id to a display token. On-board tiles (0..63) render as an
        /// algebraic square like "e4"; off-board deathjump tiles (negative ids) render as
        /// "#&lt;id&gt;" (e.g. "#-14"). This round-trips with <see cref="TryParseTile"/>.
        /// </summary>
        public static string TileToSquare (int tileId) {
            if (tileId < 0 || tileId > 63) {
                return "#" + tileId; // off-board deathjump tile -- address it by raw id
            }
            int row = tileId / 8;
            int col = tileId % 8;
            return $"{(char) ('a' + col)}{row + 1}";
        }

        /// <summary>
        /// Parse a location token into a tile id. Accepts:
        ///   "e4"      -> algebraic square (0..63)
        ///   "#-14"    -> a raw tile id (any int, including negative deathjump tiles)
        /// Returns false if the token is not a valid location. NOTE: bare "d7" is the square
        /// d7, never a deathjump -- deathjump tiles must use the unambiguous "#" form so they
        /// don't collide with the d-file.
        /// </summary>
        public static bool TryParseTile (string token, out int tileId) {
            tileId = int.MinValue;
            if (string.IsNullOrWhiteSpace(token)) {
                return false;
            }

            token = token.Trim().ToLowerInvariant();

            if (token[0] == '#') {
                return int.TryParse(token.Substring(1), out tileId);
            }

            if (token.Length == 2 && token[0] >= 'a' && token[0] <= 'h' && token[1] >= '1' && token[1] <= '8') {
                int col = token[0] - 'a';
                int row = token[1] - '1';
                tileId = (row * 8) + col;
                return true;
            }

            return false;
        }

        #endregion

        /// <summary>Human-readable summary line for a move result, e.g. "Pe2e4" plus flags.</summary>
        public static string DescribeMove (MoveResult r) {
            var flags = new List<string>();
            if (r.wasPieceCaptured) flags.Add("capture");
            if (r.wasPieceJumped) flags.Add("jump");
            if (r.polarityChanged) flags.Add("flip");
            if (r.kinged) flags.Add("kinged");
            if (r.promotionOccurred) flags.Add("promote=" + r.promotionRank);
            if (r.isCastle) flags.Add("castle");
            if (r.isWinningMove) flags.Add("CHECKMATE");
            else if (r.isInCheck) flags.Add("check");
            if (r.isStalemate) flags.Add("STALEMATE");

            string suffix = flags.Count > 0 ? "  (" + string.Join(", ", flags) + ")" : "";
            return $"{TileToSquare(r.fromTileId)}{TileToSquare(r.tileId)}  [{r.CreateNotation()}]{suffix}";
        }
    }
}
