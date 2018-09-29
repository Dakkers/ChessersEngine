namespace ChessersEngine {
    public class ChessmanSchema {
        // NOTE -- keep in sync with Chessman.CreateSchema()

        public int colorId = -1;
        public bool hasMoved = false;
        public long guid;
        // For games that do not use external global identifiers, guid == id
        public int id;
        public bool isActive = true;
        public bool isChecker = false;
        public bool isKinged = false;
        public bool isPromoted = false;
        public int kind;
        public int location;
    }
}
