using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class RefereeController2D : MonoBehaviour
{
    private const float SeparationMargin = 0.05f;
    private const int TargetDirectionSamples = 72;

    [SerializeField, Min(0.1f)] private float fieldHorizontalSize = 15.4f;
    [SerializeField, Min(0.1f)] private float fieldVerticalSize = 8.4f;
    [SerializeField, Min(0.1f)] private float followDistance = 2.8f;
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField, Min(0.1f)] private float moveAcceleration = 12f;
    [SerializeField, Min(0.01f)] private float targetSmoothTime = 0.3f;

    private Rigidbody2D body;
    private FootballBall ball;
    private Rigidbody2D ballBody;
    private PlayerController2D[] players;
    private Vector2 smoothedTarget;
    private Vector2 smoothedTargetVelocity;
    private bool hasSmoothedTarget;

    private static Sprite outlineSprite;
    private static Sprite stripedSprite;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearDamping = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
            circleCollider = gameObject.AddComponent<CircleCollider2D>();

        circleCollider.radius = 0.48f;

        ball = FindAnyObjectByType<FootballBall>();
        if (ball != null)
            ballBody = ball.GetComponent<Rigidbody2D>();

        players = FindObjectsByType<PlayerController2D>();
        CreateVisuals();
    }

    private void FixedUpdate()
    {
        ClampCurrentPositionToField();

        if (ball == null)
        {
            ball = FindAnyObjectByType<FootballBall>();
            if (ball == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            ballBody = ball.GetComponent<Rigidbody2D>();
        }

        Vector2 ballPosition = ball.transform.position;
        Vector2 ballVelocity = ballBody != null ? ballBody.linearVelocity : Vector2.zero;
        Vector2 trailingDirection = ballVelocity.sqrMagnitude > 0.04f
            ? -ballVelocity.normalized
            : Vector2.down;
        Vector2 targetPosition = GetFollowTarget(ballPosition, trailingDirection);
        if (!hasSmoothedTarget)
        {
            smoothedTarget = body.position;
            hasSmoothedTarget = true;
        }

        smoothedTarget = Vector2.SmoothDamp(
            smoothedTarget,
            targetPosition,
            ref smoothedTargetVelocity,
            targetSmoothTime,
            moveSpeed,
            Time.fixedDeltaTime);
        smoothedTarget = ClampToField(smoothedTarget);

        Vector2 movement = smoothedTarget - body.position;
        Vector2 desiredVelocity = movement.sqrMagnitude > 0.01f
            ? movement.normalized * moveSpeed
            : Vector2.zero;
        Vector2 velocity = Vector2.MoveTowards(
            body.linearVelocity,
            desiredVelocity,
            moveAcceleration * Time.fixedDeltaTime);

        float minimumSeparation = GetMinimumSeparation();
        for (int pass = 0; pass < 4; pass++)
        {
            velocity = KeepMinimumDistance(velocity, ballPosition, ballVelocity, minimumSeparation);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController2D player = players[i];
                if (player == null)
                    continue;

                Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
                Vector2 playerVelocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
                velocity = KeepMinimumDistance(
                    velocity,
                    player.transform.position,
                    playerVelocity,
                    minimumSeparation);
            }
        }

        body.linearVelocity = ConstrainToField(velocity);
    }

    private void LateUpdate()
    {
        ClampCurrentPositionToField();
    }

    public void ResetPosition(Vector3 ballPosition)
    {
        float resetDistance = Mathf.Max(followDistance, GetMinimumSeparation() + SeparationMargin);
        Vector3 position = new Vector3(
            ballPosition.x,
            ballPosition.y - resetDistance,
            transform.position.z);
        Vector2 resetPosition = ClampToField(position);
        position.x = resetPosition.x;
        position.y = resetPosition.y;
        transform.position = position;
        body.position = resetPosition;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        smoothedTarget = resetPosition;
        smoothedTargetVelocity = Vector2.zero;
        hasSmoothedTarget = true;
    }

    private Vector2 GetFollowTarget(Vector2 ballPosition, Vector2 trailingDirection)
    {
        float targetDistance = Mathf.Max(followDistance, GetMinimumSeparation() + SeparationMargin);
        float bestViolation = float.PositiveInfinity;
        float bestAlignment = float.NegativeInfinity;
        Vector2 bestDirection = trailingDirection;
        float minimumSeparation = GetMinimumSeparation() + SeparationMargin;

        for (int i = 0; i < TargetDirectionSamples; i++)
        {
            float angle = i * Mathf.PI * 2f / TargetDirectionSamples;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 candidate = ballPosition + direction * targetDistance;
            float violation = 0f;
            if (!IsInsideField(candidate))
                violation += 1000f;

            for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
            {
                PlayerController2D player = players[playerIndex];
                if (player == null)
                    continue;

                float shortfall = Mathf.Max(
                    0f,
                    minimumSeparation - Vector2.Distance(candidate, player.transform.position));
                violation += shortfall * shortfall;
            }

            float alignment = Vector2.Dot(direction, trailingDirection);
            if (violation < bestViolation ||
                (Mathf.Approximately(violation, bestViolation) && alignment > bestAlignment))
            {
                bestViolation = violation;
                bestAlignment = alignment;
                bestDirection = direction;
            }
        }

        Vector2 preferredTarget = ballPosition + trailingDirection * targetDistance;
        float preferredViolation = GetPlayerSeparationViolation(preferredTarget, minimumSeparation);
        if (!IsInsideField(preferredTarget))
            preferredViolation += 1000f;

        if (preferredViolation < bestViolation ||
            (Mathf.Approximately(preferredViolation, bestViolation) && bestAlignment < 1f))
        {
            bestDirection = trailingDirection;
        }

        return ballPosition + bestDirection * targetDistance;
    }

    private float GetPlayerSeparationViolation(Vector2 position, float minimumSeparation)
    {
        float violation = 0f;
        for (int i = 0; i < players.Length; i++)
        {
            PlayerController2D player = players[i];
            if (player == null)
                continue;

            float shortfall = Mathf.Max(
                0f,
                minimumSeparation - Vector2.Distance(position, player.transform.position));
            violation += shortfall * shortfall;
        }

        return violation;
    }

    private Vector2 KeepMinimumDistance(
        Vector2 velocity,
        Vector2 obstaclePosition,
        Vector2 obstacleVelocity,
        float minimumSeparation)
    {
        Vector2 away = body.position - obstaclePosition;
        float currentDistance = away.magnitude;
        away = currentDistance > 0.001f ? away / currentDistance : Vector2.down;

        float requiredDistance = minimumSeparation + SeparationMargin;
        Vector2 relativeVelocity = velocity - obstacleVelocity;
        if (currentDistance < requiredDistance)
        {
            float radialSpeed = Vector2.Dot(relativeVelocity, away);
            float correctionSpeed = Mathf.Min(moveSpeed, (requiredDistance - currentDistance) * 4f);
            if (radialSpeed < correctionSpeed)
                velocity += away * (correctionSpeed - radialSpeed);

            return velocity;
        }

        Vector2 nextRelativePosition = body.position - obstaclePosition +
            relativeVelocity * Time.fixedDeltaTime;
        if (nextRelativePosition.sqrMagnitude < requiredDistance * requiredDistance)
        {
            float radialSpeed = Vector2.Dot(relativeVelocity, away);
            if (radialSpeed < 0f)
                velocity -= away * radialSpeed;
        }

        return velocity;
    }

    private Vector2 ConstrainToField(Vector2 velocity)
    {
        Vector2 position = body.position;
        float deltaTime = Time.fixedDeltaTime;
        float minX = GetMinX();
        float maxX = GetMaxX();
        float minY = GetMinY();
        float maxY = GetMaxY();

        velocity.x = Mathf.Clamp(velocity.x, (minX - position.x) / deltaTime, (maxX - position.x) / deltaTime);
        velocity.y = Mathf.Clamp(velocity.y, (minY - position.y) / deltaTime, (maxY - position.y) / deltaTime);
        return velocity;
    }

    private Vector2 ClampToField(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.x, GetMinX(), GetMaxX()),
            Mathf.Clamp(position.y, GetMinY(), GetMaxY()));
    }

    private void ClampCurrentPositionToField()
    {
        Vector2 position = body.position;
        Vector2 boundedPosition = ClampToField(position);
        if (position == boundedPosition)
            return;

        Vector2 velocity = body.linearVelocity;
        if (!Mathf.Approximately(position.x, boundedPosition.x))
            velocity.x = 0f;
        if (!Mathf.Approximately(position.y, boundedPosition.y))
            velocity.y = 0f;

        body.position = boundedPosition;
        body.linearVelocity = velocity;
        Vector3 worldPosition = transform.position;
        worldPosition.x = boundedPosition.x;
        worldPosition.y = boundedPosition.y;
        transform.position = worldPosition;
    }

    private bool IsInsideField(Vector2 position)
    {
        return position.x >= GetMinX() && position.x <= GetMaxX() &&
            position.y >= GetMinY() && position.y <= GetMaxY();
    }

    private float GetMinX()
    {
        return -Mathf.Max(0.96f, fieldHorizontalSize) * 0.5f + 0.48f;
    }

    private float GetMaxX()
    {
        return Mathf.Max(0.96f, fieldHorizontalSize) * 0.5f - 0.48f;
    }

    private float GetMinY()
    {
        return -Mathf.Max(0.96f, fieldVerticalSize) * 0.5f + 0.48f;
    }

    private float GetMaxY()
    {
        return Mathf.Max(0.96f, fieldVerticalSize) * 0.5f - 0.48f;
    }

    private float GetMinimumSeparation()
    {
        return Mathf.Max(0.1f, fieldVerticalSize) / 3f;
    }

    private void CreateVisuals()
    {
        SpriteRenderer outline = GetComponent<SpriteRenderer>();
        if (outline == null)
            outline = gameObject.AddComponent<SpriteRenderer>();

        outline.sprite = GetOutlineSprite();
        outline.color = Color.white;
        outline.sortingOrder = 15;
        transform.localScale = Vector3.one * 0.96f;

        Transform existingCore = transform.Find("Referee Stripes");
        if (existingCore != null)
            return;

        GameObject core = new GameObject("Referee Stripes");
        core.transform.SetParent(transform, false);
        core.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        core.transform.localScale = Vector3.one * 0.79f;

        SpriteRenderer coreRenderer = core.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = GetStripedSprite();
        coreRenderer.sortingOrder = 16;
    }

    private static Sprite GetOutlineSprite()
    {
        if (outlineSprite != null)
            return outlineSprite;

        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? new Color32(35, 40, 50, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        outlineSprite = CreateSprite(size, pixels);
        return outlineSprite;
    }

    private static Sprite GetStripedSprite()
    {
        if (stripedSprite != null)
            return stripedSprite;

        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), center) > radius)
                {
                    pixels[y * size + x] = new Color32(255, 255, 255, 0);
                    continue;
                }

                float stripePosition = (y - center.y) - (x - center.x) * 0.5f;
                float nearestStripe = Mathf.Abs(Mathf.Repeat(stripePosition + 7f, 14f) - 7f);
                pixels[y * size + x] = nearestStripe < 2.2f
                    ? new Color32(20, 23, 29, 255)
                    : new Color32(255, 255, 255, 255);
            }
        }

        stripedSprite = CreateSprite(size, pixels);
        return stripedSprite;
    }

    private static Sprite CreateSprite(int size, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
