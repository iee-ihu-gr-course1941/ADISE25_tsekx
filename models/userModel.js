const mongoose = require("mongoose");
const userSchema = new mongoose.Schema({});
const Game = mongoose.model("Game", userSchema);
module.exports = User; // the only thing that iam exporting
