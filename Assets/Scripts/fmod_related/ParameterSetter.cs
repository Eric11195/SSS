using FMODUnity;
using UnityEngine;

public sealed class ParameterSetter : MonoBehaviour
{
    private const string BallHeightParameter = "BallHeight";
    private static float ballHeight;
    private bool hasLoggedParameterError;

    public static void SetBallHeight(float value)
    {
        ballHeight = value;
    }

    private void Update()
    {
        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(BallHeightParameter, ballHeight);
        if (result == FMOD.RESULT.OK)
        {
            hasLoggedParameterError = false;
            return;
        }

        if (!hasLoggedParameterError)
            Debug.LogError($"Failed to set FMOD global parameter '{BallHeightParameter}': {result}", this);

        hasLoggedParameterError = true;
    }
}
