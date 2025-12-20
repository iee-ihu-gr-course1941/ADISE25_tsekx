const app = require("./app");
const mongoose = require("mongoose");
const dotenv = require("dotenv");
dotenv.config({ path: "./config.env" });
const http = require("http");
const WebSocket = require("ws");
const webSocketRouter = require("./websocketHandler"); // importing the websocket routing where i have all the route handler functions .

const DB = process.env.DATABASE.replace(
  "<db_password>",
  process.env.DATABASE_PASSWORD
);
mongoose
  .connect(DB, {
    useNewUrlParser: true,
    useCreateIndex: true,
    useFindAndModify: false,
  })
  .then((con) => {
    console.log(con.connections);
    console.log("DB connection successful");
  });
// CREATING THE HYBRID SERVER :
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });
wss.on("connection", (ws) => {
  // whenever a client creates a connection this is executed.
  console.log("Client connected to WebSocket");
  ws.on("message", (raw) => {
    // raw is the yet unprocessed message from the client
    // for whenever the client is sending a message.
    let msg;
    try {
      msg = JSON.parse(raw); // iam turning the JSON string to a javascript object.
    } catch (e) {
      // if its not a JSON string the catch code will be executed
      return;
    }
    webSocketRouter.handleMessage(ws, msg); // i'll just pass the msg to the routing logic and the proper process , depending of the message , will happen !
  });
});

// END OF CREATING THE HYBRID SERVER.
const port = 3000;
server.listen(port, () => {
  console.log(`App running on port ${port}...`);
  console.log("webserver also active");
});
