namespace ChessersEngine {
    public class Tile {

        public int id;
        public Chessman occupant;

        public void SetPiece (Chessman chessman) {
            occupant = chessman;
            occupant.underlyingTile = this;
        }

        public void RemovePiece () {
            occupant = null;
        }

        public bool IsOccupied () {
            return occupant != null;
        }
    }
}
