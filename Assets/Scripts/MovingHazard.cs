using UnityEngine;

[RequireComponent(typeof(Hazard))]
public class MovingHazard : MonoBehaviour
{
    [SerializeField] private Vector2 movement = new Vector2(0f, 1.2f);
    [SerializeField] private float speed = 2f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float t = Mathf.Sin(Time.time * speed);
        transform.position = startPosition + (Vector3)(movement * t);
    }
}
