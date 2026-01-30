using UnityEngine;
using System.Collections;
using System;
using UnityRandom = UnityEngine.Random; // i was getting an error since there is a UnityEngine.Random and a System.Random

public class DiceController : MonoBehaviour
{
  // The code thats actually gonna use the functionality that the dice c# script gives to each dice game element.

  // global variables
  public Dice[] diceArray;
  public Transform rollSpawnPoint; // place where the rolling begins.
  public AudioSource rollSound; // sound for the dice (WOW)
  public AudioClip rollClip;
  public ParticleSystem settleParticles;
  public DiceSelectionUI diceSelectionUI;
  
  void Start()
  {
  
  }
  public void RollAll()
  {
    StartCoroutine(RollRoutine()); // RollRoutine is gonna run as a routine due to the startCoroutine.
  }
  IEnumerator RollRoutine()
  {
    // Firstly iam gonna lift them up as an extra animation
    foreach(var d in diceArray)
    {
      d.rb.isKinematic=false;
      d.transform.position= rollSpawnPoint != null ? rollSpawnPoint.position + UnityRandom.insideUnitSphere * 0.2f : d.transform.position + Vector3.up * 0.3f; // thats the statement thats actually doing the lift
    }
    if(rollSound != null && rollClip != null)
      rollSound.PlayOneShot(rollClip);
    
    foreach(var d in diceArray)
    {
      d.Roll(); // the most important part where iam using the Roll() functionality from the monobehaviour script thats attached to each dice game object.
      yield return new WaitForSeconds(0.08f); // i can also return something each time iam waiting for the next frame !
    }
    // The part where iam waiting and checking for all dices to stop rolling:
    bool anyRolling = true;
    while(anyRolling)
    {
      anyRolling=false; // once i enter the while loop i turn the variable that matters to false and once i make sure nothing is rolling through the state variable isRolling that every dice has , i will turn it back to true to escape the while loop
      foreach(var d in diceArray)
      {
        if(d.isRolling){anyRolling=true;break;}
      }
      yield return null;
    }
   // Once i make sure nothing is rolling i will add each result from the dices:
   int sum=0;
   foreach(var d in diceArray)
    {
      sum += d.Result;

    }
    //Debug.Log("Dices have stopped rolling , Result :" + sum ); doesnt work well so i wont use it , i also wont delete the code that produce this result for legacy reasons and because the code was actually difficult to type...
    // then right here i can trigger whatever logic i want , like a certain event.
  }

}
