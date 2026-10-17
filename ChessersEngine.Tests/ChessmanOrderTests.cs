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

        static List<int> SortedIds (IEnumerable<int> ids) {
            return ids.OrderBy((id) => id).ToList();
        }

        [Test]
        public void ActiveChessmenAreInAscendingIdOrder () {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetActiveChessmen().Select((c) => c.id).ToList();

            Assert.That(ids, Is.EqualTo(SortedIds(ids)));
            Assert.That(ids, Has.Count.EqualTo(Constants.NUM_CHESSMEN));
        }

        [Test]
        public void SchemasAreInAscendingIdOrder () {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetChessmanSchemas().Select((cs) => cs.id).ToList();

            Assert.That(ids, Is.EqualTo(SortedIds(ids)));
        }

        [Test]
        public void AllChessmenAreInAscendingIdOrder () {
            Board board = MakeBoardFromReversedSchemas();

            List<int> ids = board.GetAllChessmen().Keys.ToList();

            Assert.That(ids, Is.EqualTo(SortedIds(ids)));
        }

        [Test]
        public void CopyKeepsAscendingIdOrder () {
            Board copy = MakeBoardFromReversedSchemas().CreateCopy();

            List<int> ids = copy.GetActiveChessmen().Select((c) => c.id).ToList();

            Assert.That(ids, Is.EqualTo(SortedIds(ids)));
        }
    }
}
