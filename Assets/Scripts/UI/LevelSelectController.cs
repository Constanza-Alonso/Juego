using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShadowBeat.UI
{

/// <summary>
/// Shadow Beat – Level Select Controller
/// Spawns polished level cards from data, handles scroll-snap, unlock animations,
/// and delegates scene loading back to MainMenuController.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    // ── Inspector references ───────────────────────────────────────────────────
    [Header("=== Card System ===")]
    [SerializeField] private Transform      cardContainer;      // Horizontal layout group parent
    [SerializeField] private GameObject     levelCardPrefab;    // See LevelCard prefab setup below
    [SerializeField] private ScrollRect     scrollRect;

    [Header("=== Snap Settings ===")]
    [SerializeField] private float snapSpeed       = 12f;
    [SerializeField] private float cardSpacing     = 320f;      // Match HorizontalLayoutGroup spacing + card width

    [Header("=== Shape Selector ===")]
    [SerializeField] private Button[]           shapeButtons;   // Cube / Sphere / Pyramid / Diamond
    [SerializeField] private Image[]            shapeIcons;
    [SerializeField] private Color              selectedTint  = new Color(0.4f, 1f, 0.9f);
    [SerializeField] private Color              normalTint    = new Color(1f, 1f, 1f, 0.45f);

    [Header("=== Play Button ===")]
    [SerializeField] private Button            playNowButton;
    [SerializeField] private CanvasGroup       playNowGroup;
    [SerializeField] private TextMeshProUGUI   playNowLabel;

    [Header("=== Back ===")]
    [SerializeField] private Button            backButton;

    [Header("=== Audio ===")]
    [SerializeField] private AudioSource       sfxSource;
    [SerializeField] private AudioClip         cardSelectSFX;
    [SerializeField] private AudioClip         lockedSFX;
    [SerializeField] private AudioClip         unlockSFX;
    [SerializeField] private AudioClip         playSFX;

    // ── level data (edit here or swap for ScriptableObjects) ──────────────────
    [System.Serializable]
    public class LevelData
    {
        public string  sceneName;
        public string  displayName;
        public string  worldName;       // e.g. "Bosque Neon"
        public Sprite  thumbnail;
        public Color   accentColor;
        public int     starsEarned;     // 0-3, loaded from PlayerPrefs at runtime
        public bool    isLocked;
        public string  prefs_BestScore; // PlayerPrefs key
        public string  prefs_Unlocked;  // PlayerPrefs key
    }

    [SerializeField] private List<LevelData> levels = new List<LevelData>
    {
        new LevelData { displayName = "Neon Cero",            worldName = "Ciudad Neon",     sceneName = "Level01_NeonCero",           accentColor = new Color(0.2f, 1f, 0.5f), prefs_BestScore = "ShadowBeat_Level_1_BestScore", isLocked = false },
        new LevelData { displayName = "Ciudad del Vortice",   worldName = "Ciudad Neon",     sceneName = "Level02_CiudadDelVortice",  accentColor = new Color(0.3f, 0.7f, 1f), prefs_BestScore = "ShadowBeat_Level_2_BestScore", isLocked = true  },
        new LevelData { displayName = "Abismo Pulsante",      worldName = "Ciudad Neon",     sceneName = "Level03_AbismoPulsante",    accentColor = new Color(1f, 0.4f, 0.9f), prefs_BestScore = "ShadowBeat_Level_3_BestScore", isLocked = true  },
        new LevelData { displayName = "Ciclo Galactico",      worldName = "Ruinas Cyber",    sceneName = "Level04_CicloGalactico",    accentColor = new Color(1f, 0.6f, 0.2f), prefs_BestScore = "ShadowBeat_Level_4_BestScore", isLocked = true  },
        new LevelData { displayName = "Ecos de Cristal",      worldName = "Ruinas Cyber",    sceneName = "Level05_EcosDeCristal",     accentColor = new Color(0.8f, 0.3f, 1f), prefs_BestScore = "ShadowBeat_Level_5_BestScore", isLocked = true  },
        new LevelData { displayName = "Sincronia Ritmica",    worldName = "Ruinas Cyber",    sceneName = "Level06_SincroniaRitmica", accentColor = new Color(1f, 1f, 0.3f),   prefs_BestScore = "ShadowBeat_Level_6_BestScore", isLocked = true  },
        new LevelData { displayName = "Portal Infinito",      worldName = "Ciudad Invertida", sceneName = "Level07_PortalInfinito",    accentColor = new Color(1f, 0.3f, 0.4f), prefs_BestScore = "ShadowBeat_Level_7_BestScore", isLocked = true  },
    };

    // ── state ─────────────────────────────────────────────────────────────────
    private int               _selectedIndex  = 0;
    private int               _selectedShape  = 0;
    private static readonly int[] ShapePreferenceByButton = { 0, 2, 3, 1 };
    private List<LevelCard>   _cards          = new();
    private MainMenuController _mainMenu;
    private bool              _isDragging;
    private bool              _isSnapping;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _mainMenu = FindObjectOfType<MainMenuController>();
        int savedShape = PlayerPrefs.GetInt("ShadowBeat_LuxShape", 0);
        for (int i = 0; i < ShapePreferenceByButton.Length; i++)
        {
            if (ShapePreferenceByButton[i] == savedShape)
            {
                _selectedShape = i;
                break;
            }
        }
    }

    private void OnEnable()
    {
        RefreshLevelData();
        SpawnCards();
        RefreshShapeButtons();
        UpdatePlayButton();
        ScrollToCard(_selectedIndex, instant: true);
    }

    private void OnDisable()
    {
        DestroyCards();
    }

    private void Update()
    {
        HandleScrollSnap();
    }

    // ── level data ────────────────────────────────────────────────────────────
    private void RefreshLevelData()
    {
        int unlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt("ShadowBeat_UnlockedLevel", 1), 1, levels.Count);
        for (int i = 0; i < levels.Count; i++)
        {
            var lvl = levels[i];
            int levelNumber = i + 1;
            bool completed = PlayerPrefs.GetInt($"ShadowBeat_Level_{levelNumber}_Complete", 0) == 1;
            lvl.isLocked = levelNumber > unlockedLevel;
            lvl.starsEarned = completed ? 3 : PlayerPrefs.GetInt(lvl.prefs_BestScore, 0) > 0 ? 1 : 0;
        }
    }

    // ── card spawning ─────────────────────────────────────────────────────────
    private void SpawnCards()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            var go   = Instantiate(levelCardPrefab, cardContainer);
            var card = go.GetComponent<LevelCard>();
            if (card == null) card = go.AddComponent<LevelCard>();

            int capturedIndex = i;
            card.Setup(levels[i], () => OnCardSelected(capturedIndex));
            _cards.Add(card);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer as RectTransform);
    }

    private void DestroyCards()
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    // ── card selection ────────────────────────────────────────────────────────
    private void OnCardSelected(int index)
    {
        if (index == _selectedIndex)
        {
            // Tap same card → try to play
            TryPlay();
            return;
        }

        _selectedIndex = index;
        RefreshCardVisuals();
        UpdatePlayButton();
        ScrollToCard(index, instant: false);
        PlaySFX(levels[index].isLocked ? lockedSFX : cardSelectSFX);

        if (levels[index].isLocked)
            StartCoroutine(ShakeCard(_cards[index].GetComponent<RectTransform>()));
    }

    private void RefreshCardVisuals()
    {
        for (int i = 0; i < _cards.Count; i++)
            _cards[i].SetSelected(i == _selectedIndex);
    }

    // ── shape selector ────────────────────────────────────────────────────────
    private void RefreshShapeButtons()
    {
        for (int i = 0; i < shapeButtons.Length; i++)
        {
            int captured = i;
            shapeButtons[i].onClick.RemoveAllListeners();
            shapeButtons[i].onClick.AddListener(() => SelectShape(captured));
            if (shapeIcons != null && i < shapeIcons.Length)
                shapeIcons[i].color = (i == _selectedShape) ? selectedTint : normalTint;
        }
    }

    private void SelectShape(int index)
    {
        _selectedShape = index;
        PlayerPrefs.SetInt("ShadowBeat_LuxShape", ShapePreferenceByButton[index]);
        for (int i = 0; i < shapeIcons.Length; i++)
            shapeIcons[i].color = (i == index) ? selectedTint : normalTint;

        // Bounce the selected icon
        if (shapeIcons != null && index < shapeIcons.Length)
            StartCoroutine(BounceScale(shapeIcons[index].rectTransform, 0.25f, 0.25f));

        PlaySFX(cardSelectSFX);
    }

    // ── play button ───────────────────────────────────────────────────────────
    private void UpdatePlayButton()
    {
        bool locked = levels[_selectedIndex].isLocked;
        if (playNowLabel != null)
            playNowLabel.text = locked ? "BLOQUEADO" : "JUGAR AHORA";
        if (playNowGroup  != null)
            playNowGroup.alpha = locked ? 0.45f : 1f;

        playNowButton.interactable = !locked;
        playNowButton.onClick.RemoveAllListeners();
        if (!locked) playNowButton.onClick.AddListener(TryPlay);
    }

    private void TryPlay()
    {
        var lvl = levels[_selectedIndex];
        if (lvl.isLocked) { PlaySFX(lockedSFX); return; }

        PlaySFX(playSFX);
        PlayerPrefs.SetInt("ShadowBeat_LuxShape", ShapePreferenceByButton[_selectedShape]);
        PlayerPrefs.SetString("CurrentLevel",    lvl.sceneName);
        PlayerPrefs.Save();

        _mainMenu?.LoadLevel(lvl.sceneName);
    }

    // ── back button ───────────────────────────────────────────────────────────
    public void OnBackClicked()
    {
        _mainMenu?.OnBackToMain();
    }

    // ── scroll-snap ───────────────────────────────────────────────────────────
    private void ScrollToCard(int index, bool instant)
    {
        if (scrollRect == null || _cards.Count == 0) return;

        float contentWidth = cardContainer.GetComponent<RectTransform>().rect.width;
        float viewWidth    = scrollRect.viewport.rect.width;
        float maxScroll    = Mathf.Max(0f, contentWidth - viewWidth);

        float targetX = index * cardSpacing - (viewWidth - cardSpacing) * 0.5f;
        float norm    = Mathf.Clamp01(targetX / maxScroll);

        if (instant)
            scrollRect.horizontalNormalizedPosition = norm;
        else
            StartCoroutine(AnimateScrollTo(norm));
    }

    private IEnumerator AnimateScrollTo(float target)
    {
        _isSnapping = true;
        float t = 0f;
        float start = scrollRect.horizontalNormalizedPosition;
        while (t < 1f)
        {
            t += Time.deltaTime * snapSpeed * 0.5f;
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, EaseOutCubic(t));
            yield return null;
        }
        scrollRect.horizontalNormalizedPosition = target;
        _isSnapping = false;
    }

    private void HandleScrollSnap()
    {
        if (scrollRect == null || _isSnapping) return;

        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            // Find nearest card after drag-release
            float viewWidth = scrollRect.viewport.rect.width;
            float scrollX   = scrollRect.content.anchoredPosition.x * -1f + (viewWidth - cardSpacing) * 0.5f;
            int   nearest   = Mathf.RoundToInt(scrollX / cardSpacing);
            nearest = Mathf.Clamp(nearest, 0, _cards.Count - 1);

            if (nearest != _selectedIndex)
                _selectedIndex = nearest;

            RefreshCardVisuals();
            UpdatePlayButton();
            ScrollToCard(_selectedIndex, instant: false);
        }
    }

    // ── animations ────────────────────────────────────────────────────────────
    private static IEnumerator ShakeCard(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector2 orig = rt.anchoredPosition;
        float   dur  = 0.4f;
        float   t    = 0f;
        float   mag  = 10f;
        float   freq = 28f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float decay = 1f - (t / dur);
            float offset = Mathf.Sin(t * freq) * mag * decay;
            rt.anchoredPosition = orig + new Vector2(offset, 0f);
            yield return null;
        }
        rt.anchoredPosition = orig;
    }

    private static IEnumerator BounceScale(RectTransform rt, float amount, float duration)
    {
        if (rt == null) yield break;
        Vector3 orig = rt.localScale;
        float   t    = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float s = 1f + amount * Mathf.Sin(p * Mathf.PI);
            rt.localScale = orig * s;
            yield return null;
        }
        rt.localScale = orig;
    }

    // ── utilities ─────────────────────────────────────────────────────────────
    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}
}
