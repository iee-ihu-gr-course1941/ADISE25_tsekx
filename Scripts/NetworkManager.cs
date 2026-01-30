using System.Security.Cryptography;
using System;
//using System.Net.WebSockets; // WebSocket is an interface that describes how a websocket should behave so its something abstract and i cant make an object out of it . For that i need a built-in class like the ClientWebSocket that implements the WebSocket interface .
using UnityEngine;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEditor.PackageManager; // For legacy reasons i will leave it here.
using System.Collections.Generic; // Need that so i can use the <> in the new List<Transform>() for instance
using Newtonsoft.Json; // I need to download this for formatting the data.
using System.Linq;


public class NetworkManager : MonoBehaviour // This one is attached to an empty game object.
{
  // Its better to have the code for netowrking inside of a script that its attached to a single empty game object and not mix it with the Gamemanager game object .
  public static NetworkManager I; // i create an object of type NetworkManager thats works as a singleton . So other c# scripts can have access to the networking functionality of a certain NetworkManager.
  private WebSocket websocket; // the one and only websocket for my front-end.
  public BoardLayoutTemplate boardLayoutData; // here i will save the initial board data for each pawn.
  public Action<BoardLayoutTemplate> OnBoardDataReceived; // Creating an event which is in fact a delegate method in c# public Action<Tile[]> OnTileRowsDataReceived; .Iam gonna use this event to inform the PawnMover of all the possible places to move a pawn.
  public Action<int[]> OnDiceResultsReceived;
  public bool isSending = false; // Gonna use this to make sure the state variables of the PawnMover are seted before i actually move the pawn.
  public bool pawnDidntMoveAtServer = false;
  // Variables regarding the move of a pawn :
  public int start;
  public int end;
  // Variables for saving the data from the server and use them in the c# code :
  public List<PawnStats> pawnsAtStart;
  public List<PawnStats> pawnsAtTarget;
  public PawnStats pawnAfter;
  public PawnStats enemyPawn;
  public bool iamFree = false; // the state for a specific pawn that the response from the server has to do with.
  async void Start()
  {
    I = this; // setting the NetworkManager object to the variable of type NetworkManager in order to create the logic of a singleton
              //socket = new WebSocket("ws://server:127.0.0.1:3000/api/v1/backgammon");
    websocket = new WebSocket("ws://127.0.0.1:3000/api/v1/backgammon");


    websocket.OnOpen += async () => // iam adding a function to be executed on a certain event. the event is the opening of the socket with the server . Whatver lamda expression i add to the event , where a lamda expression is the arrow function of c# , will run once the connection happens to determine how the just the beginning of the websocket connection will be handled .
    {
      Console.WriteLine("connection is open");
      //SendJsonMessage(new ClientMessage{type = "get_player_stats"});
      // SendMessage(new {type="get_player_stats"}); // here for instance i can send a condition that once its fulfilled a get_player_stats kind of messeage will be sent to the server to actually get ther player stats. This one is an anonymous object and it wont work with JsonUltility.
      Debug.Log("connection is open");
      await Task.Delay(100); // a small delay just for the sake of delaying.
      SendJsonMessage(new ClientMessage { type = "get_tile_rows" }); // sending a request to get the tile rows , i will save the outcome on the PawnMover.
    };

    websocket.OnMessage += (bytes) =>
    {
      string msg = System.Text.Encoding.UTF8.GetString(bytes); // turning the low level response into an object of objects. {"type":"value","data":[{},{},{}]} , its a json string
      var res = JsonUtility.FromJson<ServerResponse>(msg); // taking the json string and turning it into a c# object of type ServerResponse with the parser JsonUtility.FromJson<>()

      // Here i will handle all the responses coming from the server : 
      switch (res.type)
      {
        case "board_initial_stats":
          HandleBoardInitialStats(msg); // With this data iam placing the pawns at their initial position on board.
          break;
        case "board_tile_rows":
          HandleSettingTileRowsForPawnMover(msg); // With this iam defining the 24 positions on which the pawns will be moving.
          break;
        case "dice_results":
          HandleDiceResults(msg); // With this iam processing the results from rolling 2 dices at the server-side.
          break;
        case "pawn_move_preview":
          handlePawnMovePreview(msg); // with this iam updating the positions of the pawns on board everytime i move a pawn.
          break;
        case "free_pawn_happened":
          Debug.Log("I got a response from freeing the pawn!");
          handleFreePawn(msg);
          break;
        case "free_pawn_error":
          Debug.LogError(msg);
          handleFreePawn(msg); // i will handle it the same way as the free_pawn_happened.
          break;
        case "move_error":
          Debug.Log(msg);
          handlePawnMovePreview(msg); // i will handle it the same way as the pawn_move_preview.
          break;
        case "turn_changed":
          Debug.Log("msg");
          OnTurnChanged(msg);
          break;
        case "joined_game_with_id":
          joinedGame(msg);
          Debug.Log(msg);
          break;
      }

    }; // what to happen for when the server is sending something. Its the heart of the client-side logic for the networking of my application !

    websocket.OnError += e => Debug.LogError(e); // for when an error occurs with the connection between the c# and the (javascript) server.

    websocket.OnClose += (e) =>
    {
      Debug.Log("Connection closed with code:" + e);
    };

    try
    {
      await websocket.Connect(); // here is where the connection is being opened!!!. and since it exists inside of the start method a connection will start at the very begining.
    }
    catch (Exception e)
    {
      Debug.LogError("WebSocket connection failed " + e.Message);
    }
  }//end of start method

  // How to keep the websocket updated:
  void Update()
  {
#if !UNITY_WEBGL || UNITY_EDITOR
    websocket.DispatchMessageQueue();
#endif // this will keep the websocket updated , for upcoming messages from the server . 
  }
  //
  // A method to send requests to the server :
  //
  public async void SendJsonMessage(object obj) // iam giving a c# obj as a parameter
  {
    string json = JsonUtility.ToJson(obj); // iam turning this obj into a JSON string
    if (websocket.State == WebSocketState.Open) // checking if the websocket is open otherwise whatever i send from here will never reach the server. 
    {
      await websocket.SendText(json);
      Debug.Log("Sent message: " + json);
      // iam sending the json to the websocket server and iam awaiing the response. Also the json string that ian sending has to be of a certain c# class instead of an anonymous object otherwise the JsonUtility wont recognise it and it will just send an empty obj in the end to the client. So whenever i use the SendJsonMessage() on the rest of the code i'll do this something like this : SendJsonMessage(new ClientMessage{type ="get_player_stats"});

    }
    else
    {
      Debug.LogWarning("WebSocket not open, cannot send message!");
    }


  }
  //
  // The route handler methods :
  //
  //1.)
  public void HandleBoardInitialStats(string dataJSON)
  {
    try
    {
      var boardRes = JsonUtility.FromJson<BoardResponse>(dataJSON); // with the JsonUtility.FromJson<>() parser i turn dataJSON JSON string to a variable of type BoardResponse named boardRes .
      if (boardRes == null || boardRes.data == null || boardRes.data.Length == 0)
      {
        Debug.LogError("BoardResponse is empty or invalid!");
      }
      boardLayoutData = boardRes.data[0]; // saving the data field that contains the core data into a global variable of type boardLayout[].
      BoardLayoutTemplate layout = boardRes.data[0]; // The boardRes variable of type BoardResponse contains just one BoardLayout obj which is the initial one , so all i need is the first element out of the data variable which is a BoardLayout[] type.
                                                     // i can access whatever field i want from this type of BoardLayout obj boardLayoutData.
      OnBoardDataReceived?.Invoke(boardLayoutData); // this sends the data boardLayoutData to anyone thats listening for this event named OnBoardDataReceived . (iam listening for that inside of the GameManager c# script)
    }
    catch (Exception e)
    {
      Debug.LogError("Failed" + e.Message);
    }
  }
  //2.)
  public void HandleSettingTileRowsForPawnMover(string dataJSON)
  {
    var tileRes = JsonUtility.FromJson<tileRowsResponse>(dataJSON);
    if (tileRes == null)
    {
      Debug.LogError("tileResponse is empty or invalid!");
    }
    Tile[] tileData = tileRes.data[0].tiles; // it contains the 24 tiles.

    PawnMover.Pn.tiles = new List<Vector3>(); // creating the actual list obj for the PawnMover field here in the NetworkManager.
    foreach (Tile t in tileData)
    {
      PawnMover.Pn.tiles.Add(new Vector3(t.pos.x, t.pos.y, t.pos.z));
      //Debug.Log(PawnMover.Pn.tiles); too many results for my console with this .
    }
  }
  //3.)
  public void HandleDiceResults(string dataJSON)
  {
    var arrRes = JsonUtility.FromJson<DiceResultsMessage>(dataJSON);
    Debug.Log("Server :" + arrRes.data[0] + " and " + arrRes.data[1]);
    OnDiceResultsReceived?.Invoke(arrRes.data);

  }
  //4.)
  public void handlePawnMovePreview(string dataJSON)
  {
    PawnMovePreviewMessage msg = JsonConvert.DeserializeObject<PawnMovePreviewMessage>(dataJSON);

    PawnStats pawnBefore = msg.data.pawnBefore;
    pawnAfter = msg.data.pawnAfter;
    pawnsAtStart = msg.data.pawnsAtStart;
    pawnsAtTarget = msg.data.pawnsAtTarget;
    enemyPawn = msg.data.enemyPawn; // new assignment.

    if (pawnBefore.boardIndex == pawnAfter.boardIndex)
      pawnDidntMoveAtServer = true;

    // updating PawnMover script or other scripts in general :
    PawnMover.Pn.currentTileIndex = pawnBefore.boardIndex;
    Debug.Log("Pawn will move from: " + pawnBefore.boardIndex + " to: " + pawnAfter.boardIndex);


    foreach (var p in pawnsAtStart)
    {
      Debug.Log("Pawn at start: " + p.boardIndex + " Color: " + p.color);
    }

    foreach (var p in pawnsAtTarget)
    {
      Debug.Log("Pawn at target: " + p.boardIndex + " Color: " + p.color);
    }

    if (enemyPawn != null)
      Debug.Log($"Enemy pawn captured: {enemyPawn.id} at boardIndex {enemyPawn.boardIndex}");

    start = pawnBefore.boardIndex;
    end = pawnAfter.boardIndex;
    Debug.Log("show me where it will end up:" + end);
    isSending = false;
  }

  public void handleFreePawn(string dataJSON)
  {
    Debug.Log("Iam gonna free the pawn:");
    PawnMovePreviewMessage msg = JsonConvert.DeserializeObject<PawnMovePreviewMessage>(dataJSON);

    PawnStats pawnBefore = msg.data.pawnBefore;
    pawnAfter = msg.data.pawnAfter;
    pawnsAtStart = msg.data.pawnsAtStart;
    pawnsAtTarget = msg.data.pawnsAtTarget;
    enemyPawn = msg.data.enemyPawn; // new assignment.

    if (pawnBefore.boardIndex == pawnAfter.boardIndex)
    {
      pawnDidntMoveAtServer = true;
    }

    //---- move from captured to active ----
    if (pawnAfter.state == "active")
    {
      // capturedA :

      GameObject capturedGO_A = GameManager.Gm.capturedA
          .FirstOrDefault(go => go != null && go.GetComponent<Pawn>()?.index == int.Parse(pawnAfter.id));
      Debug.Log("i found an active pawn in the captured list: " + capturedGO_A);
      if (capturedGO_A != null)
      {
        // duplicate code(needs to be fixed) :
        GameManager.Gm.capturedA.Remove(capturedGO_A);
        GameManager.Gm.activePawnsA.Add(capturedGO_A);
        Debug.Log($"Moved pawn {pawnAfter.id} from capturedA to activeA");
        capturedGO_A.GetComponent<Pawn>().clickable = true;
        start = 0; // it will always be the boardindex=1 for when iam freeing a white pawn.
        iamFree = true;
      }

      // capturedB :

      GameObject capturedGO_B = GameManager.Gm.capturedB
          .FirstOrDefault(go => go != null && go.GetComponent<Pawn>()?.index == int.Parse(pawnAfter.id));

      if (capturedGO_B != null)
      {
        // duplicate code(needs to be fixed) :
        GameManager.Gm.capturedB.Remove(capturedGO_B);
        GameManager.Gm.activePawnsB.Add(capturedGO_B);
        Debug.Log($"Moved pawn {pawnAfter.id} from capturedB to activeB");
        capturedGO_B.GetComponent<Pawn>().clickable = true;
        start = 23; // it will always be the boardindex=24 for when iam freeing a black pawn.
        iamFree = true;
      }
    }
    //---- stay from captured to captured ----
    else
    {
      // capturedA :

      GameObject capturedNOTGO_A = GameManager.Gm.capturedA
          .FirstOrDefault(go => go != null && go.GetComponent<Pawn>()?.index == int.Parse(pawnAfter.id));
      Debug.Log("i found an captured pawn in the captured list: " + capturedNOTGO_A);
      if (capturedNOTGO_A != null)
      {
        // duplicate code(needs to be fixed) :
        Debug.Log($"Moved pawn {pawnAfter.id} from capturedA to activeA");
        capturedNOTGO_A.GetComponent<Pawn>().clickable = true;
        iamFree = true;
      }

      // capturedB :

      GameObject capturedNOTGO_B = GameManager.Gm.capturedB
         .FirstOrDefault(go => go != null && go.GetComponent<Pawn>()?.index == int.Parse(pawnAfter.id));
      Debug.Log("i found an captured pawn in the captured list: " + capturedNOTGO_B);
      if (capturedNOTGO_B != null)
      {
        // duplicate code(needs to be fixed) :
        Debug.Log($"Moved pawn {pawnAfter.id} from capturedA to activeA");
        capturedNOTGO_B.GetComponent<Pawn>().clickable = true;
        iamFree = true;
      }

    }

    Debug.Log("Pawn will move from: " + pawnBefore.boardIndex + " to: " + pawnAfter.boardIndex);


    foreach (var p in pawnsAtStart)
    {
      Debug.Log("Pawn at start: " + p.boardIndex + " Color: " + p.color);
    }

    foreach (var p in pawnsAtTarget)
    {
      Debug.Log("Pawn at target: " + p.boardIndex + " Color: " + p.color);
    }

    end = pawnAfter.boardIndex;
    Debug.Log("show me where it will end up:" + end);
    isSending = false;
  }

  public void joinedGame(string dataJSON)
  {
    Debug.Log("HIIIIIIIII");
  }


  //
  //
  //

  public void OnTurnChanged(string dataJSON)
  {
    Debug.Log("HIIIIIIIII");
  }


} // end of NetworkManager class.

//
// I need specific data classes that match the format of the data iam getting from the server:
//

//
// Data Classes :
//

//
// Most general data classes
//

//1.)
[Serializable] // necessary for JsonUtility
public class ServerResponse // generally for everything.
{
  public string type;
  public string data; // generized (raw inner json)
}

//2.)
[Serializable] // necessary for JsonUtility
public class ClientMessage  // generally.
{
  public string type;
}

//
// For joining a game 
//

// This one inherits  the field type from ClientMessage :
[Serializable]
public class JoinGameMessage : ClientMessage
{
  public int userId;
  public string gameId;
}

//
// For ending the current turn :
//

// This one also inherits  the field type from ClientMessage :
[Serializable]
public class EndTurnMessage : ClientMessage
{
  public int userId;
}

//
// For the initial pawn board positions iam getting from the server :
//

//.1) The format of the response iam getting from the server :
[System.Serializable]
public class BoardResponse
{ // specifically for accepting the board data from the server.
  public string type;
  public BoardLayoutTemplate[] data; // it contains objects of type BoardLayout.
}

//2.) This one contains all the rest
[System.Serializable]
public class BoardLayoutTemplate
{ // main data class.
  public Panel panel;
  public string _id; // the field has to have the exact same name as the field from the json , _id and not id . 
  public string name;
  public int margin;
  public int tileCount;
  public Tile[] tiles;
}

//3.)
[System.Serializable]
public class Panel
{ // inner field of BoardLayout.
  public float width;
  public float height;
}

//4.)
[System.Serializable]
public class Tile
{ // inner field of BoardLayout.
  public Position pos;
  public int index;
}

//5.)
[System.Serializable]
public class Position
{ // inner field of Tile which is an inner field of BoardLayout in the end.
  public float x;
  public float y;
  public float z;
}

//
// For the 24 tile row positions iam getting from the server to perform the moving animation :
//

//1.) The format of the response iam getting from the server:
[System.Serializable]
public class tileRowsResponse
{
  public string type;
  public tileRowsTemplate[] data;
}

//2.)
[System.Serializable]
public class tileRowsTemplate
{
  public string _id;  // instead of plain id
  public Tile[] tiles; // this one also needs to be named tiles just like in the json i get.
}

//
// For the dice results iam getting from the server :
//

//.1) The format of the response iam getting from the server :
[Serializable]
public class DiceResultsMessage
{
  public int[] data;
}

//
// For the updated pawn infos iam getting from the server :
//

//.1) The format of the response iam getting from the server :
[Serializable]
public class PawnMovePreviewMessage
{
  public string type;
  public PawnMovePreviewData data;
}

//2.)
[Serializable]
public class PawnMovePreviewData
{
  public PawnStats pawnBefore;
  public PawnStats pawnAfter;
  public List<PawnStats> pawnsAtStart;
  public List<PawnStats> pawnsAtTarget;
  public PawnStats enemyPawn; // new field.
}

//3.)
[Serializable]
public class PawnStats
{
  public string state;
  public string _id; // MongoDB id
  public string id;  // pawn id (string from server)
  public string color;
  public int boardIndex;
  public Position3D position3D;
  public int stackOrder;
}

//4.)
[Serializable]
public class Position3D
{
  public float x;
  public float y;
  public float z;

  public Vector3 ToVector3()
  {
    return new Vector3(x, y, z);
  }
}

//5.) i didnt use it.
[Serializable]
public class BoardLines
{
  public Dictionary<int, int> lines;
}

