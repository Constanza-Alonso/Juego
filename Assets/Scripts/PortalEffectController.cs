using UnityEngine;

public class PortalEffectController : MonoBehaviour
{
    public static PortalEffectController Instance { get; private set; }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color[] backgroundColors =
    {
        new Color(0.006f, 0.01f, 0.025f),
        new Color(0.02f, 0.07f, 0.1f),
        new Color(0.08f, 0.02f, 0.11f),
        new Color(0.09f, 0.045f, 0.01f)
    };

    private int colorIndex;

    private void Awake()
    {
        Instance = this;
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    public void TriggerPortalEffect(PortalType portalType)
    {
        if (targetCamera == null || backgroundColors.Length == 0)
        {
            return;
        }

        colorIndex = (colorIndex + 1) % backgroundColors.Length;
        targetCamera.backgroundColor = backgroundColors[colorIndex];
    }
}
