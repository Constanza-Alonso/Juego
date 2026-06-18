using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Animated Toggle Switch
/// A polished iOS-style toggle with a sliding knob, color tween, and glow.
///
/// === Prefab hierarchy ===
/// ToggleSwitch (this script, Button)
///   ├─ Track     (Image – rounded rect, changes color)
///   ├─ Knob      (Image – circle, slides left/right)
///   └─ KnobGlow  (Image – soft shadow behind knob)
/// </summary>
[RequireComponent(typeof(Button))]
public class ToggleSwitch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("=== References ===")]
    [SerializeField] private RectTransform knob;
    [SerializeField] private RectTransform knobGlow;
    [SerializeField] private Image         track;

    [Header("=== Colors ===")]
    [SerializeField] private Color onColor   = new Color(0.3f, 1f, 0.75f);
    [SerializeField] private Color offColor  = new Color(0.25f, 0.25f, 0.35f);

    [Header("=== Layout ===")]
    [SerializeField] private float knobOnX   =  22f;
    [SerializeField] private float knobOffX  = -22f;
    [SerializeField] private float animSpeed =  10f;

    // ── events ────────────────────────────────────────────────────────────────
    public event Action<bool> OnValueChanged;

    // ── state ─────────────────────────────────────────────────────────────────
    private bool  _isOn;
    private float _targetX;
    private Color _targetColor;
    private bool  _hovered;

    public bool IsOn => _isOn;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
        _targetX     = _isOn ? knobOnX : knobOffX;
        _targetColor = _isOn ? onColor  : offColor;
        ApplyInstant();
    }

    private void Update()
    {
        // Smooth knob slide
        if (knob != null)
        {
            var pos = knob.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, _targetX, Time.deltaTime * animSpeed);
            knob.anchoredPosition = pos;

            // Squish knob while moving
            float delta = Mathf.Abs(pos.x - _targetX);
            float squish = 1f + Mathf.Clamp01(delta / 30f) * 0.3f;
            knob.localScale = new Vector3(squish, 1f / squish, 1f);
        }

        // Smooth track color
        if (track != null)
            track.color = Color.Lerp(track.color, _targetColor, Time.deltaTime * animSpeed);

        // Sync glow to knob
        if (knobGlow != null && knob != null)
            knobGlow.anchoredPosition = knob.anchoredPosition;
    }

    // ── public API ────────────────────────────────────────────────────────────
    public void Toggle()
    {
        SetState(!_isOn, animate: true);
    }

    public void SetState(bool on, bool animate = true)
    {
        _isOn        = on;
        _targetX     = on ? knobOnX  : knobOffX;
        _targetColor = on ? onColor  : offColor;

        if (!animate) ApplyInstant();
        OnValueChanged?.Invoke(_isOn);

        // Haptic feedback (mobile)
#if UNITY_IOS || UNITY_ANDROID
        if (on) Handheld.Vibrate();
#endif
    }

    // ── pointer ───────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        _hovered = true;
        if (knobGlow != null)
            StartCoroutine(FadeGlow(0.5f, 0.15f));
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovered = false;
        if (knobGlow != null)
            StartCoroutine(FadeGlow(0f, 0.2f));
    }

    // ── instant apply ─────────────────────────────────────────────────────────
    private void ApplyInstant()
    {
        if (knob  != null) knob.anchoredPosition  = new Vector2(_targetX, 0f);
        if (track != null) track.color             = _targetColor;
        if (knobGlow != null)
        {
            var c = Color.white; c.a = 0f;
            knobGlow.GetComponent<Image>().color = c;
        }
    }

    // ── glow ─────────────────────────────────────────────────────────────────
    private IEnumerator FadeGlow(float targetAlpha, float dur)
    {
        var img = knobGlow?.GetComponent<Image>();
        if (img == null) yield break;
        float start = img.color.a, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var c = img.color; c.a = Mathf.Lerp(start, targetAlpha, t / dur);
            img.color = c;
            yield return null;
        }
        var fc = img.color; fc.a = targetAlpha; img.color = fc;
    }
}
}
