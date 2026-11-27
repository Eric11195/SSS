using UnityEngine;

public class FollowOnH_ConserveY : MonoBehaviour
{
    [SerializeField]
    float soundStartsAtHeight;
    [SerializeField]
    float soundEndsAtHeight;
    [SerializeField]
    float soundDecayDistanceDeltaFromCenter;
    float fixedHeight;
    [SerializeField]
    Transform ballTr;
    Transform myTr;
    void Start()
    {
        myTr = transform;
        FMODUnity.StudioEventEmitter eventEmitter = GetComponent<FMODUnity.StudioEventEmitter>();
        eventEmitter.OverrideAttenuation = true;
        eventEmitter.OverrideMinDistance = soundDecayDistanceDeltaFromCenter;
        eventEmitter.OverrideMaxDistance = soundEndsAtHeight - soundStartsAtHeight;
        fixedHeight = (soundEndsAtHeight + soundStartsAtHeight) / 2.0f;
    }
    // Update is called once per frame
    void Update()
    {
        myTr.position = new Vector3(ballTr.position.x, ballTr.position.y, fixedHeight);
    }
}
