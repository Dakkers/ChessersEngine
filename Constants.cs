namespace ChessersEngine {
    public static class Constants {
        public const int DEFAULT_WHITE_PLAYER_ID = 0;
        public const int DEFAULT_BLACK_PLAYER_ID = 1;

        public const int TOP_ROW = 7;
        public const int BOTTOM_ROW = 0;

        public const int RIGHT_COLUMN = 7;
        public const int LEFT_COLUMN = 0;

        #region Piece IDs

        public const int ID_WHITE = 0;
        public const int ID_BLACK = 1;

        public const int ID_WHITE_PAWN_1 = 0;
        public const int ID_BLACK_PAWN_1 = 1;
        public const int ID_WHITE_PAWN_2 = 2;
        public const int ID_BLACK_PAWN_2 = 3;
        public const int ID_WHITE_PAWN_3 = 4;
        public const int ID_BLACK_PAWN_3 = 5;
        public const int ID_WHITE_PAWN_4 = 6;
        public const int ID_BLACK_PAWN_4 = 7;
        public const int ID_WHITE_PAWN_5 = 8;
        public const int ID_BLACK_PAWN_5 = 9;
        public const int ID_WHITE_PAWN_6 = 10;
        public const int ID_BLACK_PAWN_6 = 11;
        public const int ID_WHITE_PAWN_7 = 12;
        public const int ID_BLACK_PAWN_7 = 13;
        public const int ID_WHITE_PAWN_8 = 14;
        public const int ID_BLACK_PAWN_8 = 15;

        public const int ID_WHITE_ROOK_1 = 16;
        public const int ID_BLACK_ROOK_1 = 17;
        public const int ID_WHITE_ROOK_2 = 18;
        public const int ID_BLACK_ROOK_2 = 19;

        public const int ID_WHITE_KNIGHT_1 = 20;
        public const int ID_BLACK_KNIGHT_1 = 21;
        public const int ID_WHITE_KNIGHT_2 = 22;
        public const int ID_BLACK_KNIGHT_2 = 23;

        public const int ID_WHITE_BISHOP_1 = 24;
        public const int ID_BLACK_BISHOP_1 = 25;
        public const int ID_WHITE_BISHOP_2 = 26;
        public const int ID_BLACK_BISHOP_2 = 27;

        public const int ID_WHITE_QUEEN = 28;
        public const int ID_BLACK_QUEEN = 29;

        public const int ID_WHITE_KING = 30;
        public const int ID_BLACK_KING = 31;

        #endregion

        #region Move Types

        public const int MOVE_TYPE_REGULAR = 0;
        public const int MOVE_TYPE_JUMP = 1;
        public const int MOVE_TYPE_TAKE = 2;
        public const int MOVE_TYPE_CASTLING = 3;

        #endregion

        #region Chessman kinds

        public const int CHESSMAN_KIND_PAWN = 0;
        public const int CHESSMAN_KIND_KNIGHT = 1;
        public const int CHESSMAN_KIND_BISHOP = 2;
        public const int CHESSMAN_KIND_ROOK = 3;
        public const int CHESSMAN_KIND_QUEEN = 4;
        public const int CHESSMAN_KIND_KING = 5;

        #endregion

        #region Ranks (rows), Files (columns)

        public const int RANK_1 = 0;
        public const int RANK_2 = 1;
        public const int RANK_3 = 2;
        public const int RANK_4 = 3;
        public const int RANK_5 = 4;
        public const int RANK_6 = 5;
        public const int RANK_7 = 6;
        public const int RANK_8 = 7;

        public const int FILE_A = 0;
        public const int FILE_B = 1;
        public const int FILE_C = 2;
        public const int FILE_D = 3;
        public const int FILE_E = 4;
        public const int FILE_F = 5;
        public const int FILE_G = 6;
        public const int FILE_H = 7;

        #endregion
    }
}
