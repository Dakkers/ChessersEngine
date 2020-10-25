using System.Collections.Generic;

namespace ChessersEngine {
    /// <summary>
    /// A minified version of the ChessmanSchema class, optimized for previewing
    /// a picture of the board.
    /// </summary>
    public class ChessmanSchemaPreview {
        /// <summary>colorId</summary>
        public int c;

        /// <summary>isActive</summary>
        public int a;

        /// <summary>isChecker</summary>
        public int h;

        /// <summary>isKinged</summary>
        public int k;

        /// <summary>kind</summary>
        public int t;

        /// <summary>location</summary>
        public int l;
    }

    /// <summary>
    /// A minified version of the ChessmanSchema class.
    /// </summary>
    public class ChessmanSchemaMinified : ChessmanSchemaPreview {
        /// <summary>guid</summary>
        public int g;

        /// <summary>hasMoved</summary>
        public int m;

        /// <summary>id</summary>
        public int i;

        /// <summary>isPromoted</summary>
        public int p;

        public ChessmanSchema GetChessmanSchema () {
            return new ChessmanSchema {
                colorId = c,
                guid = g,
                hasMoved = m == 1,
                id = i,
                isActive = a == 1,
                isChecker = h == 1,
                isKinged = k == 1,
                isPromoted = p == 1,
                kind = t,
                location = l
            };
        }
    }

    public class ChessmanSchema {
        // NOTE -- keep in sync with Chessman.CreateSchema()

        public int colorId = -1;
        public int guid;
        public bool hasMoved = false;
        // For games that do not use external global identifiers, guid == id
        public int id;
        public bool isActive = true;
        public bool isChecker = false;
        public bool isKinged = false;
        public bool isPromoted = false;
        public int kind;
        public int location;

        public ChessmanSchemaMinified GetMinified () {
            return new ChessmanSchemaMinified {
                c = colorId,
                g = guid,
                m = hasMoved ? 1 : 0,
                i = id,
                a = isActive ? 1 : 0,
                h = isChecker ? 1 : 0,
                k = isKinged ? 1 : 0,
                p = isPromoted ? 1 : 0,
                t = kind,
                l = location,
            };
        }

        public static List<ChessmanSchema> CreateDefaults () {
            List<ChessmanSchema> chessmanSchemas = new List<ChessmanSchema> {
                new ChessmanSchema  {
                    location = 0,
                    id = Constants.ID_WHITE_ROOK_1
                },
                new ChessmanSchema  {
                    location = 1,
                    id = Constants.ID_WHITE_KNIGHT_1
                },
                new ChessmanSchema  {
                    location = 2,
                    id = Constants.ID_WHITE_BISHOP_1
                },
                new ChessmanSchema  {
                    location = 3,
                    id = Constants.ID_WHITE_QUEEN
                },
                new ChessmanSchema  {
                    location = 4,
                    id = Constants.ID_WHITE_KING
                },
                new ChessmanSchema  {
                    location = 5,
                    id = Constants.ID_WHITE_BISHOP_2
                },
                new ChessmanSchema  {
                    location = 6,
                    id = Constants.ID_WHITE_KNIGHT_2
                },
                new ChessmanSchema  {
                    location = 7,
                    id = Constants.ID_WHITE_ROOK_2
                },
                new ChessmanSchema  {
                    location = 8,
                    id = Constants.ID_WHITE_PAWN_1
                },
                new ChessmanSchema  {
                    location = 9,
                    id = Constants.ID_WHITE_PAWN_2
                },
                new ChessmanSchema  {
                    location = 10,
                    id = Constants.ID_WHITE_PAWN_3
                },
                new ChessmanSchema  {
                    location = 11,
                    id = Constants.ID_WHITE_PAWN_4
                },
                new ChessmanSchema  {
                    location = 12,
                    id = Constants.ID_WHITE_PAWN_5
                },
                new ChessmanSchema  {
                    location = 13,
                    id = Constants.ID_WHITE_PAWN_6
                },
                new ChessmanSchema  {
                    location = 14,
                    id = Constants.ID_WHITE_PAWN_7
                },
                new ChessmanSchema  {
                    location = 15,
                    id = Constants.ID_WHITE_PAWN_8
                },
                new ChessmanSchema  {
                    location = 48,
                    id = Constants.ID_BLACK_PAWN_1
                },
                new ChessmanSchema  {
                    location = 49,
                    id = Constants.ID_BLACK_PAWN_2
                },
                new ChessmanSchema  {
                    location = 50,
                    id = Constants.ID_BLACK_PAWN_3
                },
                new ChessmanSchema  {
                    location = 51,
                    id = Constants.ID_BLACK_PAWN_4
                },
                new ChessmanSchema  {
                    location = 52,
                    id = Constants.ID_BLACK_PAWN_5
                },
                new ChessmanSchema  {
                    location = 53,
                    id = Constants.ID_BLACK_PAWN_6
                },
                new ChessmanSchema  {
                    location = 54,
                    id = Constants.ID_BLACK_PAWN_7
                },
                new ChessmanSchema  {
                    location = 55,
                    id = Constants.ID_BLACK_PAWN_8
                },
                new ChessmanSchema  {
                    location = 56,
                    id = Constants.ID_BLACK_ROOK_1
                },
                new ChessmanSchema  {
                    location = 57,
                    id = Constants.ID_BLACK_KNIGHT_1
                },
                new ChessmanSchema  {
                    location = 58,
                    id = Constants.ID_BLACK_BISHOP_1
                },
                new ChessmanSchema  {
                    location = 59,
                    id = Constants.ID_BLACK_QUEEN
                },
                new ChessmanSchema  {
                    location = 60,
                    id = Constants.ID_BLACK_KING
                },
                new ChessmanSchema  {
                    location = 61,
                    id = Constants.ID_BLACK_BISHOP_2
                },
                new ChessmanSchema  {
                    location = 62,
                    id = Constants.ID_BLACK_KNIGHT_2
                },
                new ChessmanSchema  {
                    location = 63,
                    id = Constants.ID_BLACK_ROOK_2
                },
            };

            foreach (ChessmanSchema cs in chessmanSchemas) {
                cs.kind = Helpers.GetKind(cs.id);
            }

            return chessmanSchemas;
        }
    }
}
