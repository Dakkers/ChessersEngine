using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessersEngine {
    public static class Helpers {

        #region Color helpers

        public static ColorEnum ConvertIntToColorEnum (int val) {
            return (val == Constants.ID_WHITE) ? ColorEnum.WHITE : ColorEnum.BLACK;
        }

        public static ColorEnum GetOppositeColor (ColorEnum color) {
            return (color == ColorEnum.BLACK) ?
                ColorEnum.WHITE :
                ColorEnum.BLACK;
        }

        public static ColorEnum GetColorFromPieceId (int pieceId) {
            if (
                GetBlackPawnIds().Contains(pieceId) ||
                GetBlackKnightIds().Contains(pieceId) ||
                GetBlackRookIds().Contains(pieceId) ||
                GetBlackBishopIds().Contains(pieceId) ||
                (pieceId == Constants.ID_BLACK_QUEEN) ||
                (pieceId == Constants.ID_BLACK_KING)
            ) {
                return ColorEnum.BLACK;
            }

            return ColorEnum.WHITE;
        }

        #endregion

        #region Math

        public static int Mod (int x, int m) {
            return (x % m + m) % m;
        }

        public static List<int> Range (int start, int stop, int step) {
            List<int> range = new List<int>();
            int i = start;

            if (step == 0) {
                throw new System.Exception("Step cannot be 0.");
            }

            if (step > 0) {
                while (i < stop) {
                    range.Add(i);
                    i += step;
                }
            } else {
                while (i > stop) {
                    range.Add(i);
                    i -= step;
                }
            }

            return range;
        }

        #endregion

        #region Chessman ID related things

        public static List<int> GetBlackPawnIds () {
            return new List<int> {
                Constants.ID_BLACK_PAWN_1,
                Constants.ID_BLACK_PAWN_2,
                Constants.ID_BLACK_PAWN_3,
                Constants.ID_BLACK_PAWN_4,
                Constants.ID_BLACK_PAWN_5,
                Constants.ID_BLACK_PAWN_6,
                Constants.ID_BLACK_PAWN_7,
                Constants.ID_BLACK_PAWN_8
            };
        }

        public static List<int> GetBlackBishopIds () {
            return new List<int> {
                Constants.ID_BLACK_BISHOP_1,
                Constants.ID_BLACK_BISHOP_2
            };
        }

        public static List<int> GetBlackKnightIds () {
            return new List<int> {
                Constants.ID_BLACK_KNIGHT_1,
                Constants.ID_BLACK_KNIGHT_2
            };
        }

        public static List<int> GetBlackRookIds () {
            return new List<int> {
                Constants.ID_BLACK_ROOK_1,
                Constants.ID_BLACK_ROOK_2
            };
        }

        public static List<int> GetWhitePawnIds () {
            return new List<int> {
                Constants.ID_WHITE_PAWN_1,
                Constants.ID_WHITE_PAWN_2,
                Constants.ID_WHITE_PAWN_3,
                Constants.ID_WHITE_PAWN_4,
                Constants.ID_WHITE_PAWN_5,
                Constants.ID_WHITE_PAWN_6,
                Constants.ID_WHITE_PAWN_7,
                Constants.ID_WHITE_PAWN_8
            };
        }

        public static List<int> GetWhiteBishopIds () {
            return new List<int> {
                Constants.ID_WHITE_BISHOP_1,
                Constants.ID_WHITE_BISHOP_2
            };
        }

        public static List<int> GetWhiteKnightIds () {
            return new List<int> {
                Constants.ID_WHITE_KNIGHT_1,
                Constants.ID_WHITE_KNIGHT_2
            };
        }

        public static List<int> GetWhiteRookIds () {
            return new List<int> {
                Constants.ID_WHITE_ROOK_1,
                Constants.ID_WHITE_ROOK_2
            };
        }

        #endregion

        #region Chessman kinds

        public static bool IsKnight (int pieceId) {
            return GetWhiteKnightIds().Contains(pieceId) || GetBlackKnightIds().Contains(pieceId);
        }

        public static bool IsPawn (int pieceId) {
            return GetWhitePawnIds().Contains(pieceId) || GetBlackPawnIds().Contains(pieceId);
        }

        public static bool IsBishop (int pieceId) {
            return GetWhiteBishopIds().Contains(pieceId) || GetBlackBishopIds().Contains(pieceId);
        }

        public static bool IsRook (int pieceId) {
            return GetWhiteRookIds().Contains(pieceId) || GetBlackRookIds().Contains(pieceId);
        }

        public static bool IsKing (int pieceId) {
            return pieceId == Constants.ID_WHITE_KING || pieceId == Constants.ID_BLACK_KING;
        }

        public static bool IsQueen (int pieceId) {
            return pieceId == Constants.ID_WHITE_QUEEN || pieceId == Constants.ID_BLACK_QUEEN;
        }

        public static int GetKind (int pieceId) {
            if (IsPawn(pieceId)) {
                return Constants.CHESSMAN_KIND_PAWN;
            }

            if (IsKnight(pieceId)) {
                return Constants.CHESSMAN_KIND_KNIGHT;
            }

            if (IsRook(pieceId)) {
                return Constants.CHESSMAN_KIND_ROOK;
            }

            if (IsBishop(pieceId)) {
                return Constants.CHESSMAN_KIND_BISHOP;
            }

            if (IsQueen(pieceId)) {
                return Constants.CHESSMAN_KIND_QUEEN;
            }

            if (IsKing(pieceId)) {
                return Constants.CHESSMAN_KIND_KING;
            }

            return -1;
        }

        #endregion

        #region Tile helpers

        public static int GetColumn (int tileId) {
            if (tileId >= 0) {
                return tileId % 8;
            } else if (tileId >= -10) {
                return Math.Abs(tileId) - 2;
            } else if (tileId <= Constants.DEATHJUMP_TOP_ROW_START_ID) {
                // + bcecause the ID is negative
                return Math.Abs(tileId) + Constants.DEATHJUMP_TOP_ROW_START_ID - 1;
            } else if (Helpers.Mod(tileId, 2) == 1) {
                // Negative tileId that is odd is in the leftmost column
                return -1;
            } else {
                return 8;
            }
        }

        public static int GetRow (int tileId) {
            if (tileId >= 0) {
                return tileId / 8;
            } else if (tileId >= -10) {
                return -1;
            } else if (tileId <= Constants.DEATHJUMP_TOP_ROW_START_ID) {
                return 8;
            }


            // Either the lefthand or righthand deathjump columns
            int diff = tileId + (Constants.DEATHJUMP_NUM_ROW_TILES + 1);

            // E.g. -11, -12 --> 0, 1 --> both are in row 0
            // E.g. -15, -16 --> -4, -5 --> both are in row 2
            return Math.Abs(diff / 2);
        }

        public static int GetTileIdFromRowColumn (int row, int col) {
            if (row == -1) {
                return (-1 * col) - 2;
            } else if (row == 8) {
                return Constants.DEATHJUMP_TOP_ROW_START_ID - (col + 1);
            } else if (col == -1) {
                return -1 * (Constants.DEATHJUMP_NUM_ROW_TILES + 1 + (2 * row));
            } else if (col == 8) {
                return -1 * (Constants.DEATHJUMP_NUM_ROW_TILES + 1 + (2 * row) + 1);
            }

            return (8 * row) + col;
        }

        public static string FormatTiles (List<Tile> tiles) {
            return string.Join(",", (tiles ?? new List<Tile>()).Select((t) => t?.id.ToString() ?? "x"));
        }

        public static void PrintTiles (List<Tile> tiles, int indent = 0) {
            Match.Log(FormatTiles(tiles), indent);
        }

        #endregion

        #region Notation

        public static string ConvertRowToRank (int row) {
            return (row + 1).ToString();
        }

        public static int ConvertRankToRow (char rank) {
            return int.Parse(rank.ToString()) - 1;
        }

        public static string ConvertColumnToFile (int column) {
            switch (column + 1) {
                case 1:
                    return "a";
                case 2:
                    return "b";
                case 3:
                    return "c";
                case 4:
                    return "d";
                case 5:
                    return "e";
                case 6:
                    return "f";
                case 7:
                    return "g";
                case 8:
                    return "h";
                default:
                    throw new System.Exception($"Invalid column: {column}");
            }
        }

        public static int ConvertFileToColumn (char file) {
            switch (file) {
                case 'a':
                    return 0;
                case 'b':
                    return 1;
                case 'c':
                    return 2;
                case 'd':
                    return 3;
                case 'e':
                    return 4;
                case 'f':
                    return 5;
                case 'g':
                    return 6;
                case 'h':
                    return 7;
                default:
                    throw new System.Exception($"Invalid file: {file}");
            }
        }

        public static string ConvertChessmanKindToNotationSymbol (ChessmanKindEnum kind) {
            switch (kind) {
                case ChessmanKindEnum.PAWN:
                    return "P";
                case ChessmanKindEnum.ROOK:
                    return "R";
                case ChessmanKindEnum.KNIGHT:
                    return "N";
                case ChessmanKindEnum.KING:
                    return "K";
                case ChessmanKindEnum.QUEEN:
                    return "Q";
                case ChessmanKindEnum.BISHOP:
                    return "B";
                default:
                    throw new Exception($"Invalid chessman kind: {kind}");
            }
        }

        public static ChessmanKindEnum? ConvertNotationSymbolToChessmanKind (char kindChar) {
            switch (kindChar) {
                case 'P':
                    return ChessmanKindEnum.PAWN;
                case 'R':
                    return ChessmanKindEnum.ROOK;
                case 'N':
                    return ChessmanKindEnum.KNIGHT;
                case 'K':
                    return ChessmanKindEnum.KING;
                case 'Q':
                    return ChessmanKindEnum.QUEEN;
                case 'B':
                    return ChessmanKindEnum.BISHOP;
                default:
                    return null;
            }
        }

        #endregion

        public static void Shuffle<T> (Random rng, List<T> array) {
            int n = array.Count;
            while (n > 1) {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        public static bool CanBePromoted (Chessman chessman, Tile tile) {
            return (
                chessman.IsPawn() &&
                !chessman.isPromoted && (
                    ((chessman.color == ColorEnum.BLACK) && (GetRow(tile.id) == 0)) ||
                    ((chessman.color == ColorEnum.WHITE) && (GetRow(tile.id) == 7))
                )
            );
        }
    }
}
