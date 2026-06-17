using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 movement = new Vector2(0f, 1.5f);
    [SerializeField] private float speed = 1.5f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        transform.position = startPosition + (Vector3)(movement * t);
    }
}
