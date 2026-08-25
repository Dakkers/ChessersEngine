using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChessersEngine;

namespace ChessersEngine.Cli {
    // ---------------------------------------------------------------------------
    // Golden-corpus schema. Each game is a self-contained record that a differential
    // test (e.g. the Rust port) can replay: build the board from `initialPieces`, then
    // for each ply feed every move `attempt` back through the engine and assert the
    // resulting `MoveResult` and post-commit `boardAfter` match. Engine determinism is
    // NOT required to consume this -- the actual attempts are recorded, not re-derived.
    // ---------------------------------------------------------------------------

    class OracleGame {
        // The format version lives in this `$schema` URI (JSON Schema, draft 2020-12): it pins to
        // one immutable, versioned schema file under ChessersEngine.Cli/schemas/. Bump the path
        // (oracle.v2.schema.json, ...) for a breaking change and keep the old file in place.
        [JsonPropertyName("$schema")]
        public string Schema { get; set; } =
            "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/oracle.v1.schema.json";

        public string engine { get; set; } = "ChessersEngine (C#)";
        public GameConfigDto config { get; set; }
        public List<ChessmanSchema> initialPieces { get; set; }
        public List<OraclePlyDto> plies { get; set; } = new List<OraclePlyDto>();
        public GameOutcomeDto outcome { get; set; }
        // Moves the engine rejected during play (illegal moves). Empty for AI-only games,
        // since the search only ever produces legal moves. Each entry is self-contained: the
        // board it was tried against, the attempt, and the engine's response.
        public List<RejectedAttemptDto> rejectedAttempts { get; set; } = new List<RejectedAttemptDto>();
    }

    class GameConfigDto {
        public string white { get; set; }       // "human" | "ai"
        public string black { get; set; }       // "human" | "ai"
        public int aiLevel { get; set; }
        public int? randomSeed { get; set; }     // null => engine used a time-seeded RNG
        public string deathjumpSetting { get; set; }
        public int whitePlayerId { get; set; }
        public int blackPlayerId { get; set; }
    }

    class OraclePlyDto {
        public int index { get; set; }           // 1-based ply (one full turn, may hold several jumps)
        public string turnColor { get; set; }    // color to move at the start of this turn
        public int playerId { get; set; }
        public string actor { get; set; }        // "human" | "ai"
        public List<OracleMoveDto> moves { get; set; } = new List<OracleMoveDto>();
        public string committedNotation { get; set; } // Match.GetMoves() entry for this turn
        public List<ChessmanSchema> boardAfter { get; set; }
    }

    class OracleMoveDto {
        public MoveAttempt attempt { get; set; } // the input handed to Match.MoveChessman
        public string notation { get; set; }     // MoveResult.CreateNotation()
        public MoveResult result { get; set; }   // the full engine output (every flag)
    }

    class RejectedAttemptDto {
        public string turnColor { get; set; }    // whose turn it was
        public int playerId { get; set; }
        public MoveAttempt attempt { get; set; } // the rejected input
        // The engine's response: null when Match.MoveChessman returned null (e.g. target is the
        // player's own piece / wrong turn); otherwise a MoveResult with valid == false. Only the
        // rejection itself is a contract -- the other fields of an invalid result are incidental.
        public MoveResult result { get; set; }
        public List<ChessmanSchema> board { get; set; } // the position the move was attempted against
    }

    class GameOutcomeDto {
        public bool gameOver { get; set; }
        public int winningPlayerId { get; set; } = -1;
        public string winningColor { get; set; } // null when there is no winner
        public bool isDraw { get; set; }
        public bool isResignation { get; set; }
        public string reason { get; set; }       // "checkmate" | "resignation" | "stalemate" | "move-limit" ...
    }

    class OracleRecorder {
        public OracleGame Game { get; }

        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions {
            WriteIndented = true,
            // ChessmanSchema exposes its state as public fields, not properties.
            IncludeFields = true,
            // Serialize enums (ColorEnum, ChessmanKindEnum) by name so the corpus is
            // self-documenting and language-neutral for the port to consume.
            Converters = { new JsonStringEnumConverter() },
        };

        public OracleRecorder (GameConfigDto config, List<ChessmanSchema> initialPieces) {
            Game = new OracleGame {
                config = config,
                initialPieces = initialPieces,
            };
        }

        public void AddPly (OraclePlyDto ply) => Game.plies.Add(ply);

        public void AddRejected (RejectedAttemptDto rejected) => Game.rejectedAttempts.Add(rejected);

        public void SetOutcome (GameOutcomeDto outcome) => Game.outcome = outcome;

        public string ToJson () => JsonSerializer.Serialize(Game, JsonOpts);

        public void Write (string path) {
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, ToJson());
        }
    }
}
