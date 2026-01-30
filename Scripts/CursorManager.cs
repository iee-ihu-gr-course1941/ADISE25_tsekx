using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public Texture2D cursorTexture; // this is where i add the photo of the cursor. I also need to alter some of the options of the image that iam going to use as a cursor like the transparency and the RGBA32.
    public Vector2 hotSpot = Vector2.zero; // the point where i click with my cursor.
    public CursorMode cursorMode = CursorMode.Auto; // with this i let unity define the cursor its way.

    void Start()
    {
        SetCursor(); 
    }

    public void SetCursor()
    {   
        // Cursor is a static class that unity provides that contains all the functionality to implement a cursor :
        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; // cursor wont be locked at the center of the screen.
    }

    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    } // iam not using it yet.
}