using UnityEngine;

public class CrystalCollectible : MonoBehaviour
{
    [SerializeField] private int value = 1;

    public int Value => value;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out LuxController _))
        {
            return;
        }

        GameEvents.CrystalCollected();
        Destroy(gameObject);
    }
}
