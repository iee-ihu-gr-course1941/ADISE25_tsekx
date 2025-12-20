// models/TilesRow.js
const mongoose = require("mongoose");

// schema for a single tile row
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

// schema for all the rows
const TilesRowSchema = new mongoose.Schema(
  {
    _id: String, // p.x "tileRows_countedAt24"
    tiles: [TileSchema],
    createdAt: { type: Date, default: Date.now },
  },
  { timestamps: true } // it will create automatically createdAt and updatedAt
);

module.exports = mongoose.model(
  "TilesRow",
  TilesRowSchema,
  "tileRows_countedAt24"
);
