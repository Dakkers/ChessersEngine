namespace ChessersEngine {
    public enum ColorEnum {
        WHITE = 0,
        BLACK,
    }

    public enum ChessmanKindEnum {
        PAWN = 0,
        KNIGHT = 1,
        BISHOP = 2,
        ROOK = 3,
        QUEEN = 4,
        KING = 5,
    }

    public class Chessman {
        public int colorId;

        public long guid;
        public bool hasMoved = false;
        public bool isActive = true;
        public int id;
        public bool isKinged = false;
        public bool isPromoted = false;
        public ChessmanKindEnum kind;
        private bool isChecker = false;

        public Tile underlyingTile;

        public Chessman (ChessmanSchema cs) {
            colorId = ((cs.id % 2) == 0) ?
                Constants.ID_WHITE :
                Constants.ID_BLACK;

            guid = cs.guid == 0 ?
                     cs.id :
                     cs.guid;

            hasMoved = cs.hasMoved;
            id = cs.id;
            isActive = cs.isActive;
            isKinged = cs.isKinged;
            isPromoted = cs.isPromoted;

            kind = (ChessmanKindEnum) cs.kind;
        }

        public void SetActive (bool _active) {
            isActive = _active;
        }

        public void Deactivate () {
            underlyingTile.RemovePiece();
            SetActive(false);
        }

        public void SetHasMoved (bool newHasMoved) {
            hasMoved = newHasMoved;
        }

        public static Chessman CreateFromSchema (ChessmanSchema cs) {
            return new Chessman(cs);
        }

        #region Color checks

        public bool IsWhite () {
            return colorId == Constants.ID_WHITE;
        }

        public bool IsBlack () {
            return colorId == Constants.ID_BLACK;
        }

        #endregion

        #region Kind checks

        public bool IsBishop () {
            return kind == ChessmanKindEnum.BISHOP;
        }

        public bool IsKing () {
            return kind == ChessmanKindEnum.KING;
        }

        public bool IsKnight () {
            return kind == ChessmanKindEnum.KNIGHT;
        }

        public bool IsPawn () {
            return kind == ChessmanKindEnum.PAWN;
        }

        public bool IsQueen () {
            return kind == ChessmanKindEnum.QUEEN;
        }

        public bool IsRook () {
            return kind == ChessmanKindEnum.ROOK;
        }

        #endregion

        public void Promote (ChessmanKindEnum newKind) {
            kind = newKind;
        }

        public bool IsChecker () {
            return isChecker;
        }

        public ChessmanSchema CreateSchema () {
            return new ChessmanSchema {
                id = id,
                hasMoved = hasMoved,
                isActive = isActive,
                isKinged = isKinged,
                isPromoted = isPromoted,
                kind = (int) kind
            };
        }
    }
}
