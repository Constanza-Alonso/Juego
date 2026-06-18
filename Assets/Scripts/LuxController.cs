using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class LuxController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float autoSpeed = 5.6f;
    [SerializeField] private float jumpForce = 13.5f;
    [SerializeField] private float shipLift = 18f;
    [SerializeField] private float raySpeedMultiplier = 1.6f;
    [SerializeField] private float jumpBufferTime = 0.16f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float fallDeathY = -7f;
    [SerializeField] private float ceilingDeathY = 7f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.34f;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Visuals")]
    [SerializeField] private Color cubeColor = new Color(0.2f, 0.95f, 1f);
    [SerializeField] private Color sphereColor = new Color(0.55f, 1f, 0.35f);
    [SerializeField] private Color shipColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color shadowColor = new Color(0.65f, 0.35f, 1f);
    [SerializeField] private Color rayColor = new Color(1f, 1f, 0.45f);

    private readonly Color[] randomLuxColors =
    {
        new Color(0.2f, 0.95f, 1f),
        new Color(1f, 0.92f, 0.25f),
        new Color(0.65f, 0.35f, 1f)
    };

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private Sprite circleSprite;
    private Sprite triangleSprite;
    private LuxForm currentForm = LuxForm.Cube;
    private bool gravityInverted;
    private bool isDead;
    private Vector3 startPosition;
    private float lastGroundedTime;
    private float lastActionPressedTime = -10f;
    private Color activeCubeColor;

    public LuxForm CurrentForm => currentForm;
    public bool IsDead => isDead;
    public float CurrentSpeed => autoSpeed * (currentForm == LuxForm.Ray ? raySpeedMultiplier : 1f);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultSprite = spriteRenderer.sprite;
        startPosition = transform.position;
        RandomizeCubeColor();
        ApplyFormVisuals();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (transform.position.y <= fallDeathY || transform.position.y >= ceilingDeathY)
        {
            Die();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetMouseButtonDown(0))
        {
            lastActionPressedTime = Time.time;
            Act();
        }

        if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetMouseButton(0)) && currentForm == LuxForm.Ship)
        {
            Fly();
        }

        if (IsGrounded())
        {
            lastGroundedTime = Time.time;
        }

        if (currentForm != LuxForm.Ship && Time.time - lastActionPressedTime <= jumpBufferTime)
        {
            TryJump();
        }
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            body.velocity = Vector2.zero;
            return;
        }

        body.velocity = new Vector2(CurrentSpeed, body.velocity.y);
    }

    public void Respawn(Vector3? checkpoint = null)
    {
        transform.position = checkpoint ?? startPosition;
        body.velocity = Vector2.zero;
        isDead = false;
        RandomizeCubeColor();
        SetForm(LuxForm.Cube);
        SetGravity(false);
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        body.velocity = Vector2.zero;
        GameEvents.PlayerDied();
    }

    public void Freeze()
    {
        isDead = true;
        body.velocity = Vector2.zero;
    }

    public void SetForm(LuxForm form)
    {
        currentForm = form;
        ApplyFormVisuals();
    }

    public void ToggleGravity()
    {
        SetGravity(!gravityInverted);
    }

    public void SetGravity(bool inverted)
    {
        gravityInverted = inverted;
        body.gravityScale = inverted ? -4f : 4f;
        transform.localScale = new Vector3(1f, inverted ? -1f : 1f, 1f);
    }

    private void Act()
    {
        switch (currentForm)
        {
            case LuxForm.Ship:
                Fly();
                break;
            case LuxForm.Sphere:
                body.velocity = new Vector2(body.velocity.x, -body.velocity.y);
                ToggleGravity();
                break;
            default:
                TryJump();
                break;
        }
    }

    private void TryJump()
    {
        if (Time.time - lastGroundedTime > coyoteTime)
        {
            return;
        }

        float direction = gravityInverted ? -1f : 1f;
        body.velocity = new Vector2(body.velocity.x, jumpForce * direction);
        lastActionPressedTime = -10f;
        lastGroundedTime = -10f;
    }

    private void Fly()
    {
        float direction = gravityInverted ? -1f : 1f;
        body.AddForce(Vector2.up * (shipLift * direction), ForceMode2D.Force);
    }

    private bool IsGrounded()
    {
        Vector2 checkPosition = groundCheck != null ? groundCheck.position : transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, groundCheckRadius, groundLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody != body)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyFormVisuals()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = currentForm switch
        {
            LuxForm.Sphere => sphereColor,
            LuxForm.Ship => shipColor,
            LuxForm.Shadow => shadowColor,
            LuxForm.Ray => rayColor,
            _ => activeCubeColor
        };

        ApplySelectedShape();
    }

    private void RandomizeCubeColor()
    {
        activeCubeColor = randomLuxColors[Random.Range(0, randomLuxColors.Length)];
    }

    private void ApplySelectedShape()
    {
        if (currentForm != LuxForm.Cube)
        {
            spriteRenderer.sprite = defaultSprite;
            transform.localRotation = Quaternion.identity;
            return;
        }

        int shape = PlayerPrefs.GetInt("ShadowBeat_LuxShape", 0);
        if (shape == 2)
        {
            spriteRenderer.sprite = GetCircleSprite();
            transform.localRotation = Quaternion.identity;
            return;
        }

        if (shape == 3)
        {
            spriteRenderer.sprite = GetTriangleSprite();
            transform.localRotation = Quaternion.identity;
            return;
        }

        spriteRenderer.sprite = defaultSprite;
        transform.localRotation = Quaternion.Euler(0f, 0f, shape == 1 ? 45f : 0f);
    }

    private Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    private Sprite GetTriangleSprite()
    {
        if (triangleSprite != null)
        {
            return triangleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float halfWidth = Mathf.Lerp(2f, 28f, y / (float)(size - 1));
                bool inside = Mathf.Abs(x - 31.5f) <= halfWidth && y <= 58;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        triangleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return triangleSprite;
    }
}
