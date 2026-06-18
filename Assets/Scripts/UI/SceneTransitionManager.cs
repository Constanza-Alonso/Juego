using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Scene Transition Manager
/// Singleton. Provides cinematic transitions between any two scenes:
///   • Fade (classic black)
///   • Slide wipe (panel slides across screen)
///   • Radial wipe (circular reveal using an Image with radial fill)
///   • Neon flash (white flash → black fade)
///
/// === Setup ===
/// 1. Create a Canvas (Sort Order 999, Screen Space Overlay).
/// 2. Add a child Image covering full screen → assign to `wipePanel`.
/// 3. Add another Image with Type = Filled, Fill Method = Radial360 → `radialPanel`.
/// 4. Place this script on a GameObject in the canvas and mark DontDestroyOnLoad.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public enum TransitionType { Fade, SlideLeft, SlideUp, RadialWipe, NeonFlash }

    [Header("=== Panels ===")]
    [SerializeField] private CanvasGroup  wipeGroup;
    [SerializeField] private Image        wipePanel;      // solid color panel
    [SerializeField] private Image        radialPanel;    // Filled / Radial360
    [SerializeField] private CanvasGroup  flashGroup;     // white flash layer
    [SerializeField] private Image        flashPanel;

    [Header("=== Defaults ===")]
    [SerializeField] private TransitionType defaultTransition = TransitionType.Fade;
    [SerializeField] private float          defaultDuration   = 0.45f;
    [SerializeField] private Color          wipeColor         = Color.black;

    // ── singleton ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetPanels();
    }

    // ── public API ────────────────────────────────────────────────────────────

    /// <summary>Load a scene by name with the specified transition.</summary>
    public void LoadScene(string sceneName,
                          TransitionType type     = TransitionType.Fade,
                          float          duration = -1f,
                          Action         onMidpoint = null)
    {
        float dur = duration > 0f ? duration : defaultDuration;
        StartCoroutine(TransitionRoutine(sceneName, type, dur, onMidpoint));
    }

    /// <summary>Load a scene by build index.</summary>
    public void LoadScene(int buildIndex,
                          TransitionType type     = TransitionType.Fade,
                          float          duration = -1f)
    {
        string name = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        LoadScene(name, type, duration);
    }

    /// <summary>Reload the current scene.</summary>
    public void ReloadCurrent(TransitionType type = TransitionType.NeonFlash)
    {
        LoadScene(SceneManager.GetActiveScene().name, type);
    }

    // ── core routine ──────────────────────────────────────────────────────────
    private IEnumerator TransitionRoutine(string sceneName, TransitionType type, float dur, Action onMidpoint)
    {
        float half = dur * 0.5f;

        // ── Phase 1: Cover screen ────────────────────────────────────────────
        yield return StartCoroutine(CoverScreen(type, half));

        onMidpoint?.Invoke();

        // ── Load scene ───────────────────────────────────────────────────────
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        // Wait one frame for new scene to initialize
        yield return null;
        yield return null;

        // ── Phase 2: Reveal screen ───────────────────────────────────────────
        yield return StartCoroutine(RevealScreen(type, half));

        ResetPanels();
    }

    // ── cover (animate IN) ────────────────────────────────────────────────────
    private IEnumerator CoverScreen(TransitionType type, float dur)
    {
        switch (type)
        {
            case TransitionType.Fade:
                wipePanel.color = wipeColor;
                yield return StartCoroutine(FadeGroup(wipeGroup, 0f, 1f, dur));
                break;

            case TransitionType.SlideLeft:
                wipePanel.color = wipeColor;
                wipeGroup.alpha = 1f;
                yield return StartCoroutine(SlidePanel(wipePanel.rectTransform,
                    new Vector2(Screen.width, 0f), Vector2.zero, dur));
                break;

            case TransitionType.SlideUp:
                wipePanel.color = wipeColor;
                wipeGroup.alpha = 1f;
                yield return StartCoroutine(SlidePanel(wipePanel.rectTransform,
                    new Vector2(0f, -Screen.height), Vector2.zero, dur));
                break;

            case TransitionType.RadialWipe:
                radialPanel.color       = wipeColor;
                radialPanel.fillAmount  = 0f;
                radialPanel.gameObject.SetActive(true);
                yield return StartCoroutine(AnimateFill(radialPanel, 0f, 1f, dur));
                break;

            case TransitionType.NeonFlash:
                // 1. White flash
                flashPanel.color = Color.white;
                yield return StartCoroutine(FadeGroup(flashGroup, 0f, 1f, dur * 0.25f));
                yield return StartCoroutine(FadeGroup(flashGroup, 1f, 0f, dur * 0.25f));
                // 2. Black cover
                wipePanel.color = wipeColor;
                yield return StartCoroutine(FadeGroup(wipeGroup, 0f, 1f, dur * 0.5f));
                break;
        }
    }

    // ── reveal (animate OUT) ──────────────────────────────────────────────────
    private IEnumerator RevealScreen(TransitionType type, float dur)
    {
        switch (type)
        {
            case TransitionType.Fade:
                yield return StartCoroutine(FadeGroup(wipeGroup, 1f, 0f, dur));
                break;

            case TransitionType.SlideLeft:
                yield return StartCoroutine(SlidePanel(wipePanel.rectTransform,
                    Vector2.zero, new Vector2(-Screen.width, 0f), dur));
                wipeGroup.alpha = 0f;
                break;

            case TransitionType.SlideUp:
                yield return StartCoroutine(SlidePanel(wipePanel.rectTransform,
                    Vector2.zero, new Vector2(0f, Screen.height), dur));
                wipeGroup.alpha = 0f;
                break;

            case TransitionType.RadialWipe:
                yield return StartCoroutine(AnimateFill(radialPanel, 1f, 0f, dur));
                radialPanel.gameObject.SetActive(false);
                break;

            case TransitionType.NeonFlash:
                yield return StartCoroutine(FadeGroup(wipeGroup, 1f, 0f, dur));
                break;
        }
    }

    // ── reset ─────────────────────────────────────────────────────────────────
    private void ResetPanels()
    {
        if (wipeGroup  != null) { wipeGroup.alpha  = 0f; wipeGroup.interactable = wipeGroup.blocksRaycasts = false; }
        if (flashGroup != null) { flashGroup.alpha  = 0f; }
        if (radialPanel != null) { radialPanel.fillAmount = 0f; radialPanel.gameObject.SetActive(false); }
        if (wipePanel   != null) wipePanel.rectTransform.anchoredPosition = Vector2.zero;
    }

    // ── coroutine helpers ─────────────────────────────────────────────────────
    private static IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float dur)
    {
        if (cg == null) yield break;
        cg.blocksRaycasts = true;
        float t = 0f; cg.alpha = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, EaseInOut(t / dur));
            yield return null;
        }
        cg.alpha = to;
        cg.blocksRaycasts = to > 0.5f;
    }

    private static IEnumerator SlidePanel(RectTransform rt, Vector2 from, Vector2 to, float dur)
    {
        if (rt == null) yield break;
        float t = 0f; rt.anchoredPosition = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseInOut(t / dur));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    private static IEnumerator AnimateFill(Image img, float from, float to, float dur)
    {
        if (img == null) yield break;
        float t = 0f; img.fillAmount = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            img.fillAmount = Mathf.Lerp(from, to, EaseInOut(t / dur));
            yield return null;
        }
        img.fillAmount = to;
    }

    private static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
}
