using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class ChessmanOrderTests {
        static Board MakeBoardFromReversedSchemas () {
            List<ChessmanSchema> pieces = ChessmanSchema.CreateDefaults();
            pieces.Reverse();
            return new Board(pieces, new MatchConfig());
        }

        static void AssertStrictlyAscending (List<int> ids) {
            Assert.That(ids, Is.Ordered.Ascending);
            Assert.That(ids, Is.Unique);
        }

        [Test]
        public void ActiveChessmenAreInAscendingIdOrder () {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetActiveChessmen().Select((c) => c.id).ToList();

            AssertStrictlyAscending(ids);
            Assert.That(ids, Has.Count.EqualTo(Constants.NUM_CHESSMEN));
        }

        [TestCase(ColorEnum.WHITE)]
        [TestCase(ColorEnum.BLACK)]
        public void ActiveChessmenOfColorAreInAscendingIdOrder (ColorEnum color) {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetActiveChessmenOfColor(color).Select((c) => c.id).ToList();

            AssertStrictlyAscending(ids);
            Assert.That(ids, Is.Not.Empty);
        }

        [Test]
        public void SchemasAreInAscendingIdOrder () {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetChessmanSchemas().Select((cs) => cs.id).ToList();

            AssertStrictlyAscending(ids);
        }

        [Test]
        public void AllChessmenAreInAscendingIdOrder () {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetAllChessmen().Keys.ToList();

            AssertStrictlyAscending(ids);
        }

        [Test]
        public void CopyKeepsAscendingIdOrder () {
            Board copy = MakeBoardFromReversedSchemas().CreateCopy();

            List<int> ids = copy.GetActiveChessmen().Select((c) => c.id).ToList();

            AssertStrictlyAscending(ids);
        }

        [Test]
        public void AssignKeepsAscendingIdOrderAndMatchesSource () {
            Board target = MakeBoardFromReversedSchemas();
            Board source = MakeBoardFromReversedSchemas();
            Chessman sourcePawn = source.GetChessman(Constants.ID_WHITE_PAWN_1);
            source.Clear(source.TileOf(sourcePawn));
            source.Deactivate(sourcePawn);

            target.Assign(source);

            List<int> activeIds = target.GetActiveChessmen().Select((c) => c.id).ToList();
            List<int> sourceActiveIds = source.GetActiveChessmen().Select((c) => c.id).ToList();
            List<int> whiteIds = target.GetActiveChessmenOfColor(ColorEnum.WHITE).Select((c) => c.id).ToList();
            List<int> schemaIds = target.GetChessmanSchemas().Select((cs) => cs.id).ToList();
            AssertStrictlyAscending(activeIds);
            AssertStrictlyAscending(whiteIds);
            AssertStrictlyAscending(schemaIds);
            Assert.That(activeIds, Is.EqualTo(sourceActiveIds));
            Assert.That(activeIds, Does.Not.Contain(Constants.ID_WHITE_PAWN_1));
            Assert.That(activeIds, Has.Count.EqualTo(Constants.NUM_CHESSMEN - 1));
        }
    }
}
