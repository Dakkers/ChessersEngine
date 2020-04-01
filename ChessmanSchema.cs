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
    }
}
