using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level")]
    [SerializeField] private string levelName = "Primer destello";
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private string nextSceneName;

    [SerializeField] private LuxController player;
    [SerializeField] private Transform levelStart;
    [SerializeField] private Transform levelEnd;
    [SerializeField] private bool useCheckpoints = true;
    [SerializeField] private float respawnDelay = 0.45f;

    private Vector3 checkpoint;
    private int attempts = 1;
    private int crystals;
    private bool completed;
    private bool paused;

    public string LevelName => levelName;
    public int LevelIndex => levelIndex;
    public int Attempts => attempts;
    public int Crystals => crystals;
    public bool Completed => completed;
    public bool IsPaused => paused;
    public int Score
    {
        get
        {
            int progressScore = Mathf.RoundToInt(Progress01 * 1000f);
            int crystalScore = crystals * 100;
            int attemptPenalty = Mathf.Max(0, attempts - 1) * 25;
            int flawlessBonus = completed && attempts == 1 ? 250 : 0;
            return Mathf.Max(0, progressScore + crystalScore + flawlessBonus - attemptPenalty);
        }
    }

    public float Progress01
    {
        get
        {
            if (player == null || levelStart == null || levelEnd == null)
            {
                return 0f;
            }

            float total = Mathf.Max(1f, levelEnd.position.x - levelStart.position.x);
            return Mathf.Clamp01((player.transform.position.x - levelStart.position.x) / total);
        }
    }

    private void Awake()
    {
        Instance = this;

        if (levelStart != null)
        {
            checkpoint = levelStart.position;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnCrystalCollected += HandleCrystalCollected;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnCrystalCollected -= HandleCrystalCollected;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            LoadMainMenu();
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        if (useCheckpoints)
        {
            checkpoint = position;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadNextLevel()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        LoadMainMenu();
    }

    private void HandlePlayerDied()
    {
        if (!completed)
        {
            Invoke(nameof(RespawnPlayer), respawnDelay);
        }
    }

    private void HandleCrystalCollected()
    {
        crystals++;
    }

    private void HandleLevelCompleted()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        if (player != null)
        {
            player.Freeze();
        }

        string completeKey = $"ShadowBeat_Level_{levelIndex}_Complete";
        string scoreKey = $"ShadowBeat_Level_{levelIndex}_BestScore";
        PlayerPrefs.SetInt(completeKey, 1);
        PlayerPrefs.SetInt(scoreKey, Mathf.Max(PlayerPrefs.GetInt(scoreKey, 0), Score));
        PlayerPrefs.SetInt("ShadowBeat_UnlockedLevel", Mathf.Max(PlayerPrefs.GetInt("ShadowBeat_UnlockedLevel", 1), levelIndex + 1));
        PlayerPrefs.Save();
    }

    private void RespawnPlayer()
    {
        if (completed || player == null || levelStart == null)
        {
            return;
        }

        attempts++;
        player.Respawn(useCheckpoints ? checkpoint : levelStart.position);
    }

    private void TogglePause()
    {
        if (completed)
        {
            return;
        }

        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }
}
