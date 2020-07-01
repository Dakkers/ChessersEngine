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
        public static MatchData Bug20200701 () {
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


        #endregion
    }
}
