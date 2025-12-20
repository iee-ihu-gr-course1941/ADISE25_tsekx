const mongoose = require("mongoose");
/**
 * Schema for position field
 */
const Position3DSchema = new mongoose.Schema(
  {
    x: { type: Number, required: true },
    y: { type: Number, required: true },
    z: { type: Number, required: true },
  },
  { _id: false }
);

/**
 * Schema for pawn field
 */
const PawnSchema = new mongoose.Schema({
  id: { type: String, required: true }, // π.χ. w1, b12
  color: {
    type: String,
    enum: ["white", "black"],
    required: true,
  },
  boardIndex: { type: Number, required: true },
  state: {
    type: String,
    enum: ["active", "captured", "offboard"],
    default: "active",
  },
  position3D: {
    type: Position3DSchema,
    required: true,
  },
  stackOrder: {
    type: Number,
    required: true, // 1 = κάτω/πρώτο
  },
});

/**
 * Schema for player field
 */
const PlayerSchema = new mongoose.Schema({
  userId: { type: Number, required: true },
  pawns: {
    type: [PawnSchema],
    validate: [
      (v) => v.length <= 15,
      "A player cannot have more than 15 pawns",
    ],
  },
  playerState: {
    type: String,
    enum: ["Waiting", "Playing", "Notplaying"],
  },
});

/**
 * Schema for game field
 */
const GameSchema = new mongoose.Schema(
  {
    _id: { type: String }, // "game_state"

    players: {
      type: [PlayerSchema],
      validate: [
        (v) => v.length === 2,
        "Backgammon game must have exactly 2 players",
      ],
      required: true,
    },

    boardLines: {
      type: Map,
      of: Number, // π.χ. { "1": 5, "2": 0, ... }
      required: true,
      default: () =>
        Object.fromEntries(
          Array.from({ length: 24 }, (_, i) => [String(i + 1), 0])
        ),
    },

    status: {
      type: String,
      enum: ["notstartedyet", "active", "finished"],
      default: "notstartedyet",
    },

    turn: {
      type: Number,
      default: 0,
    },
  },
  { timestamps: true }
);

const Game = mongoose.model("Game", GameSchema);
module.exports = Game;
