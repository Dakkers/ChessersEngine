namespace ChessersEngine {
    public class Tile {

        /// <summary>
        /// Tile identifier, one of [0 ... 63]. Represents the location on the board.
        /// </summary>
        public int id;
        public Chessman occupant;

        public Tile Clone () {
            return new Tile { id = id };
        }

        public void CopyFrom (Tile tile) {
            this.id = tile.id;
        }

        public Chessman GetPiece () {
            return occupant;
        }

        public void SetPiece (Chessman chessman) {
            occupant = chessman;
        }

        public void RemovePiece () {
            occupant = null;
        }

        public bool IsOccupied () {
            return occupant != null;
        }
    }
}
