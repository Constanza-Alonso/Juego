using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Level Card
/// Self-contained card component: thumbnail, title, world name, star rating,
/// best score, lock overlay and animated selection ring.
/// Attach this to the LevelCardPrefab root.
/// 
/// === Prefab hierarchy ===
/// LevelCard (RectTransform 280×360)
///   ├─ Background (Image – rounded rect)
///   ├─ Thumbnail  (RawImage or Image)
///   ├─ GradientOverlay (Image – bottom gradient)
///   ├─ AccentLine  (Image – 3px top bar, receives accent color)
///   ├─ SelectionRing (Image – outline, hidden when not selected)
///   ├─ GlowPulse (Image – soft glow behind card, pulsed when selected)
///   ├─ InfoArea
///   │   ├─ WorldNameLabel  (TextMeshProUGUI – e.g. "Mundo 1")
///   │   ├─ LevelNameLabel  (TextMeshProUGUI – e.g. "Bosque Neon")
///   │   ├─ StarsContainer  (HorizontalLayoutGroup)
///   │   │   └─ Star_0..2  (Image × 3)
///   │   └─ BestScoreLabel (TextMeshProUGUI)
///   └─ LockOverlay
///       ├─ LockDim (Image – dark semi-transparent fill)
///       └─ LockIcon (Image – padlock sprite)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LevelCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("=== Visuals ===")]
    [SerializeField] private Image            background;
    [SerializeField] private Image            thumbnail;       // swap to RawImage if using Texture2D
    [SerializeField] private Image            accentLine;
    [SerializeField] private Image            selectionRing;
    [SerializeField] private Image            glowPulse;

    [Header("=== Labels ===")]
    [SerializeField] private TextMeshProUGUI  worldNameLabel;
    [SerializeField] private TextMeshProUGUI  levelNameLabel;
    [SerializeField] private TextMeshProUGUI  bestScoreLabel;

    [Header("=== Stars ===")]
    [SerializeField] private Image[]          starImages;      // 3 images
    [SerializeField] private Sprite           starFilled;
    [SerializeField] private Sprite           starEmpty;
    [SerializeField] private Color            starOnColor  = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color            starOffColor = new Color(1f, 1f, 1f, 0.2f);

    [Header("=== Lock ===")]
    [SerializeField] private GameObject       lockOverlay;

    [Header("=== Animation ===")]
    [SerializeField] private float hoverScaleTarget = 1.06f;
    [SerializeField] private float hoverSpeed       = 8f;
    [SerializeField] private float pressScale       = 0.95f;

    // ── runtime state ─────────────────────────────────────────────────────────
    private LevelSelectController.LevelData _data;
    private Action                          _onTap;
    private bool                            _isSelected;
    private bool                            _isHovered;
    private bool                            _isPressed;
    private RectTransform                   _rt;
    private Coroutine                       _glowRoutine;

    private float   _targetScale  = 1f;
    private float   _currentScale = 1f;

    // ── init ──────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void Setup(LevelSelectController.LevelData data, Action onTap)
    {
        _data  = data;
        _onTap = onTap;

        // Thumbnail
        if (thumbnail != null && data.thumbnail != null)
            thumbnail.sprite = data.thumbnail;

        // Accent color
        if (accentLine != null)
            accentLine.color = data.accentColor;
        if (selectionRing != null)
            selectionRing.color = data.accentColor;
        if (glowPulse != null)
        {
            var c = data.accentColor;
            c.a = 0f;
            glowPulse.color = c;
        }

        // Labels
        if (worldNameLabel != null) worldNameLabel.text = data.worldName.ToUpper();
        if (levelNameLabel != null) levelNameLabel.text  = data.displayName;

        // Best score
        int best = PlayerPrefs.GetInt(data.prefs_BestScore, 0);
        if (bestScoreLabel != null)
            bestScoreLabel.text = best > 0 ? $"MEJOR: {best:N0}" : "SIN JUGAR";

        // Stars
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            bool on = i < data.starsEarned;
            starImages[i].sprite = on ? starFilled : starEmpty;
            starImages[i].color  = on ? starOnColor : starOffColor;
        }

        // Lock
        if (lockOverlay != null)
            lockOverlay.SetActive(data.isLocked);

        SetSelected(false);
    }

    // ── selection ─────────────────────────────────────────────────────────────
    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        if (selectionRing != null)
            selectionRing.gameObject.SetActive(selected);

        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        _glowRoutine = selected
            ? StartCoroutine(PulseGlow())
            : StartCoroutine(FadeGlow(0f, 0.3f));

        _targetScale = selected ? hoverScaleTarget : 1f;
    }

    // ── pointer events ────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        _isHovered = true;
        if (!_isSelected) _targetScale = hoverScaleTarget * 0.98f;
    }

    public void OnPointerExit(PointerEventData e)
    {
        _isHovered = false;
        if (!_isSelected) _targetScale = 1f;
        if (_isPressed) _isPressed = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        _isPressed    = true;
        _targetScale  = pressScale;
    }

    public void OnPointerUp(PointerEventData e)
    {
        _isPressed   = false;
        _targetScale = _isSelected ? hoverScaleTarget : (_isHovered ? hoverScaleTarget * 0.98f : 1f);
    }

    public void OnPointerClick(PointerEventData e)
    {
        _onTap?.Invoke();
    }

    // ── update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        // Smooth scale spring
        _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * hoverSpeed);
        _rt.localScale = Vector3.one * _currentScale;
    }

    // ── glow pulse ────────────────────────────────────────────────────────────
    private IEnumerator PulseGlow()
    {
        if (glowPulse == null) yield break;
        Color baseColor = _data.accentColor;

        while (_isSelected)
        {
            float alpha = 0.12f + 0.08f * Mathf.Sin(Time.time * 2.5f);
            baseColor.a     = alpha;
            glowPulse.color = baseColor;
            yield return null;
        }
    }

    private IEnumerator FadeGlow(float target, float duration)
    {
        if (glowPulse == null) yield break;
        float start = glowPulse.color.a;
        float t     = 0f;
        Color c     = glowPulse.color;
        while (t < duration)
        {
            t     += Time.deltaTime;
            c.a    = Mathf.Lerp(start, target, t / duration);
            glowPulse.color = c;
            yield return null;
        }
        c.a             = target;
        glowPulse.color = c;
    }

    // ── unlock animation (called externally when a level is newly unlocked) ───
    public IEnumerator PlayUnlockAnimation()
    {
        // Quick shake + star cascade
        Vector3 orig = _rt.localScale;
        for (int i = 0; i < 2; i++)
        {
            _rt.localScale = orig * 1.12f;
            yield return new WaitForSeconds(0.08f);
            _rt.localScale = orig * 0.95f;
            yield return new WaitForSeconds(0.08f);
        }
        _rt.localScale = orig;

        // Cascade stars one by one
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            starImages[i].sprite = starFilled;
            starImages[i].color  = starOnColor;
            yield return StartCoroutine(BounceImage(starImages[i], 0.4f, 0.2f));
        }

        // Flash glow
        if (glowPulse != null)
        {
            Color c = _data.accentColor;
            c.a             = 0.6f;
            glowPulse.color = c;
            yield return new WaitForSeconds(0.15f);
        }

        // Remove lock overlay
        if (lockOverlay != null)
        {
            var cg = lockOverlay.GetComponent<CanvasGroup>() ?? lockOverlay.AddComponent<CanvasGroup>();
            float t = 0f;
            while (t < 0.4f)
            {
                t      += Time.deltaTime;
                cg.alpha = 1f - (t / 0.4f);
                yield return null;
            }
            lockOverlay.SetActive(false);
        }
    }

    private static IEnumerator BounceImage(Image img, float amount, float duration)
    {
        Vector3 orig = img.rectTransform.localScale;
        float   t    = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float s = 1f + amount * Mathf.Sin((t / duration) * Mathf.PI);
            img.rectTransform.localScale = orig * s;
            yield return null;
        }
        img.rectTransform.localScale = orig;
    }
}
}
