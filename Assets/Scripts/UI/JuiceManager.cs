using System.Collections;
using UnityEngine;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Juice Effects Manager
/// Singleton providing: screen shake, camera zoom punch, hit flash overlay,
/// time-scale freeze-frame, and a pooled particle burst spawner.
/// 
/// Place on a persistent GameObject (DontDestroyOnLoad optional).
/// The camera should be tagged "MainCamera".
/// </summary>
public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    [Header("=== Hit Flash ===")]
    [SerializeField] private UnityEngine.UI.Image hitFlashOverlay;  // Full-screen white Image (alpha 0 normally)
    [SerializeField] private float                flashDuration = 0.08f;

    [Header("=== Particle Prefabs ===")]
    [SerializeField] private ParticleSystem crystalBurstPrefab;
    [SerializeField] private ParticleSystem deathBurstPrefab;
    [SerializeField] private ParticleSystem scorePopBurstPrefab;

    [Header("=== Shake Defaults ===")]
    [SerializeField] private float defaultShakeMagnitude = 0.18f;
    [SerializeField] private float defaultShakeDuration  = 0.25f;

    // ── internal ──────────────────────────────────────────────────────────────
    private Transform    _camTransform;
    private Vector3      _camOriginalPos;
    private Coroutine    _shakeRoutine;
    private Coroutine    _flashRoutine;
    private Coroutine    _freezeRoutine;

    // Simple pool
    private ParticleSystem[] _crystalPool;
    private int              _crystalPoolIdx;
    private const int        POOL_SIZE = 12;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var cam = Camera.main;
        if (cam != null)
        {
            _camTransform   = cam.transform;
            _camOriginalPos = _camTransform.localPosition;
        }

        BuildPool();
    }

    private void BuildPool()
    {
        if (crystalBurstPrefab == null) return;
        _crystalPool = new ParticleSystem[POOL_SIZE];
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var ps = Instantiate(crystalBurstPrefab, transform);
            ps.gameObject.SetActive(false);
            _crystalPool[i] = ps;
        }
    }

    // ── public API ────────────────────────────────────────────────────────────

    /// <summary>Trauma-based screen shake. strength 0..1.</summary>
    public void Shake(float strength = 1f, float duration = -1f)
    {
        float dur = duration > 0f ? duration : defaultShakeDuration;
        float mag = defaultShakeMagnitude * strength;

        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(mag, dur));
    }

    /// <summary>Quick white hit flash overlay.</summary>
    public void HitFlash(Color color = default, float duration = -1f)
    {
        if (hitFlashOverlay == null) return;
        Color c = color == default ? Color.white : color;
        float d = duration > 0f ? duration : flashDuration;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(c, d));
    }

    /// <summary>Briefly freeze time then restore. Great for big hits.</summary>
    public void FreezeFrame(float duration = 0.06f, float timeScale = 0.05f)
    {
        if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
        _freezeRoutine = StartCoroutine(FreezeRoutine(duration, timeScale));
    }

    /// <summary>Camera zoom punch: briefly zooms in then springs back.</summary>
    public void ZoomPunch(float amount = 0.08f, float duration = 0.2f)
    {
        StartCoroutine(ZoomPunchRoutine(amount, duration));
    }

    /// <summary>Spawn a pooled crystal burst at world position.</summary>
    public void SpawnCrystalBurst(Vector3 worldPos, Color color = default)
    {
        if (_crystalPool == null) return;
        var ps = _crystalPool[_crystalPoolIdx % POOL_SIZE];
        _crystalPoolIdx++;

        ps.gameObject.SetActive(true);
        ps.transform.position = worldPos;

        if (color != default)
        {
            var main = ps.main;
            main.startColor = color;
        }

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();

        StartCoroutine(ReturnToPool(ps, ps.main.duration + ps.main.startLifetime.constantMax));
    }

    /// <summary>Spawn a death explosion burst at world position.</summary>
    public void SpawnDeathBurst(Vector3 worldPos)
    {
        if (deathBurstPrefab == null) return;
        var ps = Instantiate(deathBurstPrefab, worldPos, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, 3f);

        HitFlash(new Color(1f, 0.3f, 0.3f, 0.4f), 0.15f);
        Shake(0.8f, 0.4f);
        FreezeFrame(0.06f);
    }

    /// <summary>Combo effect: shake + flash + zoom.</summary>
    public void ComboEffect(int comboCount)
    {
        float intensity = Mathf.Clamp01(comboCount / 10f);
        Shake(0.3f + intensity * 0.5f, 0.2f);
        HitFlash(new Color(0.4f, 1f, 0.85f, 0.25f + intensity * 0.2f), 0.1f);
        if (comboCount >= 10) ZoomPunch(0.06f, 0.25f);
    }

    /// <summary>Crystal collect: small burst + tiny shake.</summary>
    public void CrystalCollectEffect(Vector3 worldPos, Color crystalColor)
    {
        SpawnCrystalBurst(worldPos, crystalColor);
        Shake(0.15f, 0.1f);
    }

    // ── routines ──────────────────────────────────────────────────────────────
    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        if (_camTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Trauma decay: shake lessens over time
            float currentMag = magnitude * (1f - progress);
            float angle      = Random.value * Mathf.PI * 2f;

            _camTransform.localPosition = _camOriginalPos + new Vector3(
                Mathf.Cos(angle) * currentMag,
                Mathf.Sin(angle) * currentMag,
                0f);
            yield return null;
        }
        _camTransform.localPosition = _camOriginalPos;
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        hitFlashOverlay.color = new Color(color.r, color.g, color.b, 0.75f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.75f, 0f, t / duration);
            hitFlashOverlay.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        hitFlashOverlay.color = Color.clear;
    }

    private static IEnumerator FreezeRoutine(float duration, float timeScale)
    {
        float original = Time.timeScale;
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = original;
    }

    private IEnumerator ZoomPunchRoutine(float amount, float duration)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        float origFOV = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        float half    = duration * 0.5f;
        float t       = 0f;

        // Zoom in
        while (t < half)
        {
            t += Time.deltaTime;
            float size = origFOV - amount * (t / half);
            SetCameraSize(cam, size);
            yield return null;
        }
        // Zoom out with spring
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p    = t / half;
            float size = origFOV - amount * (1f - EaseOutBack(p));
            SetCameraSize(cam, size);
            yield return null;
        }
        SetCameraSize(cam, origFOV);
    }

    private static void SetCameraSize(Camera cam, float size)
    {
        if (cam.orthographic) cam.orthographicSize = size;
        else                  cam.fieldOfView       = size;
    }

    private static IEnumerator ReturnToPool(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        ps.gameObject.SetActive(false);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
}
