using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class OFFMeshLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] protected string ImportPath;
    [SerializeField] protected string ExportPath = "Assets/Meshes/off_export.off";
    protected Mesh mesh;
    
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

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
                    Debug.DrawLine(transform.position + vertices[i], transform.position + vertices[i] + normals[i] * 0.02f, Color.yellow, 600.0f);
                    // Drawing normals for 10min 
                }


                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);

                //mesh.RecalculateNormals();

                mesh.normals = normals;
            }
        }
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
