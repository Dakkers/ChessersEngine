using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ChessersEngine;

namespace ChessersOracle {
    /// <summary>
    /// Phase-0 golden-master oracle. Turns the (untested, unmaintained) C# engine into a
    /// reproducible behavioral reference by driving every TestScenarios fixture through it and
    /// serializing canonical, id-sorted JSON.
    ///
    /// Corpora emitted (under &lt;outDir&gt;):
    ///   codec/tile-codec.json          - id -> (row,col) -> id round-trip for every tile id -36..63
    ///   rules/&lt;Fixture&gt;.&lt;Setting&gt;.json  - move-gen + single-ply execution + terminal state + eval
    ///   ai/&lt;Fixture&gt;.L&lt;level&gt;.json      - minimax chosen moves (ONLY if the Determinism patch is applied)
    ///
    /// The rules + codec corpora need ZERO engine changes: the only nondeterminism in those paths
    /// is Dictionary iteration order, which we neutralize by sorting every emitted collection by id.
    /// Only the AI uses System.Random, so the AI corpus is gated behind an optional engine patch
    /// (see README "Enabling the AI corpus").
    /// </summary>
    public static class Program {
        static readonly JsonSerializerOptions J = new JsonSerializerOptions { WriteIndented = true };

        // Every fixture is captured under all four deathjump settings, because the fixtures do not
        // encode the setting (so they would otherwise silently run OFF) and deathjump legality is
        // config-dependent and heavily undertested.
        static readonly DeathjumpSetting[] Settings = {
            DeathjumpSetting.OFF, DeathjumpSetting.SIDES, DeathjumpSetting.BACK, DeathjumpSetting.ALL,
        };

        public static int Main(string[] args) {
            string outDir = args.Length > 0
                ? args[0]
                : Path.Combine(Directory.GetCurrentDirectory(), "golden");

            Directory.CreateDirectory(Path.Combine(outDir, "rules"));
            Directory.CreateDirectory(Path.Combine(outDir, "codec"));
            Directory.CreateDirectory(Path.Combine(outDir, "ai"));

            List<MethodInfo> fixtures = Fixtures().ToList();
            Console.WriteLine($"Discovered {fixtures.Count} fixtures in TestScenarios.");

            File.WriteAllText(
                Path.Combine(outDir, "manifest.json"),
                JsonSerializer.Serialize(new {
                    fixtureCount = fixtures.Count,
                    fixtures = fixtures.Select(f => f.Name).OrderBy(n => n).ToArray(),
                    deathjumpSettings = Settings.Select(s => s.ToString()).ToArray(),
                }, J));

            // 1) Coordinate-codec golden table (untested Helpers magic, pinned exhaustively).
            DumpCodecTable(Path.Combine(outDir, "codec", "tile-codec.json"));

            // 2) Rules corpus: every fixture x every deathjump setting.
            int written = 0, errors = 0;
            foreach (MethodInfo f in fixtures) {
                foreach (DeathjumpSetting dj in Settings) {
                    try {
                        object record = CaptureFixture(f, dj);
                        File.WriteAllText(
                            Path.Combine(outDir, "rules", $"{f.Name}.{dj}.json"),
                            JsonSerializer.Serialize(record, J));
                        written++;
                    } catch (Exception e) {
                        errors++;
                        File.WriteAllText(
                            Path.Combine(outDir, "rules", $"{f.Name}.{dj}.ERROR.txt"),
                            (e.InnerException ?? e).ToString());
                    }
                }
            }
            Console.WriteLine($"Rules corpus: {written} files written, {errors} errored.");

            // 3) Behavioral expectations mined from the fixture prose comments.
            Expectations.RunAll();

            // 4) AI corpus (only if the Determinism patch is present in the engine).
            TryDumpAiCorpus(fixtures, Path.Combine(outDir, "ai"));

            Console.WriteLine($"Done. Golden corpus at: {Path.GetFullPath(outDir)}");
            return 0;
        }

        // -- fixture discovery: every `public static MatchData Xxx()` in TestScenarios ------------
        static IEnumerable<MethodInfo> Fixtures() =>
            typeof(TestScenarios)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(MatchData) && m.GetParameters().Length == 0)
                .OrderBy(m => m.Name);

        /// <summary>Build a fresh Match from a fixture under a specific deathjump setting.</summary>
        public static Match Build(MethodInfo f, DeathjumpSetting dj) {
            MatchData md = (MatchData) f.Invoke(null, null);          // fresh MatchData each call
            return new Match(md, new MatchConfig { deathjumpSetting = dj });
        }

        // -- one fixture -> one canonical record -------------------------------------------------
        static object CaptureFixture(MethodInfo f, DeathjumpSetting dj) {
            Match m = Build(f, dj);
            Board board = m._GetCommittedBoard();

            // Pseudo-legal move generation for EVERY active piece (both colors), sorted by id.
            var moveGen = m.GetAllPendingChessmen().Values
                .Where(c => c.isActive)
                .OrderBy(c => c.id)
                .Select(c => new {
                    pieceId = c.id,
                    kind = c.kind.ToString(),
                    color = c.color.ToString(),
                    fromTileId = c.location,
                    tiles = m.GetPotentialTilesForMovement(c).Select(t => t.id).OrderBy(x => x).ToArray(),
                })
                .ToArray();

            ColorEnum turn = m.GetTurnColor();
            int pid = turn == ColorEnum.WHITE ? m.whitePlayerId : m.blackPlayerId;

            // Execution corpus: apply every legal single move for the side to move on a FRESH match
            // (fresh per attempt => zero state bleed), recording the full MoveResult + resulting board.
            var attempts = new List<object>();
            foreach (Chessman c in m.GetAllPendingChessmen().Values
                                    .Where(c => c.isActive && c.color == turn)
                                    .OrderBy(c => c.id)) {
                foreach (int toTileId in m.GetPotentialTilesForMovement(c)
                                          .Select(t => t.id).OrderBy(x => x).ToArray()) {
                    Match fresh = Build(f, dj);
                    Chessman piece = fresh.GetPendingChessman(c.id);
                    Tile toTile = fresh.GetPendingTile(toTileId);

                    var attempt = new MoveAttempt {
                        pieceId = c.id,
                        pieceGuid = piece.guid,
                        playerId = pid,
                        tileId = toTileId,
                        promotionRank = Helpers.CanBePromoted(piece, toTile) ? (int) ChessmanKindEnum.QUEEN : -1,
                    };

                    MoveResult r = fresh.MoveChessman(attempt);      // NOTE: null on base-level rejection
                    attempts.Add(new {
                        pieceId = c.id,
                        toTileId,
                        rejected = r == null,
                        result = r == null ? null : MapResult(r),
                        boardAfter = (r != null && r.valid) ? Snapshot(fresh.GetAllPendingChessmen()) : null,
                    });
                }
            }

            return new {
                name = f.Name,
                deathjump = dj.ToString(),
                turn = turn.ToString(),
                pieces = Snapshot(m.GetAllCommittedChessmen()),
                terminal = new {
                    gameOver = m.IsGameOver(),
                    isDraw = m.IsDraw(),
                    hasWinner = m.HasWinner(),
                    winner = m.GetWinner(),
                },
                boardValue = board.CalculateBoardValue(0),   // deterministic int (a sum); pins eval
                moveGen,
                moves = attempts,
            };
        }

        // -- canonical serializers ---------------------------------------------------------------
        public static object[] Snapshot(Dictionary<int, Chessman> men) =>
            men.Values.OrderBy(c => c.id).Select(c => new {
                id = c.id,
                kind = c.kind.ToString(),
                color = c.color.ToString(),
                isActive = c.isActive,
                isChecker = c.isChecker,
                isKinged = c.isKinged,
                isPromoted = c.isPromoted,
                hasMoved = c.hasMoved,
                location = c.location,           // matchId / guid deliberately omitted (volatile, not game state)
            }).ToArray();

        static object MapResult(MoveResult r) => new {
            valid = r.valid,
            type = r.type,
            turnChanged = r.turnChanged,
            fromTileId = r.fromTileId,
            tileId = r.tileId,
            fromRow = r.fromRow, fromColumn = r.fromColumn, toRow = r.toRow, toColumn = r.toColumn,
            polarityChanged = r.polarityChanged,
            kinged = r.kinged,
            isCastle = r.isCastle,
            wasFirstMoveForPiece = r.wasFirstMoveForPiece,
            promotionOccurred = r.promotionOccurred,
            promotionRank = r.promotionRank?.ToString(),
            wasPieceJumped = r.wasPieceJumped, jumpedPieceId = r.jumpedPieceId, jumpedTileId = r.jumpedTileId,
            wasPieceCaptured = r.wasPieceCaptured, capturedPieceId = r.capturedPieceId,
            isInCheck = r.isInCheck, isWinningMove = r.isWinningMove, isStalemate = r.isStalemate,
            chessmanKind = r.chessmanKind.ToString(),
            notation = r.CreateNotation(),        // the cross-language golden move string
        };

        // -- coordinate codec: id -> (row,col) -> id, exhaustive round-trip ----------------------
        static void DumpCodecTable(string path) {
            var rows = new List<object>();
            int roundTripFailures = 0;
            for (int id = -36; id <= 63; id++) {
                int row = Helpers.GetRow(id);
                int col = Helpers.GetColumn(id);
                int back = Helpers.GetTileIdFromRowColumn(row, col);
                bool ok = back == id;
                if (!ok) roundTripFailures++;
                rows.Add(new { tileId = id, row, col, roundTrip = back, ok });
            }
            File.WriteAllText(path, JsonSerializer.Serialize(rows, J));
            // Surfaced honestly: the codec's negative-id branches may NOT round-trip everywhere.
            Console.WriteLine($"Codec table: {rows.Count} ids, {roundTripFailures} round-trip mismatches.");
        }

        // -- AI corpus (opt-in; requires the Determinism patch) ----------------------------------
        static void TryDumpAiCorpus(List<MethodInfo> fixtures, string outDir) {
            Type det = Type.GetType("ChessersEngine.Determinism, ChessersEngine");
            if (det == null) {
                Console.WriteLine("[ai] skipped - apply the Determinism patch to emit reproducible AI goldens (see README).");
                return;
            }
            det.GetField("Enabled").SetValue(null, true);

            int n = 0;
            foreach (MethodInfo f in fixtures) {
                foreach (int level in new[] { 0, 1, 2 }) {
                    try {
                        Match m = Build(f, DeathjumpSetting.OFF);
                        List<MoveResult> chosen = m.DoBestMovesForCurrentPlayer(level);
                        var rec = new {
                            name = f.Name,
                            level,
                            chosen = chosen?.Select(r => new { r.pieceId, r.tileId, notation = r.CreateNotation() }).ToArray(),
                            finalBoard = Snapshot(m.GetAllPendingChessmen()),
                        };
                        File.WriteAllText(Path.Combine(outDir, $"{f.Name}.L{level}.json"), JsonSerializer.Serialize(rec, J));
                        n++;
                    } catch (Exception e) {
                        File.WriteAllText(Path.Combine(outDir, $"{f.Name}.L{level}.ERROR.txt"), (e.InnerException ?? e).ToString());
                    }
                }
            }
            Console.WriteLine($"[ai] {n} AI goldens written (Determinism patch detected).");
        }
    }
}
