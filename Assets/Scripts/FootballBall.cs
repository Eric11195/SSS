using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class FootballBall : MonoBehaviour
{
    private const string BallBounceEventPath = "event:/Ball/BallFall";
    private const string BallRustlingEventPath = "event:/Ball/BallRustling";
    [SerializeField] private float minYSpeedToRustle = 0.15f;
    [SerializeField] private float minHeightToRustle = 0.05f;
    [SerializeField] private float minXSpeedToRustle = 0.15f;

    [SerializeField] private float groundDrag = 2.2f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float minBallSize = 0.62f;
    [SerializeField] private float maxBallSize = 0.82f;
    [SerializeField] private float gravity = 8f;
    [SerializeField] private float airRestitution = 0.35f;
    [SerializeField, Min(0.01f)] private float fullBounceVolumeImpactSpeed = 10f;
    [SerializeField, Min(0.01f)] private float fullRustlingVolumeSpeed = 7f;
    [SerializeField] private float playerInteractionHeight = 0.5f;
    [SerializeField] private Transform listener;

    private Rigidbody2D body;
    private Collider2D ballCollider;
    private Collider2D[] playerColliders;
    private SpriteRenderer ballRenderer;
    private SpriteRenderer ballCoreRenderer;
    private SpriteRenderer shadowRenderer;
    private Color originalBallCoreColor;
    private IGameAudio audioSystem;
    private FMOD.Studio.EventInstance ballRustlingEvent;
    [SerializeField]
    private float height;
    private float verticalVelocity;
    private bool wasGrounded = true;
    private bool isIgnoringPlayerCollisions;
    private bool hasBallRustlingEvent;
    private bool isBallRustlingPaused = true;

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

    private float toMeters(float virtualHeight)
    {
        return virtualHeight * 8.0f;
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
                    PlayBallBounce(impactSpeed);
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
        listener.position = new Vector3(listener.position.x, listener.position.y, toMeters(height));
        ParameterSetter.SetBallHeight(height);
        UpdatePlayerCollisionState();

        bool grounded = height <= 0.02f;
        if (grounded != wasGrounded)
        {
            wasGrounded = grounded;
            audioSystem?.PlayBallRoll(grounded && body.linearVelocity.sqrMagnitude > 0.08f, body.linearVelocity.magnitude);
        }

        if (grounded)
            audioSystem?.PlayBallRoll(body.linearVelocity.sqrMagnitude > 0.08f, body.linearVelocity.magnitude);

        UpdateBallRustling(height <= minHeightToRustle && verticalVelocity < minYSpeedToRustle, body.linearVelocity.magnitude);
        UpdateVisualHeight();
    }

    private void UpdateBallRustling(bool isOnFloor, float horizontalSpeed)
    {
        float volume = isOnFloor
            ? Mathf.Clamp01((horizontalSpeed-minXSpeedToRustle) / Mathf.Max(0.01f, fullRustlingVolumeSpeed))
            : 0f;

        if (volume <= 0f)
        {
            if (!hasBallRustlingEvent || isBallRustlingPaused)
                return;

            FMOD.RESULT result = ballRustlingEvent.setVolume(0f);
            if (result != FMOD.RESULT.OK)
                Debug.LogError($"Failed to set FMOD ball-rustling volume: {result}", this);

            result = ballRustlingEvent.setPaused(true);
            if (result != FMOD.RESULT.OK)
                Debug.LogError($"Failed to pause FMOD ball-rustling event: {result}", this);
            else
                isBallRustlingPaused = true;

            return;
        }

        if (!hasBallRustlingEvent)
        {
            FMOD.Studio.EventInstance newEvent = RuntimeManager.CreateInstance(BallRustlingEventPath);
            if (!newEvent.isValid())
            {
                Debug.LogError($"Failed to create FMOD event: {BallRustlingEventPath}", this);
                return;
            }

            RuntimeManager.AttachInstanceToGameObject(newEvent, gameObject, body);
            FMOD.RESULT result = newEvent.setVolume(volume);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to set FMOD ball-rustling volume: {result}", this);
                newEvent.release();
                return;
            }

            result = newEvent.start();
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to start FMOD ball-rustling event: {result}", this);
                newEvent.release();
                return;
            }

            ballRustlingEvent = newEvent;
            hasBallRustlingEvent = true;
            isBallRustlingPaused = false;
            return;
        }

        FMOD.RESULT updateResult = ballRustlingEvent.setVolume(volume);
        if (updateResult != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set FMOD ball-rustling volume: {updateResult}", this);
            return;
        }

        if (isBallRustlingPaused)
        {
            updateResult = ballRustlingEvent.setPaused(false);
            if (updateResult != FMOD.RESULT.OK)
                Debug.LogError($"Failed to resume FMOD ball-rustling event: {updateResult}", this);
            else
                isBallRustlingPaused = false;
        }
    }

    private void PlayBallBounce(float impactSpeed, Vector2? direction = null)
    {
        FMOD.Studio.EventInstance bounceEvent = RuntimeManager.CreateInstance(BallBounceEventPath);
        float volume = Mathf.Clamp01(impactSpeed / Mathf.Max(0.01f, fullBounceVolumeImpactSpeed));
        FMOD.RESULT result = bounceEvent.setVolume(volume);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set FMOD ball-bounce volume: {result}", this);
            bounceEvent.release();
            return;
        }

        if (direction.HasValue && direction.Value.sqrMagnitude > 0f)
        {
            Vector3 forward = new Vector3(direction.Value.x, direction.Value.y, 0f).normalized;
            FMOD.ATTRIBUTES_3D attributes = RuntimeUtils.To3DAttributes(transform, body);
            attributes.forward = forward.ToFMODVector();
            attributes.up = Vector3.Cross(Vector3.forward, forward).ToFMODVector();
            result = bounceEvent.set3DAttributes(attributes);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to set FMOD ball-bounce position and direction: {result}", this);
                bounceEvent.release();
                return;
            }
        }
        else
        {
            RuntimeManager.AttachInstanceToGameObject(bounceEvent, gameObject, body);
        }

        result = bounceEvent.start();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to start FMOD ball-bounce event: {result}", this);

        result = bounceEvent.release();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to release FMOD ball-bounce event: {result}", this);
    }

    private void OnDestroy()
    {
        if (!hasBallRustlingEvent)
            return;

        FMOD.RESULT result = ballRustlingEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to stop FMOD ball-rustling event: {result}", this);

        result = ballRustlingEvent.release();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to release FMOD ball-rustling event: {result}", this);
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
            transform.localScale = Vector3.one * Mathf.Lerp(minBallSize, maxBallSize, normalized);

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

        bool hitWall = collision.gameObject.CompareTag("Wall");
        float impactSpeed = collision.relativeVelocity.magnitude;
        audioSystem?.PlayBallImpact(hitWall ? "wall" : "player", impactSpeed);
        if (hitWall)
        {
            Vector2 direction = collision.GetContact(0).point - body.position;
            if (direction.sqrMagnitude == 0f)
                direction = collision.relativeVelocity;

            PlayBallBounce(impactSpeed, direction);
        }
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
