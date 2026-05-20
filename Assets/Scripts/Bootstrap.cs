using UnityEngine;

/// <summary>
/// Attach to an empty GameObject named "_Bootstrap" in the Bootstrap scene.
/// Kickstarts the persistent Managers and transitions immediately to the Lobby.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    private void Start()
    {
        // Singletons on _GameManager (GameManager, MoraleSystem, SceneLoader)
        // initialize in their own Awake(). Bootstrap just kicks to Lobby.
        SceneLoader.GoToLobby();
    }
}
