using System;
using UnityEngine;
using UnityEngine.UI;

public class DicePopUpUI : MonoBehaviour
{
    public GameObject window; // i have connected the DicePoPup window game object
    public Button rollButton; // i have connected it with button thats under the DicePopUp window game object.
    
  public void Show()
  {
    window.SetActive(true);
  }

  public void Hide()
  {
    window.SetActive(false);
  }


}
