using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class BoardCopyTests {
        const int CheckerFromTileId = 51;
        const int DeathTileId = -33;

        static Match MakeDeathjumpMatch () {
            ChessmanSchema checker = TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, CheckerFromTileId);
            checker.isChecker = true;

            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                deathjumpSetting = (int) DeathjumpSetting.ALL,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    checker,
                    TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_1, 60),
                    TestScenarios.CreateBlackKing(63),
                },
            };
            return new Match(md);
        }

        static List<string> Describe (Board board) {
            return board.GetChessmanSchemas()
                .Select((cs) => $"{cs.id}:{cs.guid}:{cs.colorId}:{cs.kind}:{cs.location}:{cs.isActive}:{cs.hasMoved}:{cs.isChecker}:{cs.isKinged}:{cs.isPromoted}")
                .ToList();
        }

        static List<int> Ids (IEnumerable<Chessman> chessmen) {
            return chessmen.Select((c) => c.id).ToList();
        }

        [Test]
        public void CopyHasTheSameSchemasInTheSameOrder () {
            Board board = new Match(null)._GetCommittedBoard();

            Board copy = board.CreateCopy();

            Assert.That(Describe(copy), Is.EqualTo(Describe(board)));
            Assert.That(Ids(copy.GetAllChessmen().Values), Is.EqualTo(Ids(board.GetAllChessmen().Values)));
        }

        [Test]
        public void CopyOwnsItsChessmenAndTiles () {
            Board board = new Match(null)._GetCommittedBoard();

            Board copy = board.CreateCopy();

            Chessman original = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Chessman copied = copy.GetChessman(Constants.ID_WHITE_PAWN_1);
            Assert.That(copied, Is.Not.SameAs(original));
            Assert.That(copy.TileOf(copied), Is.Not.SameAs(board.TileOf(original)));
            Assert.That(copy.PieceAt(copy.TileOf(copied)), Is.SameAs(copied));
        }

        [Test]
        public void MutatingTheCopyLeavesTheOriginalAlone () {
            Board board = new Match(null)._GetCommittedBoard();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Tile fromTile = board.TileOf(pawn);

            Board copy = board.CreateCopy();
            Chessman copiedPawn = copy.GetChessman(Constants.ID_WHITE_PAWN_1);
            copy.Clear(copy.TileOf(copiedPawn));
            copy.Deactivate(copiedPawn);

            Assert.That(pawn.isActive, Is.True);
            Assert.That(board.PieceAt(fromTile), Is.SameAs(pawn));
        }

        [Test]
        public void CopyKeepsTheDeathjumpGhost () {
            Match match = MakeDeathjumpMatch();
            match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = DeathTileId,
            });
            Board board = match._GetPendingBoard();

            Board copy = board.CreateCopy();

            Chessman ghost = copy.PieceAt(copy.GetTile(DeathTileId));
            Assert.That(ghost, Is.SameAs(copy.GetChessman(Constants.ID_WHITE_PAWN_1)));
            Assert.That(ghost.isActive, Is.False);
        }

        [Test]
        public void AssignCopiesStateAndKeepsObjectIdentity () {
            Board board = new Match(null)._GetCommittedBoard();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Tile fromTile = board.TileOf(pawn);

            Board source = board.CreateCopy();
            Chessman sourcePawn = source.GetChessman(Constants.ID_WHITE_PAWN_1);
            source.Clear(source.TileOf(sourcePawn));
            source.Deactivate(sourcePawn);

            board.Assign(source);

            Assert.That(board.GetChessman(Constants.ID_WHITE_PAWN_1), Is.SameAs(pawn));
            Assert.That(pawn.isActive, Is.False);
            Assert.That(board.IsOccupied(fromTile), Is.False);
            Assert.That(Describe(board), Is.EqualTo(Describe(source)));
        }
    }
}
