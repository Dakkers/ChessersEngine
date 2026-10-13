namespace ChessersEngine {
    public class Tile {

        /// <summary>
        /// Tile identifier. On-board tiles are [0 ... 63]; deathjump tiles use negative ids.
        /// </summary>
        public int id;

        public Tile Clone () {
            return new Tile { id = id };
        }

        public void CopyFrom (Tile tile) {
            this.id = tile.id;
        }

        public bool IsDeathjumpTile () {
            return this.id < 0;
        }
    }
}
