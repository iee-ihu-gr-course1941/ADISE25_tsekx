using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using System.Linq;


//
// This script will use the moving functionality that exists in the pawn script thats connected to each pawn prefab on the board :
//
public class PawnMover : MonoBehaviour
{
  public Pawn pawn; // the pawn thats gonna perform the move.
  public List<Vector3> tiles; // The 24 possible positions , repressenting the 24 rows , for the pawn to move.
  public float stepDelay = 0.08f;
  public event Action<int> OnStep; // an event to happen each time i perform a step . i dont think i will need it too
  public event Action onComplete; // didnt use it yet.
  public int currentTileIndex; // The tile from which i want the pawn to start moving. (iam updating it at the networkmanager)
  private Coroutine movementCoroutine; // Here i will save the process of moving(meaning the method MoveStepsRoutine) and i will interact with the move of the pawn itself through this variable through out the whole code of th PawnMover script.
  public static PawnMover Pn; // i wanna use it inside of the ClickHandler to assign the pawn that iam clicking with the cursor .
  public int direction = 0; // setting the direction so white pawns can move one way and black pawns can move the other way .
  public bool pawnHasReachedItsFinalDestination = false; // i dont think i will need it but anyways...

  // Initialization part :
  void Awake()
  {
    Pn = this; // I want the PawMover variable to point to this exact PawnMover script thats attached to an empty game object . if i type " new PawnMover() " , i will create a PawnMover object thats not attached to anything and thats useless in Unity programming .
    if (pawn = null) pawn = GetComponentInChildren<Pawn>();
    if (tiles == null) tiles = new List<Vector3>();
  }

  //
  // MoveSteps(steps) starts the MoveStepsRoutine(steps) as a Coroutine and that MoveStepsRoutine(steps) contains the DoStepTo(targetPosition) 
  // that runs for a pawn as many times as the steps argument i'll give . 
  //

  //2.1)
  public void MoveFromTo(int startIndex, int targetIndex)
  {
    //Debug.Log("Gonna move pawn from" + startIndex + "to" + targetIndex);
    if (movementCoroutine != null)
      StopCoroutine(movementCoroutine);

    movementCoroutine = StartCoroutine(
        MoveFromToRoutine(startIndex, targetIndex)
    );
  }
  //2.2)
  private IEnumerator MoveFromToRoutine(int startIndex, int targetIndex)
  {
    if (pawn == null || tiles.Count == 0)
      yield break;

    currentTileIndex = startIndex;

    int dir = targetIndex > startIndex ? 1 : -1;

    while (currentTileIndex != targetIndex - 1)
    {
      // computing next tile index that iam gonna move to:
      int nextIndex = currentTileIndex + dir;

      Vector3 targetPos = tiles[nextIndex]; // defining the position of the tile that iam gonna move to
      bool stepDone = false; // gonna turn it to true once iam done with the step.

      StartCoroutine(
          pawn.DoStepTo(
              targetPos,
              _ => stepDone = true
          ) // run the pawn.DoStepTo(targetPosition) asynchronously.
      );
      //waiting for the step animation to be done since the DoStepTo() functionality runs asynchronously as a coroutine:
      while (pawn.isMoving || !stepDone)
        yield return null;

      currentTileIndex = nextIndex; // updating the global variable current index for the next step .
      yield return new WaitForSeconds(stepDelay);
    }
    //pawnHasReachedItsFinalDestination = true;
    onComplete?.Invoke();
  }
  //.2)
  public void TeleportToTile(int tileIndex) // tileIndex is the tile that i want my pawn to end up at.
  {
    if (tileIndex < 0 || tileIndex >= tiles.Count) return;
    currentTileIndex = tileIndex;
    Vector3 t = tiles[currentTileIndex];
    pawn.transform.position = new Vector3(t.x, pawn.transform.position.y, t.z);
  } // i instatly teleport the pawn to the final tile without any animation happening. I didnt use it though


  //
  // Rest of the methods :
  //
  public void CancelMovement() // with this iam canceling the whole movement 
  {
    if (movementCoroutine != null) StopCoroutine(movementCoroutine);
    movementCoroutine = null; // setting the state variable to null so this way iam saying there is no move happening for a pawn.
  }
  //
  public void SelectPawn(Pawn selectedPawn)
  {
    pawn = selectedPawn;
    //pawn.clickable=true; // thanks to this my pawns where always moving . quite a bug.
    Debug.Log("Pawn selected!");
  }
  // 

  /*
  public void OnClicked(Pawn pawn,int numberOfSteps)
  {
    // Since iam passing the pawn that i clicked with raycast as a parameter , that means i can proceed to the fields of a Pawn script thats connected to a real pawn prefab on the scene :
    if (pawn.clickable)
    { 
      Debug.Log("I got inside clickable");
      MoveSteps(numberOfSteps);      
    }
  }
  */
  public void ArrangePawnsAtTarget(PawnStats pawnAfter, List<PawnStats> pawnsAtTarget)
  {
    Debug.Log("Iam starting the rearrangement!");

    if (NetworkManager.I.enemyPawn != null && NetworkManager.I.enemyPawn.state == "captured")
    {
      Debug.Log("the color of the enemy pawn iam gonna add at a capturedList:" + NetworkManager.I.enemyPawn.color);
      captureEnemyPawn(pawnAfter, pawnsAtTarget, new Vector3(2f, 30f, 2f));
      //return; // i already captured it and updated the gui i dont want to continue any further.
    }
    // 2. Rearrange all pawns at the target tile according to stackOrder
    // pawnsAtTarget includes the pawnAfter too
    var pawnsSorted = pawnsAtTarget.OrderBy(p => p.stackOrder).ToList(); // OrderBy is an extension method that needs System.Linq to be imported.

    for (int i = 0; i < pawnsSorted.Count; i++)
    {
      PawnStats pStat = pawnsSorted[i];

      // Find the actual gameobject thats connected to the Pawn script that contains the index field data i inserted in it from the initial data : 
      GameObject gameObject = FindPawnById(pStat.id); // FindPawnById() returns the Gameobject that contains the pawn script.
      if (gameObject == null) continue;

      // Iam interested in taking the updated 3d positions from the server and pass them to the game objects representing the pawns in the GUI :
      Vector3 tilePos = pawnsSorted[i].position3D.ToVector3();
      // Teleport the pawn instantly on the GUI:
      gameObject.transform.position = tilePos;
      gameObject.GetComponent<Pawn>().pos = tilePos;
    }
  }

  /// <summary>
  /// Utility to find the Pawn component in the scene by its ID
  /// </summary>
  public GameObject FindPawnById(string pawnId)
  {
    foreach (var p in GameManager.Gm.activePawnsA)
    {
      Pawn pawnScript = p.GetComponent<Pawn>();
      if (pawnScript.index.ToString() == pawnId)
        return p;
    }
    // This is why the functionality werent working for black pawns , i was interracting at all with the game objects that represent the black pawns in the GUI :
    foreach (var p in GameManager.Gm.activePawnsB)
    {
      Pawn pawnScript = p.GetComponent<Pawn>();
      if (pawnScript.index.ToString() == pawnId)
        return p;
    }
    return null;
  }

  public void captureEnemyPawn(PawnStats pawnAfter, List<PawnStats> pawnsAtTarget, Vector3 capturedStackPosition)
  {
    Debug.Log("I entered the captureEnemyPawnFunctionality");
    // I assume that the pawnsAtTarget contains just the enemy pawn , So iam taking it away from the activepawns list :
    //PawnStats capturedPawn = pawnsAtTarget.FirstOrDefault(p => p.state == "captured");
    PawnStats capturedPawn = NetworkManager.I.enemyPawn;
    if (capturedPawn == null) return;
    Debug.Log($"Captured pawn color: {capturedPawn.color}"); // template literal but for c# not javascript.
                                                             // If there is a captured pawn iam adding it to the logic list :

    // Iam ordering the active pawns without the captured one:
    //List<PawnStats> activePawns = pawnsAtTarget.Where(p => p.state != "captured").OrderBy(p => p.stackOrder).ToList();
    //Debug.Log("The pawns that remain in the active list" + activePawns.Count);

    // If there is a captured pawn i place the gameobject of it in the GUI on a stack :

    GameObject capturedGO = FindPawnById(capturedPawn.id); // the pawn i will capture
    if (capturedGO == null) return;

    // Creating a list variabled to save the list with the captured pawns i will work with:
    List<GameObject> capturedList;
    float zOffset;

    if (capturedPawn.color == "white")
    {
      capturedList = GameManager.Gm.capturedA; // from now on capturedList points to the list for the captured pawns
      zOffset = +10f;   // white side
    }
    else
    {
      capturedList = GameManager.Gm.capturedB;
      zOffset = -40f;   // black side
    }
    Debug.Log("What the capturedA or B contains :" + capturedList);
    // I need to actually add the captured pawn in the list that represents the captured pawns(game objects) at prison.
    if (!capturedList.Contains(capturedGO))
      capturedList.Add(capturedGO);
    // placing the captured pawns on a stack :
    float yStep = 4f; // distance between captured pawns
    int stackIndex = capturedList.Count; // increasing the index for the capturedpawns list each time i add a captured pawn.
    Vector3 capturedPos = capturedStackPosition + new Vector3(0, yStep * stackIndex, zOffset); // creating the position i will place the captured pawn in the GUI.
    //Moving the captured pawn to prison :
    Debug.Log("Now iam gonna place it in the GUI:");
    capturedGO.transform.position = capturedPos;
    capturedGO.GetComponent<Pawn>().pos = capturedPos;
    // iam deleting the captured pawn from the list of active pawns , so iam updating the active lists:
    GameManager.Gm.activePawnsA.Remove(capturedGO);
    GameManager.Gm.activePawnsB.Remove(capturedGO);

    // iam moving the pawn that captured the enemy one at the tile:
    GameObject movingGO = FindPawnById(pawnAfter.id); // the pawn i will move 
    if (movingGO != null)
    {
      Vector3 targetPos = pawnAfter.position3D.ToVector3(); // taking the position from the server data.

      movingGO.transform.position = targetPos;
      movingGO.GetComponent<Pawn>().pos = targetPos;
    }

  }
}
