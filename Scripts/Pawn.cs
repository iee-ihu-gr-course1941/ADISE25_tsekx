using UnityEngine;
using System.Collections;
using System; // i was getting a bug because i dont have an automation system that set ups the imports. And i was missing this one for the IEnumarator type.

public class Pawn : MonoBehaviour
{
    // the properties of a prefab pawn:
    public string ownderId;
    public int index; // identification to know to what pawn iam refering to , its position wont be stored in the front-end in order to have control over its position only at the server ! I'll be using this index to send requests for the specific pawn to the server.
    public Vector3 pos; 
    public bool clickable = false; // i will turn this to true everytime i want to move the actual pawn thats attached to this c# script .

    // initiating of the properties of the component Pawn thats always attached to game objects coming  out of the prefab pawn:
    public void Init(string ownderId,int index)
  {
    this.ownderId = ownderId;
    this.index=index;
    //this.pos = pos;
  }
   // maybe i will need a method to set the position of each pawn inside of the Pawn script:
   public void SetPositionImmediate(Vector3 pos)
  {
    transform.position = pos; // depending on what position i'll give when i call this method through a game object that came out of a pawn prefab type of template .
  }



  //From now on is the Logic of stepping animation :
  public float stepDuration=0.35f; // time for performing each step.
  public float jumpHeight=0.35f; // height of the jump i perform.
  public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0,0,1,1); // controlling the curve thats gonna happen with the move animation. AnimationCurve is a type of object that defines a curve. EaseInOut(0,0,1,1) performs a slow start a fast mid move and a slow last one.
  public float scalePunch=0.12f;// slightly altering the size to produce some kind of effect
  public float scalePunchDuration=0.18f; // i didnt use it.
  public bool isMoving; // i wanna define a state where the pawn is moving so that another move wont overlap this one.
  public IEnumerator DoStepTo(Vector3 targetPosition,Action<int> onStepComplete=null,int stepIndex=0) // the parameters : (where i need the pawn to end up,an event to happen once the move has been made,starting from step zero) . This self-made method will come in handy as a coroutine ! 
  {
    if(isMoving)yield break; // in case a move is already happening i wanna stop the execution of the coroutine. (This way iam getting rid of alot of bugs...)
    isMoving=true; // if the isMoving was false , i escape the first yield break and now i want to set the isMoving to true once again !
    // Defining a start and a final position:
    Vector3 startPos = transform.position; // getting the current position of the pawn
    Vector3 endPos = new Vector3(targetPosition.x,startPos.y,targetPosition.z); // setting same y as starting position

    // An extra animation (this one works fine, i tested it):
    
    
    float elapsed = 0f;
    while (elapsed<stepDuration)
    {
      elapsed += Time.deltaTime;
      float t = Mathf.Clamp01(elapsed/stepDuration); // i perform a normalization inside of the parenthesis and Mathf.Clamp01 makes sure that the resulting number will be something between 0 and 1 .
      Vector3 horizontal = Vector3.Lerp(startPos,endPos,t); // this one controls the move only on the horizontal level (x,z).
      // moving on the vertical level:
      float v = heightCurve.Evaluate(t); 
      float vertical= v*jumpHeight;
      //final placement:
      transform.position=new Vector3(horizontal.x,startPos.y + vertical,horizontal.z);
      //altering the size of the object logic to create an extra effect:
      if (scalePunch > 0f)
      {
        float s = 1f+ Mathf.Sin(t*Mathf.PI)* scalePunch; // peak of size at mid
        //transform.localScale = Vector3.one*s; // actually altering the size of the game object (it keeps my pawn's size small error)
      }
      yield return null; // this whole thing inside of the while(true) is being executed with different values at each frame ! wow , the t is always gonna be something different each time since its value is determined by elapsed which is something inconsistent.
    }
    
    
    // finalize the state of the pawn:
    transform.position=endPos;
    //transform.localScale=Vector3.one; (it keeps my pawn's size small error)
    isMoving=false; // it doesnt move anymore.
    onStepComplete?.Invoke(stepIndex); // iam sending away the stepIndex data to anyone thats listening for an onStepComplete event.
  }
} // end of Pawn class


// 1.) Mathf.Clamp01() it basically sets a limit on the value with the max being 1 and min being 0 :
/*
float t = -0.5f;
t = Mathf.Clamp01(t); // t turns into 0

float t2 = 0.7f;
t2 = Mathf.Clamp01(t2); // t2 stays 0.7

float t3 = 1.5f;
t3 = Mathf.Clamp01(t3); // t3 turns into 1

*/

// 2.) Vector3.Lerp(start, end, t) if t is 0.2 i'll take the 20% of the distance between start and end positions.

// 3.) heightCurve.Evaluate(t) 
/*
  ^  height (output)
1 |        ●
  |     ●
0 |  ●
  +----------------→ t (input)
    0      0.5     1
the function will ask what is the height of t , and it will return the height ! it depends on the curve if its an easy-in-out or something else then the same t will have a different coresponding height.
*/

//4.) transform.localScale alters the size of the object

