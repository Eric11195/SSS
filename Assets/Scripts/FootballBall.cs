using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class FootballBall : MonoBehaviour
{
    [SerializeField] private float groundDrag = 2.2f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float gravity = 8f;
    [SerializeField] private float airRestitution = 0.72f;

    private Rigidbody2D body;
    private SpriteRenderer ballRenderer;
    private SpriteRenderer shadowRenderer;
    private IGameAudio audioSystem;
    private float height;
    private float verticalVelocity;
    private bool wasGrounded = true;
    private float baseBallScale = 0.62f;

    public float Height => height;
    public float GroundSpeed => body != null ? body.linearVelocity.magnitude : 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearDamping = groundDrag;
        body.freezeRotation = true;

        ballRenderer = GetComponent<SpriteRenderer>();
        baseBallScale = transform.localScale.x;
    }

    private void Start()
    {
        audioSystem = FindAnyObjectByType<GameAudioMock>();
        FootballShadow shadow = GetComponentInChildren<FootballShadow>();
        if (shadow != null)
            shadowRenderer = shadow.GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (height > 0f || verticalVelocity > 0f)
        {
            verticalVelocity -= gravity * Time.fixedDeltaTime;
            height += verticalVelocity * Time.fixedDeltaTime;

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
        }

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

        if (shadowRenderer != null)
        {
            shadowRenderer.transform.localPosition = new Vector3(0f, -0.12f - normalized * 0.08f, 0f);
            float shadowScale = Mathf.Lerp(1f, 0.55f, normalized);
            shadowRenderer.transform.localScale = Vector3.one * shadowScale;
        }
    }

    public void Kick(Vector2 direction, float force, int team)
    {
        body.linearVelocity = direction * force;
        verticalVelocity = Mathf.Lerp(2.4f, 6f, Mathf.Clamp01(force / 10f));
        height = Mathf.Max(height, 0.05f);
        wasGrounded = false;
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
