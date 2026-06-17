using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShadowBeatSceneCreator
{
    private const string GeneratedFolder = "Assets/Generated";
    private const string SpritePath = GeneratedFolder + "/WhitePixel.png";
    private const string ScenesFolder = "Assets/Scenes";
    private const string MenuScenePath = ScenesFolder + "/MainMenu.unity";

    private readonly struct LevelSpec
    {
        public LevelSpec(int index, string name, string sceneName, string world, Color ground, Color accent, Color background, int difficulty)
        {
            Index = index;
            Name = name;
            SceneName = sceneName;
            World = world;
            Ground = ground;
            Accent = accent;
            Background = background;
            Difficulty = difficulty;
        }

        public int Index { get; }
        public string Name { get; }
        public string SceneName { get; }
        public string World { get; }
        public Color Ground { get; }
        public Color Accent { get; }
        public Color Background { get; }
        public int Difficulty { get; }
    }

    [MenuItem("Shadow Beat/Create Complete Game")]
    public static void CreateCompleteGame()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Shadow Beat", "Sal de Play Mode antes de generar las escenas.", "OK");
            return;
        }

        EnsureSpriteAsset();
        Directory.CreateDirectory(ScenesFolder);

        LevelSpec[] levels = GetLevels();
        CreateMenuScene(levels);

        for (int i = 0; i < levels.Length; i++)
        {
            string nextScene = i + 1 < levels.Length ? levels[i + 1].SceneName : "MainMenu";
            CreateLevelScene(levels[i], nextScene);
        }

        SetBuildScenes(levels);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(MenuScenePath);
        EditorUtility.DisplayDialog("Shadow Beat", "Juego completo generado. Se abrio el menu principal.", "OK");
    }

    [MenuItem("Shadow Beat/Create MVP Scene")]
    public static void CreateMvpScene()
    {
        CreateCompleteGame();
    }

    private static LevelSpec[] GetLevels()
    {
        return new[]
        {
            new LevelSpec(1, "Primer destello", "Level01_PrimerDestello", "Bosque Oscuro", new Color(0.12f, 0.55f, 0.45f), new Color(0.2f, 0.95f, 1f), new Color(0.02f, 0.04f, 0.07f), 1),
            new LevelSpec(2, "Camino de sombras", "Level02_CaminoDeSombras", "Bosque Oscuro", new Color(0.08f, 0.42f, 0.36f), new Color(0.65f, 0.35f, 1f), new Color(0.015f, 0.03f, 0.055f), 2),
            new LevelSpec(3, "El salto perdido", "Level03_ElSaltoPerdido", "Bosque Oscuro", new Color(0.1f, 0.48f, 0.5f), new Color(0.55f, 1f, 0.35f), new Color(0.02f, 0.035f, 0.06f), 3),
            new LevelSpec(4, "Fragmentos rotos", "Level04_FragmentosRotos", "Ruinas de Cristal", new Color(0.24f, 0.58f, 0.72f), new Color(0.72f, 0.9f, 1f), new Color(0.035f, 0.025f, 0.08f), 4),
            new LevelSpec(5, "Torres caidas", "Level05_TorresCaidas", "Ruinas de Cristal", new Color(0.38f, 0.4f, 0.78f), new Color(0.95f, 0.65f, 1f), new Color(0.04f, 0.025f, 0.085f), 5),
            new LevelSpec(6, "Cristal inestable", "Level06_CristalInestable", "Ruinas de Cristal", new Color(0.42f, 0.52f, 0.86f), new Color(1f, 1f, 0.45f), new Color(0.045f, 0.035f, 0.095f), 6),
            new LevelSpec(7, "Gravedad cero", "Level07_GravedadCero", "Ciudad Invertida", new Color(0.28f, 0.32f, 0.88f), new Color(1f, 0.25f, 0.7f), new Color(0.015f, 0.02f, 0.06f), 7)
        };
    }

    private static void CreateMenuScene(LevelSpec[] levels)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        CreateMenuCamera(new Color(0.02f, 0.025f, 0.04f));
        CreateSpriteObject("Menu Background", sprite, new Vector3(0f, 0f, 3f), new Vector2(24f, 14f), new Color(0.02f, 0.025f, 0.04f), -10);

        GameObject canvasObject = CreateCanvas();
        CreateEventSystem();
        MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();

        Text title = CreateText(canvasObject.transform, "Title", "Shadow Beat", new Vector2(0f, -70f), TextAnchor.MiddleCenter, 54, new Vector2(700f, 70f));
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        CreateText(canvasObject.transform, "Subtitle", "Fragmentos de Luz", new Vector2(0f, -126f), TextAnchor.MiddleCenter, 28, new Vector2(700f, 44f));

        for (int i = 0; i < levels.Length; i++)
        {
            LevelSpec level = levels[i];
            int row = i / 2;
            int col = i % 2;
            Vector2 position = new Vector2(col == 0 ? -220f : 220f, -220f - row * 84f);
            Button button = CreateButton(canvasObject.transform, $"Level {level.Index}", $"{level.Index}. {level.Name}\n{level.World}", position, new Vector2(390f, 64f), level.Accent);
            LevelSelectButton selectButton = button.gameObject.AddComponent<LevelSelectButton>();
            SerializedObject selectSo = new SerializedObject(selectButton);
            selectSo.FindProperty("menuController").objectReferenceValue = controller;
            selectSo.FindProperty("sceneName").stringValue = level.SceneName;
            selectSo.ApplyModifiedProperties();
            UnityEventTools.AddPersistentListener(button.onClick, selectButton.LoadAssignedLevel);
        }

        Text hint = CreateText(canvasObject.transform, "Hint", "Space o click para saltar. Completa niveles, recoge cristales y recupera los fragmentos.", new Vector2(0f, 54f), TextAnchor.MiddleCenter, 20, new Vector2(900f, 48f));
        hint.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    private static void CreateLevelScene(LevelSpec spec, string nextSceneName)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = spec.SceneName;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        float endX = 92f + spec.Difficulty * 5f;

        GameObject levelStart = Marker("LevelStart", new Vector3(0f, 0f, 0f));
        GameObject levelEnd = Marker("LevelEnd", new Vector3(endX, 0f, 0f));

        LuxController player = CreateLux(sprite, spec.Difficulty);
        CreateCamera(player.transform, spec.Background);
        CreateLight();
        CreateBackground(sprite, spec.Background, endX);

        CreateCorePath(sprite, spec);
        CreateLevelFeatures(sprite, spec);
        CreateGround(sprite, "Goal Runway", new Vector3(endX - 8f, -2.2f, 0f), new Vector2(48f, 1f), spec.Ground);
        CreateCheckpoint(sprite, new Vector3(45f, -1.2f, 0f), spec.Accent);
        CreateGoal(sprite, new Vector3(endX + 2f, -0.6f, 0f));
        CreateDeathZone(sprite, endX);

        LevelManager manager = new GameObject("LevelManager").AddComponent<LevelManager>();
        SerializedObject managerSo = new SerializedObject(manager);
        managerSo.FindProperty("levelName").stringValue = $"{spec.Index}. {spec.Name}";
        managerSo.FindProperty("levelIndex").intValue = spec.Index;
        managerSo.FindProperty("nextSceneName").stringValue = nextSceneName;
        managerSo.FindProperty("player").objectReferenceValue = player;
        managerSo.FindProperty("levelStart").objectReferenceValue = levelStart.transform;
        managerSo.FindProperty("levelEnd").objectReferenceValue = levelEnd.transform;
        managerSo.ApplyModifiedProperties();

        CreateUi(manager, spec.Index == 7);
        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{spec.SceneName}.unity");
    }

    private static void CreateCorePath(Sprite sprite, LevelSpec spec)
    {
        if (spec.Index == 1)
        {
            CreateGround(sprite, "Tutorial Ground", new Vector3(46f, -2.2f, 0f), new Vector2(98f, 1f), spec.Ground);
            CreateGround(sprite, "Low Step", new Vector3(31f, -1.05f, 0f), new Vector2(6f, 0.45f), spec.Ground);
            CreateGround(sprite, "Safe Step", new Vector3(52f, -1.05f, 0f), new Vector2(7f, 0.45f), spec.Ground);

            CreateHazard(sprite, "Spike 1", new Vector3(18f, -1.35f, 0f), new Vector2(0.5f, 0.5f));
            CreateHazard(sprite, "Spike 2", new Vector3(39f, -1.35f, 0f), new Vector2(0.55f, 0.55f));
            CreateHazard(sprite, "Spike 3", new Vector3(66f, -1.35f, 0f), new Vector2(0.6f, 0.6f));

            CreateCrystal(sprite, new Vector3(12f, -0.8f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(31f, -0.25f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(52f, -0.25f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(78f, -0.8f, 0f), spec.Accent);
            return;
        }

        CreateGround(sprite, "Ground Start", new Vector3(14f, -2.2f, 0f), new Vector2(36f, 1f), spec.Ground);
        CreateGround(sprite, "Platform A", new Vector3(24f, -0.45f, 0f), new Vector2(7f, 0.45f), spec.Ground);
        CreateGround(sprite, "Platform B", new Vector3(34f, 0.35f, 0f), new Vector2(8f, 0.45f), spec.Ground);
        CreateGround(sprite, "Platform C", new Vector3(44f, -0.45f, 0f), new Vector2(8f, 0.45f), spec.Ground);
        CreateGround(sprite, "Ground Mid", new Vector3(60f, -2.2f, 0f), new Vector2(32f, 1f), spec.Ground);
        CreateGround(sprite, "Final Ground", new Vector3(91f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);

        CreateHazard(sprite, "Spike 1", new Vector3(18f, -1.35f, 0f), new Vector2(0.55f, 0.55f));
        CreateHazard(sprite, "Spike 2", new Vector3(29f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
        CreateHazard(sprite, "Spike 3", new Vector3(41f, -1.35f, 0f), new Vector2(0.65f, 0.65f));

        CreateCrystal(sprite, new Vector3(12f, -0.8f, 0f), spec.Accent);
        CreateCrystal(sprite, new Vector3(24f, 0.55f, 0f), spec.Accent);
        CreateCrystal(sprite, new Vector3(34f, 1.25f, 0f), spec.Accent);
        CreateCrystal(sprite, new Vector3(80f, -0.8f, 0f), spec.Accent);
    }

    private static void CreateLevelFeatures(Sprite sprite, LevelSpec spec)
    {
        if (spec.Index == 1)
        {
            CreatePortal(sprite, "Portal Cubo", PortalType.Cube, new Vector3(66f, -0.6f, 0f), new Color(0.2f, 0.95f, 1f));
            return;
        }

        if (spec.Index == 2)
        {
            CreatePortal(sprite, "Portal Sombra", PortalType.Shadow, new Vector3(50f, -0.6f, 0f), new Color(0.65f, 0.35f, 1f));
            CreateShadowBarrier(sprite, new Vector3(62f, -0.7f, 0f));
            CreatePortal(sprite, "Portal Cubo", PortalType.Cube, new Vector3(72f, -0.6f, 0f), new Color(0.2f, 0.95f, 1f));
            return;
        }

        if (spec.Index == 3)
        {
            CreatePortal(sprite, "Portal Esfera", PortalType.Sphere, new Vector3(52f, -0.6f, 0f), new Color(0.55f, 1f, 0.35f));
            CreateGround(sprite, "Sphere Ceiling", new Vector3(62f, 2.9f, 0f), new Vector2(12f, 0.45f), spec.Ground);
            CreatePortal(sprite, "Portal Cubo", PortalType.Cube, new Vector3(72f, -0.6f, 0f), new Color(0.2f, 0.95f, 1f));
            return;
        }

        if (spec.Index == 4)
        {
            CreatePortal(sprite, "Portal Nave", PortalType.Ship, new Vector3(52f, -0.6f, 0f), new Color(1f, 0.8f, 0.2f));
            CreateHazard(sprite, "Air Spike A", new Vector3(64f, 0.2f, 0f), new Vector2(0.65f, 0.65f));
            CreateHazard(sprite, "Air Spike B", new Vector3(70f, 1.2f, 0f), new Vector2(0.65f, 0.65f));
            CreatePortal(sprite, "Portal Cubo", PortalType.Cube, new Vector3(78f, -0.6f, 0f), new Color(0.2f, 0.95f, 1f));
            return;
        }

        if (spec.Index == 5)
        {
            CreateMovingGround(sprite, "Moving Platform A", new Vector3(55f, -0.6f, 0f), new Vector2(7f, 0.45f), spec.Ground, new Vector2(0f, 1.1f), 1.5f);
            CreateMovingGround(sprite, "Moving Platform B", new Vector3(70f, 0.4f, 0f), new Vector2(7f, 0.45f), spec.Ground, new Vector2(0f, -1.1f), 1.7f);
            CreateMovingHazard(sprite, new Vector3(76f, -0.7f, 0f));
            return;
        }

        if (spec.Index == 6)
        {
            CreatePortal(sprite, "Portal Rayo", PortalType.Ray, new Vector3(52f, -0.6f, 0f), new Color(1f, 1f, 0.45f));
            CreateHazard(sprite, "Ray Spike A", new Vector3(62f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateHazard(sprite, "Ray Spike B", new Vector3(70f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreatePortal(sprite, "Portal Cubo", PortalType.Cube, new Vector3(82f, -0.6f, 0f), new Color(0.2f, 0.95f, 1f));
            return;
        }

        CreatePortal(sprite, "Portal Gravedad", PortalType.FlipGravity, new Vector3(52f, -0.6f, 0f), new Color(1f, 0.25f, 0.7f));
        CreateGround(sprite, "Ceiling Path", new Vector3(64f, 3.2f, 0f), new Vector2(18f, 0.45f), spec.Ground);
        CreateHazard(sprite, "Inverted Spike", new Vector3(66f, 2.55f, 0f), new Vector2(0.65f, 0.65f), true);
        CreatePortal(sprite, "Portal Normal", PortalType.Cube, new Vector3(76f, 2.4f, 0f), new Color(0.2f, 0.95f, 1f));
        CreateGround(sprite, "Recovery Platform", new Vector3(82f, -0.8f, 0f), new Vector2(8f, 0.45f), spec.Ground);
    }

    private static LuxController CreateLux(Sprite sprite, int difficulty)
    {
        GameObject lux = CreateSpriteObject("Lux", sprite, new Vector3(0f, -0.7f, 0f), new Vector2(0.9f, 0.9f), new Color(0.2f, 0.95f, 1f), 3);
        Rigidbody2D body = lux.AddComponent<Rigidbody2D>();
        body.gravityScale = 4f;
        body.freezeRotation = true;
        BoxCollider2D collider = lux.AddComponent<BoxCollider2D>();
        collider.sharedMaterial = CreatePhysicsMaterial();

        GameObject groundCheck = Marker("GroundCheck", lux.transform.position + Vector3.down * 0.78f);
        groundCheck.transform.SetParent(lux.transform);

        LuxController controller = lux.AddComponent<LuxController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        so.FindProperty("autoSpeed").floatValue = Mathf.Lerp(5.25f, 6.45f, (difficulty - 1f) / 6f);
        so.ApplyModifiedProperties();
        return controller;
    }

    private static void CreateCamera(Transform target, Color background)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = background;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(4f, 1f, -10f);
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = target;
        so.ApplyModifiedProperties();
    }

    private static void CreateMenuCamera(Color background)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = background;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateUi(LevelManager manager, bool isFinalLevel)
    {
        GameObject canvasObject = CreateCanvas();
        CreateEventSystem();

        Text levelName = CreateText(canvasObject.transform, "LevelNameText", "", new Vector2(0f, -28f), TextAnchor.MiddleCenter, 22, new Vector2(380f, 36f));
        levelName.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        levelName.rectTransform.anchorMax = new Vector2(0.5f, 1f);

        Text progressText = CreateText(canvasObject.transform, "ProgressText", "0%", new Vector2(60f, -28f), TextAnchor.MiddleLeft, 22, new Vector2(100f, 36f));
        Text crystalsText = CreateText(canvasObject.transform, "CrystalsText", "Cristales: 0", new Vector2(180f, -28f), TextAnchor.MiddleLeft, 20, new Vector2(180f, 36f));
        Text attemptsText = CreateText(canvasObject.transform, "AttemptsText", "Intentos: 1", new Vector2(380f, -28f), TextAnchor.MiddleLeft, 20, new Vector2(180f, 36f));
        Text scoreText = CreateText(canvasObject.transform, "ScoreText", "Puntos: 0", new Vector2(570f, -28f), TextAnchor.MiddleLeft, 20, new Vector2(180f, 36f));
        Button menuButton = CreateButton(canvasObject.transform, "TopMenuButton", "Menu", new Vector2(-72f, -28f), new Vector2(110f, 34f), new Color(0.65f, 0.35f, 1f));
        RectTransform menuButtonRect = menuButton.GetComponent<RectTransform>();
        menuButtonRect.anchorMin = new Vector2(1f, 1f);
        menuButtonRect.anchorMax = new Vector2(1f, 1f);
        UnityEventTools.AddPersistentListener(menuButton.onClick, manager.LoadMainMenu);

        Slider slider = CreateSlider(canvasObject.transform);
        GameObject completedPanel = CreateCompletedPanel(canvasObject.transform, manager, isFinalLevel);

        ShadowBeatUI ui = canvasObject.AddComponent<ShadowBeatUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("levelManager").objectReferenceValue = manager;
        so.FindProperty("progressText").objectReferenceValue = progressText;
        so.FindProperty("crystalsText").objectReferenceValue = crystalsText;
        so.FindProperty("attemptsText").objectReferenceValue = attemptsText;
        so.FindProperty("levelNameText").objectReferenceValue = levelName;
        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("progressBar").objectReferenceValue = slider;
        so.FindProperty("completedPanel").objectReferenceValue = completedPanel;
        so.ApplyModifiedProperties();
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Text CreateText(Transform parent, string name, string value, Vector2 anchoredPosition, TextAnchor alignment, int size, Vector2 rectSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.sizeDelta = rectSize;
        rect.anchoredPosition = anchoredPosition;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(color.r, color.g, color.b, 0.72f);
        Button button = buttonObject.AddComponent<Button>();

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Text text = CreateText(buttonObject.transform, "Label", label, Vector2.zero, TextAnchor.MiddleCenter, 19, size - new Vector2(20f, 8f));
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        textRect.anchoredPosition = Vector2.zero;
        return button;
    }

    private static Slider CreateSlider(Transform parent)
    {
        GameObject sliderObject = new GameObject("ProgressBar");
        sliderObject.transform.SetParent(parent, false);
        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.18f);
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;

        RectTransform rect = slider.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(320f, 18f);
        rect.anchoredPosition = new Vector2(0f, -66f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.95f, 1f, 0.9f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = background;
        return slider;
    }

    private static GameObject CreateCompletedPanel(Transform parent, LevelManager manager, bool isFinalLevel)
    {
        GameObject panel = new GameObject("CompletedPanel");
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(500f, 210f);

        Text text = CreateText(panel.transform, "CompletedText", "JUEGO COMPLETADO", new Vector2(0f, 54f), TextAnchor.MiddleCenter, 30, new Vector2(460f, 54f));
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        Button next = CreateButton(panel.transform, "NextButton", isFinalLevel ? "Menu" : "Siguiente", new Vector2(-125f, -48f), new Vector2(180f, 46f), new Color(0.2f, 0.95f, 1f));
        Button restart = CreateButton(panel.transform, "RestartButton", "Reintentar", new Vector2(85f, -48f), new Vector2(180f, 46f), new Color(0.72f, 0.9f, 1f));
        Button menu = CreateButton(panel.transform, "MenuButton", "Menu", new Vector2(0f, -102f), new Vector2(180f, 40f), new Color(0.65f, 0.35f, 1f));
        next.GetComponent<RectTransform>().anchorMin = next.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        restart.GetComponent<RectTransform>().anchorMin = restart.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        menu.GetComponent<RectTransform>().anchorMin = menu.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        UnityEventTools.AddPersistentListener(next.onClick, manager.LoadNextLevel);
        UnityEventTools.AddPersistentListener(restart.onClick, manager.RestartLevel);
        UnityEventTools.AddPersistentListener(menu.onClick, manager.LoadMainMenu);
        panel.SetActive(false);
        return panel;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.7f;
    }

    private static void CreateGround(Sprite sprite, string name, Vector3 position, Vector2 size, Color color)
    {
        GameObject ground = CreateSpriteObject(name, sprite, position, size, color, 0);
        ground.AddComponent<BoxCollider2D>();
    }

    private static void CreateMovingGround(Sprite sprite, string name, Vector3 position, Vector2 size, Color color, Vector2 movement, float speed)
    {
        GameObject ground = CreateSpriteObject(name, sprite, position, size, color, 0);
        ground.AddComponent<BoxCollider2D>();
        MovingPlatform moving = ground.AddComponent<MovingPlatform>();
        SerializedObject so = new SerializedObject(moving);
        so.FindProperty("movement").vector2Value = movement;
        so.FindProperty("speed").floatValue = speed;
        so.ApplyModifiedProperties();
    }

    private static void CreateHazard(Sprite sprite, string name, Vector3 position, bool inverted = false)
    {
        CreateHazard(sprite, name, position, new Vector2(0.8f, 0.8f), inverted);
    }

    private static void CreateHazard(Sprite sprite, string name, Vector3 position, Vector2 size, bool inverted = false)
    {
        GameObject hazard = CreateSpriteObject(name, sprite, position, size, new Color(1f, 0.15f, 0.25f), 2);
        hazard.transform.rotation = Quaternion.Euler(0f, 0f, inverted ? 45f : -45f);
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        hazard.AddComponent<Hazard>();
    }

    private static void CreateMovingHazard(Sprite sprite, Vector3 position)
    {
        GameObject hazard = CreateSpriteObject("Moving Enemy", sprite, position, new Vector2(0.75f, 0.75f), new Color(1f, 0.15f, 0.25f), 2);
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        hazard.AddComponent<Hazard>();
        hazard.AddComponent<MovingHazard>();
    }

    private static void CreateCrystal(Sprite sprite, Vector3 position, Color color)
    {
        GameObject crystal = CreateSpriteObject("Crystal", sprite, position, new Vector2(0.45f, 0.45f), color, 2);
        crystal.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        BoxCollider2D collider = crystal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        crystal.AddComponent<CrystalCollectible>();
    }

    private static void CreatePortal(Sprite sprite, string name, PortalType type, Vector3 position, Color color)
    {
        GameObject portal = CreateSpriteObject(name, sprite, position, new Vector2(0.55f, 2.2f), color, 1);
        BoxCollider2D collider = portal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        Portal portalComponent = portal.AddComponent<Portal>();
        SerializedObject so = new SerializedObject(portalComponent);
        so.FindProperty("portalType").enumValueIndex = (int)type;
        so.ApplyModifiedProperties();
    }

    private static void CreateShadowBarrier(Sprite sprite, Vector3 position)
    {
        GameObject barrier = CreateSpriteObject("Shadow Barrier", sprite, position, new Vector2(0.5f, 2.4f), new Color(0.08f, 0.02f, 0.12f), 1);
        BoxCollider2D collider = barrier.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        barrier.AddComponent<ShadowBarrier>();
    }

    private static void CreateCheckpoint(Sprite sprite, Vector3 position, Color color)
    {
        GameObject checkpoint = CreateSpriteObject("Checkpoint", sprite, position, new Vector2(0.4f, 2f), color, 1);
        BoxCollider2D collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        checkpoint.AddComponent<Checkpoint>();
    }

    private static void CreateGoal(Sprite sprite, Vector3 position)
    {
        GameObject goal = CreateSpriteObject("Goal", sprite, position, new Vector2(0.7f, 2.7f), new Color(1f, 0.92f, 0.25f), 1);
        BoxCollider2D collider = goal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        goal.AddComponent<Goal>();
    }

    private static void CreateDeathZone(Sprite sprite, float endX)
    {
        GameObject deathZone = CreateSpriteObject("Death Zone", sprite, new Vector3(endX * 0.5f, -7.5f, 0f), new Vector2(endX + 24f, 1f), new Color(0f, 0f, 0f, 0f), -2);
        BoxCollider2D collider = deathZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        deathZone.AddComponent<DeathZone>();
        deathZone.GetComponent<SpriteRenderer>().enabled = false;
    }

    private static void CreateBackground(Sprite sprite, Color background, float endX)
    {
        CreateSpriteObject("Background", sprite, new Vector3(endX * 0.5f, 0f, 4f), new Vector2(endX + 40f, 12f), background, -10);
        for (int i = 0; i < 9; i++)
        {
            float x = 8f + i * 13f;
            float height = 1.2f + (i % 3) * 0.8f;
            CreateSpriteObject("Distant Shape", sprite, new Vector3(x, -2.6f + height * 0.5f, 2f), new Vector2(1.4f, height), new Color(1f, 1f, 1f, 0.06f), -8);
        }
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return obj;
    }

    private static GameObject Marker(string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = position;
        return marker;
    }

    private static PhysicsMaterial2D CreatePhysicsMaterial()
    {
        PhysicsMaterial2D material = new PhysicsMaterial2D("Lux No Friction");
        material.friction = 0f;
        material.bounciness = 0f;
        return material;
    }

    private static void SetBuildScenes(LevelSpec[] levels)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true)
        };

        foreach (LevelSpec level in levels)
        {
            scenes.Add(new EditorBuildSettingsScene($"{ScenesFolder}/{level.SceneName}.unity", true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureSpriteAsset()
    {
        Directory.CreateDirectory(GeneratedFolder);
        if (!File.Exists(SpritePath))
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(SpritePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 1f;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
    }
}
