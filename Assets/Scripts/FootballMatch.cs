using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FootballMatch : MonoBehaviour
{
    [SerializeField] private GameObject refereeGO;
    [SerializeField] private float goalResetDelay = 2f;
    [SerializeField] private int goalsToWin = 3;
    private string whistleEventPath = "event:/StartRoundSFX";

    private int leftScore;
    private int rightScore;
    private FootballBall ball;
    private float resetTimer = -1f;
    private bool matchOver;
    private IGameAudio audioSystem;

    private void Start()
    {
        ball = FindAnyObjectByType<FootballBall>();
        audioSystem = FindAnyObjectByType<GameAudioMock>();
        ResetPositions();
    }

    private void Update()
    {
        if (resetTimer >= 0f)
        {
            resetTimer -= Time.deltaTime;
            if (resetTimer <= 0f)
            {
                ResetPositions();
                resetTimer = -1f;
            }
        }

        float progress = Mathf.Clamp01(Mathf.Max(leftScore, rightScore) / (float)goalsToWin);
        float goalProximity = ball != null
            ? Mathf.Clamp01(1f - Mathf.Abs(ball.transform.position.x) / 9f)
            : 0f;
        audioSystem?.SetCrowdParameters(0.25f + progress * 0.75f, goalProximity, progress);

        if (matchOver && Keyboard.current.rKey.wasPressedThisFrame)
            RestartMatch();
    }

    public void ScoreGoal(PlayerTeam concedingTeam)
    {
        if (resetTimer >= 0f || matchOver)
            return;

        int scoringTeam = concedingTeam == PlayerTeam.Left ? 1 : 0;
        if (scoringTeam == 0)
            leftScore++;
        else
            rightScore++;

        audioSystem?.PlayGoal(scoringTeam);
        if (ball != null)
            audioSystem?.PlayGoalNet(ball.GroundSpeed);

        if (leftScore >= goalsToWin || rightScore >= goalsToWin)
            matchOver = true;

        resetTimer = goalResetDelay;
    }

    private void ResetPositions()
    {
        ball?.ResetBall(Vector3.zero);
        foreach (PlayerController2D player in FindObjectsByType<PlayerController2D>())
        {
            player.transform.position = player.team == PlayerTeam.Left
                ? new Vector3(-4.5f, 0f, 0f)
                : new Vector3(4.5f, 0f, 0f);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        FMOD.Studio.EventInstance newEvent = RuntimeManager.CreateInstance(whistleEventPath);
        if (!newEvent.isValid())
        {
            Debug.LogError($"Failed to create FMOD event: {whistleEventPath}", this);
            return;
        }

        RuntimeManager.AttachInstanceToGameObject(newEvent, refereeGO);
        FMOD.RESULT result = newEvent.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to start FMOD ball-rustling event: {result}", this);
            newEvent.release();
            return;
        }
    }

    private void RestartMatch()
    {
        leftScore = 0;
        rightScore = 0;
        matchOver = false;
        ResetPositions();
    }

    private void OnGUI()
    {
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 34,
            fontStyle = FontStyle.Bold
        };
        GUIStyle normal = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16
        };

        float width = Mathf.Min(500f, Screen.width * 0.5f);
        Rect scoreRect = new Rect((Screen.width - width) * 0.5f, 10f, width, 50f);
        GUI.Label(scoreRect, leftScore + "  :  " + rightScore, title);

        Rect controlsRect = new Rect(10f, Screen.height - 72f, 280f, 60f);
        GUI.Label(controlsRect,
            "Left: WASD + E\nRight: Arrows + Right Shift",
            normal);

        if (matchOver)
        {
            Rect endRect = new Rect((Screen.width - 300f) * 0.5f, Screen.height * 0.4f, 300f, 160f);
            GUI.Label(endRect,
                (leftScore >= goalsToWin ? "Pastel Blue" : "Coral Red") + " wins!\nPress R to restart",
                title);
        }
    }
}
