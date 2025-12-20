const mongoose = require("mongoose");
const lobbySchema = new mongoose.Schema({});
const Game = mongoose.model("Game", lobbySchema);
module.exports = Lobby; // the only thing that iam exporting
