using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Neon Button
/// Drop-in replacement for boring Unity buttons.
/// Features: glow pulse on hover, ripple effect on click, spring press animation,
/// accent color propagation, and disabled state.
///
/// === Prefab hierarchy ===
/// NeonButton (this script, Image – rounded background)
///   ├─ GlowBorder   (Image – outline, receives accent color)
///   ├─ GlowAura     (Image – soft bloom behind button)
///   ├─ Label        (TextMeshProUGUI)
///   ├─ Icon         (Image – optional left icon)
///   └─ RipplePool
///       └─ Ripple_0..2  (Image – circle, pooled)
/// </summary>
[RequireComponent(typeof(Image), typeof(Button))]
public class NeonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                         IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("=== Visuals ===")]
    [SerializeField] private Image            background;
    [SerializeField] private Image            glowBorder;
    [SerializeField] private Image            glowAura;
    [SerializeField] private TextMeshProUGUI  label;

    [Header("=== Colors ===")]
    [SerializeField] private Color accentColor      = new Color(0.3f, 1f, 0.8f);
    [SerializeField] private Color bgNormalColor    = new Color(0.05f, 0.06f, 0.12f, 0.92f);
    [SerializeField] private Color bgHoverColor     = new Color(0.08f, 0.10f, 0.20f, 0.98f);
    [SerializeField] private Color bgPressColor     = new Color(0.02f, 0.03f, 0.08f, 1f);
    [SerializeField] private Color labelNormalColor = Color.white;
    [SerializeField] private Color labelHoverColor  = new Color(0.3f, 1f, 0.8f);

    [Header("=== Animation ===")]
    [SerializeField] private float hoverScale   = 1.04f;
    [SerializeField] private float pressScale   = 0.95f;
    [SerializeField] private float springSpeed  = 14f;

    [Header("=== Ripple ===")]
    [SerializeField] private Image[] rippleImages;     // 2-3 circles in pool
    [SerializeField] private float   rippleDuration = 0.55f;
    [SerializeField] private float   rippleMaxScale = 3.5f;

    [Header("=== Glow Pulse ===")]
    [SerializeField] private float glowPulseSpeed    = 2.0f;
    [SerializeField] private float glowMinAlpha      = 0.0f;
    [SerializeField] private float glowHoverAlpha    = 0.55f;

    // ── state ─────────────────────────────────────────────────────────────────
    private float     _targetScale  = 1f;
    private float     _currentScale = 1f;
    private bool      _isHovered;
    private bool      _isPressed;
    private int       _rippleIdx;
    private Button    _button;
    private Coroutine _glowRoutine;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _button = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();

        ApplyAccentColor();
        SetBgColor(bgNormalColor, instant: true);
        SetLabelColor(labelNormalColor, instant: true);
        if (glowAura != null) { var c = accentColor; c.a = 0f; glowAura.color = c; }

        if (rippleImages != null)
            foreach (var r in rippleImages) { if (r) r.gameObject.SetActive(false); }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        SmoothScale();
        if (_isHovered && !_isPressed) PulseGlow();
    }

    // ── pointer events ────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        if (!_button.interactable) return;
        _isHovered    = true;
        _targetScale  = hoverScale;

        SetBgColor(bgHoverColor);
        SetLabelColor(labelHoverColor);

        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        _glowRoutine = StartCoroutine(FadeGlow(glowHoverAlpha, 0.2f));
    }

    public void OnPointerExit(PointerEventData e)
    {
        _isHovered   = false;
        _isPressed   = false;
        _targetScale = 1f;

        SetBgColor(bgNormalColor);
        SetLabelColor(labelNormalColor);

        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        _glowRoutine = StartCoroutine(FadeGlow(glowMinAlpha, 0.3f));
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!_button.interactable) return;
        _isPressed   = true;
        _targetScale = pressScale;
        SetBgColor(bgPressColor);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _isPressed   = false;
        _targetScale = _isHovered ? hoverScale : 1f;
        SetBgColor(_isHovered ? bgHoverColor : bgNormalColor);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!_button.interactable) return;
        SpawnRipple(e.position);
    }

    // ── scale spring ──────────────────────────────────────────────────────────
    private void SmoothScale()
    {
        _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * springSpeed);
        transform.localScale = Vector3.one * _currentScale;
    }

    // ── glow pulse ────────────────────────────────────────────────────────────
    private void PulseGlow()
    {
        if (glowAura == null) return;
        float a = glowHoverAlpha * (0.75f + 0.25f * Mathf.Sin(Time.time * glowPulseSpeed));
        var c = glowAura.color; c.a = a; glowAura.color = c;
    }

    private IEnumerator FadeGlow(float target, float dur)
    {
        if (glowAura == null) yield break;
        float start = glowAura.color.a, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var c = glowAura.color; c.a = Mathf.Lerp(start, target, t / dur);
            glowAura.color = c; yield return null;
        }
        var fc = glowAura.color; fc.a = target; glowAura.color = fc;
    }

    // ── ripple ────────────────────────────────────────────────────────────────
    private void SpawnRipple(Vector2 screenPos)
    {
        if (rippleImages == null || rippleImages.Length == 0) return;

        var ripple = rippleImages[_rippleIdx % rippleImages.Length];
        _rippleIdx++;
        if (ripple == null) return;

        // Convert screen pos → local canvas position
        var rt = ripple.rectTransform.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, screenPos, null, out Vector2 localPos);

        ripple.rectTransform.anchoredPosition = localPos;
        ripple.gameObject.SetActive(true);
        StartCoroutine(AnimateRipple(ripple));
    }

    private IEnumerator AnimateRipple(Image ripple)
    {
        ripple.rectTransform.localScale = Vector3.zero;
        var c = accentColor; c.a = 0.6f; ripple.color = c;

        float t = 0f;
        while (t < rippleDuration)
        {
            t += Time.deltaTime;
            float p = t / rippleDuration;
            float s = Mathf.Lerp(0f, rippleMaxScale, EaseOutCubic(p));
            float a = Mathf.Lerp(0.6f, 0f, p);
            ripple.rectTransform.localScale = Vector3.one * s;
            c.a = a; ripple.color = c;
            yield return null;
        }
        ripple.gameObject.SetActive(false);
    }

    // ── color helpers ─────────────────────────────────────────────────────────
    private void SetBgColor(Color target, bool instant = false)
    {
        if (background == null) return;
        if (instant) { background.color = target; return; }
        StartCoroutine(TweenColor(background, background.color, target, 0.15f));
    }

    private void SetLabelColor(Color target, bool instant = false)
    {
        if (label == null) return;
        if (instant) { label.color = target; return; }
        StartCoroutine(TweenTMPColor(label, label.color, target, 0.15f));
    }

    private void ApplyAccentColor()
    {
        if (glowBorder != null) glowBorder.color = accentColor;
    }

    // ── public setters ────────────────────────────────────────────────────────
    public void SetAccentColor(Color color)
    {
        accentColor = color;
        ApplyAccentColor();
        labelHoverColor = color;
    }

    public void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }

    // ── tweens ────────────────────────────────────────────────────────────────
    private static IEnumerator TweenColor(Image img, Color from, Color to, float dur)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; img.color = Color.Lerp(from, to, t / dur); yield return null; }
        img.color = to;
    }

    private static IEnumerator TweenTMPColor(TextMeshProUGUI tmp, Color from, Color to, float dur)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; tmp.color = Color.Lerp(from, to, t / dur); yield return null; }
        tmp.color = to;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}
}
