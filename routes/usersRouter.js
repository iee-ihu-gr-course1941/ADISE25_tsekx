const express = require("express");
const gameController = require("./../controllers/userController");
const router = express.Router(); // creating the actual router that i will export later

router.route("/").get(userController.aDefaultFunction);

module.exports = router;
