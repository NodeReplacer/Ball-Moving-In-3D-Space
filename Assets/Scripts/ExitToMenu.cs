using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitToMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Quit the application
#if UNITY_EDITOR
        // If running in the Unity Editor, stop the Play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // If running in a standalone build, quit the application
            Application.Quit();
#endif
        }
    }
}
