using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Settings Panel Controller
/// Music volume, SFX volume, vibration toggle, fullscreen toggle,
/// language selector, and a polished "saved!" confirmation flash.
///
/// Designed to work both as a standalone panel in the Main Menu
/// and as the in-game pause settings tab.
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    // ── Audio ─────────────────────────────────────────────────────────────────
    [Header("=== Audio ===")]
    [SerializeField] private Slider          musicSlider;
    [SerializeField] private TextMeshProUGUI musicValueLabel;   // "75%"
    [SerializeField] private Slider          sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueLabel;
    [SerializeField] private Button          musicMuteButton;
    [SerializeField] private Image           musicMuteIcon;     // swap sprite: speaker / muted
    [SerializeField] private Sprite          speakerOnSprite;
    [SerializeField] private Sprite          speakerOffSprite;

    // ── Toggles ───────────────────────────────────────────────────────────────
    [Header("=== Toggles ===")]
    [SerializeField] private ToggleSwitch    vibrationToggle;
    [SerializeField] private ToggleSwitch    fullscreenToggle;
    [SerializeField] private ToggleSwitch    particlesToggle;

    // ── Language ──────────────────────────────────────────────────────────────
    [Header("=== Language ===")]
    [SerializeField] private Button          langPrevButton;
    [SerializeField] private Button          langNextButton;
    [SerializeField] private TextMeshProUGUI langLabel;

    // ── Save feedback ─────────────────────────────────────────────────────────
    [Header("=== Save Feedback ===")]
    [SerializeField] private CanvasGroup     savedBadge;        // "¡Guardado!" green badge
    [SerializeField] private Button          saveButton;
    [SerializeField] private Button          backButton;

    // ── Audio reference ───────────────────────────────────────────────────────
    [Header("=== AudioManager reference ===")]
    [SerializeField] private AudioSource     musicSource;       // main BGM source
    [SerializeField] private AudioSource     sfxSource;

    // ── state ─────────────────────────────────────────────────────────────────
    private static readonly string[] _languages = { "Español", "English", "Português", "Français", "Deutsch" };
    private int    _langIndex;
    private bool   _musicMuted;
    private float  _savedMusicVolume;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        LoadSettings();
        ApplyToUI();
        WireControls();
        if (savedBadge != null) savedBadge.alpha = 0f;
    }

    // ── load / save ───────────────────────────────────────────────────────────
    private void LoadSettings()
    {
        _langIndex    = PlayerPrefs.GetInt  ("Language",     0);
        _musicMuted   = PlayerPrefs.GetInt  ("MusicMuted",   0) == 1;
        _savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume",  musicSlider?.value ?? 0.75f);
        PlayerPrefs.SetFloat("SFXVolume",    sfxSlider?.value   ?? 1f);
        PlayerPrefs.SetInt  ("MusicMuted",   _musicMuted ? 1 : 0);
        PlayerPrefs.SetInt  ("Vibration",    (vibrationToggle?.IsOn ?? true) ? 1 : 0);
        PlayerPrefs.SetInt  ("Fullscreen",   (fullscreenToggle?.IsOn ?? true) ? 1 : 0);
        PlayerPrefs.SetInt  ("Particles",    (particlesToggle?.IsOn ?? true)  ? 1 : 0);
        PlayerPrefs.SetInt  ("Language",     _langIndex);
        PlayerPrefs.Save();

        ApplyAudio();
        ApplyGraphics();
        StartCoroutine(ShowSavedBadge());
    }

    // ── apply to scene ────────────────────────────────────────────────────────
    private void ApplyAudio()
    {
        float mv = _musicMuted ? 0f : (musicSlider?.value ?? 0.75f);
        if (musicSource != null) musicSource.volume = mv;

        float sv = sfxSlider?.value ?? 1f;
        if (sfxSource != null) sfxSource.volume = sv;
    }

    private void ApplyGraphics()
    {
        Screen.fullScreen = fullscreenToggle?.IsOn ?? true;
        // Particle quality: hook to your QualityManager
    }

    private void ApplyToUI()
    {
        // Sliders
        if (musicSlider != null)  musicSlider.value  = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        if (sfxSlider   != null)  sfxSlider.value    = PlayerPrefs.GetFloat("SFXVolume",   1f);
        RefreshMusicLabel();
        RefreshSFXLabel();
        RefreshMuteIcon();

        // Toggles
        vibrationToggle?.SetState(PlayerPrefs.GetInt("Vibration",  1) == 1, animate: false);
        fullscreenToggle?.SetState(PlayerPrefs.GetInt("Fullscreen", 1) == 1, animate: false);
        particlesToggle?.SetState(PlayerPrefs.GetInt("Particles",  1) == 1, animate: false);

        // Language
        if (langLabel != null) langLabel.text = _languages[_langIndex];
    }

    // ── wire controls ─────────────────────────────────────────────────────────
    private void WireControls()
    {
        musicSlider?.onValueChanged.AddListener(_ => { RefreshMusicLabel(); ApplyAudio(); });
        sfxSlider  ?.onValueChanged.AddListener(_ => { RefreshSFXLabel();   ApplyAudio(); });

        musicMuteButton?.onClick.AddListener(ToggleMusicMute);
        saveButton     ?.onClick.AddListener(SaveSettings);
        backButton     ?.onClick.AddListener(OnBackClicked);

        langPrevButton?.onClick.AddListener(() => CycleLanguage(-1));
        langNextButton?.onClick.AddListener(() => CycleLanguage(+1));
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private void RefreshMusicLabel()
    {
        if (musicValueLabel != null && musicSlider != null)
            musicValueLabel.text = $"{Mathf.RoundToInt(musicSlider.value * 100f)}%";
    }

    private void RefreshSFXLabel()
    {
        if (sfxValueLabel != null && sfxSlider != null)
            sfxValueLabel.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
    }

    private void ToggleMusicMute()
    {
        _musicMuted = !_musicMuted;
        if (_musicMuted)
        {
            _savedMusicVolume = musicSlider?.value ?? 0.75f;
            if (musicSlider != null) musicSlider.value = 0f;
        }
        else
        {
            if (musicSlider != null) musicSlider.value = _savedMusicVolume;
        }
        RefreshMuteIcon();
        ApplyAudio();
    }

    private void RefreshMuteIcon()
    {
        if (musicMuteIcon == null) return;
        musicMuteIcon.sprite = _musicMuted ? speakerOffSprite : speakerOnSprite;
        musicMuteIcon.color  = _musicMuted
            ? new Color(1f, 0.35f, 0.35f)
            : new Color(0.4f, 1f, 0.85f);
    }

    private void CycleLanguage(int dir)
    {
        _langIndex = (_langIndex + dir + _languages.Length) % _languages.Length;
        if (langLabel != null)
        {
            langLabel.text = _languages[_langIndex];
            StartCoroutine(PunchScale(langLabel.rectTransform, 0.12f, 0.18f));
        }
    }

    private void OnBackClicked()
    {
        // Auto-save on back
        SaveSettings();
        var mainMenu = FindObjectOfType<MainMenuController>();
        mainMenu?.OnBackToMain();
    }

    // ── saved badge ───────────────────────────────────────────────────────────
    private IEnumerator ShowSavedBadge()
    {
        if (savedBadge == null) yield break;

        // Bounce in
        savedBadge.alpha = 1f;
        var rt = savedBadge.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one * 0.5f;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(0.5f, 1f, EaseOutBack(t / 0.3f));
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(1.4f);

        // Fade out
        float f = 0f;
        while (f < 0.4f)
        {
            f += Time.deltaTime;
            savedBadge.alpha = 1f - (f / 0.4f);
            yield return null;
        }
        savedBadge.alpha = 0f;
    }

    // ── easing ────────────────────────────────────────────────────────────────
    private static IEnumerator PunchScale(RectTransform rt, float amount, float dur)
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

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
}
