using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private PortalType portalType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        LuxController lux = other.GetComponent<LuxController>();
        if (lux == null)
        {
            return;
        }

        switch (portalType)
        {
            case PortalType.FlipGravity:
                lux.ToggleGravity();
                break;
            case PortalType.Sphere:
                lux.SetForm(LuxForm.Sphere);
                break;
            case PortalType.Ship:
                lux.SetForm(LuxForm.Ship);
                break;
            case PortalType.Shadow:
                lux.SetForm(LuxForm.Shadow);
                break;
            case PortalType.Ray:
                lux.SetForm(LuxForm.Ray);
                break;
            default:
                lux.SetForm(LuxForm.Cube);
                lux.SetGravity(false);
                break;
        }
    }
}
