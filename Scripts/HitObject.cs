using UnityEngine;

public class HitObject : MonoBehaviour
{
  private void OnCollisionEnter(Collision collision)
  {
    GetComponent<MeshRenderer>().material.color = Color.red;
    //Debug.Log("Something hit me");
  }
}
