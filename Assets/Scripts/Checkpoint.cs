using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out LuxController _) && LevelManager.Instance != null)
        {
            LevelManager.Instance.SetCheckpoint(transform.position);
        }
    }
}
