// models/BoardLayout.js
const mongoose = require("mongoose");
// schema for each tile:
const TileSchema = new mongoose.Schema(
  {
    index: { type: Number, required: true },
    pos: {
      x: { type: Number, required: true },
      y: { type: Number, required: true },
      z: { type: Number, required: true, default: 0 },
    },
  },
  { _id: false }
);

// schema for the whole board:
const BoardLayoutSchema = new mongoose.Schema(
  {
    _id: String, // <-- προσθέτουμε εδώ
    name: String,
    panel: { width: Number, height: Number },
    margin: Number,
    tileCount: Number,
    tiles: [TileSchema],
  },
  { timestamps: true }
);

module.exports = mongoose.model(
  "BoardLayout",
  BoardLayoutSchema,
  "boardLayout_backgammon_200x250_centered"
);
