using System;
using System.Collections.Generic;

namespace ChessersEngine {
    public static class TestScenarios {
        static MatchData CreateMatchData () {
            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
            };
        }

        static ChessmanSchema CreateKing (bool isWhite = true) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_KING,
                id = isWhite ? Constants.ID_WHITE_KING : Constants.ID_BLACK_KING,
                guid = isWhite ? Constants.ID_WHITE_KING : Constants.ID_BLACK_KING,
            };
        }

        public static ChessmanSchema CreateWhiteKing (int location = 4) {
            ChessmanSchema cs = CreateKing();
            cs.location = location;
            return cs;
        }
        public static ChessmanSchema CreateBlackKing (int location = 60) {
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
        public static ChessmanSchema CreateKnight (int id, int location) {
            return new ChessmanSchema {
                kind = Constants.CHESSMAN_KIND_KNIGHT,
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

        public static ChessmanSchema CreateWhiteQueen (int location = 3) {
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
                isKinged = false
            };
        }

        #region Check

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
                    CreateBlackQueen(58),
                    CreateBlackKing()
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

                    CreateBlackChecker(Constants.ID_BLACK_PAWN_1, 23),
                    CreateBlackKing()
                }
            };
        }

        #endregion

        #region Checkmate

        public static MatchData Checkmate1 () {
            // If Rook @ 56 captures the pawn @ 32, White will be in checkmate.
            return new MatchData {
                currentTurn = ColorEnum.BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(0),
                    CreateWhiteQueen(7),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                    CreateRook(Constants.ID_BLACK_ROOK_1, 15),
                    CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                    CreateBlackQueen(57),
                    CreateBlackKing(58),
                }
            };
        }

        public static MatchData AlmostCheckmate1 () {
            // Black Rook @ 39 captures pawn @ 32.
            // White Rook @ 15 moves to 8 to block.
            return new MatchData {
                currentTurn = ColorEnum.BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(0),
                    CreateRook(Constants.ID_WHITE_ROOK_1, 15),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                    CreateRook(Constants.ID_BLACK_ROOK_1, 23),
                    CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                    CreateBlackQueen(57),
                    CreateBlackKing(58),
                }
            };
        }

        public static MatchData AlmostCheckmate2 () {
            // Black Bishop @ 43 moves to 36.
            // White Bishop @ 22 moves to 36 to capture it.
            return new MatchData {
                currentTurn = ColorEnum.BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(0),
                    CreateBishop(Constants.ID_WHITE_BISHOP_1, 22),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                    CreateBishop(Constants.ID_BLACK_BISHOP_1, 43),
                    CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                    CreateBlackQueen(57),
                    CreateBlackKing(58),
                }
            };
        }

        public static MatchData AlmostCheckmate3 () {
            // Pawn @ 52 moves to 36.
            // King can capture the pawn but there's another pawn to capture it.
            return new MatchData {
                currentTurn = ColorEnum.BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(27),

                    CreatePawn(Constants.ID_BLACK_PAWN_1, 52),
                    CreatePawn(Constants.ID_BLACK_PAWN_2, 45),
                    CreateBishop(Constants.ID_BLACK_BISHOP_1, 38),
                    CreateBishop(Constants.ID_BLACK_BISHOP_2, 46),
                    CreateRook(Constants.ID_BLACK_ROOK_2, 59),
                    CreateBlackQueen(58),
                    CreateBlackKing(56),
                }
            };
        }

        #endregion

        public static MatchData Promotion () {
            ChessmanSchema pawnCS = CreatePawn(Constants.ID_WHITE_PAWN_1, 48);
            pawnCS.isChecker = true;

            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(0),
                    pawnCS,

                    CreateBlackKing(60),
                }
            };
        }

        public static MatchData Jump1 () {
            var checkerCS = CreateWhiteQueen(61);
            checkerCS.isChecker = true;
            checkerCS.isKinged = true;

            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(0),
                    checkerCS,

                    CreateBlackQueen(52),
                    CreateBlackKing(56),
                }
            };
        }

        public static MatchData Jump2 () {
            var checkerCS = CreateWhiteQueen(45);
            checkerCS.isChecker = true;

            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(0),
                    checkerCS,

                    CreateBlackQueen(52),
                    CreateBlackKing(56),
                }
            };
        }

        /// <summary>
        /// Simple multijump, and also a helper for the AI to determine which move is better between
        /// jumping over 2 knights or capturing 1 rook (the former is better from a Chess value standpoint.)
        /// </summary>
        /// <returns>The multijump1.</returns>
        public static MatchData Multijump1 () {
            var checkerCS = CreateWhiteQueen(63);
            checkerCS.isChecker = true;
            checkerCS.isKinged = true;

            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(2),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 28),
                    checkerCS,

                    CreateKnight(Constants.ID_BLACK_KNIGHT_1, 54),
                    CreateKnight(Constants.ID_BLACK_KNIGHT_2, 52),
                    CreateRook(Constants.ID_BLACK_ROOK_1, 37),
                    CreateBlackKing(56),
                }
            };
        }

        #region Castling

        public static MatchData Castling () {
            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(),
                    CreateRook(Constants.ID_WHITE_ROOK_1, 0),
                    CreateRook(Constants.ID_WHITE_ROOK_2, 7),
                    CreatePawn(Constants.ID_WHITE_PAWN_1, 8),
                    CreatePawn(Constants.ID_WHITE_PAWN_2, 15),
                    //TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_1, 8),
                    //TestScenarios.CreatePawn(Constants.ID_BLACK_PAWN_2, 15),

                    CreateRook(Constants.ID_BLACK_ROOK_1, 56),
                    CreateRook(Constants.ID_BLACK_ROOK_2, 63),
                    CreateBlackKing(),
                }
            };
        }

        public static MatchData CastlingWhite () {
            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(),
                    CreateRook(Constants.ID_WHITE_ROOK_1, 0),
                    CreateRook(Constants.ID_WHITE_ROOK_2, 7),
                    CreateBlackKing(),
                }
            };
        }
        public static MatchData CastlingBlack () {
            return new MatchData {
                currentTurn = ColorEnum.BLACK,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(),
                    CreateRook(Constants.ID_BLACK_ROOK_1, 56),
                    CreateRook(Constants.ID_BLACK_ROOK_2, 63),
                    CreateBlackKing(),
                }
            };
        }

        #endregion

        #region Bugs

        /// <summary>
        /// False positive on being in check via jumping.
        /// </summary>
        /// <returns>The bug20200405.</returns>
        public static MatchData Bug20200405 () {
            var checker = CreateBlackChecker(Constants.ID_BLACK_PAWN_1, 1);
            checker.isKinged = true;

            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(3),
                    CreateWhiteQueen(19),

                    checker,
                    CreateBlackKing(56),
                }
            };
        }

        /// <summary>
        /// When I added logic to the "PostValidation" method in MoveResult I accidentally made it so
        /// that if a capture occurred and the piece became a checker, the piece was allowed to move
        /// again in the same turn even when there WASN'T a jump available. This is to test that.
        /// </summary>
        /// <returns>The minimax calculation20200627.</returns>
        public static MatchData BugMinimaxCalculation20200627 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(2),
                CreateRook(Constants.ID_WHITE_ROOK_1, 4),
                CreateRook(Constants.ID_BLACK_ROOK_1, 60),
                CreateBlackKing(56),
            };
            return md;
        }

        /// <summary>
        /// Bug in determining potential tiles for a checker. The bug was that, when a piece was captured,
        /// it also allowed them to move as a normal checker if they were able to do a capture-jump
        /// (and chose not to).
        /// </summary>
        public static MatchData Bug20200701_01 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(),
                CreatePawn(Constants.ID_WHITE_PAWN_1, 24),
                CreateRook(Constants.ID_BLACK_PAWN_1, 33),
                CreateRook(Constants.ID_BLACK_PAWN_2, 42),
                CreateBlackKing(63),
            };
            return md;
        }

        /// <summary>
        /// Bug I found while playing with Victor online. I was playing as White. Moving the pawn at
        /// 25 to 34 (capturing a piece)
        /// </summary>
        /// <returns>The 02.</returns>
        public static MatchData Bug20200701_02 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                new ChessmanSchema { colorId = 0, id = 16, kind = 3, location = 0 },
                new ChessmanSchema { colorId = 0, id = 20, kind = 1, location = 1 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 24, kind = 2, location = 16 },
                new ChessmanSchema { colorId = 0, id = 28, kind = 4, location = 3 },
                new ChessmanSchema { colorId = 0, id = 30, kind = 5, location = 4 },
                new ChessmanSchema { colorId = 0, id = 26, kind = 2, location = 5 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 22, kind = 1, location = 21 },
                new ChessmanSchema { colorId = 0, id = 18, kind = 3, location = 7 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 0, kind = 0, location = 24 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 2, kind = 0, location = 25 },
                new ChessmanSchema { colorId = 0, id = 4, kind = 0, location = 10 },
                new ChessmanSchema { colorId = 0, id = 6, kind = 0, location = 11 },
                new ChessmanSchema { colorId = 0, id = 8, kind = 0, location = 12 },
                new ChessmanSchema { colorId = 0, id = 10, kind = 0, location = 13 },
                new ChessmanSchema { colorId = 0, id = 12, kind = 0, location = 14 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 14, kind = 0, location = 31 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 1, kind = 0, location = 32 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 3, kind = 0, location = 41 },
                new ChessmanSchema { colorId = 1, id = 5, kind = 0, location = 50 },
                new ChessmanSchema { colorId = 1, id = 7, kind = 0, location = 51 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 9, kind = 0, location = 36 },
                new ChessmanSchema { colorId = 1, id = 11, kind = 0, location = 53 },
                new ChessmanSchema { colorId = 1, id = 13, kind = 0, location = 54 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 15, kind = 0, location = 39 },
                new ChessmanSchema { colorId = 1, id = 17, kind = 3, location = 56 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 21, kind = 1, location = 42 },
                new ChessmanSchema { colorId = 1, id = 25, kind = 2, location = 58 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 29, kind = 4, location = 52 },
                new ChessmanSchema { colorId = 1, id = 31, kind = 5, location = 60 },
                new ChessmanSchema { colorId = 1, id = 23, kind = 1, location = 62 },
                new ChessmanSchema { colorId = 1, id = 19, kind = 3, location = 63 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 27, kind = 2, location = 34 },
            };
            return md;
        }

        /// <summary>
        /// A bug I found with Pieter. (One of the many...)
        /// </summary>
        /// <returns>Junk.</returns>
        public static MatchData Bug20200705 () {
            return new ChessersEngine.MatchData {
                currentTurn = 0,
                moves = new List<string>() { "Pd2d4", "Pb7b5", "Pc2c3", "Nb8c6", "Ng1f3", "Ph7h5", "Pg2g3", "Pg7g6", "Bf1h3", "Bf8h6", "Pa2a3", "Nc6a5", "O-O", "Ng8f6", "Pa3a4", "Pb5xa4", "Ra1xa4", "Bh6xc1", "Nb1a3", "Pg6g5", "Pe2e3", "Pg5g4", "Nf3h4", "Pc7c6", "Rf1e1", "Rh8g8", "Pe3e4", "Rg8g5", "Pe4e5", "Nf6d5", "Pc3c4", "Nd5b6", "Pf2f4", "Rg5g7" },
                pieces = new List<ChessersEngine.ChessmanSchema>() {
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 16, isActive = true, kind = 3, location = 24 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 20, isActive = true, kind = 1, location = 16 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, id = 24, isActive = false, kind = 2, location = 2 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, id = 28, isActive = true, kind = 4, location = 3 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 30, isActive = true, kind = 5, location = 6 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 26, isActive = true, kind = 2, location = 23 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 22, isActive = true, kind = 1, location = 31 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 18, isActive = true, kind = 3, location = 4 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 0, isActive = false, kind = 0, location = 24 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, id = 2, isActive = true, kind = 0, location = 9 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 4, isActive = true, kind = 0, location = 26 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 6, isActive = true, kind = 0, location = 27 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 8, isActive = true, isChecker = true, kind = 0, location = 36 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 10, isActive = true, kind = 0, location = 29 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, hasMoved = true, id = 12, isActive = true, kind = 0, location = 22 },
                    new ChessersEngine.ChessmanSchema { colorId = 0, id = 14, isActive = true, kind = 0, location = 15 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 1, isActive = true, kind = 0, location = 48 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 3, isActive = false, isChecker = true, kind = 0, location = 24 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 5, isActive = true, kind = 0, location = 42 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 7, isActive = true, kind = 0, location = 51 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 9, isActive = true, kind = 0, location = 52 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 11, isActive = true, kind = 0, location = 53 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 13, isActive = true, isChecker = true, kind = 0, location = 30 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 15, isActive = true, kind = 0, location = 39 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 17, isActive = true, kind = 3, location = 56 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 21, isActive = true, kind = 1, location = 32 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 25, isActive = true, kind = 2, location = 58 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 29, isActive = true, kind = 4, location = 59 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, id = 31, isActive = true, kind = 5, location = 60 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 27, isActive = true, isChecker = true, isKinged = true, kind = 2, location = 2 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 23, isActive = true, kind = 1, location = 41 },
                    new ChessersEngine.ChessmanSchema { colorId = 1, hasMoved = true, id = 19, isActive = true, kind = 3, location = 54 }
                  },
            };
        }

        #endregion
    }
}
