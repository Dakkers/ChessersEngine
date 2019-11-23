﻿using System;
using System.Collections.Generic;

namespace ChessersEngine {
    public static class TestScenarios {
        static ChessmanSchema CreateKing (bool isWhite = true) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_KING,
                id = isWhite ? Constants.ID_WHITE_KING : Constants.ID_BLACK_KING,
                guid = isWhite ? Constants.ID_WHITE_KING : Constants.ID_BLACK_KING,
            };
        }

        public static ChessmanSchema CreateWhiteKing (int location) {
            ChessmanSchema cs = CreateKing();
            cs.location = location;
            return cs;
        }
        public static ChessmanSchema CreateBlackKing (int location) {
            ChessmanSchema cs = CreateKing(false);
            cs.location = location;
            return cs;
        }
        public static ChessmanSchema CreatePawn (int id, int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_PAWN,
                id = id,
                guid = id,
                location = location,
            };
        }
        public static ChessmanSchema CreateRook (int id, int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_ROOK,
                id = id,
                guid = id,
                location = location,
            };
        }
        public static ChessmanSchema CreateBishop (int id, int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_BISHOP,
                id = id,
                guid = id,
                location = location,
            };
        }

        public static ChessmanSchema CreateBlackQueen (int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_QUEEN,
                id = Constants.ID_BLACK_QUEEN,
                guid = Constants.ID_BLACK_QUEEN,
                location = location,
            };
        }

        public static ChessmanSchema CreateWhiteQueen (int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_QUEEN,
                id = Constants.ID_WHITE_QUEEN,
                guid = Constants.ID_WHITE_QUEEN,
                location = location,
            };
        }

        public static ChessmanSchema CreateBlackChecker (int id, int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_PAWN,
                id = id,
                guid = id,
                location = location,
                isChecker = true,
                isKinged = true
            };
        }

        public static MatchData InCheckFromJump () {
            return new MatchData {
                currentTurn = 0,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(3),
                    CreateBlackChecker(Constants.ID_BLACK_PAWN_1, 18)
                }
            };
        }

        public static MatchData InCheckFromReverseJump () {
            return new MatchData {
                currentTurn = 0,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(3),
                    CreateBlackChecker(Constants.ID_BLACK_PAWN_1, 4)
                }
            };
        }

        public static MatchData InCheckFromCaptureJump () {
            return new MatchData {
                currentTurn = 0,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(3),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 18),
                    CreateBlackQueen(58)
                }
            };
        }

        public static MatchData InCheckFromCaptureReverseJump () {
            return new MatchData {
                currentTurn = 0,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(3),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 4),
                    CreateBlackQueen(60)
                }
            };
        }

        public static MatchData InCheckFromMultiJump () {
            return new MatchData {
                currentTurn = 0,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(2),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 12),
                    CreatePawn(Constants.ID_WHITE_PAWN_2, 14),

                    CreateBlackChecker(Constants.ID_BLACK_PAWN_1, 23)
                }
            };
        }

        public static MatchData Checkmate1 () {
            // If Rook @ 56 captures the pawn @ 32, White will be in checkmate.
            return new MatchData {
                currentTurn = Constants.ID_BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    CreateWhiteQueen(7),
                    TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_1, 15),
                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                    TestScenarios.CreateBlackQueen(57),
                    TestScenarios.CreateBlackKing(58),
                }
            };
        }

        public static MatchData AlmostCheckmate1 () {
            // Black Rook @ 39 captures pawn @ 32.
            // White Rook @ 15 moves to 8 to block.
            return new MatchData {
                currentTurn = Constants.ID_BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    TestScenarios.CreateRook(Constants.ID_WHITE_ROOK_1, 15),
                    TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_1, 23),
                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                    TestScenarios.CreateBlackQueen(57),
                    TestScenarios.CreateBlackKing(58),
                }
            };
        }

        public static MatchData AlmostCheckmate2 () {
            // Black Bishop @ 43 moves to 36.
            // White Bishop @ 22 moves to 36 to capture it.
            return new MatchData {
                currentTurn = Constants.ID_BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    TestScenarios.CreateBishop(Constants.ID_WHITE_BISHOP_1, 22),
                    TestScenarios.CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                    TestScenarios.CreateBishop(Constants.ID_BLACK_BISHOP_1, 43),
                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                    TestScenarios.CreateBlackQueen(57),
                    TestScenarios.CreateBlackKing(58),
                }
            };
        }

        public static MatchData AlmostCheckmate3 () {
            // Pawn @ 52 moves to 36.
            // King can capture the pawn but there's another pawn to capture it.
            return new MatchData {
                currentTurn = Constants.ID_BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(27),

                    TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_1, 52),
                    TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_2, 45),
                    TestScenarios.CreateBishop(Constants.ID_BLACK_BISHOP_1, 38),
                    TestScenarios.CreateBishop(Constants.ID_BLACK_BISHOP_2, 46),
                    TestScenarios.CreateRook(Constants.ID_BLACK_ROOK_2, 59),
                    TestScenarios.CreateBlackQueen(58),
                    TestScenarios.CreateBlackKing(56),
                }
            };
        }

        public static MatchData Promotion () {
            ChessmanSchema pawnCS = CreatePawn(Constants.ID_WHITE_PAWN_1, 48);
            pawnCS.isChecker = true;

            return new MatchData {
                currentTurn = Constants.ID_WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    TestScenarios.CreateWhiteKing(0),
                    pawnCS,

                    TestScenarios.CreateBlackKing(60),
                }
            };
        }
    }
}