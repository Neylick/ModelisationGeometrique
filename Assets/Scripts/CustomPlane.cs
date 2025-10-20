using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomPlane: MonoBehaviour
{
    [SerializeField] int line_count = 10;
    [SerializeField] int column_count = 10;

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();


        for (int j = 0; j < line_count; j++)
        {
            for(int i = 0; i < column_count; i++)
            {
                vertices.Add(new(i, j, 0));
            }
        }

        for (int j = 0; j < line_count-1; j++)
        {
            for (int i = 0; i < column_count-1; i++)
            {
                triangles.Add(j*column_count + i);
                triangles.Add((j+1)*column_count + i);
                triangles.Add(j*column_count + (i+1));

                triangles.Add((j+1)*column_count + i);
                triangles.Add((j+1)*column_count + (i + 1));
                triangles.Add(j*column_count + (i + 1));
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        //mesh.RecalculateNormals();
    }
}
