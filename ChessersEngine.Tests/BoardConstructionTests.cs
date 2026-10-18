using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    [TestFixture]
    public class BoardConstructionTests {
        [Test]
        public void ConstructorRejectsSchemasSharingAnId () {
            List<ChessmanSchema> pieces = new List<ChessmanSchema> {
                TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 8),
                TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 16),
            };

            Assert.Throws<ArgumentException>(() => new Board(pieces, default(MatchConfig)));
        }
    }
}
