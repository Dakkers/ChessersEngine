namespace ChessersEngine {
    public class ChessmanSchema {
        public bool hasMoved = false;
        public long guid;
        // For games that do not use external global identifiers, guid == id
        public int id;
        public bool isActive = true;
        public bool isKinged = false;
        public bool isPromoted = false;
        public int kind;
        public int location;
    }
}
