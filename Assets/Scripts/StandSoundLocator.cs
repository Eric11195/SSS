using System;
using UnityEngine;

public class StandSoundLocator : MonoBehaviour
{
    [SerializeField] Transform destination0Emisor0 = null;
    [SerializeField] Transform destination0Emisor1 = null;
    [SerializeField] Transform destination1Emisor0 = null;
    [SerializeField] Transform destination1Emisor1 = null;

    [SerializeField] Transform source0 = null;
    [SerializeField] Transform source1 = null;

    [SerializeField] Transform vertex = null;
    [SerializeField] Transform emisor0 = null;
    [SerializeField] Transform emisor1 = null;
    [SerializeField] AnimationCurve projectionRemap = null;

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

    static Vector3 PositionPreserveArea(Vector3 destination0, Vector3 destination1, double t, double area, Vector3 vertex, out double b, out Vector3 d)
    {
        d = destination1 - destination0;
        double distance = (
            (vertex - destination0)
            - (d *  (float)Project01(destination0, destination1, vertex))
        ).magnitude;
        // a = b * h / 2
        // 2 * a / h = b
        b = 2.0 * area / distance;

        return Vector3.LerpUnclamped(destination0, destination1, Mathf.Clamp((float)t, -1.0f, 1.0f));
    }

    void Update()
    {
        double t = Project01(source0.position, source1.position, vertex.position);
        
        Vector3 d;
        double b;
        if (t >= 0)
        {
            double s = projectionRemap.Evaluate((float)t);
            Vector3 position0 = PositionPreserveArea(
                destination0Emisor0.position, destination1Emisor0.position, s, area, vertex.position, out b, out d
            );

            emisor0.position = position0;
            emisor1.position = emisor0.position - (float)b * d.normalized;
        }
        else
        {
            double s = projectionRemap.Evaluate((float)(-t));
            Vector3 position1 = PositionPreserveArea(
                destination0Emisor1.position, destination1Emisor1.position, s, area, vertex.position, out b, out d
            );
            emisor1.position = position1;
            emisor0.position = emisor1.position - (float)b * d.normalized;
        }
    }
}
