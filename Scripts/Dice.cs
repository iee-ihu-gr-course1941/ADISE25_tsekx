using UnityEngine;
using System.Collections; // i was getting a bug because i dont have an automation system that set ups the imports. And i was missing this one for the IEnumarator type.


public class Dice : MonoBehaviour
{   // Here i will implement the physics of the dice.
   // global variables for this script:
   public Rigidbody rb; // component thats responsible for the dice's physics.
   public Transform[] faceMarkers = new Transform[6];// each element defines a side of the dice. The order that iam assigning each face with an index of the array faceMarkers determines the face's value , the last face that iam gonna assign is gonna be the value 6 for the dice.
   // variables for the random force & torque (ormh & stroformh) :
   public float minForce = 10f;
   public float maxForce = 100f;
   public float minTorque = 12f;
   public float maxTorque = 120f;
   // end of variables for force & torque.
   //
    public float settleVelocityThreshold = 0.08f;
    public float settleAngularThreshold = 0.08f;
    public float settleTime = 1.0f;
   //
   public bool isRolling = false; // a variables that determines the 2 different states of the dice
   public int Result = 0; // saves the outcome of the rolling (1-6)
   public System.Action<int> OnSettled; // the event 
   void Awake()
  {
    if(rb == null)
      rb = GetComponent<Rigidbody>();
    if(faceMarkers == null || faceMarkers.Length != 6)
      Debug.LogWarning("First set the 6 faceMakers in the inspector (one per face)"); 
  } // it contains optional functionality to set the dice in case i didnt do it myself.

   public void Roll()
  {
    //setting fields from the component rigidbody:
    rb.isKinematic=false;
    rb.linearVelocity=Vector3.zero;
    rb.angularVelocity=Vector3.zero;
    //Creating the force thats gonna actually push the dice:
    Vector3 force = (Vector3.up + new Vector3(Random.Range(-1f,1f),0f,Random.Range(-1f,1f)).normalized)*Random.Range(minForce,maxForce);
    //adding the force i just created to the rigidbody component of the dice:
    rb.AddForce(force,ForceMode.Impulse);
    //Creating the torque thats gonna rotate the dice:
    Vector3 torque=new Vector3(
      Random.Range(-1f,1f),
      Random.Range(-1f,1f),
      Random.Range(-1f,1f)
    ).normalized*Random.Range(minTorque,maxTorque);
    //adding the torque i just created to the rigidbody component of the dice:
    rb.AddTorque(torque,ForceMode.Impulse);
    //Beginning the coroutine which is basically code thats running in the background (in the same thread) without blocking the whole game :
    isRolling = true;
    StartCoroutine(WaitForSettle());  
  }
  
  
  IEnumerator WaitForSettle() // I use this as a coroutine 
  {
    //fields:
    float timer =0f;
    //in case of infinity roll i have the 2 below variables:
    float maxWait=10f;
    float elapsed=0f;
    //
    while (true) // an infinite loop that will be terminated with a break statement , obviously .
    {
      elapsed += Time.deltaTime; // the coroutine will happen per frame.
      if(rb.linearVelocity.magnitude<settleVelocityThreshold && rb.angularVelocity.magnitude < settleAngularThreshold) // if both linear and angular speed are below what i define as a state where the dice is stoped , then i procceed to the code inside thats gonna make the dice move :
      {
         timer+=Time.deltaTime;
         if(timer>=settleTime)break;
      }
      else
      {
        timer=0f;
      }
      if(elapsed>maxWait) // forcing the stopping of the WaitForSettle()
      {
        Debug.LogWarning("Dice:settle timeout, forcing read of face"); // face is the result which gonna appear on the dice.
        break;
      }
      yield return null; // stopping the execution here and wait for the next frame to start
    }
    // Now that iam done with the while(true) i will update some of the state variables that describe the dice's state:
    rb.linearVelocity=Vector3.zero;
    rb.angularVelocity=Vector3.zero;
    rb.isKinematic=true;
    isRolling=false;
    Result=DetermineTopFace();
    OnSettled?.Invoke(Result); // ima creating an event of type OnSettled so other eventlisteners that listen to this type of event will actually listen and execute their functionality.
    yield return null; 
  }
  

  int DetermineTopFace()
  {
    if (faceMarkers != null && faceMarkers.Length == 6)
    {
      int bestIndex=-1;
      float bestDot=-1f;
      for(int i = 0; i < faceMarkers.Length; i++)
      {
        Vector3 worldDir=faceMarkers[i].transform.up; // every object has a vector property and iam accessing it by using transform.up on a object in the graphical user interface.
        float dot = Vector3.Dot(worldDir,Vector3.up); // it returns an evaluation of how similar the direction for 2 vectors are. This way iam comparing the direction of one of the faces with a standard direction that looks straight in the sky. Whatver comes closer to the vector that looks in the sky is the face which containing the value that iam gonna use as a result for the rolling of the dice.
        if(dot>bestDot)
        {
          bestDot=dot; // updating the best new direction.
          bestIndex=i;
        }
      }
      // Now that i have the index of best direction i wanna extract the value of the dice:
      if(bestIndex>=0)return bestIndex+1; // the value for the dice after the rolling
    }
    return 0;
  }



}
