using UnityEngine;

public interface IGameAudio
{
    void SetBallParameters(Vector3 position, float height, float groundSpeed, float goalProximity);
    void SetCrowdParameters(float excitement, float goalProximity, float matchProgress);
    void PlayPlayerStep(int team, float speed);
    void PlayBallRoll(bool isRolling, float speed);
    void PlayBallKick(int team, float speed);
    void PlayBallImpact(string surface, float speed);
    void PlayGoal(int team);
    void PlayGoalNet(float speed);
}
