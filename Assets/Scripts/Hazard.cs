using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private bool shadowCanPass;

    private void OnTriggerEnter2D(Collider2D other)
    {
        LuxController lux = other.GetComponent<LuxController>();
        if (lux == null)
        {
            return;
        }

        if (shadowCanPass && lux.CurrentForm == LuxForm.Shadow)
        {
            return;
        }

        lux.Die();
    }
}
