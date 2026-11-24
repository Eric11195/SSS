using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class FootballBall : MonoBehaviour
{
    [SerializeField] private float groundDrag = 2.2f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float gravity = 8f;
    [SerializeField] private float airRestitution = 0.35f;
    [SerializeField] private float playerInteractionHeight = 0.5f;

    private Rigidbody2D body;
    private Collider2D ballCollider;
    private Collider2D[] playerColliders;
    private SpriteRenderer ballRenderer;
    private SpriteRenderer ballCoreRenderer;
    private SpriteRenderer shadowRenderer;
    private Color originalBallCoreColor;
    private IGameAudio audioSystem;
    [SerializeField]
    private float height;
    private float verticalVelocity;
    private bool wasGrounded = true;
    private bool isIgnoringPlayerCollisions;
    private float baseBallScale = 0.62f;

    public float Height => height;
    public bool CanBeKicked => height <= playerInteractionHeight;
    public float GroundSpeed => body != null ? body.linearVelocity.magnitude : 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
        body.gravityScale = 0f;
        body.linearDamping = groundDrag;
        body.freezeRotation = true;

        ballRenderer = GetComponent<SpriteRenderer>();
        baseBallScale = transform.localScale.x;
        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer childRenderer in childRenderers)
        {
            if (childRenderer != ballRenderer && childRenderer.GetComponentInParent<FootballShadow>() == null)
            {
                ballCoreRenderer = childRenderer;
                originalBallCoreColor = ballCoreRenderer.color;
                break;
            }
        }
    }

    private void Start()
    {
        audioSystem = FindAnyObjectByType<GameAudioMock>();
        PlayerController2D[] players = FindObjectsByType<PlayerController2D>();
        playerColliders = new Collider2D[players.Length];
        for (int i = 0; i < players.Length; i++)
            playerColliders[i] = players[i].GetComponent<Collider2D>();

        FootballShadow shadow = GetComponentInChildren<FootballShadow>();
        if (shadow != null)
            shadowRenderer = shadow.GetComponent<SpriteRenderer>();

        UpdatePlayerCollisionState();
    }

    private void FixedUpdate()
    {
        if (height > 0f || verticalVelocity > 0f)
        {
            float previousVerticalVelocity = verticalVelocity;
            verticalVelocity -= gravity * Time.fixedDeltaTime;
            height += (previousVerticalVelocity + verticalVelocity) * 0.5f * Time.fixedDeltaTime;

            if (height <= 0f)
            {
                height = 0f;
                if (verticalVelocity < -0.5f)
                {
                    float impactSpeed = Mathf.Abs(verticalVelocity);
                    audioSystem?.PlayBallImpact("ground", impactSpeed);
                    verticalVelocity = -verticalVelocity * airRestitution;
                    if (verticalVelocity < 1f)
                        verticalVelocity = 0f;
                }
                else
                {
                    verticalVelocity = 0f;
                }
            }
            ParameterSetter.setBallHeight(height);
        }

        UpdatePlayerCollisionState();

        bool grounded = height <= 0.02f;
        if (grounded != wasGrounded)
        {
            wasGrounded = grounded;
            audioSystem?.PlayBallRoll(grounded && body.linearVelocity.sqrMagnitude > 0.08f, body.linearVelocity.magnitude);
        }

        if (grounded)
            audioSystem?.PlayBallRoll(body.linearVelocity.sqrMagnitude > 0.08f, body.linearVelocity.magnitude);

        UpdateVisualHeight();
    }

    private void Update()
    {
        float goalProximity = Mathf.Clamp01(1f - Mathf.Abs(transform.position.x) / 9f);
        audioSystem?.SetBallParameters(transform.position, height / maxHeight, GroundSpeed, goalProximity);
    }

    private void UpdateVisualHeight()
    {
        float normalized = Mathf.Clamp01(height / maxHeight);
        if (ballRenderer != null)
            transform.localScale = Vector3.one * baseBallScale * Mathf.Lerp(1f, 1.32f, normalized);

        if (ballCoreRenderer != null)
        {
            float brightness = CanBeKicked ? 1f : 0.8f;
            ballCoreRenderer.color = new Color(
                originalBallCoreColor.r * brightness,
                originalBallCoreColor.g * brightness,
                originalBallCoreColor.b * brightness,
                originalBallCoreColor.a);
        }

        if (shadowRenderer != null)
        {
            shadowRenderer.transform.localPosition = new Vector3(0f, -0.12f - normalized * 0.08f, 0f);
            float shadowScale = Mathf.Lerp(1f, 0.55f, normalized);
            shadowRenderer.transform.localScale = Vector3.one * shadowScale;
        }
    }

    public void Kick(Vector2 direction, float force, int team, float targetHeight)
    {
        if (!CanBeKicked)
            return;

        body.linearVelocity = direction * force;
        height = 0f;
        verticalVelocity = Mathf.Sqrt(2f * gravity * Mathf.Max(0f, targetHeight));
        wasGrounded = verticalVelocity <= 0f;
        audioSystem?.PlayBallKick(team, force);
    }

    public void Launch(Vector2 direction, float force, float launchHeight)
    {
        body.linearVelocity = direction * force;
        height = Mathf.Max(height, launchHeight);
        verticalVelocity = Mathf.Sqrt(2f * gravity * height);
        wasGrounded = false;
        audioSystem?.PlayBallKick(0, force);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.magnitude < 0.35f)
            return;

        audioSystem?.PlayBallImpact(collision.gameObject.CompareTag("Wall") ? "wall" : "player", collision.relativeVelocity.magnitude);
    }

    private void UpdatePlayerCollisionState()
    {
        bool ignorePlayerCollisions = height > playerInteractionHeight;
        if (ignorePlayerCollisions == isIgnoringPlayerCollisions)
            return;

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null)
                Physics2D.IgnoreCollision(ballCollider, playerCollider, ignorePlayerCollisions);
        }

        isIgnoringPlayerCollisions = ignorePlayerCollisions;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GoalTrigger goal = other.GetComponent<GoalTrigger>();
        if (goal != null)
            FindAnyObjectByType<FootballMatch>()?.ScoreGoal(goal.Team);
    }

    public void ResetBall(Vector3 position)
    {
        transform.position = position;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        height = 0f;
        verticalVelocity = 0f;
        wasGrounded = true;
        UpdateVisualHeight();
    }
}
