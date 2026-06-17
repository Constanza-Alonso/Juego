using UnityEngine;

public class LevelSelectButton : MonoBehaviour
{
    [SerializeField] private MainMenuController menuController;
    [SerializeField] private string sceneName;
    [SerializeField] private string levelLabel;

    public void LoadAssignedLevel()
    {
        if (menuController != null && !string.IsNullOrWhiteSpace(sceneName))
        {
            menuController.LoadLevel(sceneName);
        }
    }

    public void SelectAssignedLevel()
    {
        if (menuController != null && !string.IsNullOrWhiteSpace(sceneName))
        {
            menuController.SelectLevel(sceneName, string.IsNullOrWhiteSpace(levelLabel) ? sceneName : levelLabel);
        }
    }
}
