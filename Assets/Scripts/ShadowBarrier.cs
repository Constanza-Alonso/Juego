using UnityEngine;

public class ShadowBarrier : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        LuxController lux = other.GetComponent<LuxController>();
        if (lux == null)
        {
            return;
        }

        if (lux.CurrentForm != LuxForm.Shadow)
        {
            lux.Die();
        }
    }
}
