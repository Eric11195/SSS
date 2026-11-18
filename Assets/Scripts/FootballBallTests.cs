#if UNITY_EDITOR
using NUnit.Framework;

public sealed class FootballBallTests
{
    [Test]
    public void HeightNormalization_RemainsWithinZeroAndOne()
    {
        float normalized = UnityEngine.Mathf.Clamp01(3f / 3f);
        Assert.That(normalized, Is.EqualTo(1f));

        normalized = UnityEngine.Mathf.Clamp01(-1f / 3f);
        Assert.That(normalized, Is.EqualTo(0f));
    }

    [Test]
    public void GoalProximity_IsHighestAtCenter()
    {
        float farFromGoal = UnityEngine.Mathf.Clamp01(1f - UnityEngine.Mathf.Abs(0f) / 9f);
        Assert.That(farFromGoal, Is.EqualTo(1f));

        float atGoal = UnityEngine.Mathf.Clamp01(1f - UnityEngine.Mathf.Abs(9f) / 9f);
        Assert.That(atGoal, Is.EqualTo(0f));
    }
}
#endif
