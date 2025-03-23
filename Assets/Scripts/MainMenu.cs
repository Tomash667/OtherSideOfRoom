using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button level2Button, level3Button;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;

        GameData gameData = SaveLoadManager.Load();
        level2Button.interactable = gameData.level2Unlocked;
        level3Button.interactable = gameData.level3Unlocked;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Q))
            UnityEditor.EditorApplication.isPlaying = false;
#else
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit(0);
#endif
    }

    public void StartGame(int level)
    {
        SceneManager.LoadScene(level);
    }
}
