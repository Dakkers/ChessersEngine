using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class MatchTests {
        // Default board (0-63, row-major): white pawns occupy rank 2 (tiles 8-15),
        // so white pawn 1 (id 0) sits on tile 8 and advances to 16 (single) or 24 (double).
        const int WhitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID;
        const int WhitePawn1Id = Constants.ID_WHITE_PAWN_1;

        static Match NewDefaultMatch () => new Match(null);

        [Test]
        public void NewMatch_StartsWithWhiteToMove () {
            Match match = NewDefaultMatch();
            Assert.That(match.GetTurnColor(), Is.EqualTo(ColorEnum.WHITE));
        }

        [Test]
        public void NewMatch_SetsUpAllThirtyTwoPieces () {
            Match match = NewDefaultMatch();
            Assert.That(match.GetAllCommittedChessmen(), Has.Count.EqualTo(32));
        }

        [Test]
        public void MoveChessman_SinglePawnAdvance_IsValid () {
            Match match = NewDefaultMatch();

            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = WhitePlayerId,
                pieceId = WhitePawn1Id,
                tileId = 16,
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True);
        }

        [Test]
        public void MoveChessman_DoublePawnAdvance_PassesTurnToBlack () {
            Match match = NewDefaultMatch();

            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = WhitePlayerId,
                pieceId = WhitePawn1Id,
                tileId = 24,
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True);
            // A plain pawn move (no checker jump pending) hands the turn to black.
            Assert.That(match.GetTurnColor(), Is.EqualTo(ColorEnum.BLACK));
        }

        [Test]
        public void MoveChessman_WrongPlayer_IsRejected () {
            Match match = NewDefaultMatch();

            // Black attempting to move on white's turn is rejected outright (null).
            MoveResult result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                pieceId = WhitePawn1Id,
                tileId = 16,
            });

            Assert.That(result, Is.Null);
        }
    }
}
