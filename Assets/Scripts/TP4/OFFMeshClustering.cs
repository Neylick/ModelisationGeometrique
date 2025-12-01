using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class OFFMeshCluster: MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] protected string ImportPath;
    [SerializeField] protected string ExportPath = "Assets/Meshes/off_export.off";
    [SerializeField] protected float epsilon = .1f;
    protected Mesh mesh;
    protected Bounds bounds;

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

                bounds.center = new Vector3(0,0,0);
                bounds.size = new(2, 2, 2);
                //bounds.size = new(1, 1, 1);
            }
        }

        Simplify();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        int ei = 0;
        Vector3 cubeSize = new Vector3(epsilon, epsilon, epsilon);
        Vector3 maxCoord = bounds.size / epsilon;
        Vector3 offset = bounds.center - bounds.size / 2.0f;
        while (ei < (int)(maxCoord.x)+1)
        {
            int ej = 0;
            while (ej < (int)(maxCoord.y)+1)
            {
                int ek = 0;
                while (ek < (int)(maxCoord.z)+1)
                {
                    Vector3 cubePos = new(ei * cubeSize.x, ej * cubeSize.y, ek * cubeSize.z);
                    Gizmos.DrawWireCube(transform.position + bounds.center + cubePos + offset, cubeSize);
                    ek++;
                }
                ej ++;
            }
            ei++;
        }

        Gizmos.color = Color.cyan;
        if(otherMesh)
        {
            foreach(Vector3 v in otherMesh.vertices)
            {
                Gizmos.DrawWireSphere(otherMeshObj.transform.position + v, .01f);
            }
        }
    }

    private Vector3Int ToCubeCoord(Vector3 coord)
    {
        Vector3 cubeSize = new Vector3(epsilon, epsilon, epsilon);
        //Vector3 maxCoord = bounds.size / epsilon;
        Vector3 offset = bounds.center - bounds.size / 2.0f;

        Vector3 out_c = coord - transform.position - bounds.center - offset;
        return new(
            Mathf.FloorToInt(out_c.x / cubeSize.x),
            Mathf.FloorToInt(out_c.y / cubeSize.y),
            Mathf.FloorToInt(out_c.z / cubeSize.z)
        );
    }

    private void Simplify()
    {
        Vector3 cubeSize = new Vector3(epsilon, epsilon, epsilon);
        Vector3 maxCoord = bounds.size / epsilon;
        Vector3 offset = bounds.center - bounds.size / 2.0f;

        Dictionary<Vector3Int, List<int>> dict = new Dictionary<Vector3Int, List<int>>();

        Color[] colors = new Color[mesh.vertices.Length];

        int filledCubeCount = 0;

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3Int cubeIntCoord = ToCubeCoord(mesh.vertices[i]);
            if (!dict.ContainsKey(cubeIntCoord))
            {
                dict[cubeIntCoord] = new List<int>();
                filledCubeCount++;
            }
            dict[cubeIntCoord].Add(i);

            Vector3 gradient = new (cubeIntCoord.x / maxCoord.x, cubeIntCoord.y / maxCoord.y, cubeIntCoord.z / maxCoord.z);
            colors[i] = new Color(gradient.x, gradient.y, gradient.z);
        }

        mesh.SetColors(colors);

        Vector3[] vertices = new Vector3[filledCubeCount];
        Vector3[] normals = new Vector3[filledCubeCount];
        Color[] otherColors = new Color[filledCubeCount];

        List<int> triangles = new();

        int c_index = 0;
        foreach (Vector3Int c in dict.Keys)
        {
            Vector3 cube_v = Vector3.zero;
            Vector3 cube_n = Vector3.zero;
            foreach (int i in dict[c])
            {
                cube_v += mesh.vertices[i];
                cube_n += mesh.normals[i];
            }
            cube_v /= dict[c].Count;
            cube_n /= dict[c].Count;
            // check triangles(?)

            vertices[c_index] = cube_v;
            normals[c_index] = cube_n;

            Vector3 gradient = new(c.x / maxCoord.x, c.y / maxCoord.y, c.z / maxCoord.z);
            otherColors[c_index] = new Color(gradient.x, gradient.y, gradient.z);

            c_index++;
        }

        for(int i = 0; i < (mesh.triangles.Length)/3; i++)
        {
            int i1 = mesh.triangles[i * 3];
            int i2 = mesh.triangles[i * 3 + 1];
            int i3 = mesh.triangles[i * 3 + 2];
            Vector3 v1 = mesh.vertices[i1];
            Vector3 v2 = mesh.vertices[i2];
            Vector3 v3 = mesh.vertices[i3];
            Vector3Int cv1 = ToCubeCoord(v1);
            Vector3Int cv2 = ToCubeCoord(v2);
            Vector3Int cv3 = ToCubeCoord(v3);
            if(cv1 != cv2 && cv1 != cv3 && cv2 != cv3)
            {
                int ic1 = Array.IndexOf(vertices, cv1);
                int ic2 = Array.IndexOf(vertices, cv2);
                int ic3 = Array.IndexOf(vertices, cv3);
                if(ic1 > 0 && ic2 > 0 && ic3 > 0)
                {
                    triangles.Add(ic1);
                    triangles.Add(ic2);
                    triangles.Add(ic3);
                    Debug.Log("Added triangle");
                }
            }
        }

        otherMesh.SetVertices(vertices);
        otherMesh.SetNormals(normals);
        otherMesh.SetColors(otherColors);
        otherMesh.SetTriangles(triangles.ToArray(), 0);
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
