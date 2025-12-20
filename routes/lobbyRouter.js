const express = require("express");
const gameController = require("./../controllers/lobbyController");
const router = express.Router(); // creating the actual router that i will export later

router.route("/").get(lobbyController.aDefaultFunction);

module.exports = router;
