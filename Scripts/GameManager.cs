using System.Collections.Generic; // in c# i have 2 types of queues i have to import the proper one .
using System;
//using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class GameManager : MonoBehaviour
{
  // The variables iam gonna use:
  public GameObject pawnPrefab; // i just drag the prefab to the inspector window to make the connection.
  public Player playerA;
  public Player playerB;
  private Queue<GameObject> pool = new Queue<GameObject>(); // i have to import this : System.Collections.Generic; in order to use this kind of queue .
  public List<GameObject> activePawnsA, activePawnsB;
  public List<GameObject> capturedA, capturedB;
  Vector3 initialPos = new Vector3(0f, 0f, 0f);
  public DiceController diceController; // i will connect it with the game object that has the DiceController script.
  //
  // UI variables :
  //
  public StartGameUI startGameUI; // the start game UI in my game .I need to connect it with the actual StartGameUI through the inspector window.
  public DicePopUpUI dicePopUpUI; // the roll dice UI in my game
  public DiceSelectionUI diceSelectionPopUpUI; // The dice selection UI in my game
  //
  // Singleton variables :
  //
  public PawnMover pawnMover; // Attaching the empty object that contains the script PawnMover. And then i have access to the functionality of the PawnMover script.
  public ClickHandler cursor;
  public static GameManager Gm;
  // Event variables :
  public event Action OnDiceHasBeenChoosen; // the event that will happen once i select one of the dices in the dice selection ui
  public event Action onStartGameClicked; // the event that will happen once i press the start game button
  public int currentMoveSteps;
  //
  // State variables :
  //
  public bool diceHasBeenRolled = false; // a state variable refering to the server
  public int dice1Uses = 0;
  public int dice2Uses = 0;
  public int dice1MaxUses = 0;
  public int dice2MaxUses = 0;
  public int remainingMoves = 0; // state variable that counts the possible moves and its also used to end the turn of a player .
  public bool moveInProgress = false;


  void Start()
  {
    Gm = this;
    playerA = new Player("PlayerA");
    playerB = new Player("PlayerB");
    //Creation of the pawn prefabs that iam gonna put on the queue of type gameobject:
    for (int i = 0; i < 30; i++)
    {
      var p = Instantiate(pawnPrefab); // with this i create a new pawn similar to the pawnPrefab one .
      p.SetActive(false); // once i make it i immediately deactivate it so its Pawn script wont work.
      p.transform.SetParent(transform); // iam setting as a parent of the p game object the GameManager gameobject by using its transform as an argument in the SetParent for the p.
      pool.Enqueue(p); // Iam actually adding it to the queue .
    }
    // Handling the self-made event that i occur when i get the data for the initial positions of the pawns from the server: 
    NetworkManager.I.OnBoardDataReceived += (boardLayoutData) =>
    {
      //Debug.Log(JsonUtility.ToJson(boardLayoutData.tiles[0], true));
      SpawningPawns_InitialPositions_EachPlayer(boardLayoutData); // This method uses SpawnPawn() and SpawnPawn() uses getPawn() to get the game object and finally return it through the SpawningPawns_InitialPositions_EachPlayer().
    };
    // What to appear at the start of the game out of the 3 possible UIs :
    //
    NetworkManager.I.SendJsonMessage(new JoinGameMessage
    {
      type = "join_game_with_id",
      userId = LocalPlayer.UserId,
      gameId = "game_state" // i mean the _id  of the document game_state.
    });
    //
    startGameUI.window.SetActive(true);
    dicePopUpUI.window.SetActive(false);
    diceSelectionPopUpUI.window.SetActive(false);
    // Adding functionality to be executed once i press the start button(1st ui) , the roll button(2nd ui) and the proceed button(3rd ui) :
    //1.)
    startGameUI.startButton.onClick.AddListener(onStartClicked);
    //2.)
    dicePopUpUI.rollButton.onClick.AddListener(OnRollButtonClicked);
    //3.)
    diceSelectionPopUpUI.proceed.onClick.AddListener(onProceedButtonHasBeenClicked); // the other 2 buttons of the selectionUI will just lock the result of the dice . The proceed button is gonna be the one to actually proceed to the final phase where iam selecting a pawn to be moved.
    // What to happen when i click on one of the result_dice_buttons(3rd ui) :
    cursor.OnDiceButtonHasBeenClicked += (btn) =>
    {
      HandleJustTheClickOfDiceResultButton(btn); // iam passing the button i pressed as a parameter with functionality from ClickHandler.
    };
    // What to happen when i receive the dice data result from the server :
    NetworkManager.I.OnDiceResultsReceived += (arr) =>
   {
     HandleDiceResultsFromServer(arr);
   };
    // What to happen once the movement of the pawn has been completed :
    PawnMover.Pn.onComplete += () =>
 {
   // Once the steps are completed i call the ArrangePawnsAtTarget :
   PawnMover.Pn.ArrangePawnsAtTarget(NetworkManager.I.pawnAfter, NetworkManager.I.pawnsAtTarget);
   GameManager.Gm.moveInProgress = false;
   PawnMover.Pn.pawnHasReachedItsFinalDestination = true;
   CheckingWetherToEndOrContinueTheTurn();
 };
  } // end of Start().
    //
    // This method returns pawns of type GameObject :
    //
  GameObject GetPawnFromPool()
  {
    if (pool.Count == 0)
    {
      var p = Instantiate(pawnPrefab);
      return p;
    }
    var go = pool.Dequeue();
    go.SetActive(true);
    return go;
  } // with this functionallity i get the pawn out of the pool so i activate it in the GUI

  public void DespawnPawn(Pawn pawn)
  {
    pawn.gameObject.SetActive(false);
    pool.Enqueue(pawn.gameObject);

  } // with this i put the pawn in the pool so i deactivate it from the GUI

  public GameObject SpawnPawn(int index, Vector3 pos)
  {
    var go = GetPawnFromPool(); // i get an object from the pool out of the 30 that i create at the start method.
    go.transform.position = pos; // iam setting the position of each game object
    go.GetComponent<Pawn>().pos = pos; // iam setting the position of each pawn script inside of the game object of type prefab pawn.
    // Then i need to actually get the Pawn script , and not the game object ,and assign the index :
    go.GetComponent<Pawn>().index = index; // Now each pawn will also have the tile index in which its positioned .
    return go;
  }
  //
  // Function for placing the pawns at their initial position on board :
  //

  //1.1.)
  //Spawning the pawns in the GUI for each player:
  public void SpawningPawns_InitialPositions_EachPlayer(BoardLayoutTemplate layout)
  {
    activePawnsA = new List<GameObject>();
    for (int i = 0; i < 15; i++)
    {
      // Here i'll be taking each field of the BoardLayout obj that i got from the server , and use it to set the pawn:
      initialPos.x = layout.tiles[i].pos.x;
      initialPos.y = layout.tiles[i].pos.y;
      initialPos.z = layout.tiles[i].pos.z;
      var pawn = SpawnPawn(layout.tiles[i].index, initialPos); // this line spawns them , layout.tiles[i].index goes from 1 to 30 in the json file.
      pawn.GetComponent<Renderer>().material.color = Color.white;
      activePawnsA.Add(pawn.gameObject); // here iam adding them to a list that represents the playable pawns .
    }
    //1.2.)
    // Same functionality but for player B
    activePawnsB = new List<GameObject>();
    for (int i = 15; i < 30; i++)
    {
      initialPos.x = layout.tiles[i].pos.x;
      initialPos.y = layout.tiles[i].pos.y;
      initialPos.z = layout.tiles[i].pos.z;
      var pawn = SpawnPawn(layout.tiles[i].index, initialPos);
      pawn.GetComponent<Renderer>().material.color = Color.black;
      activePawnsB.Add(pawn.gameObject);
    }
  }

  //
  // Functions to be executed once i press certain buttons in the 3 uis :
  //

  //
  //1.)
  //
  public void onStartClicked()
  {
    // here iam sending a request to the server
    Debug.Log("Start button pressed - sending request to server...");
    //Since there is an open connection already at NetworkManager i just have to do this:
    NetworkManager.I.SendJsonMessage(new ClientMessage { type = "get_board_initial_stats" }); // way too important! i get the initial data regarding the placement of the pawns from the server.
    startGameUI.Hide();
    //onStartGameClicked?.Invoke();
    // Opening the 2nd UI out of the 3 possible ones :
    dicePopUpUI.window.SetActive(true);
  }
  //
  //2.)
  //  
  public void OnRollButtonClicked() // what is actually happening once i press the roll dice button
  {
    dicePopUpUI.Hide(); // hiding the roll button ui
    diceController.RollAll();
    NetworkManager.I.SendJsonMessage(new ClientMessage { type = "get_dice_results" }); // with {} i initialize an object with its field myself , with () i would call a constructor to do so and i dont want that. 
    diceSelectionPopUpUI.window.SetActive(true);
    diceHasBeenRolled = true;
  }
  //
  //3.)
  //
  public void onProceedButtonHasBeenClicked() // what to happen when i click the proceed button
  {
    settingPawnsToClickableOrUnClickable();
    SetAllPawnsClickable(true);
    diceSelectionPopUpUI.Hide();
    DiceSelectionUI.Ds.proceedHasBeenPressed = true;
    Debug.Log("You are ready to make a pawn move, just click on it and it will happen.");
  }

  // 
  // Handlers for the events i produce :
  //

  private void HandleJustTheClickOfDiceResultButton(Button btn)
  {
    // How to handle the event of clicking on a dice button :
    int value = int.Parse(btn.GetComponentInChildren<TMPro.TMP_Text>().text); // The dice result first came from the server , then i labeled it on a button and now iam extracting it from the button and storing it to an int variable.
    if (btn == DiceSelectionUI.Ds.dice1_selection && dice1Uses < dice1MaxUses) // if i didnt use the first dice result proceed:
    {
      // Here i will save whatver comes from the first dice result :
      currentMoveSteps = value;
      //dice2Uses=false; // I use the same below .
      dice1Uses++; // informing the rest of the code that i used the first dice result.
    }
    else if (btn == DiceSelectionUI.Ds.dice2_selection && dice2Uses < dice2MaxUses)
    {
      // Here i will save whatever comes from the second dice result :
      currentMoveSteps = value;
      //dice1Uses=false; // I want to make sure i use 1 dice result each time , so if i click one the other will be set to false . In the end since the state variable regarding this result will be set to true , i wont be able to reach this if block again anyways.This also helps me prevent the error where i choose a dice result , the pawn doesnt move , yet the dice result i used is locked , but with this statement it isnt anymore !
      dice2Uses++; // informing the rest of the code that i used the second dice result. 
    }

    diceSelectionPopUpUI.proceed.interactable = true;
    currentMoveSteps = value;
  }

  public IEnumerator HandleAttachingSelectionToAPawn(Pawn pawn)
  {
    Debug.Log("Iam proceeding to see if the pawn is clickable");
    Renderer renderer = pawn.GetComponent<Renderer>(); // getting the renderer component so i can access the color later.
    if (pawn.clickable == true)
    {
      Debug.Log("It is");
      SetAllPawnsClickable(false);
      NetworkManager.I.SendJsonMessage(new ClientMessage { type = "move_pawn" + pawn.index.ToString() + "_from_" + pawn.pos + "_for_" + currentMoveSteps + "_indexes" }); // sending the move i want to perform at the server , so the server can decide the results .
      NetworkManager.I.isSending = true;
      yield return new WaitUntil(() => !NetworkManager.I.isSending); // Iam stopping the flow of the execution till isSending is set to false through the handleUpdatedPawnStats() at the NetworkManager script. This way i make sure that i'll get all the data that i need in order to make the changes that comes with the decision to move a certain pawn .
      Debug.Log("iam alsmost ready to  move the pawn:");
      if (NetworkManager.I.pawnDidntMoveAtServer)
      {
        GameManager.Gm.moveInProgress = false; // i also want to set this state variable here.
        //settingPawnsToClickableOrUnClickable(); // setting them back to unclickable.
        Debug.Log("pawn didnt move iam going to set the state variable of the dice result i used back to false");
        NetworkManager.I.pawnDidntMoveAtServer = false;
        // I need to say somehow to my code that the server didnt approve of the move so i need to undo the state variables regarding the choosing of a certain dice result :
        if (diceSelectionPopUpUI.buttonText1.text == currentMoveSteps.ToString())
        {
          dice1Uses--;
        }
        else if (diceSelectionPopUpUI.buttonText2.text == currentMoveSteps.ToString())
        {
          dice2Uses--;
        }
        CheckingWetherToEndOrContinueTheTurn();
      }
      else
      {
        // Deciding in what direction the pawn will move according to each color :
        // There is a bug where the black pawns move one less time than what they should while the white ones move one extra time , the bug is in the nature of the MoveSteps() from PawnMover but i will just fix it by adding +1 for black pawns ,at the call of the method , and -1 for the white ones. 
        Debug.Log("iam ready to  move the pawn:");

        if (pawn.GetComponent<Renderer>().material.color == Color.black)
        {
          Debug.Log("iam gonna move!");
          PawnMover.Pn.MoveFromTo(NetworkManager.I.start, NetworkManager.I.end);
          remainingMoves--; // once i make a move i immediately decreace the remaining moves state variable.
          //settingPawnsToClickableOrUnClickable();
          PawnMover.Pn.pawnHasReachedItsFinalDestination = false;
          yield return new WaitUntil(() => PawnMover.Pn.pawnHasReachedItsFinalDestination == true);
        }
        else if (pawn.GetComponent<Renderer>().material.color == Color.white)
        {
          Debug.Log("iam gonna move!");
          PawnMover.Pn.MoveFromTo(NetworkManager.I.start, NetworkManager.I.end);
          remainingMoves--; // once i make a move i immediately decreace the remaining moves state variable.
          //settingPawnsToClickableOrUnClickable(); // Now that i moved the pawn i set clickable to false
          PawnMover.Pn.pawnHasReachedItsFinalDestination = false;
          yield return new WaitUntil(() => PawnMover.Pn.pawnHasReachedItsFinalDestination == true);
        }
      }
      diceSelectionPopUpUI.proceed.interactable = false;
    }
    //
    // When the player uses his last move the remainingMoves variable will immdediately be updated and the turn will end without leaving any space for bugs :

  }

  public IEnumerator HandleAttachingSelectionToAPawnAfterLiberty(Pawn pawn)
  {
    if (pawn.clickable == true)
    {
      SetAllPawnsClickable(false);
      if (NetworkManager.I.pawnDidntMoveAtServer)
      {
        GameManager.Gm.moveInProgress = false; // i also want to set this state variable here.
        //settingPawnsToClickableOrUnClickable(); // setting them back to unclickable.
        Debug.Log("pawn didnt move iam going to set the state variable of the dice result i used back to false");
        NetworkManager.I.pawnDidntMoveAtServer = false;
        // I need to say somehow to my code that the server didnt approve of the move so i need to undo the state variables regarding the choosing of a certain dice result :
        if (diceSelectionPopUpUI.buttonText1.text == currentMoveSteps.ToString())
        {
          dice1Uses--;
          int value = Math.Clamp(dice1Uses, 0, 2); // this way i wont let dice1Uses reach values that are not logical for my game.

        }
        else if (diceSelectionPopUpUI.buttonText2.text == currentMoveSteps.ToString())
        {
          dice2Uses--;
          int value = Math.Clamp(dice2Uses, 0, 2); // this way i wont let dice2Uses reach values that are not logical for my game.

        }
        CheckingWetherToEndOrContinueTheTurn();
      }
      else
      {
        // Deciding in what direction the pawn will move according to each color :
        Debug.Log("iam ready to  move the pawn:");

        if (pawn.GetComponent<Renderer>().material.color == Color.black)
        {
          Debug.Log("iam gonna move!");
          PawnMover.Pn.MoveFromTo(NetworkManager.I.start, NetworkManager.I.end);
          remainingMoves--; // once i make a move i immediately decreace the remaining moves state variable.
          //settingPawnsToClickableOrUnClickable();
          PawnMover.Pn.pawnHasReachedItsFinalDestination = false;
          yield return new WaitUntil(() => PawnMover.Pn.pawnHasReachedItsFinalDestination == true);
        }
        else if (pawn.GetComponent<Renderer>().material.color == Color.white)
        {
          Debug.Log("iam gonna move!");
          PawnMover.Pn.MoveFromTo(NetworkManager.I.start, NetworkManager.I.end);
          remainingMoves--; // once i make a move i immediately decreace the remaining moves state variable.
          //settingPawnsToClickableOrUnClickable(); // Now that i moved the pawn i set clickable to false
          PawnMover.Pn.pawnHasReachedItsFinalDestination = false;
          yield return new WaitUntil(() => PawnMover.Pn.pawnHasReachedItsFinalDestination == true);
        }
      }
      diceSelectionPopUpUI.proceed.interactable = false;
      NetworkManager.I.iamFree = false;
    }
  }


  //
  // Handling an event that i produce on the NetworkManager :
  //
  private void HandleDiceResultsFromServer(int[] arr)
  {
    // here iam gonna update the GUI based on the dice results i got from the server:
    if (arr[0] == arr[1])
    {
      // gia diplh zaria :
      dice1MaxUses = 2;
      dice2MaxUses = 2;
      remainingMoves = 4;
      DiceSelectionUI.Ds.buttonText1.text = arr[0].ToString();
      DiceSelectionUI.Ds.buttonText2.text = arr[1].ToString();
    }
    else
    {
      dice1MaxUses = 1;
      dice2MaxUses = 1;
      remainingMoves = 2;
      DiceSelectionUI.Ds.buttonText1.text = arr[0].ToString();
      DiceSelectionUI.Ds.buttonText2.text = arr[1].ToString();
    }

  }

  //
  // Functionality to avoid duplicate code :
  //

  private void settingPawnsToClickableOrUnClickable()
  { // be careful when to use this one !
    List<GameObject> pawns = new List<GameObject>();
    //activePawnsA is still a list of gameobjects and not a list of pawns.
    pawns.AddRange(activePawnsA);
    pawns.AddRange(activePawnsB);
    foreach (var pawn in pawns)
    {
      Pawn pawnScript = pawn.GetComponent<Pawn>();
      if (pawnScript.clickable == true)
      {
        pawnScript.clickable = false;
      }
      else
      {
        pawnScript.clickable = true;
      } // i need to remove the pawn game object from the list once it reaches home so that i wont interruct with its clickable filed .    
    }
  }

  private void EndTurn()
  {
    //Setting everything back to their initial states:
    Debug.Log("resetting to initial state");
    dice1Uses = 0;
    dice2Uses = 0;
    diceSelectionPopUpUI.dice1_selection.interactable = true;
    diceSelectionPopUpUI.dice2_selection.interactable = true;
    remainingMoves = 0;
    currentMoveSteps = 0;
    diceHasBeenRolled = false;
    DiceSelectionUI.Ds.proceedHasBeenPressed = false;
    // In the end iam contacting the server to say that the current player has ended his turn :
    //NetworkManager.I.SendJsonMessage(new ClientMessage { type = "player_has_ended_his_turn" }); // type field of data class ClientMessage is type of string .
    NetworkManager.I.SendJsonMessage(new EndTurnMessage
    {
      type = "end_turn",
      userId = LocalPlayer.UserId
    });


    dicePopUpUI.window.SetActive(true);
  }
  private void continueTheTurn()
  {
    // The same player continues:
    diceSelectionPopUpUI.window.SetActive(true);
    Debug.Log("Loooooook:");
    Debug.Log("dice1Uses:" + dice1Uses);
    Debug.Log("dice2Uses:" + dice2Uses);
    DiceSelectionUI.Ds.proceedHasBeenPressed = false;
    // By using the state variables i will activate whatever dice result button i havent used yet!(very clever): :
    diceSelectionPopUpUI.dice1_selection.interactable = dice1Uses < dice1MaxUses;
    diceSelectionPopUpUI.dice2_selection.interactable = dice2Uses < dice2MaxUses;
  }

  private void SetAllPawnsClickable(bool value)
  {
    List<GameObject> pawns = new List<GameObject>();
    pawns.AddRange(activePawnsA);
    pawns.AddRange(activePawnsB);

    foreach (var pawn in pawns)
    {
      Pawn pawnScript = pawn.GetComponent<Pawn>();
      pawnScript.clickable = value;
    }
  }

  public void CheckingWetherToEndOrContinueTheTurn()
  {
    if (remainingMoves <= 0)
    {
      EndTurn(); // setting everything back to each original state so the startTurn() can run for the other player after contacting the server.
    }
    else
    {
      continueTheTurn();
    }
  }


}



