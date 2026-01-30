using UnityEngine;

public class Dropper : MonoBehaviour
{

    [SerializeField] float timeTillFall = 2f;
    MeshRenderer mymeshRenderer;
    Rigidbody myrigidbody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mymeshRenderer = GetComponent<MeshRenderer>();
        myrigidbody = GetComponent<Rigidbody>();
        mymeshRenderer.enabled = false;
        myrigidbody.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(Time.time);
        if (Time.time > timeTillFall)
    {
            mymeshRenderer.enabled = true;
            myrigidbody.useGravity = true;
    }
    }
}
