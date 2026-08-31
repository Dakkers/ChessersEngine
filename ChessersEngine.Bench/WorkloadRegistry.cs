using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ChessersEngine.Bench {
    /// <summary>One recorded move attempt of the replay fixture.</summary>
    public sealed class ReplayMove {
        public int pieceId { get; set; }
        public int tileId { get; set; }
        public int playerId { get; set; }
        public int promotionRank { get; set; } = -1;

        public MoveAttempt ToAttempt () => new MoveAttempt {
            pieceId = pieceId,
            tileId = tileId,
            playerId = playerId,
            promotionRank = promotionRank,
        };
    }

    /// <summary>The committed move sequence `replay.game` replays, turn by turn.</summary>
    public sealed class ReplaySequence {
        public int version { get; set; } = 1;
        public string description { get; set; }
        public List<List<ReplayMove>> turns { get; set; } = new List<List<ReplayMove>>();
    }

    /// <summary>
    /// Turns a <see cref="WorkloadSpec"/> into the callable it measures. <see cref="Build"/>
    /// performs the workload's setup (untimed) and returns only the body the runner times.
    /// </summary>
    public static class WorkloadRegistry {
        public const string ReplaySequenceFileName = "replay-sequence.v1.json";

        const int ReplaySeed = 1;
        const int ReplayTurns = 40;
        const int SearchSeed = 1;
        const int SelfPlayTurns = 40;
        const int SelfPlaySeed = 1;

        static readonly string[] Kinds = {
            "counter.overhead",
            "movegen.potential",
            "movegen.valid",
            "board.clone",
            "board.copystate",
            "board.apply-undo",
            "eval.boardvalue",
            "notation.create",
            "search",
            "replay.game",
            "selfplay.level0",
        };

        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = true };

        public static IEnumerable<string> KnownIds () => Kinds;

        /// <summary>Whether a manifest `fixture` value names a usable TestScenarios method.</summary>
        public static bool FixtureResolves (string fixture) => FindFixture(fixture) != null;

        public static Action Build (WorkloadSpec spec) {
            switch (Kind(spec.id)) {
                case "counter.overhead": return BuildCounterOverhead();
                case "movegen.potential": return BuildMovegenPotential(spec);
                case "movegen.valid": return BuildMovegenValid(spec);
                case "board.clone": return BuildBoardClone(spec);
                case "board.copystate": return BuildBoardCopyState(spec);
                case "board.apply-undo": return BuildApplyUndo(spec);
                case "eval.boardvalue": return BuildEval(spec);
                case "notation.create": return BuildNotation(spec);
                case "search": return BuildSearch(spec);
                case "replay.game": return BuildReplay();
                case "selfplay.level0": return BuildSelfPlay(spec);
                default:
                    throw new ArgumentException($"no workload implementation for id '{spec.id}'");
            }
        }

        static string Kind (string id) =>
            WorkloadManifest.KindOf(id, Kinds)
            ?? throw new ArgumentException($"workload id '{id}' does not start with a known kind");

        static MethodInfo FindFixture (string fixture) {
            MethodInfo m = typeof(TestScenarios).GetMethod(
                fixture, BindingFlags.Public | BindingFlags.Static
            );
            return (m != null && m.ReturnType == typeof(MatchData) && m.GetParameters().Length == 0)
                ? m
                : null;
        }

        /// <summary>
        /// Resolves the spec's fixture once and returns a factory that produces a fresh MatchData
        /// on every call; null fixture means the standard opening (factory returns null).
        /// </summary>
        static Func<MatchData> FixtureFactory (WorkloadSpec spec) {
            if (spec.fixture == null) {
                return () => null;
            }
            MethodInfo m = FindFixture(spec.fixture)
                ?? throw new ArgumentException($"unknown TestScenarios fixture '{spec.fixture}'");
            return () => (MatchData) m.Invoke(null, null);
        }

        static Board FixtureBoard (WorkloadSpec spec) => new Match(FixtureFactory(spec)())._GetCommittedBoard();

        static Action BuildCounterOverhead () => () => {
            for (int i = 0; i < 1000; i++) {
                EngineCounters.Node();
            }
        };

        static Action BuildMovegenPotential (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => {
                List<Chessman> chessmen = board.GetActiveChessmen();
                for (int i = 0; i < chessmen.Count; i++) {
                    board.GetPotentialTilesForMovement(chessmen[i]);
                }
            };
        }

        static Action BuildMovegenValid (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => {
                List<Chessman> chessmen = board.GetActiveChessmen();
                for (int i = 0; i < chessmen.Count; i++) {
                    board.GetValidTilesForMovement(chessmen[i]);
                }
            };
        }

        static Action BuildBoardClone (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => board.CreateCopy();
        }

        static Action BuildBoardCopyState (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            Board target = board.CreateCopy();
            return () => target.CopyState(board);
        }

        static Action BuildApplyUndo (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            (int pieceId, int tileId) = FirstLegalMove(board);
            return () => {
                Move move = new Move(board, pieceId, tileId);
                MoveResult result = move.GetPseudoLegalMoveResult();
                board.UndoMove(result);
            };
        }

        static Action BuildEval (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            return () => board.CalculateBoardValue(0);
        }

        static Action BuildNotation (WorkloadSpec spec) {
            Board board = FixtureBoard(spec);
            (int pieceId, int tileId) = FirstLegalMove(board);
            MoveResult result = new Move(board, pieceId, tileId).GetPseudoLegalMoveResult();
            board.UndoMove(result);
            return () => result.CreateNotation();
        }

        static Action BuildSearch (WorkloadSpec spec) {
            int level = spec.level;
            Func<MatchData> fixture = FixtureFactory(spec);
            // A fresh Match per iteration: CalculateBestMove shuffles with the match RNG, so a
            // reused Match would search a differently-ordered move list each iteration.
            return () => new Match(fixture(), null, SearchSeed).CalculateBestMove(level);
        }

        static Action BuildReplay () {
            List<List<MoveAttempt>> turns = LoadReplaySequence();
            return () => {
                Match match = new Match(null, null, ReplaySeed);
                foreach (List<MoveAttempt> turn in turns) {
                    foreach (MoveAttempt attempt in turn) {
                        if (match.MoveChessman(attempt) == null) {
                            return;
                        }
                    }
                    match.CommitTurn();
                }
            };
        }

        static Action BuildSelfPlay (WorkloadSpec spec) {
            int level = spec.level;
            return () => RecordSelfPlay(SelfPlaySeed, level, SelfPlayTurns);
        }

        /// <summary>
        /// Reads the checked-in move sequence beside the binary. It is a committed fixture rather
        /// than a self-play recording so both language implementations replay the same game.
        /// </summary>
        public static List<List<MoveAttempt>> LoadReplaySequence () {
            string path = Path.Combine(AppContext.BaseDirectory, ReplaySequenceFileName);
            ReplaySequence sequence = JsonSerializer.Deserialize<ReplaySequence>(File.ReadAllText(path));
            if (sequence == null || sequence.turns == null || sequence.turns.Count == 0) {
                throw new InvalidDataException($"{path} holds no replay turns");
            }

            List<List<MoveAttempt>> turns = new List<List<MoveAttempt>>();
            foreach (List<ReplayMove> turn in sequence.turns) {
                List<MoveAttempt> attempts = new List<MoveAttempt>();
                foreach (ReplayMove move in turn) {
                    attempts.Add(move.ToAttempt());
                }
                turns.Add(attempts);
            }
            return turns;
        }

        /// <summary>
        /// Regenerates the replay fixture from a seeded self-play game and writes it to
        /// <paramref name="path"/>, refusing to write a sequence that does not replay cleanly.
        /// </summary>
        public static void WriteReplaySequence (string path) {
            List<List<MoveAttempt>> turns = RecordSelfPlay(ReplaySeed, 0, ReplayTurns);

            ReplaySequence sequence = new ReplaySequence {
                description =
                    $"Move sequence for the replay.game workload: the first {turns.Count} turns of a " +
                    $"level-0 self-play game seeded with {ReplaySeed}, from the standard opening.",
            };
            foreach (List<MoveAttempt> turn in turns) {
                List<ReplayMove> moves = new List<ReplayMove>();
                foreach (MoveAttempt attempt in turn) {
                    moves.Add(new ReplayMove {
                        pieceId = attempt.pieceId,
                        tileId = attempt.tileId,
                        playerId = attempt.playerId,
                        promotionRank = attempt.promotionRank,
                    });
                }
                sequence.turns.Add(moves);
            }

            VerifyReplays(turns);
            File.WriteAllText(path, JsonSerializer.Serialize(sequence, JsonOpts) + Environment.NewLine);
        }

        /// <summary>Throws unless every recorded attempt is still accepted on a fresh match.</summary>
        static void VerifyReplays (List<List<MoveAttempt>> turns) {
            Match match = new Match(null, null, ReplaySeed);
            for (int t = 0; t < turns.Count; t++) {
                foreach (MoveAttempt attempt in turns[t]) {
                    MoveResult result = match.MoveChessman(attempt);
                    if (result == null || !result.valid) {
                        throw new InvalidOperationException(
                            $"replay sequence does not round-trip: turn {t} rejected " +
                            $"piece {attempt.pieceId} -> tile {attempt.tileId}"
                        );
                    }
                }
                match.CommitTurn();
            }
        }

        /// <summary>
        /// Plays a seeded AI-vs-AI game and returns the accepted move attempts, turn by turn.
        /// Mirrors the AI turn loop in ChessersEngine.Cli so the replay exercises the same path.
        /// </summary>
        static List<List<MoveAttempt>> RecordSelfPlay (int seed, int level, int maxTurns) {
            Match match = new Match(null, null, seed);
            List<List<MoveAttempt>> turns = new List<List<MoveAttempt>>();

            while (!match.IsGameOver() && turns.Count < maxTurns) {
                ColorEnum moverColor = match.GetCommittedTurnColor();
                int moverPlayerId = moverColor == ColorEnum.WHITE ? match.whitePlayerId : match.blackPlayerId;

                List<MoveAttempt> attempts = match.CalculateBestMove(level);
                if (attempts == null || attempts.Count == 0 || attempts[0] == null) {
                    break;
                }

                List<MoveAttempt> accepted = new List<MoveAttempt>();
                foreach (MoveAttempt attempt in attempts) {
                    attempt.playerId = moverPlayerId;
                    MoveResult result = match.MoveChessman(attempt);
                    if (result == null || !result.valid) {
                        break;
                    }
                    accepted.Add(attempt);
                }

                if (accepted.Count == 0) {
                    break;
                }

                match.CommitTurn();
                turns.Add(accepted);
            }

            return turns;
        }

        /// <summary>Any legal (pieceId, tileId) on the board, for the single-move micro workloads.</summary>
        static (int, int) FirstLegalMove (Board board) {
            foreach (Chessman chessman in board.GetActiveChessmenOfColor(ColorEnum.WHITE)) {
                List<Tile> tiles = board.GetValidTilesForMovement(chessman);
                if (tiles.Count > 0) {
                    return (chessman.id, tiles[0].id);
                }
            }
            throw new InvalidOperationException("fixture has no legal white move");
        }
    }
}
