const boardLayout = require("./models/boardLayout");
const tileRows = require("./models/tileRowsModel");
const gameState = require("./models/gameModel");
// const WebSocket = require("ws");
const rooms = {};

module.exports = {
  handleMessage: (ws, msg) => {
    console.log(msg);
    let pawnId = null;
    let coords = null;
    let steps = null;
    // extra testing for getting a changeable request from front-end:
    if (msg.type.startsWith("move_pawn")) {
      if (msg.type.startsWith("move_pawn")) {
        const startPawn = msg.type.indexOf("move_pawn") + "move_pawn".length;
        const endPawn = msg.type.indexOf("_from_");
        pawnId = msg.type.slice(startPawn, endPawn);

        const startCoords = msg.type.indexOf("_from_(") + "_from_(".length;
        const endCoords = msg.type.indexOf(")_for_");
        coords = msg.type.slice(startCoords, endCoords);

        const startSteps = endCoords + ")_for_".length;
        const endSteps = msg.type.indexOf("_indexes");
        steps = msg.type.slice(startSteps, endSteps);

        msg.type = "move_pawn";
      }
    } else if (msg.type.startsWith("free_pawn")) {
      const startPawn = msg.type.indexOf("free_pawn") + "free_pawn".length;
      const endPawn = msg.type.indexOf("_by_moving_it");
      pawnId = msg.type.slice(startPawn, endPawn);

      const startSteps =
        msg.type.indexOf("_by_moving_it") + "_by_moving_it".length;
      const endSteps = msg.type.indexOf("_indexes");
      steps = msg.type.slice(startSteps, endSteps);

      msg.type = "free_pawn";

      console.log("FREE PAWN PARSED :", pawnId, steps);
    }

    switch (msg.type) {
      case "move_pawn":
        movePawn(ws, msg, pawnId, coords, steps);
        break;
      case "get_board_initial_stats":
        sendInitialBoardStats(ws, msg);
        break;
      case "get_tile_rows":
        sendTilerows(ws, msg);
        break;
      case "get_dice_results":
        handleRollingTheDice(ws, msg);
        break;
      case "free_pawn":
        freePawn(ws, msg, pawnId, steps);
        break;
      default:
        console.log("i couldnt find the msg.type");
    }
  },
};
//
// Actual handlers:
//

//1.) For moving the pawn :
async function movePawn(ws, msg, pawnId, coords, steps) {
  try {
    const game = await gameState.findById("game_state");

    const result = await processPawnMove(game, pawnId, steps);

    // ❗ error αλλά ίδιο response format
    if (result.blocked) {
      ws.send(
        JSON.stringify({
          type: "pawn_move_preview",
          data: {
            pawnBefore: result.pawnBefore,
            pawnAfter: result.pawnBefore, // ίδιο index → no move
            pawnsAtStart: result.pawnsAtStart ?? [],
            pawnsAtTarget: result.pawnsAtTarget ?? [],
            enemyPawn: null,
            blocked: true,
            message: result.message,
          },
        })
      );
      return; // iam ending the execution of the function right here . So the changes wont be save on the database.
    }

    await game.save();
    console.log(
      "If there was an enemy pawn , here it is:\n" + result.enemyPawn
    );
    ws.send(
      JSON.stringify({
        type: "pawn_move_preview",
        data: {
          pawnBefore: result.pawnBefore,
          pawnAfter: result.pawnAfter,
          pawnsAtStart: result.pawnsAtStart,
          pawnsAtTarget: result.pawnsAtTarget,
          enemyPawn: result.enemyPawn,
        },
      })
    );
  } catch (err) {
    console.error(err);
  }
}

//2.) For sending the initial positions of the pawns on the board:
async function sendInitialBoardStats(ws, msg) {
  const initial_board_positions = await boardLayout.find();
  //console.log(initial_board_positions);
  ws.send(
    JSON.stringify({
      type: "board_initial_stats", // i will use this name on the client side to get the exact data
      data: initial_board_positions,
    })
  );
}

//3.) For sending the positions on which the moving animation of the pawns will happen:
async function sendTilerows(ws, msg) {
  const tile_rows = await tileRows.find();
  //console.log(tile_rows);
  ws.send(
    JSON.stringify({
      type: "board_tile_rows", // i will use this name on the client side to get the exact data
      data: tile_rows,
    })
  );
}

//4.) Producing 2 dice results and sending them front-end:
function handleRollingTheDice(ws, msg) {
  const result = new Array(
    Math.floor(Math.random() * 6) + 1,
    Math.floor(Math.random() * 6) + 1
  );
  ws.send(
    JSON.stringify({
      type: "dice_results",
      data: result,
    })
  );
}

//5.) Iam getting the pawns, from the game-state document, at a specific boardIndex by using a certain boardIndex :
function getPawnsAtIndex(game, boardIndex) {
  const pawns = [];
  for (const player of game.players) {
    for (const pawn of player.pawns) {
      if (pawn.boardIndex === boardIndex && pawn.state === "active") {
        pawns.push(pawn); // if the boardIndex of the pawn is similar to the destination boardIndex that the pawn the client wants to move , and if its in an active state , then put it inside of the pawns array i created.
      }
    }
  }
  return pawns;
}

//5.) Iam Rearranging the pawns inside of the game-state document for a specific boardIndex(targetIndex) :
function Rearrangement(targetIndex, pawnsAtTarget) {
  // sorting the pawnsAtTarget based on the stackOrder
  pawnsAtTarget.sort((a, b) => a.stackOrder - b.stackOrder); // pawnsAtTarget[0] will have the minimum stackOrder.

  const zOffset = 16.6;
  const baseX = pawnsAtTarget[0].position3D.x;
  const baseZ = pawnsAtTarget[0].position3D.z;

  for (let i = 0; i < pawnsAtTarget.length; i++) {
    const pawn = pawnsAtTarget[i]; // x is gonna be the same for all , the depth (z) is what changes.

    pawn.position3D.x = baseX;

    if (targetIndex <= 12) {
      pawn.position3D.z = baseZ + i * zOffset;
    } else {
      pawn.position3D.z = baseZ - i * zOffset;
    }
  }
}

//6.) Iam returning the bottom of a board index for a certain board index :
function boardIndexToWorldPosition(boardIndex) {
  const step = 16.6;
  let x, z;
  if (boardIndex <= 12) {
    z = -116.7; // standard
    x = 91.7 - (boardIndex - 1) * step;
  } else {
    z = 116.7;
    x = -91.7 + (boardIndex - 13) * step;
  }
  return { x, y: 0, z };
}

//7.) Iam checking if the free_pawn request from the client can actually happen for a certain pawnId :
async function freePawn(ws, msg, pawnId, steps) {
  try {
    console.log(
      "Iam gonna start the operation of freeing the pawn inside of the database..."
    );
    const game = await gameState.findById("game_state");

    // After obtaining the game-state document iam gonna call the function that handles if the move is possible:
    const result = await processPawnMove(game, pawnId, steps); // This function and the applyMoveCases call inside of it doesnt return something to the client.
    console.log(
      "Here is the pawn before the checking of whether it is possible to be free:\n" +
        JSON.stringify(result.pawnBefore, null, 2)
    );
    console.log(
      "Here is the pawn after the checking of whether it is possible to be free:\n" +
        JSON.stringify(result.pawnAfter, null, 2)
    );
    // ❗ Αν blocked → ίδιο format με pawn_move_preview
    if (result.blocked) {
      ws.send(
        JSON.stringify({
          type: "free_pawn_happened",
          data: {
            pawnBefore: result.pawnBefore,
            pawnAfter: result.pawnBefore, // no move
            pawnsAtStart: result.pawnsAtStart ?? [],
            pawnsAtTarget: result.pawnsAtTarget ?? [],
            enemyPawn: null,
            blocked: true,
            message: result.message,
          },
        })
      );
      return; // σταματάμε εκτέλεση, δεν αλλάζει τίποτα στη βάση
    }

    // Here iam changing the state from captured to active on the document that i will use to inform the database later.
    const pawn = game.players
      .flatMap((p) => p.pawns)
      .find((p) => p.id === pawnId);
    pawn.state = "active";
    // Here iam just changing it on the response iam sending to the client:
    result.pawnAfter.state = "active";
    // Then iam informing the database !
    await game.save();
    console.log(
      "Here is the pawn after the checking of whether it is possible to be free:\n" +
        JSON.stringify(result.pawnAfter, null, 2)
    );

    console.log("Or in case the move was blocked:\n" + result.blocked);
    // In the end iam sending a specific format response to the client for the free_pawn request :
    ws.send(
      JSON.stringify({
        type: "free_pawn_happened",
        data: {
          pawnBefore: result.pawnBefore,
          pawnAfter: result.pawnAfter,
          pawnsAtStart: result.pawnsAtStart,
          pawnsAtTarget: result.pawnsAtTarget,
          enemyPawn: result.enemyPawn,
        },
      })
    );
  } catch (err) {
    console.error(err);
  }
}

//8.) Iam accomplishing the checking of all the possible outcomes coming from a move of a pawn starting from a certain index and ending up on a certain target index :
function applyMoveCases({ game, foundPawn, startIndex, targetIndex }) {
  let pawnsAtStart = getPawnsAtIndex(game, startIndex);
  let pawnsAtTarget = getPawnsAtIndex(game, targetIndex);
  let enemyPawn = null;

  // CASE 1: empty destination
  if (pawnsAtTarget.length === 0) {
    game.boardLines[targetIndex] += 1;
    game.boardLines[startIndex] -= 1;

    foundPawn.boardIndex = targetIndex; // the pawn will acquire its new boardIndex on the board.
    foundPawn.stackOrder = 1; // Since theres nothing on the destination it will be the first to enter.
    foundPawn.position3D = boardIndexToWorldPosition(targetIndex); // Setting its position on the bottom of the new boardIndex.

    pawnsAtTarget.push(foundPawn); // Putting in inside of the array containing the pawns at the destination.
    Rearrangement(targetIndex, pawnsAtTarget); // Rearranging things on the destination.
    console.log("CASE 1 happened");
  }

  // CASE 2: same color pawns
  else if (pawnsAtTarget.every((p) => p.color === foundPawn.color)) {
    game.boardLines[targetIndex] += 1;
    game.boardLines[startIndex] -= 1;

    foundPawn.boardIndex = targetIndex;
    foundPawn.stackOrder = pawnsAtTarget.length + 1;
    foundPawn.position3D = boardIndexToWorldPosition(targetIndex);

    pawnsAtTarget.push(foundPawn);
    Rearrangement(targetIndex, pawnsAtTarget);
    console.log("CASE 2 happened");
  }

  // CASE 3: capture single enemy pawn
  else if (
    pawnsAtTarget.length === 1 &&
    pawnsAtTarget[0].color !== foundPawn.color
  ) {
    enemyPawn = pawnsAtTarget[0];

    enemyPawn.state = "captured";
    enemyPawn.boardIndex = -1;
    enemyPawn.stackOrder = 0;

    game.boardLines[targetIndex] = 1;
    game.boardLines[startIndex] -= 1;

    foundPawn.boardIndex = targetIndex;
    foundPawn.stackOrder = 1;
    foundPawn.position3D = boardIndexToWorldPosition(targetIndex);

    pawnsAtTarget = [foundPawn]; // Now that i se the enemy pawn , i will put the one that captured it on the pawnsAtTarget.
    Rearrangement(targetIndex, pawnsAtTarget);
    console.log("CASE 3 happened");
  }

  // CASE 4: blocked
  else {
    console.log("CASE 4 happened");
    return {
      blocked: true,
      message: "Move blocked: more than one enemy pawn",
    };
  }
  console.log("After implementing the cases function on the pawn:");
  console.log(foundPawn);

  return {
    blocked: false,
    pawnsAtStart,
    pawnsAtTarget,
    enemyPawn,
  };
}

//9.)
async function processPawnMove(game, pawnId, steps) {
  let foundPawn = null;

  //PART1:
  for (const player of game.players) {
    const pawn = player.pawns.find((p) => p.id === pawnId);
    if (pawn) {
      foundPawn = pawn;
      break;
    }
  }

  if (!foundPawn) throw new Error("Pawn not found"); // a simple error handling.

  const pawnBefore = JSON.parse(JSON.stringify(foundPawn)); // turning a JSON string to Javascript object.
  let startIndex = foundPawn.boardIndex; // storing the boardIndex of the pawn that the request is refering to.

  // Next thing i wanna avoid the bug where i move from the captured state -1 , so a 3 stepts move will lead me to boardindex 2 and not at 3(right one) for instance ...
  if (foundPawn.state === "captured") {
    startIndex = foundPawn.color === "white" ? 0 : 25;
  } else {
    startIndex = foundPawn.boardIndex;
  }

  let targetIndex =
    foundPawn.color === "white"
      ? startIndex + Number(steps)
      : startIndex - Number(steps); // black pawns move from 1 to 24 while white ones move from 24 to 1. So by following this logic i want to add the steps to the boardIndex of the pawn iam interested in or decrease them from the current boardIndex.

  targetIndex = Math.max(1, Math.min(24, targetIndex)); // limiting the result between 1 and 24 , just to be sure i wont go out of bounds.

  //PART: Calling the functionality that will test all the possible scenarios of the pawn moving :
  const result = applyMoveCases({
    game,
    foundPawn,
    startIndex,
    targetIndex,
  });

  const pawnAfter = JSON.parse(JSON.stringify(foundPawn));

  return {
    pawnBefore,
    pawnAfter,
    ...result,
  };
}
