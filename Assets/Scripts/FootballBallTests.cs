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

    [Test]
    public void StandTension_ClampsAtRadiiAndUsesCurveBetweenThem()
    {
        UnityEngine.AnimationCurve curve = UnityEngine.AnimationCurve.Linear(0f, 0f, 1f, 1f);

        Assert.That(ParameterSetter.EvaluateTension(1f, 2f, 8f, curve), Is.EqualTo(0f));
        Assert.That(ParameterSetter.EvaluateTension(5f, 2f, 8f, curve), Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(ParameterSetter.EvaluateTension(9f, 2f, 8f, curve), Is.EqualTo(1f));
    }

    [Test]
    public void StandTension_RespectsConfiguredCurve()
    {
        UnityEngine.AnimationCurve curve = new UnityEngine.AnimationCurve(
            new UnityEngine.Keyframe(0f, 0f),
            new UnityEngine.Keyframe(0.5f, 0.25f),
            new UnityEngine.Keyframe(1f, 1f));

        Assert.That(ParameterSetter.EvaluateTension(5f, 2f, 8f, curve), Is.EqualTo(0.25f).Within(0.001f));
    }
}
#endif
