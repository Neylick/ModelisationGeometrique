using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomQuad: MonoBehaviour
{
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(0, 0, 0), 
            new Vector3(1, 0, 0), 
            new Vector3(0, 1, 0), 
            new Vector3(1, 1, 0)
        };

        List<int> triangles = new List<int>
        {
            0, 2, 1,
            2, 3, 1,
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        //mesh.RecalculateNormals();
    }
}
