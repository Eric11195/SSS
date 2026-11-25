using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FootballSceneBootstrap : MonoBehaviour
{
    private static readonly Color Outline = new Color(0.16f, 0.19f, 0.24f);
    private static readonly Color Pitch = new Color(0.58f, 0.78f, 0.68f);
    private static readonly Color PitchStripe = new Color(0.68f, 0.84f, 0.74f);
    private static readonly Color Stand = new Color(0.78f, 0.75f, 0.86f);
    private static readonly Color LeftTeam = new Color(0.38f, 0.68f, 0.82f);
    private static readonly Color RightTeam = new Color(0.93f, 0.48f, 0.46f);

    private void Awake()
    {
        CreateCamera();
        CreatePitch();
        CreateStands();
        CreateGoals();
        CreateWalls();

        GameObject matchRoot = new GameObject("FootballMatch");
        matchRoot.transform.SetParent(transform);
        matchRoot.AddComponent<GameAudioMock>();
        matchRoot.AddComponent<FootballMatch>();

        CreatePlayer("Blue Player", new Vector3(-4.5f, 0f, -1f), LeftTeam, PlayerTeam.Left);
        CreatePlayer("Coral Player", new Vector3(4.5f, 0f, -1f), RightTeam, PlayerTeam.Right);
        CreateBall();
        CreateReferee();
    }

    private static void CreateCamera()
    {
        if (Camera.main != null)
            return;

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.91f, 0.88f, 0.78f);
        camera.transform.position = new Vector3(0f, 0f, -20f);
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreatePitch()
    {
        CreateBox("Pitch Outline", Vector3.zero, new Vector3(16f, 9f, 1f), Outline, -1f);
        CreateBox("Pitch", Vector3.zero, new Vector3(15.4f, 8.4f, 1f), Pitch, -0.9f);

        for (int i = -3; i <= 3; i++)
        {
            if (i % 2 == 0)
                CreateBox("Pitch Stripe", new Vector3(i * 2f, 0f, -0.85f), new Vector3(1.9f, 8.35f, 1f), PitchStripe, -0.85f);
        }

        CreateBox("Middle Line", new Vector3(0f, 0f, -0.7f), new Vector3(0.12f, 8.35f, 1f), Outline, -0.7f);
        CreateCircleOutline("Centre Circle", Vector3.zero, 1.5f, Outline, Pitch, -0.65f);
        CreatePenaltyArea("Left Penalty", -6.5f);
        CreatePenaltyArea("Right Penalty", 6.5f);
    }

    private static void CreatePenaltyArea(string name, float x)
    {
        CreateBox(name + " Outline", new Vector3(x, 0f, -0.65f), new Vector3(2.2f, 5.2f, 1f), Outline, -0.65f);
        CreateBox(name, new Vector3(x, 0f, -0.64f), new Vector3(2f, 4.95f, 1f), Pitch, -0.64f);
    }

    private static void CreateStands()
    {
        CreateBox("North Stand", new Vector3(0f, 5f, 0f), new Vector3(15.2f, 1.7f, 1f), Stand, -0.4f);
        CreateBox("South Stand", new Vector3(0f, -5f, 0f), new Vector3(15.2f, 1.7f, 1f), Stand, -0.4f);
        CreateBox("West Stand", new Vector3(-8.2f, 0f, 0f), new Vector3(1.5f, 7.8f, 1f), Stand, -0.4f);
        CreateBox("East Stand", new Vector3(8.2f, 0f, 0f), new Vector3(1.5f, 7.8f, 1f), Stand, -0.4f);

        AddCrowdDots("North Crowd", 0f, 5f, 7, true);
        AddCrowdDots("South Crowd", 0f, -5f, 7, true);
        AddCrowdDots("West Crowd", -8.2f, 0f, 5, false);
        AddCrowdDots("East Crowd", 8.2f, 0f, 5, false);
    }

    private static void AddCrowdDots(string name, float cx, float cy, int count, bool horizontal)
    {
        GameObject parent = new GameObject(name);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * 1.7f;
            Vector3 position = horizontal
                ? new Vector3(cx + offset, cy + (i % 2 == 0 ? -0.25f : 0.25f), -0.35f)
                : new Vector3(cx + (i % 2 == 0 ? -0.25f : 0.25f), cy + offset * 0.58f, -0.35f);
            Color color = i % 3 == 0 ? RightTeam : LeftTeam;
            GameObject dot = CreateCircle(name + i, position, 0.16f, color, -0.35f);
            dot.transform.SetParent(parent.transform);
        }
    }

    private static void CreateGoals()
    {
        CreateBox("Left Goal Net", new Vector3(-8.25f, 0f, -0.2f), new Vector3(1.3f, 2.2f, 1f), new Color(0.74f, 0.84f, 0.88f), -0.2f);
        CreateBox("Right Goal Net", new Vector3(8.25f, 0f, -0.2f), new Vector3(1.3f, 2.2f, 1f), new Color(0.74f, 0.84f, 0.88f), -0.2f);
        CreateBox("Left Top Post", new Vector3(-7.72f, 1.12f, -0.1f), new Vector3(1.1f, 0.15f, 1f), Outline, -0.1f);
        CreateBox("Left Bottom Post", new Vector3(-7.72f, -1.12f, -0.1f), new Vector3(1.1f, 0.15f, 1f), Outline, -0.1f);
        CreateBox("Right Top Post", new Vector3(7.72f, 1.12f, -0.1f), new Vector3(1.1f, 0.15f, 1f), Outline, -0.1f);
        CreateBox("Right Bottom Post", new Vector3(7.72f, -1.12f, -0.1f), new Vector3(1.1f, 0.15f, 1f), Outline, -0.1f);

        CreateGoalTrigger("Left Goal Trigger", new Vector3(-8.15f, 0f, -0.1f), PlayerTeam.Left);
        CreateGoalTrigger("Right Goal Trigger", new Vector3(8.15f, 0f, -0.1f), PlayerTeam.Right);
    }

    private static void CreateGoalTrigger(string name, Vector3 position, PlayerTeam team)
    {
        GameObject trigger = new GameObject(name);
        trigger.transform.position = position;
        BoxCollider2D collider = trigger.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 2f);
        collider.isTrigger = true;
        GoalTrigger goal = trigger.AddComponent<GoalTrigger>();
        goal.SetTeam(team);
    }

    private static void CreateWalls()
    {
        CreateWall("North Wall", new Vector3(0f, 4.25f, 0f), new Vector3(15.4f, 0.18f, 1f));
        CreateWall("South Wall", new Vector3(0f, -4.25f, 0f), new Vector3(15.4f, 0.18f, 1f));
        CreateWall("West North Wall", new Vector3(-7.72f, 2.7f, 0f), new Vector3(0.18f, 3.1f, 1f));
        CreateWall("West South Wall", new Vector3(-7.72f, -2.7f, 0f), new Vector3(0.18f, 3.1f, 1f));
        CreateWall("East North Wall", new Vector3(7.72f, 2.7f, 0f), new Vector3(0.18f, 3.1f, 1f));
        CreateWall("East South Wall", new Vector3(7.72f, -2.7f, 0f), new Vector3(0.18f, 3.1f, 1f));
    }

    private static void CreateWall(string name, Vector3 position, Vector3 size)
    {
        GameObject wall = CreateBox(name, position, size, Outline, -0.5f);
        wall.name = name;
        wall.AddComponent<BoxCollider2D>();
        wall.tag = "Wall";
    }

    private static void CreatePlayer(string name, Vector3 position, Color color, PlayerTeam team)
    {
        GameObject player = CreateCircle(name + " Outline", position, 0.48f, Outline, -1.2f);
        SpriteRenderer outline = player.GetComponent<SpriteRenderer>();
        outline.sortingOrder = 5;

        GameObject core = CreateCircle(name, position, 0.38f, color, -1.3f);
        core.transform.SetParent(player.transform);
        core.transform.localPosition = Vector3.zero;
        core.GetComponent<SpriteRenderer>().sortingOrder = 6;

        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = 0.48f;
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearDamping = 8f;
        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.team = team;
    }

    private static void CreateBall()
    {
        GameObject shadow = CreateCircle("Ball Shadow", new Vector3(0f, -0.12f, -1.1f), 0.34f,
            new Color(0.12f, 0.12f, 0.12f, 0.25f), -1.1f);

        GameObject ball = CreateCircle("Ball Outline", Vector3.zero, 0.36f, Outline, -1.2f);
        ball.GetComponent<SpriteRenderer>().sortingOrder = 8;

        GameObject core = CreateCircle("Ball", Vector3.zero, 0.29f, new Color(0.98f, 0.96f, 0.88f), -1.3f);
        core.transform.SetParent(ball.transform);
        core.transform.localPosition = Vector3.zero;
        core.GetComponent<SpriteRenderer>().sortingOrder = 9;

        shadow.transform.SetParent(ball.transform);
        shadow.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        shadow.AddComponent<FootballShadow>();
        ball.transform.localScale = Vector3.one * 0.62f;

        CircleCollider2D collider = ball.AddComponent<CircleCollider2D>();
        collider.radius = 0.36f;
        Rigidbody2D body = ball.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearDamping = 2.2f;
        ball.AddComponent<FootballBall>();
    }

    private static void CreateReferee()
    {
        GameObject referee = new GameObject("Referee");
        referee.AddComponent<CircleCollider2D>();
        Rigidbody2D body = referee.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearDamping = 8f;
        referee.AddComponent<RefereeController2D>();
    }

    private static void CreateCircleOutline(string name, Vector3 position, float radius, Color outline, Color fill, float z)
    {
        CreateCircle(name + " Outer", position, radius, outline, z);
        CreateCircle(name, position, radius - 0.08f, fill, z - 0.01f);
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Color color, float z)
    {
        GameObject box = new GameObject(name);
        box.transform.position = position;
        box.transform.localScale = size;
        SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = color;
        renderer.sortingOrder = Mathf.RoundToInt(-z * 10f);
        box.transform.position = new Vector3(position.x, position.y, z);
        return box;
    }

    private static GameObject CreateCircle(string name, Vector3 position, float radius, Color color, float z)
    {
        GameObject circle = new GameObject(name);
        circle.transform.position = new Vector3(position.x, position.y, z);
        circle.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer renderer = circle.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = color;
        renderer.sortingOrder = Mathf.RoundToInt(-z * 10f);
        return circle;
    }

    private static Sprite squareSprite;
    private static Sprite circleSprite;

    private static Sprite CreateSquareSprite()
    {
        if (squareSprite != null)
            return squareSprite;

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        return squareSprite;
    }

    private static Sprite CreateCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                pixels[index] = Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
