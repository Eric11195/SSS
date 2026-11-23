using System;
using UnityEngine;

public class StandSoundLocator : MonoBehaviour
{
    [SerializeField] Transform destination0 = null;
    [SerializeField] Transform destination1 = null;

    [SerializeField] Transform source0 = null;
    [SerializeField] Transform source1 = null;

    [SerializeField] Transform vertex = null;
    [SerializeField] Transform emisor0 = null;
    [SerializeField] Transform emisor1 = null;

    double area = 0.0;

    static double Area(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        double a = (v1 - v0).magnitude;
        double b = (v2 - v1).magnitude;
        double c = (v0 - v2).magnitude;
        double s = (a + b + c) * 0.5;
        return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
    }

    static double Project01(Vector3 v0, Vector3 v1, Vector3 point)
    {
        Vector3 d = v1 - v0;
        return Vector3.Dot(d, point - v0) / Vector3.Dot(d, d);
    }

    void Start()
    {
        area = Area(vertex.position, emisor0.position, emisor1.position);
    }

    void Update()
    {
        double t = Project01(source0.position, source1.position, vertex.position);
        
        Vector3 d = destination1.position - destination0.position;
        double distance = (vertex.position - (d *  (float)Project01(destination0.position, destination1.position, vertex.position))).magnitude;
        // a = b * h / 2
        // 2 * a / h = b
        double b = 2.0 * area / distance;
        if (t >= 0)
        {
            emisor0.position = Vector3.Lerp(destination0.position, destination1.position, (float)t);
            emisor1.position = emisor0.position + (float)b * d.normalized;
        }
        else
        {
            emisor1.position = Vector3.LerpUnclamped(destination0.position, destination1.position, (float)t);
            emisor0.position = emisor1.position - (float)b * d.normalized;
        }
    }
}
