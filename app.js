const express = require("express");
const app = express();
app.use(express.json());

// importing the Routers :
const gameRouter = require("./routes/gamesRouter");

// Doing the actual routing :
app.use("/api/v1/backgammon", gameRouter);

module.exports = app;
