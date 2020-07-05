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
        public ColorEnum color;

        public int guid;
        public bool hasMoved = false;
        public int id;
        /// <summary>
        /// Represents whether or not the chessman is still in play.
        /// </summary>
        public bool isActive = true;
        public bool isChecker = false;
        public bool isKinged = false;
        public bool isPromoted = false;
        /// <summary>
        /// Last location of the chessman when it was in play. (So if the chessman is no longer active,
        /// it will return the location it was captured/jumped at.)
        /// </summary>
        public int location = -1;
        public ChessmanKindEnum kind;

        Tile underlyingTile;

        public Chessman (ChessmanSchema cs) {
            guid = cs.guid == 0 ?
                     cs.id :
                     cs.guid;

            hasMoved = cs.hasMoved;
            id = cs.id;
            isActive = cs.isActive;
            isChecker = cs.isChecker;
            isKinged = cs.isKinged;
            isPromoted = cs.isPromoted;
            location = cs.location;

            color = ((id % 2) == 0) ?
                ColorEnum.WHITE :
                ColorEnum.BLACK;

            kind = (ChessmanKindEnum) cs.kind;
        }

        public void SetActive (bool val) {
            isActive = val;
        }

        public void Deactivate () {
            if (underlyingTile.GetPiece().id == this.id) {
                underlyingTile.RemovePiece();
            }
            RemoveUnderlyingTileReference();
            SetActive(false);
        }

        public void SetHasMoved (bool val) {
            hasMoved = val;
        }

        public static Chessman CreateFromSchema (ChessmanSchema cs) {
            return new Chessman(cs);
        }

        public Chessman Clone () {
            return new Chessman(CreateSchema());
        }

        public void CopyFrom (ChessmanSchema chessmanSchema) {
            this.color = Helpers.ConvertIntToColorEnum(chessmanSchema.colorId);
            this.guid = chessmanSchema.guid;
            this.hasMoved = chessmanSchema.hasMoved;
            this.id = chessmanSchema.id;
            this.isActive = chessmanSchema.isActive;
            this.isChecker = chessmanSchema.isChecker;
            this.isKinged = chessmanSchema.isKinged;
            this.isPromoted = chessmanSchema.isPromoted;
            this.location = chessmanSchema.location;
            this.kind = (ChessmanKindEnum) chessmanSchema.kind;
        }

        public void CopyFrom (Chessman chessman) {
            this.CopyFrom(chessman.CreateSchema());
        }

        #region Color checks

        public bool IsWhite () {
            return color == ColorEnum.WHITE;
        }

        public bool IsBlack () {
            return color == ColorEnum.BLACK;
        }

        /// <summary>
        /// Compare the color of this chessman to the color of another chessman.
        /// </summary>
        /// <returns><c>true</c>, if colors are the same, <c>false</c> otherwise.</returns>
        /// <param name="other">Other chessman to compare against.</param>
        public bool IsSameColor (Chessman other) {
            return this.color == other.color;
        }

        public bool IsSameColor (ColorEnum color) {
            return this.color == color;
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

        public Tile GetUnderlyingTile () {
            return underlyingTile;
        }

        public void SetUnderlyingTile (Tile tile) {
            underlyingTile = tile;
            location = tile.id;
        }

        public void RemoveUnderlyingTileReference () {
            underlyingTile = null;
        }

        public void TogglePolarity () {
            isChecker = !isChecker;
        }

        public ChessmanSchema CreateSchema () {
            return new ChessmanSchema {
                colorId = (int) color,
                guid = guid,
                hasMoved = hasMoved,
                id = id,
                isActive = isActive,
                isChecker = isChecker,
                isKinged = isKinged,
                isPromoted = isPromoted,
                kind = (int) kind,
                location = location,
            };
        }
    }
}
