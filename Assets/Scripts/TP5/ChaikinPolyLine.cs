using UnityEngine;

public class ChaikinPolyLine : MonoBehaviour
{
    [SerializeField] float u = 1.0f/ 4.0f;
    [SerializeField] float v = 1.0f/ 4.0f;

    [SerializeField] int iterations = 4;

    [SerializeField] Transform[] points;

    [SerializeField] float pointSize = 0.01f;

    private void OnDrawGizmos()
    {
        for(int i = 0; i < points.Length; i++)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(points[i].position, points[(i + 1) % points.Length].position);
            Gizmos.DrawSphere(points[i].position, pointSize);
        }

        Vector3[] workingPoints = new Vector3[points.Length];
        Vector3[] newPoints = new Vector3[points.Length * 2];
        for(int i = 0; i < points.Length; i++)
        {
            workingPoints[i] = points[i].position;
        }

        for(int it = 0; it < iterations; it++)
        {
            newPoints = new Vector3[workingPoints.Length * 2];
            for(int i = 0; i < workingPoints.Length; i++)
            {
                Vector3 pA = workingPoints[i];
                Vector3 pB = workingPoints[(i + 1) % workingPoints.Length];
                newPoints[i * 2] = (1 - u) * pA + u * pB;
                newPoints[i * 2 + 1] = v * pA + (1 - v) * pB;
            }

            workingPoints = newPoints;
        }

        Gizmos.color = Color.cyan;
        for(int i = 0; i < newPoints.Length; i++)
        {
            Gizmos.DrawLine(newPoints[i], newPoints[(i + 1) % newPoints.Length]);
            Gizmos.DrawSphere(newPoints[i], pointSize / 2);
        }
    }
}
