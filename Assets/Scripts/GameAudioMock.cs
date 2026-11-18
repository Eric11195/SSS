using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameAudioMock : MonoBehaviour, IGameAudio
{
    [Header("FMOD-ready mock state")]
    [SerializeField, Range(0f, 1f)] private float ballHeight;
    [SerializeField, Range(0f, 30f)] private float ballSpeed;
    [SerializeField, Range(-1f, 1f)] private float ballGoalProximity;
    [SerializeField, Range(0f, 1f)] private float crowdExcitement;
    [SerializeField, Range(0f, 1f)] private float matchProgress;

    private float rollTime;
    private bool rolling;

    private void LateUpdate()
    {
        if (!rolling)
            return;

        rollTime += Time.deltaTime;
    }

    public void SetBallParameters(Vector3 position, float height, float groundSpeed, float goalProximity)
    {
        ballHeight = Mathf.Clamp01(height);
        ballSpeed = Mathf.Clamp(groundSpeed, 0f, 30f);
        ballGoalProximity = Mathf.Clamp(goalProximity, -1f, 1f);
    }

    public void SetCrowdParameters(float excitement, float goalProximity, float progress)
    {
        crowdExcitement = Mathf.Clamp01(excitement);
        ballGoalProximity = Mathf.Clamp(goalProximity, -1f, 1f);
        matchProgress = Mathf.Clamp01(progress);
    }

    public void PlayPlayerStep(int team, float speed)
    {
        // TODO: fmod - replace this with a player-footsteps event driven by team, speed and turf.
    }

    public void PlayBallRoll(bool isRolling, float speed)
    {
        rolling = isRolling;
        ballSpeed = Mathf.Clamp(speed, 0f, 30f);
        // TODO: fmod - start/stop and set parameters on a grass-roll event.
    }

    public void PlayBallKick(int team, float speed)
    {
        // TODO: fmod - play kick event. Parameters: TEAM, BALL_SPEED, BALL_HEIGHT.
    }

    public void PlayBallImpact(string surface, float speed)
    {
        // TODO: fmod - play impact event with SURFACE and IMPACT_SPEED parameters.
    }

    public void PlayGoal(int team)
    {
        crowdExcitement = 1f;
        // TODO: fmod - trigger GOAL_CELEBRATION and raise crowd excitement bus.
    }

    public void PlayGoalNet(float speed)
    {
        // TODO: fmod - play net-hit event and parameterize by ball speed.
    }
}
