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
    ///   - if the player to move has no legal move, the game must be over;
    ///   - notation round-trip: CreateNotation parsed back recovers piece id, from/to tiles, promotion;
    ///   - clone fidelity: Board.CreateCopy serializes identically to the original;
    ///   - AI liveness: DoBestMovesForCurrentPlayer returns valid moves whenever a legal
    ///     move exists and the game is not over;
    ///   - determinism: the same seed replays the same game, for both random and AI play.
    ///
    /// Note: death-tile (negative id) occupancy is intentionally excluded from the state snapshot.
    /// A death tile keeps referencing the (deactivated, off-board) piece that died on it, and
    /// UndoMove does not restore that purely cosmetic reference; it is never read for game logic.
    /// </summary>
    [TestFixture]
    public class FuzzTests {
        const int GamesPerSetting = 75;
        const int MoveCap = 220;
        const int AiGamesPerSetting = 10;
        const int AiTurnCap = 120;
        const int DeterminismSeeds = 20;

        static void AssertNoFailures (List<string> failures, string label) {
            if (failures.Count == 0) return;
            var byCat = failures.GroupBy(f => f.Substring(0, f.IndexOf(']') + 1))
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Count()}x {g.Key}");
            Assert.Fail($"{failures.Count} invariant violation(s) [{label}]: {string.Join(", ", byCat)}\n" +
                string.Join("\n", failures.Take(5)));
        }

        // Canonical snapshot of the game-relevant board state (piece flags + real-tile occupancy).
        static string Snapshot (Board b) {
            string pieces = string.Join(";", b.GetChessmanSchemas()
                .OrderBy(cs => cs.id)
                .Select(cs => $"{cs.id},{cs.kind},{cs.location},{(cs.isActive ? 1 : 0)},{(cs.isChecker ? 1 : 0)},{(cs.isKinged ? 1 : 0)},{(cs.isPromoted ? 1 : 0)},{(cs.hasMoved ? 1 : 0)},{cs.colorId}"));
            var occ = new List<string>();
            for (int id = 0; id < 64; id++) occ.Add(b.GetTile(id).GetPiece()?.id.ToString() ?? "_");
            return pieces + "|" + string.Join(",", occ);
        }

        static string FullSnapshot (Board b) {
            var occ = new List<string>();
            for (int id = -36; id < 0; id++) occ.Add(b.GetTile(id).GetPiece()?.id.ToString() ?? "_");
            return Snapshot(b) + "|" + string.Join(",", occ);
        }

        static void CheckClone (Board b, Action<string> fail) {
            Board copy;
            try { copy = b.CreateCopy(); }
            catch (Exception e) { fail($"CreateCopy threw {e.GetType().Name}: {e.Message}"); return; }
            if (FullSnapshot(copy) != FullSnapshot(b)) fail("copy differs from original");
        }

        // Castle notation carries no rows, so only isCastle and toColumn are recoverable.
        static void CheckNotation (MoveResult mr, Action<string> fail) {
            string notation;
            try { notation = mr.CreateNotation(); }
            catch (Exception e) { fail($"CreateNotation threw {e.GetType().Name}: {e.Message}"); return; }

            MoveResult rt;
            try { rt = MoveResult.CreatePartialMoveResultFromNotation(notation); }
            catch (Exception e) { fail($"parse('{notation}') threw {e.GetType().Name}: {e.Message}"); return; }

            if (rt.pieceId != mr.pieceId) fail($"'{notation}' pieceId {rt.pieceId} != {mr.pieceId}");

            if (mr.isCastle) {
                if (!rt.isCastle) fail($"'{notation}' lost isCastle");
                if (rt.toColumn != mr.toColumn) fail($"'{notation}' castle toColumn {rt.toColumn} != {mr.toColumn}");
                return;
            }

            int fromRt = Helpers.GetTileIdFromRowColumn(rt.fromRow, rt.fromColumn);
            if (fromRt != mr.fromTileId) fail($"'{notation}' fromTile {fromRt} != {mr.fromTileId}");

            int toRt = (mr.tileId < 0) ? Helpers.GetTileIdFromRowColumn(rt.toRow, rt.toColumn) : (8 * rt.toRow + rt.toColumn);
            if (toRt != mr.tileId) fail($"'{notation}' toTile {toRt} != {mr.tileId}");

            if (mr.promotionOccurred && rt.promotionRank != mr.promotionRank)
                fail($"'{notation}' promo {rt.promotionRank} != {mr.promotionRank}");
        }

        static MatchData MakeMatchData (DeathjumpSetting setting) => new MatchData {
            currentTurn = ColorEnum.WHITE, matchId = 1,
            whitePlayerId = 0, blackPlayerId = 1, deathjumpSetting = (int) setting,
        };

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

        /// <summary>
        /// Plays one seeded random-legal game, recording every invariant violation.
        /// Returns the committed move list, or null if the game was aborted by a violation.
        /// </summary>
        static List<string> PlayGame (int seed, DeathjumpSetting setting, List<string> failures) {
            void Record (string cat, string detail) => failures.Add($"[{cat}] seed={seed} dj={setting}: {detail}");

            var rng = new System.Random(seed);
            Match match;
            try { match = new Match(MakeMatchData(setting), null, seed); }
            catch (Exception e) { Record("ctor-throw", e.GetType().Name + ": " + e.Message); return null; }

            int lastPiece = -1;
            for (int step = 0; step < MoveCap; step++) {
                if (match.IsGameOver()) return match.GetMoves();
                ColorEnum color = match.GetTurnColor();
                Board board = match._GetPendingBoard();
                bool midTurn = match.GetPendingMoveResults().Count > 0;

                List<(int, int)> legal;
                try { legal = CollectLegal(board, color, midTurn, midTurn ? lastPiece : -1); }
                catch (Exception e) { Record("legal-gen-throw", $"step {step}: {e.GetType().Name}: {e.Message}"); return null; }

                if (legal.Count == 0) {
                    if (midTurn) Record("stuck-midturn", $"step {step}: no continuation but turn did not change (piece {lastPiece})");
                    else if (!match.IsGameOver()) Record("no-moves-not-over", $"step {step}: {color} has no legal move but game is not over");
                    return match.GetMoves();
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
                } catch (Exception e) { Record("undo-throw", $"step {step}: move {pieceId}->{tileId} {e.GetType().Name}: {e.Message}"); return null; }

                // Play for real.
                MoveResult result;
                try { result = match.MoveChessman(new MoveAttempt { pieceId = pieceId, tileId = tileId, playerId = (color == ColorEnum.WHITE) ? 0 : 1 }); }
                catch (Exception e) { Record("move-throw", $"step {step}: move {pieceId}->{tileId} {e.GetType().Name}: {e.Message}"); return null; }

                if (result == null || !result.valid) { Record("valid-move-rejected", $"step {step}: {pieceId}->{tileId} was offered by GetValidTilesForMovement but is not valid"); return null; }

                lastPiece = pieceId;
                CheckNotation(result, d => Record("notation-mismatch", $"step {step}: {d}"));
                CheckClone(match._GetPendingBoard(), d => Record("clone-mismatch", $"step {step}: {d}"));
                CheckConsistency(match._GetPendingBoard(), d => Record("consistency", $"step {step} (pending): {d}"));

                if (result.turnChanged) {
                    try { match.CommitTurn(); }
                    catch (Exception e) { Record("commit-throw", $"step {step}: {e.GetType().Name}: {e.Message}"); return null; }
                    CheckConsistency(match._GetCommittedBoard(), d => Record("consistency", $"step {step} (committed): {d}"));
                }
            }
            return match.GetMoves();
        }

        /// <summary>
        /// Plays one seeded AI-vs-AI game at the given search level, recording every invariant
        /// violation. Returns the committed move list, or null if the game was aborted by a violation.
        /// </summary>
        static List<string> PlayAiGame (int seed, DeathjumpSetting setting, int level, List<string> failures) {
            void Record (string cat, string detail) => failures.Add($"[{cat}] seed={seed} dj={setting} ai{level}: {detail}");

            Match match;
            try { match = new Match(MakeMatchData(setting), null, seed); }
            catch (Exception e) { Record("ctor-throw", e.GetType().Name + ": " + e.Message); return null; }

            for (int turn = 0; turn < AiTurnCap; turn++) {
                if (match.IsGameOver()) return match.GetMoves();
                ColorEnum color = match.GetTurnColor();

                bool hasLegal;
                try { hasLegal = CollectLegal(match._GetCommittedBoard(), color, false, -1).Count > 0; }
                catch (Exception e) { Record("legal-gen-throw", $"turn {turn}: {e.GetType().Name}: {e.Message}"); return null; }

                List<MoveResult> results;
                try { results = match.DoBestMovesForCurrentPlayer(level); }
                catch (Exception e) { Record("ai-move-throw", $"turn {turn}: {e.GetType().Name}: {e.Message}"); return null; }

                if (results == null || results.Count == 0) {
                    if (hasLegal && !match.IsGameOver()) Record("ai-no-move", $"turn {turn}: {color} returned no moves but has a legal move");
                    return match.GetMoves();
                }

                foreach (var mr in results) {
                    if (mr == null) { Record("ai-null-move", $"turn {turn}: null MoveResult"); return null; }
                    if (!mr.valid) Record("ai-invalid-move", $"turn {turn}: {mr.pieceId}->{mr.tileId} is invalid");
                    CheckNotation(mr, d => Record("notation-mismatch", $"turn {turn}: {d}"));
                }

                Board pending = match._GetPendingBoard();
                CheckClone(pending, d => Record("clone-mismatch", $"turn {turn}: {d}"));
                CheckConsistency(pending, d => Record("consistency", $"turn {turn} (pending): {d}"));

                try { match.CommitTurn(); }
                catch (Exception e) { Record("commit-throw", $"turn {turn}: {e.GetType().Name}: {e.Message}"); return null; }
                CheckConsistency(match._GetCommittedBoard(), d => Record("consistency", $"turn {turn} (committed): {d}"));
            }
            return match.GetMoves();
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
            AssertNoFailures(failures, setting.ToString());
        }

        [TestCase(DeathjumpSetting.OFF, 0)]
        [TestCase(DeathjumpSetting.OFF, 1)]
        [TestCase(DeathjumpSetting.SIDES, 0)]
        [TestCase(DeathjumpSetting.BACK, 0)]
        [TestCase(DeathjumpSetting.ALL, 0)]
        public void AiGames_UpholdInvariants (DeathjumpSetting setting, int level) {
            var failures = new List<string>();
            for (int seed = 0; seed < AiGamesPerSetting; seed++) {
                PlayAiGame(seed, setting, level, failures);
            }
            AssertNoFailures(failures, $"{setting} ai{level}");
        }

        [TestCase(DeathjumpSetting.OFF)]
        [TestCase(DeathjumpSetting.ALL)]
        public void SameSeed_ReplaysSameGame (DeathjumpSetting setting) {
            var mismatches = new List<string>();
            for (int seed = 0; seed < DeterminismSeeds; seed++) {
                var ignored = new List<string>();
                if (!SameMoves(PlayGame(seed, setting, ignored), PlayGame(seed, setting, ignored)))
                    mismatches.Add($"random seed={seed}");
                if (seed < AiGamesPerSetting && !SameMoves(PlayAiGame(seed, setting, 0, ignored), PlayAiGame(seed, setting, 0, ignored)))
                    mismatches.Add($"ai0 seed={seed}");
            }
            Assert.That(mismatches, Is.Empty, $"nondeterministic games [{setting}]");
        }

        static bool SameMoves (List<string> a, List<string> b) =>
            (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b));
    }
}
