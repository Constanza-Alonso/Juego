using ShadowBeat.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIKitMainMenuController = ShadowBeat.UI.MainMenuController;

public static class ShadowBeatUIKitSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackgroundPath = "Assets/Generated/GameplayBackground.png";
    private const string RoundedButtonPath = "Assets/Generated/RoundedButton.png";
    private static readonly Color Cyan = new Color(0.30f, 1f, 0.78f);
    private static readonly Color Blue = new Color(0.30f, 0.78f, 1f);
    private static readonly Color Magenta = new Color(1f, 0.30f, 0.80f);
    private static readonly Color Orange = new Color(1f, 0.55f, 0.16f);

    [MenuItem("Shadow Beat/Install UI Kit Menu")]
    public static void CreateMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Shadow Beat", "Sali de Play Mode antes de instalar el UI Kit.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        CreateCamera();
        GameObject canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        CreateOverlay(canvas.transform);
        CreateEventSystem();

        CanvasGroup logoGroup = CreateGroup(canvas.transform, "LogoRoot", new Vector2(0f, 245f), new Vector2(780f, 155f));
        TextMeshProUGUI title = CreateText(logoGroup.transform, "TitleText", "", new Vector2(0f, 25f), new Vector2(760f, 80f), 66, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        TextMeshProUGUI subtitle = CreateText(logoGroup.transform, "SubtitleText", "FRAGMENTOS DE LUZ", new Vector2(0f, -48f), new Vector2(520f, 42f), 22, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.75f, 1f, 1f));
        AddTextGlow(title, Orange);
        AddTextGlow(subtitle, Cyan);

        CanvasGroup mainPanel = CreatePanel(canvas.transform, "MainPanel", new Vector2(0f, -85f), new Vector2(460f, 340f));
        Button play = CreateNeonButton(mainPanel.transform, "PlayButton", "JUGAR", new Vector2(0f, 95f), new Vector2(360f, 62f), Cyan, 26);
        Button settings = CreateNeonButton(mainPanel.transform, "SettingsButton", "AJUSTES", new Vector2(0f, 25f), new Vector2(360f, 54f), Blue, 18);
        Button credits = CreateNeonButton(mainPanel.transform, "CreditsButton", "CREDITOS", new Vector2(0f, -39f), new Vector2(360f, 54f), Magenta, 18);
        Button quit = CreateNeonButton(mainPanel.transform, "QuitButton", "SALIR", new Vector2(0f, -103f), new Vector2(360f, 54f), new Color(1f, 0.3f, 0.3f), 18);

        CanvasGroup levelPanel = CreatePanel(canvas.transform, "LevelSelectPanel", new Vector2(0f, -70f), new Vector2(1040f, 560f));
        BuildLevelSelection(levelPanel);

        CanvasGroup settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0f, -70f), new Vector2(620f, 430f));
        CreateText(settingsPanel.transform, "SettingsTitle", "AJUSTES", new Vector2(0f, 150f), new Vector2(500f, 55f), 32, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
        CreateText(settingsPanel.transform, "SettingsCopy", "VOLUMEN Y EFECTOS\n\nLos valores se guardan automaticamente.", new Vector2(0f, 25f), new Vector2(500f, 150f), 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        Button settingsBack = CreateNeonButton(settingsPanel.transform, "BackButton", "VOLVER", new Vector2(0f, -145f), new Vector2(270f, 52f), Blue, 18);

        CanvasGroup creditsPanel = CreatePanel(canvas.transform, "CreditsPanel", new Vector2(0f, -70f), new Vector2(620f, 430f));
        CreateText(creditsPanel.transform, "CreditsTitle", "SHADOW BEAT", new Vector2(0f, 140f), new Vector2(500f, 55f), 32, FontStyles.Bold, TextAlignmentOptions.Center, Magenta);
        CreateText(creditsPanel.transform, "CreditsCopy", "FRAGMENTOS DE LUZ\n\nDiseno y desarrollo: Constanza Alonso\nDesarrollado con Unity", new Vector2(0f, 10f), new Vector2(520f, 190f), 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        Button creditsBack = CreateNeonButton(creditsPanel.transform, "BackButton", "VOLVER", new Vector2(0f, -145f), new Vector2(270f, 52f), Magenta, 18);

        CanvasGroup fade = CreateGroup(canvas.transform, "FadeOverlay", Vector2.zero, new Vector2(1280f, 720f));
        Image fadeImage = fade.gameObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
        fade.transform.SetAsLastSibling();

        UIKitMainMenuController controller = new GameObject("UIKitMainMenuController").AddComponent<UIKitMainMenuController>();
        SerializedObject so = new SerializedObject(controller);
        Set(so, "mainPanel", mainPanel);
        Set(so, "levelSelectPanel", levelPanel);
        Set(so, "settingsPanel", settingsPanel);
        Set(so, "creditsPanel", creditsPanel);
        Set(so, "logoRect", logoGroup.GetComponent<RectTransform>());
        Set(so, "logoGroup", logoGroup);
        Set(so, "titleText", title);
        Set(so, "subtitleText", subtitle);
        Set(so, "playButton", play);
        Set(so, "settingsButton", settings);
        Set(so, "creditsButton", credits);
        Set(so, "quitButton", quit);
        Set(so, "fadeOverlay", fade);
        so.ApplyModifiedProperties();

        UnityEventTools.AddPersistentListener(settingsBack.onClick, controller.OnBackToMain);
        UnityEventTools.AddPersistentListener(creditsBack.onClick, controller.OnBackToMain);

        UIKitMenuSelection selection = levelPanel.GetComponent<UIKitMenuSelection>();
        SerializedObject selectionSo = new SerializedObject(selection);
        Set(selectionSo, "menuController", controller);
        selectionSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath);
    }

    private static void BuildLevelSelection(CanvasGroup panel)
    {
        CreateText(panel.transform, "LevelTitle", "SELECCION DE NIVEL", new Vector2(-250f, 220f), new Vector2(480f, 48f), 28, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
        CreateText(panel.transform, "ShapeTitle", "FORMA DE LUX", new Vector2(305f, 220f), new Vector2(390f, 48f), 28, FontStyles.Bold, TextAlignmentOptions.Center, Magenta);

        string[] levels = { "NEON CERO", "CIUDAD DEL VORTICE", "ABISMO PULSANTE", "CICLO GALACTICO", "ECOS DE CRISTAL", "SINCRONIA RITMICA", "PORTAL INFINITO" };
        Color[] colors = { Cyan, Blue, Magenta, Orange, new Color(0.72f, 0.45f, 1f), new Color(1f, 0.9f, 0.2f), Cyan };
        Button[] levelButtons = new Button[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            levelButtons[i] = CreateNeonButton(panel.transform, $"LevelButton{i + 1}", $"{i + 1}.  {levels[i]}", new Vector2(-250f, 165f - i * 48f), new Vector2(450f, 42f), colors[i], 14);
        }

        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/WhitePixel.png");
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/CircleShape.png");
        Sprite triangle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/TriangleSpike.png");
        Button[] shapeButtons =
        {
            CreateShapeButton(panel.transform, "CUBO", new Vector2(235f, 110f), white, Orange, 0f),
            CreateShapeButton(panel.transform, "ESFERA", new Vector2(395f, 110f), circle, Cyan, 0f),
            CreateShapeButton(panel.transform, "PIRAMIDE", new Vector2(235f, -60f), triangle, new Color(0.62f, 0.35f, 1f), 0f),
            CreateShapeButton(panel.transform, "DIAMANTE", new Vector2(395f, -60f), white, Magenta, 45f)
        };

        TextMeshProUGUI selectedLevel = CreateText(panel.transform, "SelectedLevelLabel", "", new Vector2(-250f, -196f), new Vector2(460f, 34f), 16, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.85f, 1f, 1f));
        TextMeshProUGUI selectedShape = CreateText(panel.transform, "SelectedShapeLabel", "", new Vector2(315f, -196f), new Vector2(390f, 34f), 16, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.95f));
        Button playNow = CreateNeonButton(panel.transform, "PlayNowButton", "JUGAR AHORA", new Vector2(180f, -245f), new Vector2(340f, 58f), Orange, 23);
        Button back = CreateNeonButton(panel.transform, "BackButton", "VOLVER", new Vector2(-350f, -245f), new Vector2(210f, 52f), Blue, 16);

        UIKitMenuSelection selection = panel.gameObject.AddComponent<UIKitMenuSelection>();
        SerializedObject so = new SerializedObject(selection);
        Set(so, "selectedLevelLabel", selectedLevel);
        Set(so, "selectedShapeLabel", selectedShape);
        Set(so, "playButton", playNow);
        Set(so, "backButton", back);
        SetArray(so, "levelButtons", levelButtons);
        SetArray(so, "shapeButtons", shapeButtons);
        so.ApplyModifiedProperties();
    }

    private static Button CreateShapeButton(Transform parent, string label, Vector2 position, Sprite sprite, Color color, float rotation)
    {
        Button button = CreateNeonButton(parent, $"Shape{label}", label, position, new Vector2(140f, 145f), color, 14);
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.rectTransform.anchoredPosition = new Vector2(0f, -50f);
        Image icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        icon.transform.SetParent(button.transform, false);
        icon.sprite = sprite;
        icon.color = color;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.rectTransform.sizeDelta = new Vector2(62f, 62f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, 15f);
        icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        return button;
    }

    private static Button CreateNeonButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color accent, int fontSize)
    {
        Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedButtonPath);
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image background = go.GetComponent<Image>();
        background.sprite = rounded;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.035f, 0.045f, 0.09f, 0.94f);

        Image aura = CreateImage(go.transform, "GlowAura", Vector2.zero, size + new Vector2(16f, 16f), accent);
        aura.sprite = rounded;
        aura.type = Image.Type.Sliced;
        aura.color = new Color(accent.r, accent.g, accent.b, 0f);
        aura.raycastTarget = false;
        aura.transform.SetAsFirstSibling();

        Image border = CreateImage(go.transform, "GlowBorder", Vector2.zero, size - new Vector2(4f, 4f), new Color(accent.r, accent.g, accent.b, 0.42f));
        border.sprite = rounded;
        border.type = Image.Type.Sliced;
        border.raycastTarget = false;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, Vector2.zero, size - new Vector2(24f, 8f), fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        NeonButton neon = go.AddComponent<NeonButton>();
        SerializedObject so = new SerializedObject(neon);
        Set(so, "background", background);
        Set(so, "glowBorder", border);
        Set(so, "glowAura", aura);
        Set(so, "label", text);
        so.FindProperty("accentColor").colorValue = accent;
        so.ApplyModifiedProperties();
        return go.GetComponent<Button>();
    }

    private static CanvasGroup CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        CanvasGroup group = CreateGroup(parent, name, position, size);
        Image image = group.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedButtonPath);
        image.type = Image.Type.Sliced;
        image.color = new Color(0.025f, 0.035f, 0.075f, 0.88f);
        Outline outline = group.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f);
        outline.effectDistance = new Vector2(2f, -2f);
        return group;
    }

    private static CanvasGroup CreateGroup(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go.GetComponent<CanvasGroup>();
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        return image;
    }

    private static void CreateBackground(Transform parent)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        Image image = CreateImage(parent, "Background", Vector2.zero, new Vector2(1280f, 720f), Color.white);
        image.sprite = sprite;
        image.preserveAspect = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.sizeDelta = Vector2.zero;
    }

    private static void CreateOverlay(Transform parent)
    {
        Image image = CreateImage(parent, "DarkOverlay", Vector2.zero, Vector2.zero, new Color(0f, 0.01f, 0.035f, 0.38f));
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.sizeDelta = Vector2.zero;
        image.raycastTarget = false;
    }

    private static GameObject CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    private static void CreateCamera()
    {
        GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        Camera camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void AddTextGlow(TextMeshProUGUI text, Color color)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(color.r, color.g, color.b, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private static void Set(SerializedObject so, string name, Object value)
    {
        so.FindProperty(name).objectReferenceValue = value;
    }

    private static void SetArray(SerializedObject so, string name, Object[] values)
    {
        SerializedProperty array = so.FindProperty(name);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
