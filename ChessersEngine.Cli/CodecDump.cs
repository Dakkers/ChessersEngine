using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChessersEngine;

namespace ChessersEngine.Cli {
    // Coordinate-codec dump: for every tile id (-36..63), its (row,col) via Helpers.GetRow/GetColumn
    // and the inverse Helpers.GetTileIdFromRowColumn, plus whether the round-trip is exact. This is a
    // sibling artifact to the game oracle -- a pure function table, not a game -- pinned so the Rust
    // port can reproduce the negative-id "deathjump" coordinate arithmetic exactly. `ok` is false for
    // ids whose (row,col) does not map back to the id (the codec has lossy branches); the count is
    // surfaced as `roundTripMismatches`.
    class CodecEntryDto {
        public int tileId { get; set; }
        public int row { get; set; }
        public int col { get; set; }
        public int roundTrip { get; set; } // GetTileIdFromRowColumn(row, col)
        public bool ok { get; set; }        // roundTrip == tileId
    }

    class CodecDumpDto {
        // Version lives in the `$schema` URI, mirrored by `schemaVersion`; check_codec.py enforces both.
        [JsonPropertyName("$schema")]
        public string Schema { get; set; } =
            "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas/codec.v1.schema.json";
        public int schemaVersion { get; set; } = 1;
        public string engine { get; set; } = "ChessersEngine (C#)";
        public string description { get; set; } =
            "Tile-id coordinate codec: (row,col) for every tile id -36..63 and the inverse round-trip.";
        public int minTileId { get; set; } = -36;
        public int maxTileId { get; set; } = 63;
        public int roundTripMismatches { get; set; }
        public List<CodecEntryDto> tiles { get; set; } = new List<CodecEntryDto>();
    }

    static class CodecDump {
        static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = true };

        public static CodecDumpDto Build () {
            var doc = new CodecDumpDto();
            for (int id = doc.minTileId; id <= doc.maxTileId; id++) {
                int row = Helpers.GetRow(id);
                int col = Helpers.GetColumn(id);
                int back = Helpers.GetTileIdFromRowColumn(row, col);
                bool ok = back == id;
                if (!ok) {
                    doc.roundTripMismatches++;
                }
                doc.tiles.Add(new CodecEntryDto { tileId = id, row = row, col = col, roundTrip = back, ok = ok });
            }
            return doc;
        }

        /// <summary>Writes the codec table to <paramref name="path"/>; returns the round-trip mismatch count.</summary>
        public static int Write (string path) {
            CodecDumpDto doc = Build();
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, JsonSerializer.Serialize(doc, JsonOpts));
            return doc.roundTripMismatches;
        }
    }
}
