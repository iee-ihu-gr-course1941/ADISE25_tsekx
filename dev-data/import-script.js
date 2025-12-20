const fs = require("fs");
const mongoose = require("mongoose");
const dotenv = require("dotenv");
const data = require("./../dev-data/tileRows.json"); // didnt use it.
const BoardLayout = require("./../models/boardLayout"); // importing the schema
const TileRows = require("./../models/tileRowsModel"); // importing another schema
const GameState = require("./../models/gameModel"); // importing another schema
//const Users = require("./../models/userModel"); // importing another shcema

dotenv.config({ path: "./config.env" });
// creating the connection:
const DB = process.env.DATABASE.replace(
  "<db_password>",
  process.env.DATABASE_PASSWORD
);
mongoose
  .connect(DB, {
    useNewUrlParser: true,
    useCreateIndex: true,
    useFindAndModify: false,
    useUnifiedTopology: true,
  })
  .then((con) => {
    console.log(con.connections);
    console.log(
      "DB connection succedded for importing or deleting data from database"
    );
  });

//Reading the JSON file:
const jsonFileReaden = JSON.parse(
  fs.readFileSync(`${__dirname}/gameState.json`, "utf-8") // just change the json and it will work for every json.
);

const importData = async () => {
  try {
    await GameState.create(jsonFileReaden); // here i put the model.create()
    console.log("Data successfully loaded!");
  } catch (err) {
    console.log(err);
  }
  process.exit();
};

const deleteData = async () => {
  try {
    await GameState.deleteMany(); // the different models as i see have access to this functionality that deletes all the documents inside of them .
    console.log("Data successfully deleted!");
  } catch (err) {
    console.log(err);
  }
  process.exit(); // this one though is an aggressive way of stopping the application .
};

if (process.argv[2] === "--import") {
  importData();
} else if (process.argv[2] === "--delete") {
  deleteData();
}

// i need to type at the terminal either this node dev-data/import-script.js --import or dev-data/import-script.js --delete.
