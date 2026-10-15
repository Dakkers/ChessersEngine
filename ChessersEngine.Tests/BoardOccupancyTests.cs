using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class BoardOccupancyTests {
        const int FromTileId = 8;
        const int ToTileId = 16;
        const int EmptyTileId = 30;

        static Board MakeBoard (params ChessmanSchema[] pieces) {
            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                pieces = new List<ChessmanSchema>(pieces),
            };
            return new Match(md)._GetPendingBoard();
        }

        static Board MakeBoardWithTwoPawns () {
            return MakeBoard(
                TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, FromTileId),
                TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_2, ToTileId));
        }

        [Test]
        public void PieceAtReturnsTheOccupant () {
            Board board = MakeBoardWithTwoPawns();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            Assert.That(board.PieceAt(board.GetTile(FromTileId)), Is.SameAs(pawn));
        }

        [Test]
        public void PieceAtReturnsNullForAnEmptyTile () {
            Board board = MakeBoardWithTwoPawns();

            Assert.That(board.PieceAt(board.GetTile(EmptyTileId)), Is.Null);
        }

        [Test]
        public void IsOccupiedIsTrueForAnOccupiedTile () {
            Board board = MakeBoardWithTwoPawns();

            Assert.That(board.IsOccupied(board.GetTile(FromTileId)), Is.True);
        }

        [Test]
        public void IsOccupiedIsFalseForAnEmptyTile () {
            Board board = MakeBoardWithTwoPawns();

            Assert.That(board.IsOccupied(board.GetTile(EmptyTileId)), Is.False);
        }

        [Test]
        public void TileOfReturnsTheTileAnActiveChessmanStandsOn () {
            Board board = MakeBoardWithTwoPawns();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            Assert.That(board.TileOf(pawn), Is.SameAs(board.GetTile(FromTileId)));
        }

        [Test]
        public void TileOfReturnsNullAfterDeactivate () {
            Board board = MakeBoardWithTwoPawns();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            board.Deactivate(pawn);

            Assert.That(pawn.isActive, Is.False);
            Assert.That(board.TileOf(pawn), Is.Null);
        }

        [Test]
        public void TileOfReturnsNullForAChessmanMarkedInactive () {
            Board board = MakeBoardWithTwoPawns();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);

            pawn.SetActive(false);

            Assert.That(board.TileOf(pawn), Is.Null);
        }

        [Test]
        public void PlaceMovesOccupancyAndLocationAndLeavesTheFromTileToTheCaller () {
            Board board = MakeBoard(TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, FromTileId));
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Tile fromTile = board.GetTile(FromTileId);
            Tile toTile = board.GetTile(ToTileId);

            board.Place(pawn, toTile);

            Assert.That(board.PieceAt(toTile), Is.SameAs(pawn));
            Assert.That(board.TileOf(pawn), Is.SameAs(toTile));
            Assert.That(pawn.location, Is.EqualTo(ToTileId));
            Assert.That(board.PieceAt(fromTile), Is.SameAs(pawn),
                "Place does not empty the from-tile");

            board.Clear(fromTile);

            Assert.That(board.IsOccupied(fromTile), Is.False);
            Assert.That(board.PieceAt(toTile), Is.SameAs(pawn));
        }

        [Test]
        public void DeactivateLeavesATileHeldByAnotherPieceAlone () {
            Board board = MakeBoardWithTwoPawns();
            Chessman first = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Chessman second = board.GetChessman(Constants.ID_WHITE_PAWN_2);
            Tile firstTile = board.GetTile(FromTileId);

            board.Clear(firstTile);
            board.Clear(board.GetTile(ToTileId));
            board.Place(second, firstTile);
            board.Deactivate(first);

            Assert.That(first.isActive, Is.False);
            Assert.That(board.PieceAt(firstTile), Is.SameAs(second));
        }

        [Test]
        public void DeactivateFreesTheTileTheChessmanStillHolds () {
            Board board = MakeBoardWithTwoPawns();
            Chessman pawn = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            Tile tile = board.GetTile(FromTileId);

            board.Deactivate(pawn);

            Assert.That(board.IsOccupied(tile), Is.False);
        }
    }
}
