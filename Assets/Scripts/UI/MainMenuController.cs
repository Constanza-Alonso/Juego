using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Main Menu Controller
/// Handles animated intro, parallax layers, button interactions and scene transitions.
/// Attach to the MainMenu scene root GameObject.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("=== Panels ===")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup levelSelectPanel;
    [SerializeField] private CanvasGroup settingsPanel;
    [SerializeField] private CanvasGroup creditsPanel;

    [Header("=== Logo ===")]
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private TextMeshProUGUI titleText;       // "SHADOW BEAT"
    [SerializeField] private TextMeshProUGUI subtitleText;    // "Fragmentos de Luz"

    [Header("=== Buttons ===")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("=== Parallax Background Layers ===")]
    [SerializeField] private RectTransform[] parallaxLayers;   // Assign furthest → nearest
    [SerializeField] private float[] parallaxSpeeds;           // e.g. 10, 20, 35 px/s

    [Header("=== Particle Stars ===")]
    [SerializeField] private ParticleSystem starParticles;
    [SerializeField] private ParticleSystem glowParticles;

    [Header("=== Audio ===")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip transitionSFX;

    [Header("=== Transition ===")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float introDuration = 1.8f;
    [SerializeField] private float fadeDuration  = 0.45f;

    // ── state ──────────────────────────────────────────────────────────────────
    private CanvasGroup _activePanel;
    private bool        _isTransitioning;

    // ── lifecycle ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Guarantee overlay is fully opaque on first frame so intro fade looks clean
        SetAlpha(fadeOverlay, 1f);
        SetAlpha(mainPanel,   0f);
        SetAlpha(logoGroup,   0f);
        HidePanel(levelSelectPanel);
        HidePanel(settingsPanel);
        HidePanel(creditsPanel);
        _activePanel = mainPanel;
    }

    private void Start()
    {
        WireButtons();
        StartCoroutine(PlayIntroSequence());

        if (musicSource != null && menuMusic != null)
        {
            musicSource.clip   = menuMusic;
            musicSource.loop   = true;
            musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicSource.Play();
        }
    }

    private void Update()
    {
        UpdateParallax();
    }

    // ── intro sequence ─────────────────────────────────────────────────────────
    private IEnumerator PlayIntroSequence()
    {
        // 1. Fade out the black overlay
        yield return StartCoroutine(FadeCanvas(fadeOverlay, 1f, 0f, fadeDuration));

        // 2. Animate logo sliding down from top + fading in
        logoRect.anchoredPosition = new Vector2(0f, 80f);
        yield return StartCoroutine(Parallel(
            FadeCanvas(logoGroup, 0f, 1f, introDuration),
            SlideTo(logoRect, new Vector2(0f, 80f), new Vector2(0f, 0f), introDuration, Ease.OutCubic)
        ));

        // 3. Pulse subtitle once
        yield return StartCoroutine(PunchScale(subtitleText.rectTransform, 0.06f, 0.3f));

        // 4. Fade in the buttons panel
        yield return StartCoroutine(FadeCanvas(mainPanel, 0f, 1f, 0.5f));
        mainPanel.interactable = mainPanel.blocksRaycasts = true;
    }

    // ── button wiring ──────────────────────────────────────────────────────────
    private void WireButtons()
    {
        playButton    .onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
        creditsButton .onClick.AddListener(() => OpenPanel(creditsPanel));
        quitButton    .onClick.AddListener(OnQuitClicked);

        // Hover sounds via EventTrigger helper (adds at runtime so designers
        // don't have to wire manually in Inspector)
        AddHoverSound(playButton);
        AddHoverSound(settingsButton);
        AddHoverSound(creditsButton);
        AddHoverSound(quitButton);
    }

    private void AddHoverSound(Button btn)
    {
        var trigger = btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                      ?? btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var entry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entry.callback.AddListener(_ => PlaySFX(buttonHoverSFX));
        trigger.triggers.Add(entry);
    }

    // ── public button handlers ─────────────────────────────────────────────────
    public void OnPlayClicked()
    {
        if (_isTransitioning) return;
        PlaySFX(buttonClickSFX);
        OpenPanel(levelSelectPanel);
    }

    public void OnQuitClicked()
    {
        if (_isTransitioning) return;
        PlaySFX(buttonClickSFX);
        StartCoroutine(QuitSequence());
    }

    public void OnBackToMain()
    {
        if (_isTransitioning) return;
        PlaySFX(buttonClickSFX);
        StartCoroutine(SwitchPanels(_activePanel, mainPanel));
    }

    // ── panel management ───────────────────────────────────────────────────────
    private void OpenPanel(CanvasGroup target)
    {
        if (_isTransitioning || target == _activePanel) return;
        PlaySFX(buttonClickSFX);
        StartCoroutine(SwitchPanels(_activePanel, target));
    }

    private IEnumerator SwitchPanels(CanvasGroup from, CanvasGroup to)
    {
        _isTransitioning = true;
        from.interactable = from.blocksRaycasts = false;

        // Slide-left out
        var fromRect = from.GetComponent<RectTransform>();
        yield return StartCoroutine(Parallel(
            FadeCanvas(from, 1f, 0f, 0.25f),
            SlideTo(fromRect, Vector2.zero, new Vector2(-60f, 0f), 0.25f, Ease.InCubic)
        ));
        HidePanel(from);
        fromRect.anchoredPosition = Vector2.zero;

        // Slide-right in
        ShowPanel(to);
        SetAlpha(to, 0f);
        var toRect = to.GetComponent<RectTransform>();
        toRect.anchoredPosition = new Vector2(60f, 0f);

        yield return StartCoroutine(Parallel(
            FadeCanvas(to, 0f, 1f, 0.3f),
            SlideTo(toRect, new Vector2(60f, 0f), Vector2.zero, 0.3f, Ease.OutCubic)
        ));

        to.interactable = to.blocksRaycasts = true;
        _activePanel     = to;
        _isTransitioning = false;
    }

    // ── scene loading ──────────────────────────────────────────────────────────
    /// <summary>Called by LevelSelectController when the player taps a level card.</summary>
    public void LoadLevel(string sceneName)
    {
        if (_isTransitioning) return;
        PlaySFX(transitionSFX);
        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        _isTransitioning = true;

        // Music fade-out in parallel with overlay fade-in
        yield return StartCoroutine(Parallel(
            FadeCanvas(fadeOverlay, 0f, 1f, fadeDuration),
            FadeAudio(musicSource, musicSource != null ? musicSource.volume : 0f, 0f, fadeDuration)
        ));

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator QuitSequence()
    {
        _isTransitioning = true;
        yield return StartCoroutine(FadeCanvas(fadeOverlay, 0f, 1f, fadeDuration));
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── parallax ──────────────────────────────────────────────────────────────
    private void UpdateParallax()
    {
        if (parallaxLayers == null) return;
        float dt = Time.deltaTime;
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            if (parallaxLayers[i] == null) continue;
            float speed = (parallaxSpeeds != null && i < parallaxSpeeds.Length) ? parallaxSpeeds[i] : 10f;
            var pos = parallaxLayers[i].anchoredPosition;
            pos.x -= speed * dt;
            // Seamless loop: assumes the layer texture tiles at width W
            // (works if the RawImage's UV tiles or you have a double-wide sprite)
            if (pos.x <= -1920f) pos.x += 1920f;
            parallaxLayers[i].anchoredPosition = pos;
        }
    }

    // ── helpers: panels ───────────────────────────────────────────────────────
    private static void ShowPanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(true);
    }

    private static void HidePanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }

    private static void SetAlpha(CanvasGroup cg, float a)
    {
        if (cg == null) return;
        cg.alpha = a;
    }

    // ── helpers: audio ────────────────────────────────────────────────────────
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        musicSource.PlayOneShot(clip, PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    // ── coroutine utilities ───────────────────────────────────────────────────
    private static IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private static IEnumerator FadeAudio(AudioSource src, float from, float to, float duration)
    {
        if (src == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        src.volume = to;
    }

    private static IEnumerator SlideTo(RectTransform rt, Vector2 from, Vector2 to, float duration, Ease ease)
    {
        if (rt == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = ApplyEase(t / duration, ease);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, p);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    private static IEnumerator PunchScale(RectTransform rt, float amount, float duration)
    {
        if (rt == null) yield break;
        Vector3 orig = rt.localScale;
        float   half = duration * 0.5f;
        float   t    = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float s = 1f + amount * (t / half);
            rt.localScale = orig * s;
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float s = 1f + amount * (1f - t / half);
            rt.localScale = orig * s;
            yield return null;
        }
        rt.localScale = orig;
    }

    // Runs two coroutines in parallel and waits for both to finish
    private IEnumerator Parallel(IEnumerator a, IEnumerator b)
    {
        bool doneA = false, doneB = false;
        StartCoroutine(RunAndFlag(a, () => doneA = true));
        StartCoroutine(RunAndFlag(b, () => doneB = true));
        yield return new WaitUntil(() => doneA && doneB);
    }

    private static IEnumerator RunAndFlag(IEnumerator routine, System.Action onDone)
    {
        yield return routine;
        onDone?.Invoke();
    }

    // ── easing ────────────────────────────────────────────────────────────────
    private enum Ease { Linear, InCubic, OutCubic, InOutCubic }

    private static float ApplyEase(float t, Ease ease)
    {
        t = Mathf.Clamp01(t);
        return ease switch
        {
            Ease.InCubic    => t * t * t,
            Ease.OutCubic   => 1f - Mathf.Pow(1f - t, 3f),
            Ease.InOutCubic => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f,
            _               => t
        };
    }
}
}
