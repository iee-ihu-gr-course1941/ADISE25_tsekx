using UnityEngine;
using UnityEngine.UI;
using System;

public class StartGameUI : MonoBehaviour
{  
    public GameObject window;
    public Button startButton;
    public void Show()
  {
    window.SetActive(true);
  }

  public void Hide()
  {
    window.SetActive(false);
  }
}
