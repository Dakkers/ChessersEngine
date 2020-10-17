using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// White moves queen from d1 to d8 to capture a queen. The queen would then LIKE to jump a
        /// pawn at c7, but there's a bishop at b8 that puts White in check as the king is at h4.
        /// </summary>
        /// <returns>The check from attempted capture jump.</returns>
        public static MatchData InCheckFromAttemptedCaptureJump () {
            return new MatchData {
                currentTurn = ColorEnum.WHITE,
                matchId = 1,
                whitePlayerId = 0,
                blackPlayerId = 1,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteQueen(3),
                    CreateWhiteKing(15),

                    CreatePawn(Constants.ID_BLACK_PAWN_1, 50),
                    CreateBishop(Constants.ID_BLACK_BISHOP_1, 57),
                    CreateBlackQueen(59),
                    CreateBlackKing(63),
                }
            };
        }

        /// <summary>
        /// Check via a capture-deathjump. Queen moves to 5 to put Black into check.
        /// </summary>
        /// <returns>The jump.</returns>
        public static MatchData InCheckFromCaptureDeathjump1 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(0),
                CreateWhiteQueen(4),

                CreatePawn(Constants.ID_BLACK_PAWN_1, 53),
                CreateBlackKing(60),
            };
            return md;
        }

        /// <summary>
        /// Check via a capturejump-deathjump. Queen moves to 5 to put Black into check.
        /// </summary>
        /// <returns>The jump.</returns>
        public static MatchData InCheckFromCaptureDeathjump2 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(0),
                CreateWhiteQueen(4),

                CreatePawn(Constants.ID_BLACK_PAWN_1, 46),
                CreatePawn(Constants.ID_BLACK_PAWN_2, 39),
                CreatePawn(Constants.ID_BLACK_PAWN_3, 56),
                CreateBlackKing(60),
            };
            return md;
        }

        #endregion

        #region Checkmate

        public static MatchData Checkmate1 () {
            // If Rook @ 56 (a8) captures the pawn @ 32 (a5), White will be in checkmate.
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
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(0),
                CreateRook(Constants.ID_WHITE_ROOK_1, 15),
                CreatePawn(Constants.ID_WHITE_PAWN_1, 32),

                CreateRook(Constants.ID_BLACK_ROOK_1, 23),
                CreateRook(Constants.ID_BLACK_ROOK_2, 56),
                CreateBlackQueen(57),
                CreateBlackKing(58),
            };
            return md;
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

        #region Stalemate

        /// <summary>
        /// If Black rook moves from 33 to 57, then White is in stalemate.
        /// </summary>
        public static MatchData Stalemate1 () {
            var md = CreateMatchData();
            md.currentTurn = ColorEnum.BLACK;

            var whiteKingCS = CreateWhiteKing(52);
            whiteKingCS.isChecker = true;

            md.pieces = new List<ChessmanSchema> {
                whiteKingCS,

                CreateBlackKing(56),
                CreateRook(Constants.ID_BLACK_ROOK_1, 33),
            };
            return md;
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

        #region Jumps

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
            var md = CreateMatchData();

            var checkerCS = CreateWhiteQueen(63);
            checkerCS.isChecker = true;
            checkerCS.isKinged = true;

            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(2),
                CreatePawn(Constants.ID_WHITE_PAWN_1, 28),
                checkerCS,

                CreateKnight(Constants.ID_BLACK_KNIGHT_1, 54),
                CreateKnight(Constants.ID_BLACK_KNIGHT_2, 52),
                CreateRook(Constants.ID_BLACK_ROOK_1, 37),
                CreateBlackKing(56),

            };
            return md;
        }

        public static MatchData CaptureMultijump1 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema>() {
                CreateWhiteKing(0),
                CreateRook(Constants.ID_WHITE_ROOK_1, 8),
                CreateWhiteQueen(1),
                CreateKnight(Constants.ID_WHITE_KNIGHT_1, 21),
                CreateKnight(Constants.ID_WHITE_KNIGHT_2, 31),

                CreatePawn(Constants.ID_BLACK_PAWN_1, 41),
                CreateKnight(Constants.ID_BLACK_KNIGHT_1, 50),
                CreateKnight(Constants.ID_BLACK_KNIGHT_2, 52),
                CreatePawn(Constants.ID_BLACK_PAWN_2, 36),
                CreateRook(Constants.ID_BLACK_ROOK_1, 55),
                CreateBlackKing(63)
            };
            return md;
        }

        /// <summary>
        /// Same as `CaptureMultijump1` but the Queen moves to 41 instead of capturing at 41.
        /// </summary>
        /// <returns>The jump.</returns>
        public static MatchData MoveJump1 () {
            var md = CaptureMultijump1();
            md.pieces = md.pieces.Where((p) => p.id != Constants.ID_BLACK_PAWN_1).ToList();
            return md;
        }

        /// <summary>
        /// A checker that can do a deathjump.
        /// </summary>
        public static MatchData DeathJump1 () {
            var md = CreateMatchData();
            md.deathjumpSetting = (int) DeathjumpSetting.ALL;

            var checkerCS = CreateWhiteQueen(52);
            checkerCS.isChecker = true;

            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(0),
                checkerCS,

                CreateBlackQueen(61),
                CreateBlackKing(56),
            };
            return md;
        }

        /// <summary>
        /// A queen that can do a move-deathjump.
        /// </summary>
        /// <returns>The jump.</returns>
        public static MatchData DeathJump2 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema>() {
                CreateWhiteKing(0),
                CreateWhiteQueen(1),

                CreatePawn(Constants.ID_BLACK_PAWN_1, 58),
                CreateBlackKing(63)
            };
            return md;
        }

        /// <summary>
        /// A queen that can do a capture-deathjump.
        /// </summary>
        /// <returns>The jump.</returns>
        public static MatchData DeathJump3 () {
            var md = DeathJump2();
            md.pieces.Add(CreatePawn(Constants.ID_BLACK_PAWN_2, 49));
            return md;
        }

        #endregion

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
            return new MatchData {
                whitePlayerId = 0,
                blackPlayerId = 1,
                currentTurn = 0,
                moves = new List<string> { "Pd2d4", "Pb7b5", "Pc2c3", "Nb8c6", "Ng1f3", "Ph7h5", "Pg2g3", "Pg7g6", "Bf1h3", "Bf8h6", "Pa2a3", "Nc6a5", "O-O", "Ng8f6", "Pa3a4", "Pb5xa4", "Ra1xa4", "Bh6xc1", "Nb1a3", "Pg6g5", "Pe2e3", "Pg5g4", "Nf3h4", "Pc7c6", "Rf1e1", "Rh8g8", "Pe3e4", "Rg8g5", "Pe4e5", "Nf6d5", "Pc3c4", "Nd5b6", "Pf2f4", "Rg5g7" },
                pieces = new List<ChessmanSchema> {
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 16, isActive = true, kind = 3, location = 24 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 20, isActive = true, kind = 1, location = 16 },
                    new ChessmanSchema { colorId = 0, id = 24, isActive = false, kind = 2, location = 2 },
                    new ChessmanSchema { colorId = 0, id = 28, isActive = true, kind = 4, location = 3 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 30, isActive = true, kind = 5, location = 6 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 26, isActive = true, kind = 2, location = 23 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 22, isActive = true, kind = 1, location = 31 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 18, isActive = true, kind = 3, location = 4 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 0, isActive = false, kind = 0, location = 24 },
                    new ChessmanSchema { colorId = 0, id = 2, isActive = true, kind = 0, location = 9 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 4, isActive = true, kind = 0, location = 26 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 6, isActive = true, kind = 0, location = 27 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 8, isActive = true, isChecker = true, kind = 0, location = 36 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 10, isActive = true, kind = 0, location = 29 },
                    new ChessmanSchema { colorId = 0, hasMoved = true, id = 12, isActive = true, kind = 0, location = 22 },
                    new ChessmanSchema { colorId = 0, id = 14, isActive = true, kind = 0, location = 15 },
                    new ChessmanSchema { colorId = 1, id = 1, isActive = true, kind = 0, location = 48 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 3, isActive = false, isChecker = true, kind = 0, location = 24 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 5, isActive = true, kind = 0, location = 42 },
                    new ChessmanSchema { colorId = 1, id = 7, isActive = true, kind = 0, location = 51 },
                    new ChessmanSchema { colorId = 1, id = 9, isActive = true, kind = 0, location = 52 },
                    new ChessmanSchema { colorId = 1, id = 11, isActive = true, kind = 0, location = 53 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 13, isActive = true, isChecker = true, kind = 0, location = 30 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 15, isActive = true, kind = 0, location = 39 },
                    new ChessmanSchema { colorId = 1, id = 17, isActive = true, kind = 3, location = 56 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 21, isActive = true, kind = 1, location = 32 },
                    new ChessmanSchema { colorId = 1, id = 25, isActive = true, kind = 2, location = 58 },
                    new ChessmanSchema { colorId = 1, id = 29, isActive = true, kind = 4, location = 59 },
                    new ChessmanSchema { colorId = 1, id = 31, isActive = true, kind = 5, location = 60 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 27, isActive = true, isChecker = true, isKinged = true, kind = 2, location = 2 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 23, isActive = true, kind = 1, location = 41 },
                    new ChessmanSchema { colorId = 1, hasMoved = true, id = 19, isActive = true, kind = 3, location = 54 }
                  },
            };
        }

        public static MatchData Bug20200907 () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(),
                CreatePawn(Constants.ID_WHITE_PAWN_2, 9),

                CreateBlackKing(),
                CreatePawn(Constants.ID_BLACK_PAWN_3, 50),
            };
            return md;
        }

        /// <summary>
        /// The stack overflow bug.
        /// </summary>
        public static MatchData Bug20200908 () {
            var md = CreateMatchData();
            md.currentTurn = ColorEnum.BLACK;
            md.pieces = new List<ChessmanSchema> {
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 16, isActive = true, kind = 3, location = 1 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 24, isActive = true, kind = 2, location = 20 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 28, isActive = true, kind = 4, location = 12 },
                new ChessmanSchema { colorId = 0, id = 30, isActive = true, kind = 5, location = 4 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 26, isActive = true, kind = 2, location = 14 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 22, isActive = true, kind = 1, location = 23 },
                new ChessmanSchema { colorId = 0, id = 18, isActive = true, kind = 3, location = 7 },
                new ChessmanSchema { colorId = 0, id = 0, isActive = true, kind = 0, location = 8 },
                new ChessmanSchema { colorId = 0, id = 4, isActive = true, kind = 0, location = 10 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 6, isActive = true, isChecker = true, kind = 0, location = 49 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 10, isActive = true, kind = 0, location = 29 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 12, isActive = true, kind = 0, location = 22 },
                new ChessmanSchema { colorId = 0, hasMoved = true, id = 14, isActive = true, kind = 0, location = 31 },
                new ChessmanSchema { colorId = 1, id = 1, isActive = true, kind = 0, location = 48 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 11, isActive = true, kind = 0, location = 37 },
                new ChessmanSchema { colorId = 1, id = 13, isActive = true, kind = 0, location = 54 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 15, isActive = true, kind = 0, location = 39 },
                new ChessmanSchema { colorId = 1, id = 17, isActive = true, kind = 3, location = 56 },
                new ChessmanSchema { colorId = 1, id = 25, isActive = true, kind = 2, location = 58 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 29, isActive = true, kind = 4, location = 50 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 31, isActive = true, kind = 5, location = 53 },
                new ChessmanSchema { colorId = 1, id = 27, isActive = true, kind = 2, location = 61 },
                new ChessmanSchema { colorId = 1, id = 23, isActive = true, kind = 1, location = 62 },
                new ChessmanSchema { colorId = 1, hasMoved = true, id = 19, isActive = true, kind = 3, location = 40 },
            };
            return md;
        }

        /// <summary>
        /// A very weird bug where a piece is trying to jump over an empty tile.
        ///
        /// THE CAUSE: the computer was attempting to do a move that was illegal (in terms of how the
        /// piece can move). It was trying to move a bishop along a diagonal that was blocked.
        ///
        /// THE SOLUTION: always validate the movements even if it *seems* redundant.
        /// </summary>
        public static MatchData Bug20200910 () {
            var md = CreateMatchData();
            md.currentTurn = ColorEnum.WHITE;
            md.pieces = new List<ChessmanSchema> {
                new ChessmanSchema { colorId= 0, hasMoved= true, id= 26, isActive= true, kind= 2, location= 12 },
                new ChessmanSchema { colorId= 0, hasMoved= true, id= 30, isActive= true, kind= 5, location= 13 },
                new ChessmanSchema { colorId= 1, hasMoved= true, id= 13, isActive= true, isChecker= true, kind= 0, location= 30 },
                new ChessmanSchema { colorId= 1, hasMoved= true, id= 15, isActive= true, isChecker= true, kind= 0, location= 31 },
                new ChessmanSchema { colorId= 1, hasMoved= true, id= 31, isActive= true, kind= 5, location= 63 },
            };
            return md;
        }

        /// <summary>
        /// A bug where a tile was not correctly marked as a potential movement. In this state, Black
        /// is in check and moving the knight to e7 should be valid to block the bishop. The knight
        /// *CAN* move there but it doesn't get marked as such.
        ///
        /// THE CAUSE: `ChessersEngine.Board.GetValidTilesForMovement` was only undoing the move if
        /// the move was valid. This is bad because the board's methods mutate the board (which is bad
        /// in itself).
        ///
        /// THE SOLUTION: always call `UndoMove`.
        /// </summary>
        /// <returns>The 01.</returns>
        public static MatchData Bug20200924_01 () {
            var md = CreateMatchData();
            md.currentTurn = ColorEnum.BLACK;
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(4),
                CreateBishop(Constants.ID_WHITE_BISHOP_1, 16),

                CreateKnight(Constants.ID_BLACK_KNIGHT_1, 42),
                CreateKnight(Constants.ID_BLACK_KNIGHT_2, 62),
                CreateBlackKing(61),
            };
            return md;
        }

        #endregion

        #region Scoring

        public static MatchData RookOpenTest () {
            return new MatchData {
                currentTurn = ColorEnum.BLACK,
                pieces = new List<ChessmanSchema> {
                    CreateWhiteKing(),
                    CreateBlackKing(),
                    CreateRook(Constants.ID_BLACK_ROOK_1, 56),
                }
            };
        }

        #endregion

        #region Utils

        public static MatchData AllKings () {
            var md = CreateMatchData();
            md.pieces = new List<ChessmanSchema> {
                CreateWhiteKing(56),
                CreateWhiteQueen(57),
                CreateRook(Constants.ID_WHITE_ROOK_1, 58),
                CreateKnight(Constants.ID_WHITE_KNIGHT_1, 59),
                CreateBishop(Constants.ID_WHITE_BISHOP_1, 60),
                CreatePawn(Constants.ID_WHITE_PAWN_1, 61),

                CreateBlackKing(0),
                CreateBlackQueen(1),
                CreateRook(Constants.ID_BLACK_ROOK_1, 2),
                CreateKnight(Constants.ID_BLACK_KNIGHT_1, 3),
                CreateBishop(Constants.ID_BLACK_BISHOP_1, 4),
                CreatePawn(Constants.ID_BLACK_PAWN_1, 5),
            };
            for (int i = 0; i < md.pieces.Count; i++) {
                md.pieces[i].isChecker = true;
                md.pieces[i].isKinged = true;
            }

            return md;
        }

        #endregion
    }
}
