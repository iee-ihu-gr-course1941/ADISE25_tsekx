using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class ClickHandler : MonoBehaviour
{

  public static ClickHandler Ch;
  public event Action<Button> OnDiceButtonHasBeenClicked;
  public Button[] totalButtons; // i forgotten to actually create an object of type Button array and i was getting errors!
  public GraphicRaycaster[] raycasters;  // i need to se this one from the inspector window in order to catch the clicks in the ui.
  private EventSystem eventSystem; // here i will save the eventsystem thats being generated with the scene !


  void Awake()
  {
    totalButtons = new Button[2];
    Ch = this;
    eventSystem = EventSystem.current; // actually assigning the eventsystem from the scene to the local variable of type eventsystem.
    raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
    if (eventSystem == null)
      Debug.Log("No EventSystem found in the scene!");
  }
  void Update() // i want the click logic to happen at each frame :
  {
    if (Input.GetMouseButtonDown(0)) // checking if the left click of the mouse has been clicked for a certain frame. The GetMouseButtonDown returns true for a certain frame.
    {
      //
      // Getting a button from the scene , by clicking on it with the cursor :
      //
      PointerEventData pointer = new PointerEventData(eventSystem);
      pointer.position = Input.mousePosition;

      foreach (var raycaster in raycasters)
      {
        if (!raycaster.gameObject.activeInHierarchy) continue;
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointer, results);

        foreach (var r in results)
        {
          Button btn = r.gameObject.GetComponent<Button>();
          if (btn != null && btn.CompareTag("dice_result"))
          {
            if (!btn.interactable) return;
            Debug.Log("Clicked button: " + btn.name);
            // here i will define whatever i want to happen after i click on the button:
            OnDiceButtonHasBeenClicked?.Invoke(btn); // i want an event to happen.
            return; // iam stopping the click processing this way , way too important !
          }
        }
      }
      // Next i wont allow the click of a pawn if iam on a UI:
      if (EventSystem.current.IsPointerOverGameObject())
        return;
      //
      // Getting a pawn out of the scene , by clicking on it with the cursor :
      //
      Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // it creates a ray(desmh) that starts from the camera and connects to the place where the cursor clicked
      RaycastHit hit; // this type of variable stores details regarding the interaction/collision that happend with the cursor and a game object
      if (Physics.Raycast(ray, out hit)) // checking if i actually interracted with a game object , if i interacted Raycast() will return true
      {
        Pawn pawn = hit.collider.GetComponent<Pawn>(); // the RaycastHit type of object contains properties like hit.point(position where the collide happened at the 3d world), hit.normal , and hit.collider which is the game object that collided with the cursor in a way. And since i want it to work for pawns i try to aqcuire it with GetComponent<Pawn>().
        if (pawn != null)
        {
          var allCaptured = GameManager.Gm.capturedA.Concat(GameManager.Gm.capturedB);
          // ===== CHECK FOR CAPTURED PAWN =====
          if (GameManager.Gm.capturedA != null || GameManager.Gm.capturedB != null)
          {
            foreach (var capturedGO in allCaptured)
            {
              if (capturedGO == null) continue;

              Pawn capturedPawn = capturedGO.GetComponent<Pawn>();
              if (capturedPawn == null) continue;

              if (capturedPawn.index == pawn.index)
              {
                Debug.Log("Clicked captured pawn, releasing from prison");

                StartCoroutine(ReleaseAndMove(pawn));
                return;
              }
            }
          }

          // Normal logic for clicking :

          if (DiceSelectionUI.Ds.proceedHasBeenPressed == true && GameManager.Gm.diceHasBeenRolled == true && !GameManager.Gm.moveInProgress)
          {
            GameManager.Gm.moveInProgress = true;
            PawnMover.Pn.SelectPawn(pawn); // I save the pawn i clicked at to the PawnMover .
            StartCoroutine(GameManager.Gm.HandleAttachingSelectionToAPawn(pawn)); // HandleAttachingSelectionToAPawn() is moving the pawn i clicked at by using the PawnMover singleton .
            return; // iam stopping the click processing this way , way too important !
          }
        }

      }
    }
  }

  private IEnumerator ReleaseAndMove(Pawn pawn)
  {

    NetworkManager.I.SendJsonMessage(new ClientMessage { type = "free_pawn" + pawn.index.ToString() + "_by_moving_it" + GameManager.Gm.currentMoveSteps + "_indexes" });
    NetworkManager.I.iamFree = false;
    yield return new WaitUntil(() => NetworkManager.I.iamFree == true); // if the pawn move wont be approved from the server then the execution of the code will never pass this point ... so what do i do with CASE 4 ?
    if (DiceSelectionUI.Ds.proceedHasBeenPressed && GameManager.Gm.diceHasBeenRolled && !GameManager.Gm.moveInProgress)
    {
      GameManager.Gm.moveInProgress = true;
      PawnMover.Pn.SelectPawn(pawn);
      Debug.Log("starting the HandleAttachingSelectionToAPawn(pawn) for the captured pawn");
      yield return StartCoroutine(GameManager.Gm.HandleAttachingSelectionToAPawnAfterLiberty(pawn));
    }
  }

}
