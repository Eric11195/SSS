using FMODUnity;
using UnityEngine;

public sealed class ParameterSetter : MonoBehaviour
{
    private const string BallHeightParameter = "BallHeight";
    private const string TensionParameter = "tension";
    private const string AmbienceEventPath = "event:/Ambience";

    [SerializeField] private Transform tensionCenter;
    [SerializeField, Min(0f)] private float innerRadius = 2f;
    [SerializeField, Min(0.01f)] private float outerRadius = 8f;
    [SerializeField] private AnimationCurve tensionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private FMOD.Studio.EventInstance ambienceEvent;
    private Vector3 ballPosition;
    private float ballHeight;
    private float tension;
    private bool hasAmbienceEvent;
    private bool hasLoggedHeightError;
    private bool hasLoggedTensionError;

    public float Tension => tension;

    public void ConfigureTensionArea(Transform center, float inner, float outer, AnimationCurve curve)
    {
        tensionCenter = center;
        innerRadius = Mathf.Max(0f, inner);
        outerRadius = Mathf.Max(innerRadius + 0.01f, outer);
        tensionCurve = curve;
    }

    private void Start()
    {
        ambienceEvent = RuntimeManager.CreateInstance(AmbienceEventPath);
        if (!ambienceEvent.isValid())
        {
            Debug.LogError($"Failed to create FMOD event: {AmbienceEventPath}", this);
            return;
        }

        GameObject eventAnchor = tensionCenter != null ? tensionCenter.gameObject : gameObject;
        RuntimeManager.AttachInstanceToGameObject(ambienceEvent, eventAnchor);
        FMOD.RESULT result = ambienceEvent.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to start FMOD ambience event: {result}", this);
            ambienceEvent.release();
            return;
        }

        hasAmbienceEvent = true;
    }

    public void SetBallState(Vector3 position, float height)
    {
        ballPosition = position;
        ballHeight = Mathf.Max(0f, height);
        tension = CalculateTension(position);
        UpdateParameters();
    }

    public float CalculateTension(Vector3 position)
    {
        Vector3 center = tensionCenter != null ? tensionCenter.position : transform.position;
        float distance = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(center.x, center.y));
        return EvaluateTension(distance, innerRadius, outerRadius, tensionCurve);
    }

    public static float EvaluateTension(float distance, float inner, float outer, AnimationCurve curve)
    {
        float normalizedDistance = Mathf.InverseLerp(inner, Mathf.Max(inner + 0.01f, outer), distance);
        float curveValue = curve != null ? curve.Evaluate(normalizedDistance) : normalizedDistance;
        return Mathf.Clamp01(curveValue);
    }

    private void LateUpdate()
    {
        tension = CalculateTension(ballPosition);
        UpdateParameters();
    }

    private void UpdateParameters()
    {
        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(BallHeightParameter, ballHeight);
        if (result == FMOD.RESULT.OK)
        {
            hasLoggedHeightError = false;
        }
        else if (!hasLoggedHeightError)
        {
            Debug.LogError($"Failed to set FMOD global parameter '{BallHeightParameter}': {result}", this);
            hasLoggedHeightError = true;
        }

        if (!hasAmbienceEvent)
            return;

        result = ambienceEvent.setParameterByName(TensionParameter, tension);
        if (result == FMOD.RESULT.OK)
        {
            hasLoggedTensionError = false;
        }
        else if (!hasLoggedTensionError)
        {
            Debug.LogError($"Failed to set FMOD ambience parameter '{TensionParameter}': {result}", this);
            hasLoggedTensionError = true;
        }
    }

    private void OnDestroy()
    {
        if (!hasAmbienceEvent)
            return;

        FMOD.RESULT result = ambienceEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to stop FMOD ambience event: {result}", this);

        result = ambienceEvent.release();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to release FMOD ambience event: {result}", this);
    }
}
