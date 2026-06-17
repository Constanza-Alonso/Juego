using UnityEngine;

public class LevelSelectButton : MonoBehaviour
{
    [SerializeField] private MainMenuController menuController;
    [SerializeField] private string sceneName;

    public void LoadAssignedLevel()
    {
        if (menuController != null && !string.IsNullOrWhiteSpace(sceneName))
        {
            menuController.LoadLevel(sceneName);
        }
    }
}
