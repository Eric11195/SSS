// using FMODUnity;
using UnityEngine;

// [RequireComponent(typeof(StudioEventEmitter))]
public class SmoothFMODEmitter : MonoBehaviour
{
    public Transform target;
    [Min(0.0f)] public float smoothTime = 0.15f; // Seconds to reach target
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Smoothly damp position to prevent sudden spatializer jumps
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            target.position, 
            ref velocity, 
            smoothTime
        );
    }
}
