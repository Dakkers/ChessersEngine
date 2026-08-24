using System;
using System.Collections.Generic;
using System.Reflection;
using ChessersEngine;

namespace ChessersOracle {
    /// <summary>
    /// Behavioral expectations MINED FROM THE PROSE COMMENTS in TestScenarios.cs.
    ///
    /// TestScenarios has no assertions - the intended outcomes live only in XML-doc comments
    /// (e.g. "If Rook @56 captures the pawn @32, White will be in checkmate"). This turns that
    /// prose into machine-checkable expectations: the oracle plays the described move and prints
    /// PASS/FAIL. These are the safety net the Rust port must also satisfy.
    ///
    /// Only single-move, setting-independent claims are auto-asserted here (encoding a wrong
    /// expectation would be worse than none). Multi-ply and AI-value claims are listed at the
    /// bottom as TODOs to encode once you confirm the exact coordinates against the engine.
    /// </summary>
    public static class Expectations {
        class Case {
            public string Fixture;
            public int PieceId;
            public int ToTile;
            public DeathjumpSetting Dj = DeathjumpSetting.OFF;
            public bool? Valid;
            public bool? WasPieceCaptured;
            public bool? WasPieceJumped;
            public bool? IsInCheck;
            public bool? IsWinningMove;
            public bool? IsStalemate;
            public string Prose;
        }

        static readonly Case[] Cases = {
            // "If Rook @56 (a8) captures the pawn @32 (a5), White will be in checkmate." (Black to move)
            new Case {
                Fixture = "Checkmate1", PieceId = Constants.ID_BLACK_ROOK_2, ToTile = 32,
                Valid = true, WasPieceCaptured = true, IsWinningMove = true,
                Prose = "Black rook 56->32 captures the pawn => White checkmate.",
            },
            // "If Black rook moves from 33 to 57, then White is in stalemate." (Black to move)
            new Case {
                Fixture = "Stalemate1", PieceId = Constants.ID_BLACK_ROOK_1, ToTile = 57,
                Valid = true, IsStalemate = true,
                Prose = "Black rook 33->57 => White stalemate.",
            },
        };

        public static void RunAll() {
            Console.WriteLine("-- Expectations mined from TestScenarios prose --");
            int pass = 0, fail = 0;

            foreach (Case c in Cases) {
                MethodInfo f = typeof(TestScenarios).GetMethod(c.Fixture, BindingFlags.Public | BindingFlags.Static);
                if (f == null) { fail++; Console.WriteLine($"  FAIL  {c.Fixture}: fixture method not found"); continue; }

                Match m = Program.Build(f, c.Dj);
                ColorEnum turn = m.GetTurnColor();
                int pid = turn == ColorEnum.WHITE ? m.whitePlayerId : m.blackPlayerId;

                Chessman piece = m.GetPendingChessman(c.PieceId);
                Tile toTile = m.GetPendingTile(c.ToTile);
                MoveResult r = m.MoveChessman(new MoveAttempt {
                    pieceId = c.PieceId,
                    pieceGuid = piece.guid,
                    playerId = pid,
                    tileId = c.ToTile,
                    promotionRank = Helpers.CanBePromoted(piece, toTile) ? (int) ChessmanKindEnum.QUEEN : -1,
                });

                List<string> problems = Check(c, r);
                if (problems.Count == 0) {
                    pass++;
                    Console.WriteLine($"  PASS  {c.Fixture}: {c.Prose}");
                } else {
                    fail++;
                    Console.WriteLine($"  FAIL  {c.Fixture}: {string.Join("; ", problems)}   <- {c.Prose}");
                }
            }

            Console.WriteLine($"-- Expectations: {pass} passed, {fail} failed --");
        }

        static List<string> Check(Case c, MoveResult r) {
            var problems = new List<string>();
            if (r == null) { problems.Add("move was rejected (null MoveResult)"); return problems; }

            void Eq(bool? expected, bool actual, string name) {
                if (expected.HasValue && expected.Value != actual)
                    problems.Add($"{name}: expected {expected.Value}, got {actual}");
            }

            Eq(c.Valid, r.valid, "valid");
            Eq(c.WasPieceCaptured, r.WasPieceCaptured(), "captured");
            Eq(c.WasPieceJumped, r.WasPieceJumped(), "jumped");
            Eq(c.IsInCheck, r.isInCheck, "isInCheck");
            Eq(c.IsWinningMove, r.isWinningMove, "isWinningMove");
            Eq(c.IsStalemate, r.isStalemate, "isStalemate");
            return problems;
        }

        // ---------------------------------------------------------------------------------------
        // Further prose expectations found in TestScenarios (encode once coordinates are confirmed):
        //
        //   Multijump1           AI should PREFER jumping 2 knights over capturing 1 rook   (AI-value, needs AI corpus)
        //   MoveJumpInvalid1     Queen -> 6 must NOT enable a follow-up move-jump to 30      (2-ply negative)
        //   MoveJumpInvalid2     checker @37 must NOT move 37->44 then jump 53->62           (2-ply negative)
        //   AlmostCheckmate1/2/3 a defensive reply exists => NOT a winning move              (multi-ply)
        //   InCheckFromCaptureDeathjump1/2, InCheckFromMoveDeathjump1
        //                        White queen -> 5 puts Black in check                        (deathjump-setting dependent)
        //   Promotion            white checker-pawn @48 promotes on reaching the back rank
        //   Jump1 / Jump2        kinged/plain white checker @61/@45 jumps the black queen @52
        //   Castling / CastlingWhite / CastlingBlack / CastlingNoPawns
        //                        king castles both sides; result.isCastle == true, rook relocates
        // ---------------------------------------------------------------------------------------
    }
}
