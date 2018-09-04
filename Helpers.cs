using System.Collections.Generic;

namespace ChessersEngine {
    public static class Helpers {

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
            return new List<int>() {
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
            return new List<int>() {
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

        public static int GetTileIdFromCoords (int x, int y) {
            return (8 * y) + x;
        }

        #region Chessman color

        public static bool IsWhitePiece (int pieceId) {
            return pieceId % 2 == 0;
        }

        public static bool IsBlackPiece (int pieceId) {
            return pieceId % 2 == 1;
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
            return tileId % 8;
        }

        public static int GetRow (int tileId) {
            return tileId / 8;
        }

        #endregion

        public static bool IsValidPromotion (ChessmanKindEnum promotionRank) {
            return (promotionRank == ChessmanKindEnum.QUEEN) ||
                (promotionRank == ChessmanKindEnum.BISHOP) ||
                (promotionRank == ChessmanKindEnum.ROOK) ||
                (promotionRank == ChessmanKindEnum.KNIGHT);
        }
    }
}