using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string selectedLevelSceneName = "Level01_PrimerDestello";
    [SerializeField] private string selectedLevelLabel = "1. Primer destello";
    [SerializeField] private Text selectedLevelText;
    [SerializeField] private Text statsText;
    [SerializeField] private Text selectedShapeText;

    private static readonly string[] ShapeLabels = { "Cubo", "Diamante", "Esfera", "Piramide" };

    private void Awake()
    {
        RefreshMenuText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            PlaySelectedLevel();
        }
    }

    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void SelectLevel(string sceneName, string levelLabel)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        selectedLevelSceneName = sceneName;
        selectedLevelLabel = levelLabel;
        RefreshMenuText();
    }

    public void PlaySelectedLevel()
    {
        if (!string.IsNullOrWhiteSpace(selectedLevelSceneName))
        {
            LoadLevel(selectedLevelSceneName);
        }
    }

    public void SelectLuxShape(int shapeIndex)
    {
        int safeShapeIndex = Mathf.Clamp(shapeIndex, 0, ShapeLabels.Length - 1);
        PlayerPrefs.SetInt("ShadowBeat_LuxShape", safeShapeIndex);
        PlayerPrefs.Save();
        RefreshMenuText();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void RefreshMenuText()
    {
        if (selectedLevelText != null)
        {
            selectedLevelText.text = $"Nivel seleccionado: {selectedLevelLabel}";
        }

        if (statsText != null)
        {
            int unlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt("ShadowBeat_UnlockedLevel", 1), 1, 7);
            int completedLevels = 0;
            int bestScore = 0;

            for (int i = 1; i <= 7; i++)
            {
                if (PlayerPrefs.GetInt($"ShadowBeat_Level_{i}_Complete", 0) == 1)
                {
                    completedLevels++;
                }

                bestScore = Mathf.Max(bestScore, PlayerPrefs.GetInt($"ShadowBeat_Level_{i}_BestScore", 0));
            }

            statsText.text = $"Estadisticas:\nMejor score: {bestScore}\nNiveles completos: {completedLevels}/7\nNivel desbloqueado: {unlockedLevel}";
        }

        if (selectedShapeText != null)
        {
            int shape = Mathf.Clamp(PlayerPrefs.GetInt("ShadowBeat_LuxShape", 0), 0, ShapeLabels.Length - 1);
            selectedShapeText.text = $"Forma: {ShapeLabels[shape]}";
        }
    }
}
