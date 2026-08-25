using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// In deathjump settings, CalculateCheckTiles simulates attacker jump paths by executing each
    /// step in sequence. Once a deathjump has left a death tile referencing an already-deactivated
    /// (null-tile) piece, a path could try to originate a step from that tile, and constructing a
    /// Move from the dead piece threw a NullReferenceException - crashing GetValidTilesForMovement /
    /// MoveChessman mid-game.
    ///
    /// The trigger state (a death tile occupied by an inactive piece) is not expressible as a static
    /// ChessmanSchema position, so this replays a specific deterministic random game found by the
    /// fuzzer (seed 3 / SIDES) that crashes around move 90.
    /// </summary>
    [TestFixture]
    public class DeathjumpCheckTilesTests {
        static List<(int, int)> CollectLegal (Board board, ColorEnum color, bool jumpsOnly, int onlyPieceId) {
            var legal = new List<(int, int)>();
            IEnumerable<Chessman> pieces = onlyPieceId >= 0 ? new[] { board.GetChessman(onlyPieceId) } : board.GetActiveChessmenOfColor(color);
            foreach (var p in pieces) {
                if (p == null || !p.isActive || p.color != color) continue;
                foreach (var t in board.GetValidTilesForMovement(p, jumpsOnly)) legal.Add((p.id, t.id));
            }
            return legal;
        }

        // Plays a deterministic random legal game; must run to completion without throwing.
        static void PlayGame (int seed, DeathjumpSetting setting) {
            var rng = new System.Random(seed);
            var md = new MatchData {
                currentTurn = ColorEnum.WHITE, matchId = 1,
                whitePlayerId = 0, blackPlayerId = 1, deathjumpSetting = (int) setting,
            };
            Match match = new Match(md, null, seed);
            int lastPiece = -1;
            for (int step = 0; step < 250; step++) {
                if (match.IsGameOver()) return;
                ColorEnum color = match.GetTurnColor();
                Board board = match._GetPendingBoard();
                bool midTurn = match.GetPendingMoveResults().Count > 0;
                var legal = CollectLegal(board, color, midTurn, midTurn ? lastPiece : -1);
                if (legal.Count == 0) return;
                var (pieceId, tileId) = legal[rng.Next(legal.Count)];
                MoveResult result = match.MoveChessman(new MoveAttempt { pieceId = pieceId, tileId = tileId, playerId = (color == ColorEnum.WHITE) ? 0 : 1 });
                lastPiece = pieceId;
                if (result.turnChanged) match.CommitTurn();
            }
        }

        // seed 3 crashes in CalculateCheckTiles' path execution (a step originating from a death
        // tile); seed 142 crashes in _CalculatePotentialJumpPaths (recursing onto a death-tile
        // landing and running the capture checks on its dead occupant).
        [TestCase(3, DeathjumpSetting.SIDES)]
        [TestCase(142, DeathjumpSetting.SIDES)]
        public void RandomGame_DoesNotThrowInDeathjumpCheckDetection (int seed, DeathjumpSetting setting) {
            Assert.DoesNotThrow(() => PlayGame(seed, setting));
        }
    }
}
