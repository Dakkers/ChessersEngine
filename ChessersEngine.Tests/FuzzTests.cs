using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessersEngine.Tests {
    /// <summary>
    /// Property-based regression fuzzer. Plays many seeded random *legal* games through the public
    /// API and, after every move, asserts invariants that must always hold. Seeds are fixed, so any
    /// failure is deterministic and reproducible from its (seed, deathjumpSetting).
    ///
    /// Oracles:
    ///   - no move / commit / legal-generation ever throws;
    ///   - MoveChessman followed by UndoMove restores the board (real-board state);
    ///   - board consistency: active piece &lt;-&gt; tile agree, no two pieces share a tile, no inactive
    ///     piece sits on a live (real) tile;
    ///   - every move offered by GetValidTilesForMovement is actually playable (valid);
    ///   - if the player to move has no legal move, the game must be over.
    ///
    /// Note: death-tile (negative id) occupancy is intentionally excluded from the state snapshot.
    /// A death tile keeps referencing the (deactivated, off-board) piece that died on it, and
    /// UndoMove does not restore that purely cosmetic reference; it is never read for game logic.
    /// </summary>
    [TestFixture]
    public class FuzzTests {
        const int GamesPerSetting = 75;
        const int MoveCap = 220;

        // Canonical snapshot of the game-relevant board state (piece flags + real-tile occupancy).
        static string Snapshot (Board b) {
            string pieces = string.Join(";", b.GetChessmanSchemas()
                .OrderBy(cs => cs.id)
                .Select(cs => $"{cs.id},{cs.kind},{cs.location},{(cs.isActive ? 1 : 0)},{(cs.isChecker ? 1 : 0)},{(cs.isKinged ? 1 : 0)},{(cs.isPromoted ? 1 : 0)},{(cs.hasMoved ? 1 : 0)},{cs.colorId}"));
            var occ = new List<string>();
            for (int id = 0; id < 64; id++) occ.Add(b.GetTile(id).GetPiece()?.id.ToString() ?? "_");
            return pieces + "|" + string.Join(",", occ);
        }

        static void CheckConsistency (Board b, Action<string> fail) {
            var seen = new Dictionary<int, int>();
            foreach (var c in b.GetActiveChessmen()) {
                Tile ut = c.GetUnderlyingTile();
                if (ut == null) { fail($"active piece {c.id} has null underlying tile"); continue; }
                if (ut.GetPiece()?.id != c.id) fail($"tile {ut.id} occupant != piece {c.id}");
                if (c.location != ut.id) fail($"piece {c.id} location {c.location} != tile {ut.id}");
                if (seen.ContainsKey(ut.id)) fail($"tile {ut.id} shared by pieces {seen[ut.id]} and {c.id}");
                else seen[ut.id] = c.id;
            }
            for (int id = 0; id < 64; id++) {
                Tile t = b.GetTile(id);
                if (t.IsOccupied() && !t.GetPiece().isActive)
                    fail($"real tile {id} holds inactive piece {t.GetPiece().id}");
            }
        }

        static List<(int, int)> CollectLegal (Board board, ColorEnum color, bool jumpsOnly, int onlyPieceId) {
            var legal = new List<(int, int)>();
            IEnumerable<Chessman> pieces = onlyPieceId >= 0
                ? new[] { board.GetChessman(onlyPieceId) }
                : board.GetActiveChessmenOfColor(color);
            foreach (var p in pieces) {
                if (p == null || !p.isActive || p.color != color) continue;
                foreach (var t in board.GetValidTilesForMovement(p, jumpsOnly)) legal.Add((p.id, t.id));
            }
            return legal;
        }

        static void PlayGame (int seed, DeathjumpSetting setting, List<string> failures) {
            void Record (string cat, string detail) => failures.Add($"[{cat}] seed={seed} dj={setting}: {detail}");

            var rng = new System.Random(seed);
            var md = new MatchData {
                currentTurn = ColorEnum.WHITE, matchId = 1,
                whitePlayerId = 0, blackPlayerId = 1, deathjumpSetting = (int) setting,
            };
            Match match;
            try { match = new Match(md, null, seed); }
            catch (Exception e) { Record("ctor-throw", e.GetType().Name + ": " + e.Message); return; }

            int lastPiece = -1;
            for (int step = 0; step < MoveCap; step++) {
                if (match.IsGameOver()) return;
                ColorEnum color = match.GetTurnColor();
                Board board = match._GetPendingBoard();
                bool midTurn = match.GetPendingMoveResults().Count > 0;

                List<(int, int)> legal;
                try { legal = CollectLegal(board, color, midTurn, midTurn ? lastPiece : -1); }
                catch (Exception e) { Record("legal-gen-throw", $"step {step}: {e.GetType().Name}: {e.Message}"); return; }

                if (legal.Count == 0) {
                    if (midTurn) Record("stuck-midturn", $"step {step}: no continuation but turn did not change (piece {lastPiece})");
                    else if (!match.IsGameOver()) Record("no-moves-not-over", $"step {step}: {color} has no legal move but game is not over");
                    return;
                }

                var (pieceId, tileId) = legal[rng.Next(legal.Count)];

                // Undo round-trip on a copy.
                try {
                    Board copy = board.CreateCopy();
                    string before = Snapshot(copy);
                    MoveResult rr = copy.MoveChessman(new MoveAttempt { pieceId = pieceId, tileId = tileId, playerId = (int) color });
                    if (rr.valid) {
                        copy.UndoMove(rr);
                        if (Snapshot(copy) != before) Record("undo-mismatch", $"step {step}: move {pieceId}->{tileId} not restored by UndoMove");
                    }
                } catch (Exception e) { Record("undo-throw", $"step {step}: move {pieceId}->{tileId} {e.GetType().Name}: {e.Message}"); return; }

                // Play for real.
                MoveResult result;
                try { result = match.MoveChessman(new MoveAttempt { pieceId = pieceId, tileId = tileId, playerId = (color == ColorEnum.WHITE) ? 0 : 1 }); }
                catch (Exception e) { Record("move-throw", $"step {step}: move {pieceId}->{tileId} {e.GetType().Name}: {e.Message}"); return; }

                if (result == null || !result.valid) { Record("valid-move-rejected", $"step {step}: {pieceId}->{tileId} was offered by GetValidTilesForMovement but is not valid"); return; }

                lastPiece = pieceId;
                CheckConsistency(match._GetPendingBoard(), d => Record("consistency", $"step {step} (pending): {d}"));

                if (result.turnChanged) {
                    try { match.CommitTurn(); }
                    catch (Exception e) { Record("commit-throw", $"step {step}: {e.GetType().Name}: {e.Message}"); return; }
                    CheckConsistency(match._GetCommittedBoard(), d => Record("consistency", $"step {step} (committed): {d}"));
                }
            }
        }

        [TestCase(DeathjumpSetting.OFF)]
        [TestCase(DeathjumpSetting.SIDES)]
        [TestCase(DeathjumpSetting.BACK)]
        [TestCase(DeathjumpSetting.ALL)]
        public void RandomLegalGames_UpholdInvariants (DeathjumpSetting setting) {
            var failures = new List<string>();
            for (int seed = 0; seed < GamesPerSetting; seed++) {
                PlayGame(seed, setting, failures);
            }

            if (failures.Count > 0) {
                var byCat = failures.GroupBy(f => f.Substring(0, f.IndexOf(']') + 1))
                    .OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Count()}x {g.Key}");
                Assert.Fail($"{failures.Count} invariant violation(s) [{setting}]: {string.Join(", ", byCat)}\n" +
                    string.Join("\n", failures.Take(5)));
            }
        }
    }
}
