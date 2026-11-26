using FMODUnity;
using UnityEngine;

[System.Serializable]
public struct StadiumTensionArea
{
    [SerializeField] private Transform center;
    [SerializeField, Min(0f)] private float innerRadius;
    [SerializeField, Min(0f)] private float outerRadius;
    [SerializeField] private AnimationCurve curve;

    public Transform Center => center;
    public float InnerRadius => Mathf.Max(0f, innerRadius);
    public float OuterRadius => Mathf.Max(InnerRadius + 0.01f, outerRadius);

    public StadiumTensionArea(Transform center, float innerRadius, float outerRadius, AnimationCurve curve)
    {
        this.center = center;
        this.innerRadius = Mathf.Max(0f, innerRadius);
        this.outerRadius = Mathf.Max(this.innerRadius + 0.01f, outerRadius);
        this.curve = curve ?? AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }

    public float Evaluate(Vector3 position, Transform fallbackCenter)
    {
        Transform areaCenter = center != null ? center : fallbackCenter;
        if (areaCenter == null)
            return 0f;

        float distance = Vector2.Distance(
            new Vector2(position.x, position.y),
            new Vector2(areaCenter.position.x, areaCenter.position.y));
        return ParameterSetter.EvaluateTension(distance, InnerRadius, OuterRadius, curve);
    }
}

public sealed class ParameterSetter : MonoBehaviour
{
    private const string BallHeightParameter = "BallHeight";
    private const string TensionParameter = "tension";
    private const string AmbienceEventPath = "event:/Ambience";

    private FMOD.Studio.EventInstance ambienceEvent;
    [SerializeField] private StadiumTensionArea[] tensionAreas = System.Array.Empty<StadiumTensionArea>();
    private Vector3 ballPosition;
    private float ballHeight;
    private float tension;
    private bool hasAmbienceEvent;
    private bool hasLoggedHeightError;
    private bool hasLoggedTensionError;

    public float Tension => tension;

    public void ConfigureTensionAreas(StadiumTensionArea[] areas)
    {
        tensionAreas = areas != null
            ? (StadiumTensionArea[])areas.Clone()
            : System.Array.Empty<StadiumTensionArea>();
    }

    private void Start()
    {
        ambienceEvent = RuntimeManager.CreateInstance(AmbienceEventPath);
        if (!ambienceEvent.isValid())
        {
            Debug.LogError($"Failed to create FMOD event: {AmbienceEventPath}", this);
            return;
        }

        GameObject eventAnchor = gameObject;
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
        float totalTension = 0f;
        foreach (StadiumTensionArea area in tensionAreas)
            totalTension += area.Evaluate(position, transform);

        return Mathf.Clamp01(totalTension);
    }

    public static float EvaluateTension(float distance, float inner, float outer, AnimationCurve curve)
    {
        float normalizedDistance = Mathf.InverseLerp(inner, Mathf.Max(inner + 0.01f, outer), distance);
        float curveValue = curve != null ? curve.Evaluate(normalizedDistance) : 1f - normalizedDistance;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        foreach (StadiumTensionArea area in tensionAreas)
        {
            Transform center = area.Center != null ? area.Center : transform;
            Gizmos.DrawWireSphere(center.position, area.InnerRadius);
            Gizmos.DrawWireSphere(center.position, area.OuterRadius);
        }
    }
}
