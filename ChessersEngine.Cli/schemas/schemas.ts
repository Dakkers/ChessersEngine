// Zod validators mirroring the published JSON Schemas in this directory.
//
// The `*.vN.schema.json` files remain the language-neutral published contract (their `$schema`
// URIs must resolve for the Rust port and any other consumer); these Zod schemas are the CI
// validator implementation. On a version bump, add the new `*.vN.schema.json` AND the matching
// Zod schema here, then register it in the `*Schemas` map below.

import { z } from "zod";

const SCHEMA_BASE =
  "https://raw.githubusercontent.com/Dakkers/ChessersEngine/master/ChessersEngine.Cli/schemas";

// -- shared enums ------------------------------------------------------------
const color = z.enum(["WHITE", "BLACK"]);
const chessmanKind = z.enum(["PAWN", "KNIGHT", "BISHOP", "ROOK", "QUEEN", "KING"]);
const playerKind = z.enum(["human", "ai"]);
const int = () => z.number().int();

// ===========================================================================
// oracle.v1
// ===========================================================================
const chessmanSchema = z
  .object({
    colorId: int(),
    guid: int(),
    hasMoved: z.boolean(),
    id: int(),
    isActive: z.boolean(),
    isChecker: z.boolean(),
    isKinged: z.boolean(),
    isPromoted: z.boolean(),
    kind: int().min(0).max(5),
    location: int(),
  })
  .strict();

const moveAttempt = z
  .object({
    pieceGuid: int(),
    pieceId: int(),
    playerId: int(),
    promotionRank: int(), // -1 for none
    tileId: int(),
  })
  .strict();

const moveResult = z
  .object({
    playerId: int(),
    pieceId: int(),
    pieceGuid: int(),
    fromTileId: int(),
    tileId: int(),
    turnChanged: z.boolean(),
    type: int(),
    polarityChanged: z.boolean(),
    wasFirstMoveForPiece: z.boolean(),
    kinged: z.boolean(),
    isCastle: z.boolean(),
    promotionOccurred: z.boolean(),
    promotionRank: chessmanKind.nullable(),
    wasPieceJumped: z.boolean(),
    jumpedPieceId: int(),
    jumpedTileId: int(),
    wasPieceCaptured: z.boolean(),
    capturedPieceId: int(),
    isWinningMove: z.boolean(),
    isInCheck: z.boolean(),
    isStalemate: z.boolean(),
    valid: z.boolean(),
    notation: z.string().nullable(),
    fromRow: int(),
    toRow: int(),
    fromColumn: int(),
    toColumn: int(),
    chessmanKind: chessmanKind,
  })
  .strict();

const move = z
  .object({
    attempt: moveAttempt,
    notation: z.string(),
    result: moveResult,
  })
  .strict();

const ply = z
  .object({
    index: int(),
    turnColor: color,
    playerId: int(),
    actor: playerKind,
    moves: z.array(move).min(1),
    committedNotation: z.string().nullable(),
    boardAfter: z.array(chessmanSchema),
  })
  .strict();

const outcome = z
  .object({
    gameOver: z.boolean(),
    winningPlayerId: int(),
    winningColor: color.nullable(),
    isDraw: z.boolean(),
    isResignation: z.boolean(),
    reason: z.enum([
      "checkmate",
      "stalemate",
      "resignation",
      "move-limit",
      "no-legal-moves",
      "quit",
      "in-progress",
    ]),
  })
  .strict();

const rejectedAttempt = z
  .object({
    turnColor: color,
    playerId: int(),
    attempt: moveAttempt,
    result: moveResult.nullable(),
    board: z.array(chessmanSchema),
  })
  .strict();

export const oracleV1 = z
  .object({
    $schema: z.literal(`${SCHEMA_BASE}/oracle.v1.schema.json`),
    schemaVersion: z.literal(1),
    engine: z.string(),
    config: z
      .object({
        white: playerKind,
        black: playerKind,
        aiLevel: int(),
        randomSeed: int().nullable(),
        deathjumpSetting: z.enum(["OFF", "SIDES", "BACK", "ALL"]),
        whitePlayerId: int(),
        blackPlayerId: int(),
      })
      .strict(),
    initialPieces: z.array(chessmanSchema),
    plies: z.array(ply),
    outcome: outcome,
    rejectedAttempts: z.array(rejectedAttempt).optional(),
  })
  .strict();

// ===========================================================================
// codec.v1
// ===========================================================================
export const codecV1 = z
  .object({
    $schema: z.literal(`${SCHEMA_BASE}/codec.v1.schema.json`),
    schemaVersion: z.literal(1),
    engine: z.string(),
    description: z.string().optional(),
    minTileId: z.literal(-36),
    maxTileId: z.literal(63),
    roundTripMismatches: int().min(0),
    tiles: z
      .array(
        z
          .object({
            tileId: int().min(-36).max(63),
            row: int(),
            col: int(),
            roundTrip: int(),
            ok: z.boolean(),
          })
          .strict()
      )
      .length(100),
  })
  .strict();

// version registries: N -> Zod schema for that version
export const oracleSchemas: Record<number, z.ZodTypeAny> = { 1: oracleV1 };
export const codecSchemas: Record<number, z.ZodTypeAny> = { 1: codecV1 };
