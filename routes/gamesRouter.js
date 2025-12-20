const express = require("express");
const gameController = require("./../controllers/gameController");
const router = express.Router(); // creating the actual router that i will export later

router.route("/").get(gameController.aDefaultFunction);

module.exports = router;
