using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowBeat.UI
{
public class UIKitMenuSelection : MonoBehaviour
{
    [SerializeField] private MainMenuController menuController;
    [SerializeField] private TextMeshProUGUI selectedLevelLabel;
    [SerializeField] private TextMeshProUGUI selectedShapeLabel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button[] shapeButtons;

    private readonly string[] sceneNames =
    {
        "Level01_NeonCero",
        "Level02_CiudadDelVortice",
        "Level03_AbismoPulsante",
        "Level04_CicloGalactico",
        "Level05_EcosDeCristal",
        "Level06_SincroniaRitmica",
        "Level07_PortalInfinito"
    };

    private readonly string[] levelNames =
    {
        "Neon Cero",
        "Ciudad del Vortice",
        "Abismo Pulsante",
        "Ciclo Galactico",
        "Ecos de Cristal",
        "Sincronia Ritmica",
        "Portal Infinito"
    };

    private readonly string[] shapeNames = { "Cubo", "Esfera", "Piramide", "Diamante" };
    private readonly int[] shapePreferences = { 0, 2, 3, 1 };

    private int selectedLevel;
    private int selectedShape;

    private void Start()
    {
        selectedShape = PreferenceToShapeButton(PlayerPrefs.GetInt("ShadowBeat_LuxShape", 0));

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;
            levelButtons[i].onClick.AddListener(() => SelectLevel(index));
        }

        for (int i = 0; i < shapeButtons.Length; i++)
        {
            int index = i;
            shapeButtons[i].onClick.AddListener(() => SelectShape(index));
        }

        playButton.onClick.AddListener(PlaySelected);
        backButton.onClick.AddListener(menuController.OnBackToMain);
        SelectLevel(0);
        SelectShape(selectedShape);
    }

    private void SelectLevel(int index)
    {
        selectedLevel = Mathf.Clamp(index, 0, sceneNames.Length - 1);
        if (selectedLevelLabel != null)
        {
            selectedLevelLabel.text = $"NIVEL {selectedLevel + 1}  /  {levelNames[selectedLevel].ToUpperInvariant()}";
        }

        RefreshButtonColors(levelButtons, selectedLevel);
    }

    private void SelectShape(int index)
    {
        selectedShape = Mathf.Clamp(index, 0, shapePreferences.Length - 1);
        PlayerPrefs.SetInt("ShadowBeat_LuxShape", shapePreferences[selectedShape]);
        PlayerPrefs.Save();

        if (selectedShapeLabel != null)
        {
            selectedShapeLabel.text = $"FORMA  /  {shapeNames[selectedShape].ToUpperInvariant()}";
        }

        RefreshButtonColors(shapeButtons, selectedShape);
    }

    private void PlaySelected()
    {
        menuController.LoadLevel(sceneNames[selectedLevel]);
    }

    private static void RefreshButtonColors(Button[] buttons, int selected)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            ColorBlock colors = buttons[i].colors;
            colors.normalColor = i == selected
                ? new Color(0.08f, 0.34f, 0.38f, 0.98f)
                : new Color(0.035f, 0.045f, 0.09f, 0.94f);
            buttons[i].colors = colors;
        }
    }

    private int PreferenceToShapeButton(int preference)
    {
        for (int i = 0; i < shapePreferences.Length; i++)
        {
            if (shapePreferences[i] == preference)
            {
                return i;
            }
        }

        return 0;
    }
}
}
