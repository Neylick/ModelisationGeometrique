using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Internal;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomCone: MonoBehaviour
{
    [SerializeField] int height = 3;
    [SerializeField] float cut_height = 1.5f;
    [SerializeField] int radius = 3;
    [SerializeField] int meridians = 32;
    protected Mesh mesh;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float cut_rad = 1 - (cut_height / (float) height) * radius;

        for(int i = 0; i < meridians; i++)
        {
            float c1 = (Mathf.Cos(i * Mathf.PI * 2.0f / meridians));
            float s1 = (Mathf.Sin(i * Mathf.PI * 2.0f / meridians));

            vertices.Add(new(radius * c1, -cut_height/2.0f, radius * s1));
            if(cut_height < height) vertices.Add(new(cut_rad * c1, cut_height/2.0f, cut_rad * s1));
        }


        // Tie to center
        if (cut_height >= height)
        {
            // Top ties
            int point_idx = vertices.Count;
            vertices.Add(new(0, cut_height / 2.0f, 0));
            for (int i = 0; i < meridians; i++)
            {
                int m = i % meridians;
                int mp = (i+1)% meridians;

                triangles.Add(point_idx);
                triangles.Add(m);
                triangles.Add(mp);
            }

            // Base disk
            int center_idx = vertices.Count;
            vertices.Add(new(0, -cut_height / 2.0f, 0));
            for (int i = 0; i < meridians; i++)
            {
                int m = i % meridians;
                int mp = (i + 1) % meridians;

                triangles.Add(center_idx);
                triangles.Add(m);
                triangles.Add(mp);
            }
        }
        else // Tie to cut disk
        {
            int center_indexes = vertices.Count;
            vertices.Add(new(0, -cut_height / 2.0f, 0));
            vertices.Add(new(0,  cut_height / 2.0f, 0));

            for (int i = 0; i < meridians; i++)
            {
                int s = (2 * i) % (2 * meridians);
                int sp = (2 * (i + 1)) % (2 * meridians);

                int sb = (2 * i + 1) % (2 * meridians);
                int spb = (2 * (i + 1) + 1) % (2 * meridians);

                // Quad Tri 1
                triangles.Add(s);
                triangles.Add(sb);
                triangles.Add(sp);

                // Quad Tri 2
                triangles.Add(sp);
                triangles.Add(sb);
                triangles.Add(spb);

                // Disc Top
                triangles.Add(center_indexes);
                triangles.Add(s);
                triangles.Add(sp);

                // Disc bot
                triangles.Add((center_indexes + 1));
                triangles.Add(spb);
                triangles.Add(sb);
            }

        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        //mesh.RecalculateNormals();
    }
}
