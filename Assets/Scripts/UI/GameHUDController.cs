using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – In-Game HUD Controller
/// Animated score roll, crystal counter, combo multiplier, health/energy bar,
/// pause menu with smooth slide-in, and game-over / level-complete overlays.
///
/// === Prefab hierarchy ===
/// HUD (Canvas – Screen Space Overlay)
///   ├─ TopBar
///   │   ├─ ScoreLabel       (TMP)
///   │   ├─ CrystalCounter   (TMP + crystal icon)
///   │   ├─ ComboDisplay     (TMP – hidden when combo < 2)
///   │   └─ PauseButton      (Button)
///   ├─ EnergyBarRoot
///   │   ├─ EnergyFill       (Image – fillAmount)
///   │   └─ EnergyGlow       (Image – synced tint)
///   ├─ PausePanel           (CanvasGroup – slides in from top)
///   ├─ GameOverPanel        (CanvasGroup)
///   └─ WinPanel             (CanvasGroup)
/// </summary>
public class GameHUDController : MonoBehaviour
{
    // ── Score ─────────────────────────────────────────────────────────────────
    [Header("=== Score ===")]
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI scorePopupLabel;   // Floating +100 text
    [SerializeField] private RectTransform   scorePopupRect;
    [SerializeField] private float           scoreRollSpeed = 1800f; // points per second

    // ── Crystals ──────────────────────────────────────────────────────────────
    [Header("=== Crystals ===")]
    [SerializeField] private TextMeshProUGUI crystalLabel;
    [SerializeField] private RectTransform   crystalIconRect;
    [SerializeField] private ParticleSystem  crystalCollectFX;

    // ── Combo ─────────────────────────────────────────────────────────────────
    [Header("=== Combo ===")]
    [SerializeField] private CanvasGroup     comboGroup;
    [SerializeField] private TextMeshProUGUI comboLabel;
    [SerializeField] private Image           comboBarFill;
    [SerializeField] private float           comboDuration = 3f;   // seconds before combo resets

    // ── Energy bar ────────────────────────────────────────────────────────────
    [Header("=== Energy Bar ===")]
    [SerializeField] private Image   energyFill;
    [SerializeField] private Image   energyGlow;
    [SerializeField] private Color   energyHighColor  = new Color(0.3f, 1f, 0.8f);
    [SerializeField] private Color   energyLowColor   = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float   lowEnergyThresh  = 0.25f;

    // ── Pause ─────────────────────────────────────────────────────────────────
    [Header("=== Pause Panel ===")]
    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private RectTransform pausePanelRect;
    [SerializeField] private Button      resumeButton;
    [SerializeField] private Button      restartButton;
    [SerializeField] private Button      mainMenuButton;
    [SerializeField] private Button      pauseButton;
    [SerializeField] private Slider      musicSlider;
    [SerializeField] private Slider      sfxSlider;

    // ── Game Over ─────────────────────────────────────────────────────────────
    [Header("=== Game Over Panel ===")]
    [SerializeField] private CanvasGroup     gameOverPanel;
    [SerializeField] private TextMeshProUGUI goFinalScoreLabel;
    [SerializeField] private TextMeshProUGUI goBestScoreLabel;
    [SerializeField] private Image[]         goStarImages;
    [SerializeField] private Sprite          goStarFilled;
    [SerializeField] private Sprite          goStarEmpty;
    [SerializeField] private Button          goRetryButton;
    [SerializeField] private Button          goMenuButton;

    // ── Win ───────────────────────────────────────────────────────────────────
    [Header("=== Win Panel ===")]
    [SerializeField] private CanvasGroup     winPanel;
    [SerializeField] private TextMeshProUGUI winScoreLabel;
    [SerializeField] private TextMeshProUGUI winBestLabel;
    [SerializeField] private Image[]         winStarImages;
    [SerializeField] private ParticleSystem  winConfettiFX;
    [SerializeField] private Button          winNextButton;
    [SerializeField] private Button          winMenuButton;

    // ── Audio ─────────────────────────────────────────────────────────────────
    [Header("=== Audio ===")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip   crystalSFX;
    [SerializeField] private AudioClip   comboSFX;
    [SerializeField] private AudioClip   pauseSFX;
    [SerializeField] private AudioClip   gameOverSFX;
    [SerializeField] private AudioClip   winSFX;

    // ── state ─────────────────────────────────────────────────────────────────
    private int   _displayScore;
    private int   _targetScore;
    private int   _crystalCount;
    private int   _combo;
    private float _comboTimer;
    private float _energy        = 1f;
    private bool  _isPaused;
    private bool  _gameEnded;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        HidePanel(pausePanel);
        HidePanel(gameOverPanel);
        HidePanel(winPanel);
        if (comboGroup != null) comboGroup.alpha = 0f;

        WireButtons();
        RefreshSliders();
    }

    private void Update()
    {
        if (_gameEnded) return;

        RollScore();
        TickCombo();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            TogglePause();
    }

    // ── public API (call from GameManager) ────────────────────────────────────
    public void AddScore(int points, Vector3 worldPos = default)
    {
        _targetScore += points;
        if (worldPos != default)
            StartCoroutine(ShowScorePopup(points, worldPos));
    }

    public void AddCrystal(int count = 1)
    {
        _crystalCount += count;
        if (crystalLabel != null) crystalLabel.text = _crystalCount.ToString();
        StartCoroutine(BounceRect(crystalIconRect, 0.35f, 0.18f));

        if (crystalCollectFX != null) crystalCollectFX.Play();
        PlaySFX(crystalSFX);
        AddCombo();
    }

    public void SetEnergy(float value)   // 0..1
    {
        _energy = Mathf.Clamp01(value);
        UpdateEnergyBar();
    }

    public void ShowGameOver(int finalScore, int stars)
    {
        if (_gameEnded) return;
        _gameEnded = true;
        PlaySFX(gameOverSFX);
        StartCoroutine(ShowGameOverSequence(finalScore, stars));
    }

    public void ShowWin(int finalScore, int stars)
    {
        if (_gameEnded) return;
        _gameEnded = true;
        PlaySFX(winSFX);
        StartCoroutine(ShowWinSequence(finalScore, stars));
    }

    // ── score roll ────────────────────────────────────────────────────────────
    private void RollScore()
    {
        if (_displayScore == _targetScore) return;

        float delta = scoreRollSpeed * Time.deltaTime;
        if (Mathf.Abs(_targetScore - _displayScore) < delta)
            _displayScore = _targetScore;
        else
            _displayScore += (int)(Mathf.Sign(_targetScore - _displayScore) * delta);

        if (scoreLabel != null)
            scoreLabel.text = _displayScore.ToString("N0");
    }

    // ── combo ─────────────────────────────────────────────────────────────────
    private void AddCombo()
    {
        _combo++;
        _comboTimer = comboDuration;

        if (_combo >= 2)
        {
            if (comboGroup != null) comboGroup.alpha = 1f;
            if (comboLabel  != null) comboLabel.text = $"x{_combo} COMBO!";
            if (comboBarFill != null) comboBarFill.fillAmount = 1f;
            StartCoroutine(PunchScale(comboLabel?.rectTransform, 0.2f, 0.2f));
            if (_combo % 5 == 0) PlaySFX(comboSFX);
        }
    }

    private void TickCombo()
    {
        if (_combo < 2) return;

        _comboTimer -= Time.deltaTime;
        if (comboBarFill != null)
            comboBarFill.fillAmount = Mathf.Clamp01(_comboTimer / comboDuration);

        if (_comboTimer <= 0f)
        {
            _combo = 0;
            StartCoroutine(FadeCanvas(comboGroup, 1f, 0f, 0.3f));
        }
    }

    // ── energy bar ────────────────────────────────────────────────────────────
    private void UpdateEnergyBar()
    {
        if (energyFill == null) return;

        // Smooth fill
        StartCoroutine(AnimateFill(energyFill, energyFill.fillAmount, _energy, 0.3f));

        // Color shift low → high
        Color target = Color.Lerp(energyLowColor, energyHighColor, _energy);
        if (energyFill != null) energyFill.color = target;
        if (energyGlow  != null)
        {
            Color gc = target; gc.a = 0.4f;
            energyGlow.color = gc;
        }

        // Pulse warning when low
        if (_energy <= lowEnergyThresh)
            StartCoroutine(PulseWarning());
    }

    private IEnumerator AnimateFill(Image img, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            img.fillAmount = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        img.fillAmount = to;
    }

    private IEnumerator PulseWarning()
    {
        if (energyFill == null) yield break;
        Color orig = energyFill.color;
        Color flash = Color.white;
        energyFill.color = flash;
        yield return new WaitForSeconds(0.06f);
        energyFill.color = orig;
    }

    // ── pause ─────────────────────────────────────────────────────────────────
    public void TogglePause()
    {
        if (_gameEnded) return;
        _isPaused = !_isPaused;
        PlaySFX(pauseSFX);

        if (_isPaused)
        {
            Time.timeScale = 0f;
            StartCoroutine(SlideInPanel(pausePanel, pausePanelRect));
        }
        else
        {
            StartCoroutine(SlideOutPanel(pausePanel, pausePanelRect, () => Time.timeScale = 1f));
        }
    }

    private void WireButtons()
    {
        pauseButton   ?.onClick.AddListener(TogglePause);
        resumeButton  ?.onClick.AddListener(TogglePause);
        restartButton ?.onClick.AddListener(RestartLevel);
        mainMenuButton?.onClick.AddListener(GoToMainMenu);
        goRetryButton ?.onClick.AddListener(RestartLevel);
        goMenuButton  ?.onClick.AddListener(GoToMainMenu);
        winNextButton ?.onClick.AddListener(LoadNextLevel);
        winMenuButton ?.onClick.AddListener(GoToMainMenu);

        musicSlider?.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("MusicVolume", v);
            // GameManager.Instance?.SetMusicVolume(v);  ← wire to your AudioManager
        });
        sfxSlider?.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("SFXVolume", v));
    }

    private void RefreshSliders()
    {
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        if (sfxSlider   != null) sfxSlider.value   = PlayerPrefs.GetFloat("SFXVolume",   1f);
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int current = SceneManager.GetActiveScene().buildIndex;
        int next    = current + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            GoToMainMenu();
    }

    // ── game-over sequence ────────────────────────────────────────────────────
    private IEnumerator ShowGameOverSequence(int finalScore, int stars)
    {
        yield return new WaitForSecondsRealtime(0.8f);
        ShowPanel(gameOverPanel);

        // Roll score in
        if (goFinalScoreLabel != null)
            yield return StartCoroutine(RollLabelTo(goFinalScoreLabel, 0, finalScore, 1.2f));

        // Best score
        string key  = $"BS_{SceneManager.GetActiveScene().name}";
        int    best = PlayerPrefs.GetInt(key, 0);
        if (finalScore > best)
        {
            PlayerPrefs.SetInt(key, finalScore);
            best = finalScore;
        }
        if (goBestScoreLabel != null) goBestScoreLabel.text = $"MEJOR: {best:N0}";

        // Cascade stars
        yield return StartCoroutine(CascadeStars(goStarImages, stars, goStarFilled, goStarEmpty));
    }

    // ── win sequence ──────────────────────────────────────────────────────────
    private IEnumerator ShowWinSequence(int finalScore, int stars)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ShowPanel(winPanel);

        if (winConfettiFX != null) winConfettiFX.Play();

        if (winScoreLabel != null)
            yield return StartCoroutine(RollLabelTo(winScoreLabel, 0, finalScore, 1.5f));

        string key  = $"BS_{SceneManager.GetActiveScene().name}";
        int    best = PlayerPrefs.GetInt(key, 0);
        if (finalScore > best) { PlayerPrefs.SetInt(key, finalScore); best = finalScore; }
        if (winBestLabel != null) winBestLabel.text = $"MEJOR: {best:N0}";

        // Unlock next level
        int nextIdx = SceneManager.GetActiveScene().buildIndex;
        PlayerPrefs.SetInt($"UL_{nextIdx:D2}", 1);
        PlayerPrefs.Save();

        yield return StartCoroutine(CascadeStars(winStarImages, stars, goStarFilled, goStarEmpty));
    }

    private IEnumerator CascadeStars(Image[] imgs, int count, Sprite filled, Sprite empty)
    {
        if (imgs == null) yield break;
        for (int i = 0; i < imgs.Length; i++)
        {
            if (imgs[i] == null) continue;
            bool on = i < count;
            imgs[i].sprite = on ? filled : empty;
            imgs[i].color  = on ? new Color(1f, 0.85f, 0.1f) : new Color(1f,1f,1f,0.2f);
            if (on) yield return StartCoroutine(PunchScale(imgs[i].rectTransform, 0.5f, 0.25f));
            else    yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    // ── score popup ───────────────────────────────────────────────────────────
    private IEnumerator ShowScorePopup(int points, Vector3 worldPos)
    {
        if (scorePopupLabel == null || scorePopupRect == null) yield break;

        // Convert world → canvas position
        Camera cam = Camera.main;
        if (cam == null) yield break;
        var canvas    = GetComponentInParent<Canvas>();
        Vector2 vp    = cam.WorldToViewportPoint(worldPos);
        Vector2 cSize = (canvas.transform as RectTransform)?.sizeDelta ?? new Vector2(1920, 1080);
        Vector2 canvasPos = new Vector2(vp.x * cSize.x - cSize.x * 0.5f,
                                        vp.y * cSize.y - cSize.y * 0.5f);

        scorePopupRect.anchoredPosition = canvasPos;
        scorePopupLabel.text = $"+{points}";
        var cg = scorePopupLabel.GetComponent<CanvasGroup>() ?? scorePopupLabel.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        float dur = 0.9f, t = 0f;
        Vector2 startPos = canvasPos;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            scorePopupRect.anchoredPosition = startPos + new Vector2(0f, 60f * p);
            cg.alpha = 1f - p;
            yield return null;
        }
        cg.alpha = 0f;
    }

    // ── panel slide helpers ───────────────────────────────────────────────────
    private IEnumerator SlideInPanel(CanvasGroup cg, RectTransform rt)
    {
        ShowPanel(cg);
        Vector2 offscreen = new Vector2(0f, rt.rect.height + 50f);
        Vector2 onscreen  = Vector2.zero;
        rt.anchoredPosition = offscreen;

        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutBack(t / dur);
            rt.anchoredPosition = Vector2.LerpUnclamped(offscreen, onscreen, p);
            cg.alpha            = Mathf.Clamp01(t / dur);
            yield return null;
        }
        rt.anchoredPosition = onscreen;
        cg.alpha = 1f;
        cg.interactable = cg.blocksRaycasts = true;
    }

    private IEnumerator SlideOutPanel(CanvasGroup cg, RectTransform rt, System.Action onDone = null)
    {
        cg.interactable = cg.blocksRaycasts = false;
        Vector2 onscreen  = Vector2.zero;
        Vector2 offscreen = new Vector2(0f, rt.rect.height + 50f);

        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur * t / dur; // ease in
            rt.anchoredPosition = Vector2.Lerp(onscreen, offscreen, p);
            cg.alpha            = 1f - p;
            yield return null;
        }
        HidePanel(cg);
        onDone?.Invoke();
    }

    // ── utilities ─────────────────────────────────────────────────────────────
    private static void ShowPanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(true);
        cg.alpha = 1f;
        cg.interactable = cg.blocksRaycasts = true;
    }

    private static void HidePanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }

    private static IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float dur)
    {
        if (cg == null) yield break;
        float t = 0f; cg.alpha = from;
        while (t < dur) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
        cg.alpha = to;
    }

    private static IEnumerator PunchScale(RectTransform rt, float amount, float dur)
    {
        if (rt == null) yield break;
        Vector3 orig = rt.localScale; float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = 1f + amount * Mathf.Sin((t / dur) * Mathf.PI);
            rt.localScale = orig * s; yield return null;
        }
        rt.localScale = orig;
    }

    private static IEnumerator BounceRect(RectTransform rt, float amount, float dur)
    {
        if (rt == null) yield break;
        Vector3 orig = rt.localScale; float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.localScale = orig * (1f + amount * Mathf.Sin((t / dur) * Mathf.PI));
            yield return null;
        }
        rt.localScale = orig;
    }

    private static IEnumerator RollLabelTo(TextMeshProUGUI label, int from, int to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            int val = (int)Mathf.Lerp(from, to, EaseOutCubic(t / dur));
            label.text = val.ToString("N0");
            yield return null;
        }
        label.text = to.ToString("N0");
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
}
