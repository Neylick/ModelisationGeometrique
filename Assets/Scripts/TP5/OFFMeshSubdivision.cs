using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class OFFMeshSubdivision : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] protected string ImportPath;
    [SerializeField] protected string ExportPath = "Assets/Meshes/off_export.off";
    protected Mesh mesh;

    [SerializeField] protected int subdivisionLevels = 3;
    [SerializeField] protected GameObject otherMeshObj;
    protected Mesh otherMesh;
    
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        otherMesh = otherMeshObj.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        otherMesh.Clear();

        if (!File.Exists(ImportPath))
        {
            Debug.LogError("Not a valid file path");
            return;
        }
        else
        {
            string[] lines = File.ReadAllLines(ImportPath);
            bool is_off = lines[0] == "OFF";
            if (!is_off)
            {
                Debug.LogError("Not a valid OFF file");
                return;
            }
            else
            {
                string[] header = lines[1].Split(' ');
                int vertex_count = int.Parse(header[0]);
                int face_count = int.Parse(header[1]);
                Vector3[] vertices = new Vector3[vertex_count];
                Vector3[] normals = new Vector3[vertex_count];
                Vector3 center = Vector3.zero;
                float max_coord = -1f;
                int[] triangles = new int[face_count * 3];

                for (int i = 0; i < vertex_count; i++)
                {
                    string[] vertex_line = lines[i + 2].Split(' ');
                    //Debug.Log("v: x(" + vertex_line[0] + ") y(" + vertex_line[1] + ") z(" + vertex_line[2] + ")");
                    float x = (float) double.Parse(vertex_line[0], CultureInfo.InvariantCulture); 
                    float y = (float) double.Parse(vertex_line[1], CultureInfo.InvariantCulture);
                    float z = (float) double.Parse(vertex_line[2], CultureInfo.InvariantCulture);
                    vertices[i] = new Vector3(x, y, z);
                    normals[i] = Vector3.zero;
                    center += vertices[i];
                    
                }

                center /= vertex_count;

                for (int i = 0; i < vertex_count; i++)
                {
                    vertices[i] -= center;
                    max_coord = Mathf.Max(max_coord, Mathf.Abs(vertices[i].x));
                    max_coord = Mathf.Max(max_coord, Mathf.Abs(vertices[i].y));
                    max_coord = Mathf.Max(max_coord, Mathf.Abs(vertices[i].z));
                }

                for (int i = 0; i < vertex_count; i++)
                {
                    vertices[i] /= max_coord;
                }

                for (int i = 0; i < face_count; i++)
                {
                    string[] face_line = lines[i + 2 + vertex_count].Split(' ');
                    //Debug.Log("f: A(" + face_line[0] + ") B(" + face_line[1] + ") C(" + face_line[2] + ")");
                    int v1 = int.Parse(face_line[1]);
                    int v2 = int.Parse(face_line[2]);
                    int v3 = int.Parse(face_line[3]);
                    triangles[i * 3] = v1;
                    triangles[i * 3 + 1] = v2;
                    triangles[i * 3 + 2] = v3;
                    normals[v1] += Vector3.Cross(vertices[v2] - vertices[v1], vertices[v3] - vertices[v1]).normalized;
                    normals[v2] += Vector3.Cross(vertices[v3] - vertices[v2], vertices[v1] - vertices[v2]).normalized;
                    normals[v3] += Vector3.Cross(vertices[v1] - vertices[v3], vertices[v2] - vertices[v3]).normalized;
                }
                

                for (int i = 0; i < vertex_count; i++)
                {
                    normals[i] = normals[i].normalized;
                    //Debug.DrawLine(transform.position + vertices[i], transform.position + vertices[i] + normals[i] * 0.02f, Color.yellow, 600.0f);
                    // Drawing normals for 10min 
                }


                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);

                //mesh.RecalculateNormals();

                mesh.SetNormals(normals);
            }
        }

        Subdivide();
    }

    //private void OnDrawGizmos()
    //{

    //}

    private bool AddEdge(int iA, int iB, Dictionary<int, List<int>> edges)
    {
        if (!edges.ContainsKey(iA)) edges[iA] = new List<int>();
        if (!edges.ContainsKey(iB)) edges[iB] = new List<int>();
        if (edges[iA].Contains(iB) || edges[iB].Contains(iA))
        {
            return false;
        }
        else
        {
            edges[iA].Add(iB);
            edges[iB].Add(iA);
            return true;
        }
    }

    private Vector2Int GetEdgeKey(int iA, int iB)
    {
        return new Vector2Int(Mathf.Min(iA, iB), Mathf.Max(iA, iB));
    }
    private float ComputeAlpha(int n)
    {
        if (n <= 3) return 3.0f / 16.0f;
        else
        {
            float theta = 2.0f * Mathf.PI / n;
            return (0.625f - Mathf.Pow(0.375f + 0.25f * Mathf.Cos(theta), 2)) / n;
        }
    }

    private void Subdivide()
    {
        List<Vector3> vertices = new(mesh.vertices);
        List<int> prevTriangles = new(mesh.triangles);
        List<int> triangles = new();

        for (int level = 0; level < subdivisionLevels; level++)
        {
            Dictionary<int, List<int>> edges = new Dictionary<int, List<int>>();
            Dictionary<Vector2Int, int> edgeMidpointIndices = new Dictionary<Vector2Int, int>();

            List<Vector3> normals = new();
            List<Color> otherColors = new();

            // Add Edges & Midpoints
            for (int i = 0; i < (mesh.triangles.Length) / 3; i++)
            {
                int i1 = prevTriangles[i * 3];
                int i2 = prevTriangles[i * 3 + 1];
                int i3 = prevTriangles[i * 3 + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                //Color c = new(Random.value, Random.value, Random.value);
                //otherColors.Add(c);
                //otherColors.Add(c);
                //otherColors.Add(c);

                if (AddEdge(i1, i2, edges))
                {
                    vertices.Add((v1 + v2) / 2.0f);
                    edgeMidpointIndices[GetEdgeKey(i1, i2)] = vertices.Count - 1;
                }
                if (AddEdge(i2, i3, edges))
                {
                    vertices.Add((v2 + v3) / 2.0f);
                    edgeMidpointIndices[GetEdgeKey(i2, i3)] = vertices.Count - 1;
                }
                if (AddEdge(i3, i1, edges))
                {
                    vertices.Add((v1 + v3) / 2.0f);
                    edgeMidpointIndices[GetEdgeKey(i1, i3)] = vertices.Count - 1;
                }

                int i12 = edgeMidpointIndices[GetEdgeKey(i1, i2)];
                int i23 = edgeMidpointIndices[GetEdgeKey(i2, i3)];
                int i31 = edgeMidpointIndices[GetEdgeKey(i1, i3)];

                // Create 4 new triangles
                {
                    triangles.Add(i1); // v1
                    triangles.Add(i12); // midpoint v1-v2
                    triangles.Add(i31); // midpoint v3-v1
                }
                {
                    triangles.Add(i2); // v2
                    triangles.Add(i23); // midpoint v2-v3
                    triangles.Add(i12); // midpoint v1-v2
                }
                {
                    triangles.Add(i3); // v3
                    triangles.Add(i31); // midpoint v3-v1
                    triangles.Add(i23); // midpoint v2-v3
                }
                {
                    triangles.Add(i12); // midpoint v1-v2
                    triangles.Add(i23); // midpoint v2-v3
                    triangles.Add(i31); // midpoint v3-v1
                }
            }

            // Recalculate vertices from edges
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                int n = edges[i].Count;
                float alpha = ComputeAlpha(n);
                Vector3 neighSum = Vector3.zero;
                foreach (int neigh in edges[i]) neighSum += vertices[neigh];
                vertices[i] = (1 - (n * alpha)) * vertices[i] + alpha * neighSum;
            }

            otherMesh.SetTriangles(triangles.ToArray(), 0);
            otherMesh.SetVertices(vertices);
            //otherMesh.SetColors(otherColors);

            prevTriangles = new(triangles);
            triangles.Clear();
        }

        //otherMesh.SetVertices(vertices);
        ////otherMesh.SetNormals(normals);
        //otherMesh.SetTriangles(prevTriangles.ToArray(), 0);
        otherMesh.RecalculateNormals();

        Debug.Log("Subvidided (["+ subdivisionLevels +"] iterations) mesh from " + mesh.vertices.Length + " vertices to " + otherMesh.vertices.Length);
    }

    public void ExportToOFF()
    {
        File.WriteAllText(ExportPath, "OFF\n");
        mesh = GetComponent<MeshFilter>().mesh;
        File.AppendAllText(ExportPath, mesh.vertices.Length + " " + mesh.triangles.Length / 3 + "\n");
        for(int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 v = mesh.vertices[i];
            File.AppendAllText(ExportPath, v.x.ToString(CultureInfo.InvariantCulture) + " " + v.y.ToString(CultureInfo.InvariantCulture) + " " + v.z.ToString(CultureInfo.InvariantCulture) + "\n");
        }

        for(int i = 0; i < mesh.triangles.Length / 3; i++)
        {
            int v1 = mesh.triangles[i * 3];
            int v2 = mesh.triangles[i * 3 + 1];
            int v3 = mesh.triangles[i * 3 + 2];
            File.AppendAllText(ExportPath, "3 " + v1 + " " + v2 + " " + v3 + "\n");
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i] * 0.05f, Color.yellow);
    }
}
