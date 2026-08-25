using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// Deathjump landing squares must honour the deathjumpSetting: SIDES enables
    /// only the side columns, BACK only the back rows. The checker move generator
    /// only special-cased OFF (using the bounds-unaware GetTileIfExists), so a SIDES
    /// game wrongly allowed back-edge deathjumps and a BACK game wrongly allowed
    /// side-edge deathjumps.
    /// </summary>
    [TestFixture]
    public class DeathjumpSettingTests {
        static List<int> CheckerPotentialTileIds (DeathjumpSetting setting, int checkerTile, int enemyTile) {
            ChessmanSchema checker = TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, checkerTile);
            checker.isChecker = true;

            MatchData md = new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = Constants.DEFAULT_WHITE_PLAYER_ID,
                blackPlayerId = Constants.DEFAULT_BLACK_PLAYER_ID,
                deathjumpSetting = (int) setting,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    checker,
                    TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_1, enemyTile),
                    TestScenarios.CreateBlackKing(63),
                },
            };
            Match match = new Match(md);
            Board board = match._GetPendingBoard();
            Chessman piece = board.GetChessman(Constants.ID_WHITE_PAWN_1);
            return board.GetPotentialTilesForMovement(piece).Select(t => t.id).ToList();
        }

        // Checker b7 (51) jumps the enemy on e8-ish (60) onto the BACK death tile -33.
        const int BackCheckerTile = 51;
        const int BackEnemyTile = 60;
        const int BackDeathTile = -33;

        // Checker b2 (9) jumps the enemy on a3 (16) onto the SIDE death tile -17.
        const int SideCheckerTile = 9;
        const int SideEnemyTile = 16;
        const int SideDeathTile = -17;

        [Test]
        public void SidesSetting_ForbidsBackDeathjump () {
            var ids = CheckerPotentialTileIds(DeathjumpSetting.SIDES, BackCheckerTile, BackEnemyTile);
            Assert.That(ids, Does.Not.Contain(BackDeathTile),
                "SIDES must not allow a back-edge deathjump");
        }

        [Test]
        public void AllSetting_AllowsBackDeathjump () {
            var ids = CheckerPotentialTileIds(DeathjumpSetting.ALL, BackCheckerTile, BackEnemyTile);
            Assert.That(ids, Does.Contain(BackDeathTile), "ALL allows back-edge deathjumps");
        }

        [Test]
        public void BackSetting_ForbidsSideDeathjump () {
            var ids = CheckerPotentialTileIds(DeathjumpSetting.BACK, SideCheckerTile, SideEnemyTile);
            Assert.That(ids, Does.Not.Contain(SideDeathTile),
                "BACK must not allow a side-edge deathjump");
        }

        [Test]
        public void AllSetting_AllowsSideDeathjump () {
            var ids = CheckerPotentialTileIds(DeathjumpSetting.ALL, SideCheckerTile, SideEnemyTile);
            Assert.That(ids, Does.Contain(SideDeathTile), "ALL allows side-edge deathjumps");
        }
    }
}
