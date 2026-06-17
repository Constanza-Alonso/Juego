using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out LuxController _))
        {
            GameEvents.LevelCompleted();
        }
    }
}
