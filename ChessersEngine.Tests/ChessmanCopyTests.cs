using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class ChessmanCopyTests {
        static Chessman MakeAlteredQueen () {
            Chessman chessman = Chessman.CreateFromSchema(TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_3, 40));
            chessman.guid = 777;
            chessman.hasMoved = true;
            chessman.isActive = false;
            chessman.isChecker = true;
            chessman.isKinged = true;
            chessman.Promote(ChessmanKindEnum.QUEEN);
            return chessman;
        }

        static void AssertSameFields (Chessman actual, Chessman expected) {
            Assert.Multiple(() => {
                Assert.That(actual.color, Is.EqualTo(expected.color));
                Assert.That(actual.guid, Is.EqualTo(expected.guid));
                Assert.That(actual.hasMoved, Is.EqualTo(expected.hasMoved));
                Assert.That(actual.id, Is.EqualTo(expected.id));
                Assert.That(actual.isActive, Is.EqualTo(expected.isActive));
                Assert.That(actual.isChecker, Is.EqualTo(expected.isChecker));
                Assert.That(actual.isKinged, Is.EqualTo(expected.isKinged));
                Assert.That(actual.isPromoted, Is.EqualTo(expected.isPromoted));
                Assert.That(actual.location, Is.EqualTo(expected.location));
                Assert.That(actual.kind, Is.EqualTo(expected.kind));
            });
        }

        [Test]
        public void CloneCopiesEveryField () {
            Chessman original = MakeAlteredQueen();

            AssertSameFields(original.Clone(), original);
        }

        [Test]
        public void CloneIsDetachedFromTheOriginal () {
            Chessman original = MakeAlteredQueen();
            Chessman clone = original.Clone();

            clone.location = 12;
            clone.SetActive(true);

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(original.location, Is.EqualTo(40));
            Assert.That(original.isActive, Is.False);
        }

        [Test]
        public void CopyFromChessmanOverwritesEveryField () {
            Chessman source = MakeAlteredQueen();
            Chessman target = Chessman.CreateFromSchema(TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 8));

            target.CopyFrom(source);

            AssertSameFields(target, source);
        }
    }
}
