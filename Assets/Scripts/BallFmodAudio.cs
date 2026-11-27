using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallFmodAudio : MonoBehaviour
{
    private const string BallBounceEventPath = "event:/Ball/BallFall";
    private const string BallRustlingEventPath = "event:/Ball/BallRustling";
    private const string BallFlyEventPath = "event:/Ball/BallFly";

    [SerializeField] private Transform listener;
    [SerializeField, Min(0f)] private float minYSpeedToFly = 0.5f;
    [SerializeField] private float minYSpeedToRustle = 0.15f;
    [SerializeField] private float minHeightToRustle = 0.05f;
    [SerializeField] private float minXSpeedToRustle = 0.15f;
    [SerializeField, Range(0f, 1f)] private float minBounceVolume = 0.6f;
    [SerializeField, Min(0.01f)] private float fullBounceVolumeImpactSpeed = 10f;
    [SerializeField, Min(0.01f)] private float fullRustlingVolumeSpeed = 7f;

    private Rigidbody2D body;
    private FMOD.Studio.EventInstance ballRustlingEvent;
    private FMOD.Studio.EventInstance ballFlyEvent;
    private bool hasBallRustlingEvent;
    private bool hasBallFlyEvent;
    private bool isBallFlying;
    private bool isBallRustlingPaused = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void SetListenerHeight(float height)
    {
        if (listener == null)
            return;

        listener.position = new Vector3(listener.position.x, listener.position.y, height * 8f);
    }

    public void UpdateAirTravel(float verticalVelocity)
    {
        bool shouldPlay = Mathf.Abs(verticalVelocity) > minYSpeedToFly;
        if (shouldPlay == isBallFlying)
            return;

        isBallFlying = shouldPlay;
        if (shouldPlay)
        {
            CreateBallFlyEvent();
            return;
        }

        StopBallFlyEvent();
    }

    public void UpdateRustling(float height, float verticalVelocity, float horizontalSpeed)
    {
        bool isOnFloor = height <= minHeightToRustle && verticalVelocity < minYSpeedToRustle;
        float volume = isOnFloor
            ? Mathf.Clamp01((horizontalSpeed - minXSpeedToRustle) / Mathf.Max(0.01f, fullRustlingVolumeSpeed))
            : 0f;

        if (volume <= 0f)
        {
            PauseRustling();
            return;
        }

        if (!hasBallRustlingEvent)
        {
            CreateRustlingEvent(volume);
            return;
        }

        FMOD.RESULT result = ballRustlingEvent.setVolume(volume);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set FMOD ball-rustling volume: {result}", this);
            return;
        }

        if (isBallRustlingPaused)
        {
            result = ballRustlingEvent.setPaused(false);
            if (result != FMOD.RESULT.OK)
                Debug.LogError($"Failed to resume FMOD ball-rustling event: {result}", this);
            else
                isBallRustlingPaused = false;
        }
    }

    public void PlayBounce(float impactSpeed, Vector2? direction = null)
    {
        FMOD.Studio.EventInstance bounceEvent = RuntimeManager.CreateInstance(BallBounceEventPath);
        if (!bounceEvent.isValid())
        {
            Debug.LogError($"Failed to create FMOD event: {BallBounceEventPath}", this);
            return;
        }

        float impactVolume = Mathf.Clamp01(impactSpeed / Mathf.Max(0.01f, fullBounceVolumeImpactSpeed));
        float volume = Mathf.Lerp(Mathf.Clamp01(minBounceVolume), 1f, impactVolume);
        FMOD.RESULT result = bounceEvent.setVolume(volume);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set FMOD ball-bounce volume: {result}", this);
            bounceEvent.release();
            return;
        }

        if (direction.HasValue && direction.Value.sqrMagnitude > 0f)
        {
            Vector3 forward = new Vector3(direction.Value.x, direction.Value.y, 0f).normalized;
            Transform eventTransform = listener != null ? listener : transform;
            FMOD.ATTRIBUTES_3D attributes = RuntimeUtils.To3DAttributes(eventTransform, body);
            attributes.forward = forward.ToFMODVector();
            attributes.up = Vector3.Cross(Vector3.forward, forward).ToFMODVector();
            result = bounceEvent.set3DAttributes(attributes);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to set FMOD ball-bounce position and direction: {result}", this);
                bounceEvent.release();
                return;
            }
        }
        else
        {
            RuntimeManager.AttachInstanceToGameObject(bounceEvent, gameObject, body);
        }

        result = bounceEvent.start();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to start FMOD ball-bounce event: {result}", this);

        result = bounceEvent.release();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to release FMOD ball-bounce event: {result}", this);
    }

    private void CreateRustlingEvent(float volume)
    {
        FMOD.Studio.EventInstance newEvent = RuntimeManager.CreateInstance(BallRustlingEventPath);
        if (!newEvent.isValid())
        {
            Debug.LogError($"Failed to create FMOD event: {BallRustlingEventPath}", this);
            return;
        }

        RuntimeManager.AttachInstanceToGameObject(newEvent, gameObject, body);
        FMOD.RESULT result = newEvent.setVolume(volume);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set FMOD ball-rustling volume: {result}", this);
            newEvent.release();
            return;
        }

        result = newEvent.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to start FMOD ball-rustling event: {result}", this);
            newEvent.release();
            return;
        }

        ballRustlingEvent = newEvent;
        hasBallRustlingEvent = true;
        isBallRustlingPaused = false;
    }

    private void CreateBallFlyEvent()
    {
        FMOD.Studio.EventInstance newEvent = RuntimeManager.CreateInstance(BallFlyEventPath);
        if (!newEvent.isValid())
        {
            Debug.LogError($"Failed to create FMOD event: {BallFlyEventPath}", this);
            return;
        }

        RuntimeManager.AttachInstanceToGameObject(newEvent, gameObject, body);
        FMOD.RESULT result = newEvent.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to start FMOD ball-fly event: {result}", this);
            newEvent.release();
            return;
        }

        ballFlyEvent = newEvent;
        hasBallFlyEvent = true;
    }

    private void StopBallFlyEvent()
    {
        if (!hasBallFlyEvent)
            return;

        FMOD.RESULT result = ballFlyEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to stop FMOD ball-fly event: {result}", this);

        result = ballFlyEvent.release();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to release FMOD ball-fly event: {result}", this);

        ballFlyEvent = default;
        hasBallFlyEvent = false;
    }

    private void PauseRustling()
    {
        if (!hasBallRustlingEvent || isBallRustlingPaused)
            return;

        FMOD.RESULT result = ballRustlingEvent.setVolume(0f);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to set FMOD ball-rustling volume: {result}", this);

        result = ballRustlingEvent.setPaused(true);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to pause FMOD ball-rustling event: {result}", this);
        else
            isBallRustlingPaused = true;
    }

    private void OnDestroy()
    {
        StopBallFlyEvent();

        if (!hasBallRustlingEvent)
            return;

        FMOD.RESULT result = ballRustlingEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to stop FMOD ball-rustling event: {result}", this);

        result = ballRustlingEvent.release();
        if (result != FMOD.RESULT.OK)
            Debug.LogError($"Failed to release FMOD ball-rustling event: {result}", this);
    }
}
