using FMOD;
using FMODUnity;
using UnityEngine;
using static UnityEditor.Profiling.RawFrameDataView;

public class ParameterSetter : MonoBehaviour
{
    [SerializeField]
    private EventReference ballHeightEvent;

    private FMOD.Studio.EventInstance instance;

    private static float ballHeight = 0f;
    void Start()
    {
        instance = FMODUnity.RuntimeManager.CreateInstance(ballHeightEvent);
        instance.start();
    }
    void Update()
    {
        instance.setParameterByName("BallHeight", ballHeight);
    }
}
