using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DiceSelectionUI : MonoBehaviour
{
  public static DiceSelectionUI Ds ;
  public GameObject window;
  public Button dice1_selection;
  public TMP_Text buttonText1; // the text iam using if of type TMP_Text and not Text . i was getting a null reference error due to this. 
  public Button dice2_selection;
  public TMP_Text buttonText2;
  public Button proceed;
  public bool proceedHasBeenPressed=false;
  public event Action OnDiceSelectionHappened;

  public void Awake()
  {
    Ds=this;
    proceed.interactable=false;

  }
  public void Show()
  {
    window.SetActive(true);
  }

  public void Hide()
  {
    window.SetActive(false);
  }

}


// i need to add a vertical layout group comonent at the main canvas ui object that contains the buttons in order to see both the buttons so that the one wont cover the other.