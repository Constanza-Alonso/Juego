using UnityEngine;
using UnityEngine.UI;

public class ShadowBeatUI : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Text progressText;
    [SerializeField] private Text crystalsText;
    [SerializeField] private Text attemptsText;
    [SerializeField] private Text levelNameText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject completedPanel;
    [SerializeField] private GameObject pausePanel;

    private void OnEnable()
    {
        GameEvents.OnLevelCompleted += ShowCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCompleted -= ShowCompleted;
    }

    private void Update()
    {
        if (levelManager == null)
        {
            return;
        }

        float progress = levelManager.Progress01;
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        if (crystalsText != null)
        {
            crystalsText.text = $"Cristales: {levelManager.Crystals}";
        }

        if (attemptsText != null)
        {
            attemptsText.text = $"Intentos: {levelManager.Attempts}";
        }

        if (levelNameText != null)
        {
            levelNameText.text = levelManager.LevelName;
        }

        if (scoreText != null)
        {
            scoreText.text = $"Puntos: {levelManager.Score}";
        }

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(levelManager.IsPaused);
        }
    }

    private void ShowCompleted()
    {
        if (completedPanel != null)
        {
            completedPanel.SetActive(true);
        }
    }
}
