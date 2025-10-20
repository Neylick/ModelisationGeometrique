using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomCylinder: MonoBehaviour
{
    [SerializeField] int segment_count = 16;
    [SerializeField] int height = 5;
    [SerializeField] int radius = 3;

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();


        for(int i = 0; i < segment_count; i++)
        {
            float x = (Mathf.Cos(i * Mathf.PI * 2.0f / segment_count) * radius);
            float z = (Mathf.Sin(i * Mathf.PI * 2.0f / segment_count) * radius);
            vertices.Add(new(x, -height / 2.0f, z));
            vertices.Add(new(x,  height / 2.0f, z));
        }

        int center_indexes = vertices.Count;

        vertices.Add(new(0, -height / 2.0f, 0));
        vertices.Add(new(0,  height / 2.0f, 0));


        for (int i = 0; i < segment_count; i++)
        {
            int s = (2*i) % (2*segment_count);
            int sp = (2*(i+1)) % (2*segment_count);

            int sb = (2*i+1) % (2 * segment_count);
            int spb = (2*(i+1)+1) % (2 * segment_count);

            // Disc Top
            triangles.Add(center_indexes);
            triangles.Add(s);
            triangles.Add(sp);

            // Disc bot
            triangles.Add((center_indexes+1));
            triangles.Add(spb);
            triangles.Add(sb);

            // Quad Tri 1
            triangles.Add(s);
            triangles.Add(sb);
            triangles.Add(sp);

            // Quad Tri 2
            triangles.Add(sp);
            triangles.Add(sb);
            triangles.Add(spb);
            
        }

        //Closing discs
        //triangles.Add(center_indexes);
        //triangles.Add((segment_count - 1) * 2);
        //triangles.Add(0);

        //triangles.Add(center_indexes + 1);
        //triangles.Add(1);
        //triangles.Add((segment_count - 1) * 2 + 1);

        ////Closing quads
        //// Quad Tri 1
        //triangles.Add(2 * (segment_count-1));
        //triangles.Add(2 * (segment_count-1) + 1);
        //triangles.Add(0);

        //// Quad Tri 2
        //triangles.Add(2 * (segment_count-1) + 1);
        //triangles.Add(1);
        //triangles.Add(0);


        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        //mesh.RecalculateNormals();
    }
}
