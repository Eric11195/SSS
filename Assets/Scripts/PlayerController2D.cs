using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
    [SerializeField] private float maxKickChargeTime = 3f;
    [SerializeField] private float minKickHeight = 0f;
    [SerializeField] private float maxKickHeight = 3f;
    [SerializeField] private float stepInterval = 0.32f;

    public PlayerTeam team;
    private Rigidbody2D body;
    private SpriteRenderer outlineRenderer;
    private Color originalOutlineColor;
    private float kickTimer;
    private float kickChargeTime;
    private float stepTimer;
    private bool isChargingKick;
    private bool kickConsumedUntilRelease;

    private IGameAudio audioSystem;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        outlineRenderer = GetComponent<SpriteRenderer>();
        if (outlineRenderer != null)
            originalOutlineColor = outlineRenderer.color;

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

        KeyControl kickKey = team == PlayerTeam.Left ? Keyboard.current.eKey : Keyboard.current.rightShiftKey;
        if (!isChargingKick && !kickConsumedUntilRelease && kickTimer <= 0f && kickKey.wasPressedThisFrame)
        {
            isChargingKick = true;
            kickChargeTime = 0f;
        }

        if (kickKey.wasReleasedThisFrame)
        {
            if (isChargingKick)
                FinishChargedKick();

            kickConsumedUntilRelease = false;
            return;
        }

        if (kickConsumedUntilRelease || !isChargingKick || !kickKey.isPressed)
            return;

        kickChargeTime = Mathf.Min(kickChargeTime + Time.deltaTime, maxKickChargeTime);
        UpdateOutlineChargeColor();

        if (kickChargeTime >= maxKickChargeTime)
        {
            FinishChargedKick();
            kickConsumedUntilRelease = true;
        }
    }

    private void FixedUpdate()
    {
        Vector2 input = GetInput().normalized;
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
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
        }

        return new Vector2(
            (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.leftArrowKey.isPressed ? 1f : 0f),
            (Keyboard.current.upArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.downArrowKey.isPressed ? 1f : 0f));
    }

    private void FinishChargedKick()
    {
        float chargeRatio = maxKickChargeTime > 0f
            ? Mathf.Clamp01(kickChargeTime / maxKickChargeTime)
            : 1f;
        float targetHeight = Mathf.Lerp(
            minKickHeight,
            maxKickHeight,
            chargeRatio);
        TryKick(targetHeight);
        isChargingKick = false;
        kickChargeTime = 0f;
        ResetOutlineColor();
    }

    private void UpdateOutlineChargeColor()
    {
        if (outlineRenderer == null)
            return;

        float chargeRatio = maxKickChargeTime > 0f ? kickChargeTime / maxKickChargeTime : 1f;
        outlineRenderer.color = Color.Lerp(Color.green, Color.red, chargeRatio);
    }

    private void ResetOutlineColor()
    {
        if (outlineRenderer != null)
            outlineRenderer.color = originalOutlineColor;
    }

    private void TryKick(float targetHeight)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, kickRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            FootballBall ball = hits[i].GetComponent<FootballBall>();
            if (ball == null || !ball.CanBeKicked)
                continue;

            Vector2 direction = (ball.transform.position - transform.position);
            if (direction.sqrMagnitude < 0.01f)
                direction = team == PlayerTeam.Left ? Vector2.right : Vector2.left;

            ball.Kick(direction.normalized, kickForce, (int)team, targetHeight);
            kickTimer = kickCooldown;
            break;
        }
    }
}
