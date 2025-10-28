using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Start()
    {
        // Ensure cursor is always visible and free
        SetCursorState(true);
    }
    
    void Update()
    {
        // Force cursor to stay visible (in case something tries to hide it)
        if (!Cursor.visible)
        {
            SetCursorState(true);
        }
    }
    
    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}