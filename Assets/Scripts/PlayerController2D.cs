using UnityEngine;

public enum PlayerTeam
{
    Left = 0,
    Right = 1
}

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float kickRadius = 0.9f;
    [SerializeField] private float kickForce = 7f;
    [SerializeField] private float kickCooldown = 0.25f;
    [SerializeField] private float stepInterval = 0.32f;

    public PlayerTeam team;
    private Rigidbody2D body;
    private float kickTimer;
    private float stepTimer;

    private IGameAudio audioSystem;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void Start()
    {
        audioSystem = FindAnyObjectByType<GameAudioMock>();
    }

    private void Update()
    {
        kickTimer -= Time.deltaTime;
        if (kickTimer <= 0f)
        {
            KeyCode kickKey = team == PlayerTeam.Left ? KeyCode.E : KeyCode.RightShift;
            if (Input.GetKeyDown(kickKey))
                TryKick();
        }
    }

    private void FixedUpdate()
    {
        Vector2 input = GetInput();
        body.linearVelocity = input * moveSpeed;

        if (input.sqrMagnitude > 0.01f)
        {
            stepTimer -= Time.fixedDeltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = Mathf.Lerp(stepInterval, 0.16f, body.linearVelocity.magnitude / moveSpeed);
                audioSystem?.PlayPlayerStep((int)team, body.linearVelocity.magnitude);
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private Vector2 GetInput()
    {
        if (team == PlayerTeam.Left)
        {
            return new Vector2(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        }

        return new Vector2(
            (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
            (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f));
    }

    private void TryKick()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, kickRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            FootballBall ball = hits[i].GetComponent<FootballBall>();
            if (ball == null)
                continue;

            Vector2 direction = (ball.transform.position - transform.position);
            if (direction.sqrMagnitude < 0.01f)
                direction = team == PlayerTeam.Left ? Vector2.right : Vector2.left;

            ball.Kick(direction.normalized, kickForce, (int)team);
            kickTimer = kickCooldown;
            break;
        }
    }
}
