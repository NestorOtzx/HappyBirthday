using UnityEngine;

public class GameExitHandler : MonoBehaviour
{
    // Call this public function from a UI Button or another script
    public void QuitGame()
    {
        // Closes the application in a standalone build
        Application.Quit();

        // Stops Play Mode if you are running in the Unity Editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif

        Debug.Log("Game is quitting...");
    }
}