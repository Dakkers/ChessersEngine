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
const int = () => z.int();

// ===========================================================================
// oracle.v1
// ===========================================================================
const chessmanSchema = z.strictObject({
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
});

const moveAttempt = z.strictObject({
  pieceGuid: int(),
  pieceId: int(),
  playerId: int(),
  promotionRank: int(), // -1 for none
  tileId: int(),
});

const moveResult = z.strictObject({
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
});

const move = z.strictObject({
  attempt: moveAttempt,
  notation: z.string(),
  result: moveResult,
});

const ply = z.strictObject({
  index: int(),
  turnColor: color,
  playerId: int(),
  actor: playerKind,
  moves: z.array(move).min(1),
  committedNotation: z.string().nullable(),
  boardAfter: z.array(chessmanSchema),
});

const outcome = z.strictObject({
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
});

const rejectedAttempt = z.strictObject({
  turnColor: color,
  playerId: int(),
  attempt: moveAttempt,
  result: moveResult.nullable(),
  board: z.array(chessmanSchema),
});

export const oracleV1 = z.strictObject({
  $schema: z.literal(`${SCHEMA_BASE}/oracle.v1.schema.json`),
  schemaVersion: z.literal(1),
  engine: z.string(),
  config: z.strictObject({
    white: playerKind,
    black: playerKind,
    aiLevel: int(),
    randomSeed: int().nullable(),
    deathjumpSetting: z.enum(["OFF", "SIDES", "BACK", "ALL"]),
    whitePlayerId: int(),
    blackPlayerId: int(),
    // Present only for --scenario games (absent for the standard opening).
    scenario: z.string().nullable().optional(),
  }),
  initialPieces: z.array(chessmanSchema),
  plies: z.array(ply),
  outcome: outcome,
  rejectedAttempts: z.array(rejectedAttempt).optional(),
});

// ===========================================================================
// codec.v1
// ===========================================================================
export const codecV1 = z.strictObject({
  $schema: z.literal(`${SCHEMA_BASE}/codec.v1.schema.json`),
  schemaVersion: z.literal(1),
  engine: z.string(),
  description: z.string().optional(),
  minTileId: z.literal(-36),
  maxTileId: z.literal(63),
  roundTripMismatches: int().min(0),
  tiles: z
    .array(
      z.strictObject({
        tileId: int().min(-36).max(63),
        row: int(),
        col: int(),
        roundTrip: int(),
        ok: z.boolean(),
      }),
    )
    .length(100),
});

export const oracleSchemas: Record<number, z.ZodType> = { 1: oracleV1 };
export const codecSchemas: Record<number, z.ZodType> = { 1: codecV1 };
