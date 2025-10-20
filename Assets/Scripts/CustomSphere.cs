using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomSphere: MonoBehaviour
{
    [SerializeField] int meridians = 16;
    [SerializeField] int parallels = 16;
    [SerializeField] int radius = 3;
    protected Mesh mesh;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();


        for(int j = 0; j < parallels; j++)
        {
            float c2 = (Mathf.Cos((j+1) * Mathf.PI / (parallels+1)));
            float s2 = (Mathf.Sin((j+1) * Mathf.PI / (parallels+1)));

            for(int i = 0; i < meridians; i++)
            {
                float c1 = (Mathf.Cos(i * Mathf.PI * 2.0f / meridians));
                float s1 = (Mathf.Sin(i * Mathf.PI * 2.0f / meridians));

                vertices.Add(new(radius * c1 * s2, radius * c2, radius * s1 * s2));
            }
        }

        for (int i = 0; i < meridians; i++)
        {
            for (int j = 0; j < parallels - 1; j++)
            {
                int m = i % meridians;
                int p = j % parallels;

                int mp = (i+1) % meridians;
                int pp = (j+1) % parallels;

                triangles.Add(m + p * meridians);
                triangles.Add(mp + p * meridians);
                triangles.Add(m + pp * meridians);

                triangles.Add(m + pp * meridians);
                triangles.Add(mp + p * meridians);
                triangles.Add(mp + pp * meridians);
            }
        }

        int center_index = vertices.Count;

        vertices.Add(new(0, radius, 0));
        vertices.Add(new(0, -radius, 0));


        for(int i = 0; i < meridians; i++)
        {
            int m = i % meridians;
            int mp = (i + 1) % meridians;

            triangles.Add(center_index);
            triangles.Add(mp);
            triangles.Add(m);

            triangles.Add(center_index + 1);
            triangles.Add(meridians * parallels - mp - 1);
            triangles.Add(meridians * parallels - m - 1);
        }


        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        //mesh.RecalculateNormals();
    }
}
