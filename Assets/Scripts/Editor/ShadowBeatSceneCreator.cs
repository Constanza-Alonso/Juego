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
    private const string MenuBackgroundPath = GeneratedFolder + "/MenuBackground.png";
    private const string GameplayBackgroundPath = GeneratedFolder + "/GameplayBackground.png";
    private const string ScenesFolder = "Assets/Scenes";
    private const string MenuScenePath = ScenesFolder + "/MainMenu.unity";
    private static readonly Color CyberSky = new Color(0.006f, 0.01f, 0.025f);
    private static readonly Color CyberRoad = new Color(0.025f, 0.035f, 0.055f);
    private static readonly Color NeonCyan = new Color(0.18f, 0.95f, 1f);
    private static readonly Color NeonOrange = new Color(1f, 0.55f, 0.16f);
    private static readonly Color NeonMagenta = new Color(1f, 0.18f, 0.85f);
    private static readonly Color NeonViolet = new Color(0.55f, 0.25f, 1f);

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
            new LevelSpec(1, "Neon Cero", "Level01_NeonCero", "Ciudad Neon", NeonCyan, NeonOrange, CyberSky, 1),
            new LevelSpec(2, "Ciudad del Vortice", "Level02_CiudadDelVortice", "Ciudad Neon", NeonViolet, NeonMagenta, new Color(0.008f, 0.008f, 0.03f), 2),
            new LevelSpec(3, "Abismo Pulsante", "Level03_AbismoPulsante", "Ciudad Neon", NeonCyan, new Color(0.62f, 1f, 0.32f), new Color(0.006f, 0.012f, 0.032f), 3),
            new LevelSpec(4, "Ciclo Galactico", "Level04_CicloGalactico", "Ruinas Cyber", NeonCyan, NeonOrange, new Color(0.012f, 0.008f, 0.04f), 4),
            new LevelSpec(5, "Ecos de Cristal", "Level05_EcosDeCristal", "Ruinas Cyber", NeonViolet, new Color(0.86f, 0.45f, 1f), new Color(0.015f, 0.008f, 0.045f), 5),
            new LevelSpec(6, "Sincronia Ritmica", "Level06_SincroniaRitmica", "Ruinas Cyber", NeonCyan, new Color(1f, 0.92f, 0.22f), new Color(0.018f, 0.012f, 0.05f), 6),
            new LevelSpec(7, "Portal Infinito", "Level07_PortalInfinito", "Ciudad Invertida", NeonMagenta, NeonCyan, new Color(0.006f, 0.006f, 0.03f), 7)
        };
    }

    private static void CreateMenuScene(LevelSpec[] levels)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        Sprite menuBackground = AssetDatabase.LoadAssetAtPath<Sprite>(GameplayBackgroundPath);
        if (menuBackground == null)
        {
            menuBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MenuBackgroundPath);
        }

        CreateMenuCamera(CyberSky);
        if (menuBackground == null)
        {
            CreateBackground(sprite, CyberSky, NeonCyan, 24f);
            CreateMenuNeonTitle(sprite);
            CreateMenuPanel(sprite);
        }

        GameObject canvasObject = CreateCanvas();
        if (menuBackground != null)
        {
            CreateCanvasBackground(canvasObject.transform, menuBackground);
        }

        CreateDarkOverlay(canvasObject.transform);
        CreateMenuNeonDecorations(canvasObject.transform);
        CreateEventSystem();

        MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();

        Image mainPanel = CreateGlassPanel(canvasObject.transform, "Main Holographic Panel", new Vector2(0f, -400f), new Vector2(915f, 390f), new Color(0f, 0f, 0f, 0.04f), NeonCyan, NeonMagenta);
        mainPanel.transform.SetAsLastSibling();

        CreateGlassPanel(canvasObject.transform, "Stats Holographic Panel", new Vector2(-505f, -380f), new Vector2(230f, 170f), new Color(0f, 0.025f, 0.04f, 0.28f), NeonCyan, NeonCyan);

        Text playHint = CreateText(canvasObject.transform, "PlayHint", "JUGAR", new Vector2(0f, -166f), TextAnchor.MiddleCenter, 35, new Vector2(300f, 56f));
        SetTopCenter(playHint.rectTransform);
        playHint.color = new Color(0.78f, 1f, 1f);
        ApplyTextGlow(playHint, NeonCyan, NeonCyan);
        CreateCapsuleGlow(canvasObject.transform, "PlayHintGlow", new Vector2(0f, -166f), new Vector2(315f, 58f), NeonCyan, NeonMagenta);

        Text statsText = CreateText(canvasObject.transform, "StatsText", "Estadisticas:\n\nScore: 45821\nLives: 3\nRacha: 12x", new Vector2(-590f, -314f), TextAnchor.UpperLeft, 19, new Vector2(220f, 142f));
        SetTopCenter(statsText.rectTransform);
        statsText.color = new Color(0.88f, 1f, 1f);
        ApplyTextGlow(statsText, NeonCyan, NeonCyan);

        Text selectedText = CreateText(canvasObject.transform, "SelectedLevelText", "Nivel seleccionado: 1. Neon Cero", new Vector2(0f, -616f), TextAnchor.MiddleCenter, 18, new Vector2(560f, 30f));
        SetTopCenter(selectedText.rectTransform);
        selectedText.color = new Color(1f, 0.94f, 0.62f);
        ApplyTextGlow(selectedText, NeonOrange, NeonOrange);

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("selectedLevelSceneName").stringValue = levels[0].SceneName;
        controllerSo.FindProperty("selectedLevelLabel").stringValue = $"{levels[0].Index}. {levels[0].Name}";
        controllerSo.FindProperty("selectedLevelText").objectReferenceValue = selectedText;
        controllerSo.FindProperty("statsText").objectReferenceValue = statsText;
        controllerSo.ApplyModifiedProperties();

        Text levelTitle = CreateText(canvasObject.transform, "LevelSelectTitle", "SELECCION DE NIVEL", new Vector2(-245f, -245f), TextAnchor.MiddleCenter, 23, new Vector2(430f, 36f));
        SetTopCenter(levelTitle.rectTransform);
        levelTitle.color = Color.white;
        ApplyTextGlow(levelTitle, NeonCyan, NeonCyan);

        for (int i = 0; i < levels.Length; i++)
        {
            LevelSpec level = levels[i];
            Vector2 position = new Vector2(-245f, -290f - i * 37f);
            Button button = CreateNeonButton(canvasObject.transform, $"Level {level.Index}", $"{level.Index}.  >  {level.Name}", position, new Vector2(420f, 31f), i == 0 ? NeonCyan : level.Accent);
            LevelSelectButton selectButton = button.gameObject.AddComponent<LevelSelectButton>();
            SerializedObject selectSo = new SerializedObject(selectButton);
            selectSo.FindProperty("menuController").objectReferenceValue = controller;
            selectSo.FindProperty("sceneName").stringValue = level.SceneName;
            selectSo.FindProperty("levelLabel").stringValue = $"{level.Index}. {level.Name}";
            selectSo.ApplyModifiedProperties();
            UnityEventTools.AddPersistentListener(button.onClick, selectButton.SelectAssignedLevel);
        }

        Text shapeTitle = CreateText(canvasObject.transform, "ShapeTitle", "FORMA DEL PERSONAJE", new Vector2(285f, -245f), TextAnchor.MiddleCenter, 23, new Vector2(420f, 36f));
        SetTopCenter(shapeTitle.rectTransform);
        shapeTitle.color = Color.white;
        ApplyTextGlow(shapeTitle, NeonCyan, NeonCyan);

        CreateShapePreviewUi(canvasObject.transform, controller, "Esfera", 2, new Vector2(190f, -360f), GetCircleSprite(), NeonCyan, 0f);
        CreateShapePreviewUi(canvasObject.transform, controller, "Cubo", 0, new Vector2(420f, -360f), sprite, NeonOrange, 0f);
        CreateShapePreviewUi(canvasObject.transform, controller, "Piramide", 3, new Vector2(190f, -510f), GetTriangleSprite(), NeonViolet, 0f);
        CreateShapePreviewUi(canvasObject.transform, controller, "Diamante", 1, new Vector2(420f, -510f), sprite, NeonMagenta, 45f);

        Button playButton = CreateNeonButton(canvasObject.transform, "PlayNowButton", "PLAY NOW", new Vector2(0f, -674f), new Vector2(410f, 64f), NeonOrange);
        Text playLabel = playButton.GetComponentInChildren<Text>();
        playLabel.fontSize = 34;
        ApplyTextGlow(playLabel, NeonOrange, NeonOrange);
        UnityEventTools.AddPersistentListener(playButton.onClick, controller.PlaySelectedLevel);

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
        Camera camera = CreateCamera(player.transform, spec.Background);
        CreatePortalEffectController(camera, spec);
        CreateLight();
        CreateBackground(sprite, spec.Background, spec.Accent, endX);

        CreateLevelStart(sprite, spec);
        if (spec.Index > 1)
        {
            CreateGround(sprite, "Safety Route", new Vector3(64f, -2.2f, 0f), new Vector2(88f, 1f), spec.Ground);
        }
        CreateLevelFeatures(sprite, spec);
        CreateGround(sprite, "Goal Runway", new Vector3(endX - 8f, -2.2f, 0f), new Vector2(48f, 1f), spec.Ground);
        CreateCheckpoint(sprite, new Vector3(45f, -1.2f, 0f), spec.Accent);
        CreateGoal(sprite, new Vector3(endX + 2f, -0.6f, 0f), spec.Accent);
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

    private static void CreateLevelStart(Sprite sprite, LevelSpec spec)
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

        if (spec.Index == 2)
        {
            CreateGround(sprite, "Shadow Start Ground", new Vector3(16f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateGround(sprite, "Shadow Step A", new Vector3(23f, -0.9f, 0f), new Vector2(5f, 0.45f), spec.Ground);
            CreateGround(sprite, "Shadow Step B", new Vector3(34f, -1.35f, 0f), new Vector2(6f, 0.45f), spec.Ground);
            CreateGround(sprite, "Ground Mid", new Vector3(61f, -2.2f, 0f), new Vector2(32f, 1f), spec.Ground);
            CreateGround(sprite, "Final Ground", new Vector3(91f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateHazard(sprite, "Shadow Spike 1", new Vector3(18f, -1.35f, 0f), new Vector2(0.55f, 0.55f));
            CreateHazard(sprite, "Shadow Spike 2", new Vector3(42f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateCrystal(sprite, new Vector3(23f, -0.1f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(35f, -0.6f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(80f, -0.8f, 0f), spec.Accent);
            return;
        }

        if (spec.Index == 3)
        {
            CreateGround(sprite, "Jump Start Ground", new Vector3(10f, -2.2f, 0f), new Vector2(24f, 1f), spec.Ground);
            CreateGround(sprite, "Jump Island A", new Vector3(24f, -0.35f, 0f), new Vector2(6f, 0.45f), spec.Ground);
            CreateGround(sprite, "Jump Island B", new Vector3(36f, 0.65f, 0f), new Vector2(6f, 0.45f), spec.Ground);
            CreateGround(sprite, "Ground Mid", new Vector3(61f, -2.2f, 0f), new Vector2(32f, 1f), spec.Ground);
            CreateGround(sprite, "Final Ground", new Vector3(91f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateHazard(sprite, "Jump Spike 1", new Vector3(16f, -1.35f, 0f), new Vector2(0.55f, 0.55f));
            CreateHazard(sprite, "Jump Spike 2", new Vector3(31f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateCrystal(sprite, new Vector3(24f, 0.45f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(36f, 1.45f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(80f, -0.8f, 0f), spec.Accent);
            return;
        }

        if (spec.Index == 4)
        {
            CreateGround(sprite, "Crystal Start Ground", new Vector3(18f, -2.2f, 0f), new Vector2(40f, 1f), spec.Ground);
            CreateGround(sprite, "Crystal Ramp Step", new Vector3(30f, -0.8f, 0f), new Vector2(8f, 0.45f), spec.Ground);
            CreateGround(sprite, "Ground Mid", new Vector3(62f, -2.2f, 0f), new Vector2(28f, 1f), spec.Ground);
            CreateGround(sprite, "Final Ground", new Vector3(91f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateHazard(sprite, "Crystal Spike 1", new Vector3(22f, -1.35f, 0f), new Vector2(0.55f, 0.55f));
            CreateHazard(sprite, "Crystal Spike 2", new Vector3(38f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateCrystal(sprite, new Vector3(30f, 0f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(58f, -0.8f, 0f), spec.Accent);
            return;
        }

        if (spec.Index == 5)
        {
            CreateGround(sprite, "Tower Start Ground", new Vector3(12f, -2.2f, 0f), new Vector2(26f, 1f), spec.Ground);
            CreateGround(sprite, "Tower Start Platform", new Vector3(28f, 0.1f, 0f), new Vector2(6f, 0.45f), spec.Ground);
            CreateGround(sprite, "Ground Mid", new Vector3(61f, -2.2f, 0f), new Vector2(32f, 1f), spec.Ground);
            CreateGround(sprite, "Final Ground", new Vector3(91f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateHazard(sprite, "Tower Spike 1", new Vector3(19f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateCrystal(sprite, new Vector3(28f, 0.9f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(80f, -0.8f, 0f), spec.Accent);
            return;
        }

        if (spec.Index == 6)
        {
            CreateGround(sprite, "Ray Start Ground", new Vector3(20f, -2.2f, 0f), new Vector2(44f, 1f), spec.Ground);
            CreateGround(sprite, "Ray Warning Step", new Vector3(35f, -1.05f, 0f), new Vector2(8f, 0.45f), spec.Ground);
            CreateGround(sprite, "Ground Mid", new Vector3(64f, -2.2f, 0f), new Vector2(28f, 1f), spec.Ground);
            CreateGround(sprite, "Final Ground", new Vector3(94f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateHazard(sprite, "Ray Start Spike", new Vector3(26f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateCrystal(sprite, new Vector3(35f, -0.25f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(80f, -0.8f, 0f), spec.Accent);
            return;
        }

        if (spec.Index == 7)
        {
            CreateGround(sprite, "Inverted Start Ground", new Vector3(18f, -2.2f, 0f), new Vector2(40f, 1f), spec.Ground);
            CreateGround(sprite, "Gravity Prep Step", new Vector3(32f, -0.7f, 0f), new Vector2(8f, 0.45f), spec.Ground);
            CreateGround(sprite, "Ground Mid", new Vector3(61f, -2.2f, 0f), new Vector2(32f, 1f), spec.Ground);
            CreateGround(sprite, "Final Ground", new Vector3(91f, -2.2f, 0f), new Vector2(34f, 1f), spec.Ground);
            CreateHazard(sprite, "Gravity Spike 1", new Vector3(22f, -1.35f, 0f), new Vector2(0.6f, 0.6f));
            CreateCrystal(sprite, new Vector3(32f, 0.1f, 0f), spec.Accent);
            CreateCrystal(sprite, new Vector3(80f, -0.8f, 0f), spec.Accent);
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
            CreateShadowBarrier(sprite, new Vector3(62f, 0.9f, 0f));
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
            CreateHazard(sprite, "Air Spike A", new Vector3(64f, 1.2f, 0f), new Vector2(0.55f, 0.55f));
            CreateHazard(sprite, "Air Spike B", new Vector3(70f, 2.1f, 0f), new Vector2(0.55f, 0.55f));
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
            CreateHazard(sprite, "Ray Spike A", new Vector3(62f, -1.35f, 0f), new Vector2(0.45f, 0.45f));
            CreateHazard(sprite, "Ray Spike B", new Vector3(72f, -1.35f, 0f), new Vector2(0.45f, 0.45f));
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

    private static Camera CreateCamera(Transform target, Color background)
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
        return camera;
    }

    private static void CreatePortalEffectController(Camera camera, LevelSpec spec)
    {
        GameObject effectObject = new GameObject("Portal Effect Controller");
        PortalEffectController effect = effectObject.AddComponent<PortalEffectController>();
        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("targetCamera").objectReferenceValue = camera;
        SerializedProperty colors = so.FindProperty("backgroundColors");
        colors.arraySize = 4;
        colors.GetArrayElementAtIndex(0).colorValue = spec.Background;
        colors.GetArrayElementAtIndex(1).colorValue = Color.Lerp(spec.Background, spec.Accent, 0.35f);
        colors.GetArrayElementAtIndex(2).colorValue = Color.Lerp(spec.Background, new Color(0.1f, 0.02f, 0.12f), 0.55f);
        colors.GetArrayElementAtIndex(3).colorValue = Color.Lerp(spec.Background, new Color(0.02f, 0.08f, 0.08f), 0.55f);
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


    private static void CreateDarkOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("Cinematic Dark Overlay");
        overlay.transform.SetParent(parent, false);
        Image image = overlay.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.14f);
        image.raycastTarget = false;

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image CreateGlassPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fill, Color outlineA, Color outlineB)
    {
        CreatePanel(parent, name + " Outer Glow", anchoredPosition, size + new Vector2(12f, 12f), WithAlpha(outlineA, 0.035f), outlineB);
        Image panel = CreatePanel(parent, name, anchoredPosition, size, fill, outlineA);

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = WithAlpha(outlineB, 0.22f);
        shadow.effectDistance = new Vector2(4f, -4f);

        return panel;
    }

    private static void CreateCapsuleGlow(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color colorA, Color colorB)
    {
        Image glow = CreatePanel(parent, name, anchoredPosition, size, WithAlpha(colorA, 0.055f), colorB);
        glow.raycastTarget = false;
    }

    private static Button CreateNeonButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        CreatePanel(parent, name + " Glow", anchoredPosition, size + new Vector2(8f, 6f), WithAlpha(color, 0.04f), color);
        Button button = CreateButton(parent, name, label, anchoredPosition, size, color);
        Image image = button.GetComponent<Image>();
        image.color = new Color(0.01f, 0.045f, 0.06f, 0.58f);

        Outline outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = WithAlpha(color, 0.82f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        Shadow shadow = button.gameObject.AddComponent<Shadow>();
        shadow.effectColor = WithAlpha(color, 0.38f);
        shadow.effectDistance = new Vector2(2f, -2f);

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.color = Color.white;
            text.fontSize = Mathf.Min(text.fontSize, 18);
            ApplyTextGlow(text, color, color);
        }

        return button;
    }

    private static void CreateMenuNeonDecorations(Transform parent)
    {
        CreatePanel(parent, "Left Cyan Beam", new Vector2(-470f, -660f), new Vector2(560f, 3f), WithAlpha(NeonCyan, 0.35f), NeonCyan);
        CreatePanel(parent, "Right Magenta Beam", new Vector2(480f, -620f), new Vector2(380f, 3f), WithAlpha(NeonMagenta, 0.32f), NeonMagenta);
        CreatePanel(parent, "Energy Core Glow", new Vector2(250f, -245f), new Vector2(150f, 150f), WithAlpha(new Color(1f, 0.9f, 0.55f), 0.07f), NeonOrange);
    }

    private static void ApplyTextGlow(Text text, Color outlineColor, Color shadowColor)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = WithAlpha(outlineColor, 0.45f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = WithAlpha(shadowColor, 0.35f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private static void CreateShapePreviewUi(Transform parent, MainMenuController controller, string label, int shapeIndex, Vector2 anchoredPosition, Sprite iconSprite, Color color, float rotation)
    {
        CreatePanel(parent, $"Shape {label} Glow", anchoredPosition + new Vector2(0f, 12f), new Vector2(90f, 90f), WithAlpha(color, 0.08f), color);
        CreatePanel(parent, $"Shape {label} Base", anchoredPosition + new Vector2(0f, -42f), new Vector2(106f, 4f), WithAlpha(NeonCyan, 0.46f), NeonCyan);

        GameObject icon = new GameObject($"Shape {label} Icon");
        icon.transform.SetParent(parent, false);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.color = color;
        iconImage.raycastTarget = false;

        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = label == "Piramide" ? new Vector2(76f, 76f) : new Vector2(72f, 72f);
        iconRect.anchoredPosition = anchoredPosition + new Vector2(0f, 12f);
        iconRect.rotation = Quaternion.Euler(0f, 0f, rotation);

        Outline iconOutline = icon.AddComponent<Outline>();
        iconOutline.effectColor = WithAlpha(Color.white, 0.55f);
        iconOutline.effectDistance = new Vector2(2f, -2f);

        Text labelText = CreateText(parent, $"Shape {label} Label", label, anchoredPosition + new Vector2(0f, -78f), TextAnchor.MiddleCenter, 20, new Vector2(150f, 30f));
        SetTopCenter(labelText.rectTransform);
        labelText.color = Color.white;
        ApplyTextGlow(labelText, color, color);

        CreateShapeHotspot(parent, controller, label, shapeIndex, anchoredPosition + new Vector2(0f, -16f), new Vector2(145f, 145f));
    }

    private static void CreateCanvasBackground(Transform parent, Sprite background)
    {
        GameObject backgroundObject = new GameObject("Menu Background");
        backgroundObject.transform.SetParent(parent, false);
        Image image = backgroundObject.AddComponent<Image>();
        image.sprite = background;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
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

    private static void SetTopCenter(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fill, Color outline)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        Image image = panelObject.AddComponent<Image>();
        image.color = fill;
        image.raycastTarget = false;

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = WithAlpha(outline, 0.58f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return image;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.01f, 0.045f, 0.06f, 0.56f);
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

    private static Button CreateInvisibleButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return button;
    }

    private static void CreateShapeButton(Transform parent, MainMenuController controller, string label, int shapeIndex, Vector2 anchoredPosition, Color color)
    {
        Button button = CreateButton(parent, $"Shape {label}", label, anchoredPosition, new Vector2(130f, 38f), color);
        ShapeSelectButton shapeButton = button.gameObject.AddComponent<ShapeSelectButton>();
        SerializedObject so = new SerializedObject(shapeButton);
        so.FindProperty("menuController").objectReferenceValue = controller;
        so.FindProperty("shapeIndex").intValue = shapeIndex;
        so.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(button.onClick, shapeButton.SelectShape);
    }

    private static void CreateShapeHotspot(Transform parent, MainMenuController controller, string label, int shapeIndex, Vector2 anchoredPosition, Vector2 size)
    {
        Button button = CreateInvisibleButton(parent, $"Shape {label} Hotspot", anchoredPosition, size);
        ShapeSelectButton shapeButton = button.gameObject.AddComponent<ShapeSelectButton>();
        SerializedObject so = new SerializedObject(shapeButton);
        so.FindProperty("menuController").objectReferenceValue = controller;
        so.FindProperty("shapeIndex").intValue = shapeIndex;
        so.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(button.onClick, shapeButton.SelectShape);
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
        GameObject ground = CreateSpriteObject(name, sprite, position, size, CyberRoad, 0);
        ground.AddComponent<BoxCollider2D>();
        CreateSpriteObject(name + " Neon Top", sprite, position + new Vector3(0f, size.y * 0.5f + 0.035f, -0.01f), new Vector2(size.x, 0.08f), WithAlpha(color, 0.92f), 1);
        CreateSpriteObject(name + " Neon Glow", sprite, position + new Vector3(0f, size.y * 0.5f + 0.02f, 0.02f), new Vector2(size.x, 0.22f), WithAlpha(color, 0.18f), -1);

        int lineCount = Mathf.Clamp(Mathf.RoundToInt(size.x / 7f), 1, 10);
        for (int i = 0; i < lineCount; i++)
        {
            float offset = -size.x * 0.42f + i * (size.x * 0.84f / Mathf.Max(1, lineCount - 1));
            CreateSpriteObject(name + " Circuit Line", sprite, position + new Vector3(offset, size.y * 0.5f + 0.08f, -0.02f), new Vector2(0.06f, 0.32f), WithAlpha(color, 0.6f), 2);
        }
    }

    private static void CreateMovingGround(Sprite sprite, string name, Vector3 position, Vector2 size, Color color, Vector2 movement, float speed)
    {
        GameObject ground = CreateSpriteObject(name, sprite, position, size, CyberRoad, 0);
        GameObject neonTop = CreateSpriteObject(name + " Neon Top", sprite, position + new Vector3(0f, size.y * 0.5f + 0.035f, -0.01f), new Vector2(size.x, 0.08f), WithAlpha(color, 0.92f), 1);
        neonTop.transform.SetParent(ground.transform);
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
        GameObject glow = CreateSpriteObject(name + " Glow", GetTriangleSprite(), position, size * 1.3f, WithAlpha(NeonOrange, 0.28f), 1);
        glow.transform.rotation = Quaternion.Euler(0f, 0f, inverted ? 180f : 0f);
        GameObject hazard = CreateSpriteObject(name, GetTriangleSprite(), position, size, NeonOrange, 2);
        hazard.transform.rotation = Quaternion.Euler(0f, 0f, inverted ? 180f : 0f);
        PolygonCollider2D collider = hazard.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        hazard.AddComponent<Hazard>();
    }

    private static void CreateMovingHazard(Sprite sprite, Vector3 position)
    {
        CreateSpriteObject("Moving Enemy Glow", sprite, position, new Vector2(1.15f, 1.15f), WithAlpha(NeonMagenta, 0.28f), 1);
        GameObject hazard = CreateSpriteObject("Moving Enemy", sprite, position, new Vector2(0.75f, 0.75f), NeonMagenta, 2);
        hazard.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        hazard.AddComponent<Hazard>();
        hazard.AddComponent<MovingHazard>();
    }

    private static void CreateCrystal(Sprite sprite, Vector3 position, Color color)
    {
        CreateSpriteObject("Crystal Glow", sprite, position, new Vector2(0.82f, 0.82f), WithAlpha(color, 0.22f), 1);
        GameObject crystal = CreateSpriteObject("Crystal", sprite, position, new Vector2(0.45f, 0.45f), color, 2);
        crystal.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        BoxCollider2D collider = crystal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        crystal.AddComponent<CrystalCollectible>();
    }

    private static void CreatePortal(Sprite sprite, string name, PortalType type, Vector3 position, Color color)
    {
        CreateSpriteObject(name + " Aura", sprite, position, new Vector2(1.15f, 2.7f), WithAlpha(color, 0.22f), 0);
        CreateSpriteObject(name + " Ring Top", sprite, position + Vector3.up * 1.08f, new Vector2(1.35f, 0.08f), WithAlpha(color, 0.85f), 2);
        CreateSpriteObject(name + " Ring Bottom", sprite, position + Vector3.down * 1.08f, new Vector2(1.35f, 0.08f), WithAlpha(color, 0.85f), 2);
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
        CreateSpriteObject("Checkpoint Glow", sprite, position, new Vector2(0.9f, 2.4f), WithAlpha(color, 0.18f), 0);
        GameObject checkpoint = CreateSpriteObject("Checkpoint", sprite, position, new Vector2(0.4f, 2f), color, 1);
        BoxCollider2D collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        checkpoint.AddComponent<Checkpoint>();
    }

    private static void CreateGoal(Sprite sprite, Vector3 position, Color accent)
    {
        CreateSpriteObject("Light Core Glow Large", sprite, position, new Vector2(3.2f, 3.2f), WithAlpha(new Color(1f, 0.9f, 0.45f), 0.18f), 0);
        CreateSpriteObject("Light Core Glow", sprite, position, new Vector2(1.8f, 1.8f), WithAlpha(new Color(1f, 0.95f, 0.72f), 0.38f), 1);
        CreateSpriteObject("Light Core Beam Vertical", sprite, position, new Vector2(0.14f, 4.1f), WithAlpha(new Color(1f, 0.95f, 0.72f), 0.8f), 2);
        CreateSpriteObject("Light Core Beam Horizontal", sprite, position, new Vector2(2.7f, 0.14f), WithAlpha(accent, 0.85f), 2);
        GameObject goal = CreateSpriteObject("Goal", sprite, position, new Vector2(0.7f, 2.7f), new Color(1f, 0.92f, 0.25f), 3);
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

    private static void CreateBackground(Sprite sprite, Color background, Color accent, float endX)
    {
        CreateSpriteObject("Background", sprite, new Vector3(endX * 0.5f, 0f, 4f), new Vector2(endX + 40f, 12f), background, -10);
        for (int i = 0; i < 22; i++)
        {
            float x = -8f + i * 6f;
            float height = 2.2f + (i % 5) * 0.7f;
            float width = 1.5f + (i % 3) * 0.45f;
            Color building = new Color(0.03f, 0.035f, 0.07f, 0.95f);
            CreateSpriteObject("Cyber Tower", sprite, new Vector3(x, -2.9f + height * 0.5f, 2f), new Vector2(width, height), building, -8);

            Color windowColor = i % 2 == 0 ? NeonMagenta : accent;
            CreateSpriteObject("Tower Neon Window", sprite, new Vector3(x + width * 0.18f, -1.5f + (i % 4) * 0.55f, 1.8f), new Vector2(width * 0.55f, 0.07f), WithAlpha(windowColor, 0.65f), -7);
            CreateSpriteObject("Tower Neon Window", sprite, new Vector3(x - width * 0.2f, -0.6f + (i % 3) * 0.5f, 1.8f), new Vector2(width * 0.28f, 0.08f), WithAlpha(NeonOrange, 0.55f), -7);
        }

        for (int i = 0; i < 34; i++)
        {
            float x = -14f + i * 3.8f;
            float y = 1.2f + (i % 6) * 0.62f;
            Color starColor = i % 3 == 0 ? NeonCyan : Color.white;
            CreateSpriteObject("Neon Dust", sprite, new Vector3(x, y, 1.5f), Vector2.one * (0.035f + (i % 3) * 0.015f), WithAlpha(starColor, 0.75f), -6);
        }

        for (int i = 0; i < 9; i++)
        {
            float x = 10f + i * 12f;
            CreateSpriteObject("Floating Neon Panel", sprite, new Vector3(x, 2.7f + (i % 2) * 0.8f, 1.3f), new Vector2(1.8f, 0.08f), WithAlpha(i % 2 == 0 ? NeonMagenta : NeonCyan, 0.7f), -5);
            CreateSpriteObject("Floating Neon Panel", sprite, new Vector3(x + 0.82f, 2.35f + (i % 2) * 0.8f, 1.3f), new Vector2(0.08f, 0.7f), WithAlpha(i % 2 == 0 ? NeonMagenta : NeonCyan, 0.65f), -5);
        }
    }

    private static void CreateMenuNeonTitle(Sprite sprite)
    {
        CreateSpriteObject("Menu Title Glow", sprite, new Vector3(0f, 3.05f, 1f), new Vector2(6.2f, 1.15f), WithAlpha(NeonOrange, 0.16f), -2);
        CreateSpriteObject("Menu Cyan Underline", sprite, new Vector3(0f, 2.35f, 1f), new Vector2(4.8f, 0.08f), WithAlpha(NeonCyan, 0.8f), -1);
        CreateSpriteObject("Menu Magenta Slash", sprite, new Vector3(2.7f, 2.62f, 1f), new Vector2(0.08f, 1.35f), WithAlpha(NeonMagenta, 0.85f), -1).transform.rotation = Quaternion.Euler(0f, 0f, -35f);
        CreateSpriteObject("Menu Orange Slash", sprite, new Vector3(-2.6f, 3.48f, 1f), new Vector2(0.08f, 1.2f), WithAlpha(NeonOrange, 0.75f), -1).transform.rotation = Quaternion.Euler(0f, 0f, 35f);
    }

    private static void CreateMenuPanel(Sprite sprite)
    {
        CreateSpriteObject("Menu Glass Panel", sprite, new Vector3(0f, -0.6f, 1.2f), new Vector2(11.8f, 5.7f), new Color(0.02f, 0.06f, 0.08f, 0.42f), -2);
        CreateSpriteObject("Menu Panel Top", sprite, new Vector3(0f, 2.25f, 1f), new Vector2(11.8f, 0.06f), WithAlpha(NeonCyan, 0.9f), -1);
        CreateSpriteObject("Menu Panel Bottom", sprite, new Vector3(0f, -3.45f, 1f), new Vector2(11.8f, 0.06f), WithAlpha(NeonMagenta, 0.9f), -1);
        CreateSpriteObject("Menu Panel Left", sprite, new Vector3(-5.9f, -0.6f, 1f), new Vector2(0.06f, 5.7f), WithAlpha(NeonCyan, 0.9f), -1);
        CreateSpriteObject("Menu Panel Right", sprite, new Vector3(5.9f, -0.6f, 1f), new Vector2(0.06f, 5.7f), WithAlpha(NeonMagenta, 0.9f), -1);
        CreateSpriteObject("Menu Division", sprite, new Vector3(0.72f, -0.55f, 1f), new Vector2(0.04f, 3.95f), WithAlpha(NeonCyan, 0.55f), -1);
        CreateSpriteObject("Play Button Glow", sprite, new Vector3(0f, -4.05f, 1f), new Vector2(4.2f, 0.72f), WithAlpha(NeonOrange, 0.26f), -2);
    }

    private static void CreateShapePreview(string name, Vector3 position, string shape, Color color, float rotation = 0f)
    {
        Sprite sprite = shape == "circle" ? GetCircleSprite() : shape == "triangle" ? GetTriangleSprite() : AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        Vector2 size = shape == "triangle" ? new Vector2(1.05f, 1.05f) : new Vector2(0.92f, 0.92f);
        CreateSpriteObject(name + " Glow", sprite, position, size * 1.55f, WithAlpha(color, 0.25f), 1).transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        GameObject preview = CreateSpriteObject(name, sprite, position, size, color, 2);
        preview.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        CreateSpriteObject(name + " Base", AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath), position + Vector3.down * 0.82f, new Vector2(1.25f, 0.08f), WithAlpha(NeonCyan, 0.75f), 1);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
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

    private static Sprite GetTriangleSprite()
    {
        const string path = GeneratedFolder + "/TriangleSpike.png";
        if (!File.Exists(path))
        {
            Texture2D texture = new Texture2D(64, 64);
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float halfWidth = Mathf.Lerp(2f, 30f, y / 63f);
                    bool inside = Mathf.Abs(x - 31.5f) <= halfWidth && y <= 60;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite GetCircleSprite()
    {
        const string path = GeneratedFolder + "/CircleShape.png";
        if (!File.Exists(path))
        {
            Texture2D texture = new Texture2D(64, 64);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(31.5f, 31.5f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 29f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
        ConfigureSpriteImporter(SpritePath, 1f, FilterMode.Point);
        ConfigureSpriteImporter(MenuBackgroundPath, 100f, FilterMode.Bilinear);
        ConfigureSpriteImporter(GameplayBackgroundPath, 100f, FilterMode.Bilinear);
    }

    private static void ConfigureSpriteImporter(string path, float pixelsPerUnit, FilterMode filterMode)
    {
        if (!File.Exists(path))
        {
            return;
        }

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
