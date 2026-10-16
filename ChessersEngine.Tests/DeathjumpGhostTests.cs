using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// A piece that moves onto a deathjump tile is left there as an inactive "ghost": the tile
    /// keeps it as occupant, but it is out of play and its location stays at the tile it left.
    /// </summary>
    [TestFixture]
    public class DeathjumpGhostTests {
        const int CheckerFromTileId = 51;
        const int EnemyTileId = 60;
        const int DeathTileId = -33;

        static Match MakeMatch () {
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
                    TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_1, EnemyTileId),
                    TestScenarios.CreateBlackKing(63),
                },
            };
            return new Match(md);
        }

        static MoveResult DeathjumpWithChecker (Match match) {
            return match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = DeathTileId,
            });
        }

        [Test]
        public void TheMoveIsLegal () {
            Match match = MakeMatch();

            MoveResult result = DeathjumpWithChecker(match);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.valid, Is.True);
            Assert.That(result.wasPieceJumped, Is.True);
        }

        [Test]
        public void TheDeathTileKeepsTheMovedPieceAsOccupant () {
            Match match = MakeMatch();
            DeathjumpWithChecker(match);
            Board board = match._GetPendingBoard();
            Chessman checker = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            Assert.That(board.PieceAt(board.GetTile(DeathTileId)), Is.SameAs(checker));
        }

        [Test]
        public void TheMovedPieceIsInactiveAndHasNoTile () {
            Match match = MakeMatch();
            DeathjumpWithChecker(match);
            Board board = match._GetPendingBoard();
            Chessman checker = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            Assert.That(checker.isActive, Is.False);
            Assert.That(board.TileOf(checker), Is.Null);
        }

        [Test]
        public void TheMovedPieceKeepsItsFromTileAsLocation () {
            Match match = MakeMatch();
            DeathjumpWithChecker(match);
            Board board = match._GetPendingBoard();
            Chessman checker = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            Assert.That(checker.location, Is.EqualTo(CheckerFromTileId));
        }

        [Test]
        public void TheFromTileAndTheJumpedTileAreEmptied () {
            Match match = MakeMatch();
            DeathjumpWithChecker(match);
            Board board = match._GetPendingBoard();
            Chessman enemy = board.GetChessman(Constants.ID_BLACK_PAWN_1);

            Assert.That(board.IsOccupied(board.GetTile(CheckerFromTileId)), Is.False);
            Assert.That(board.IsOccupied(board.GetTile(EnemyTileId)), Is.False);
            Assert.That(enemy.isActive, Is.False);
        }

        [Test]
        public void MovingAnInactivePieceIsRejectedWithoutThrowing () {
            Match match = MakeMatch();
            match._GetPendingBoard().GetChessman(Constants.ID_WHITE_PAWN_1).Deactivate();

            MoveResult result = null;
            Assert.DoesNotThrow(() => result = match.MoveChessman(new MoveAttempt {
                playerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                pieceId = Constants.ID_WHITE_PAWN_1,
                tileId = CheckerFromTileId + 8,
            }));

            Assert.That(result == null || !result.valid, Is.True);
        }
    }
}
