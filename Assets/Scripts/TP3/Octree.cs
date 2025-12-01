using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class Octree : MonoBehaviour
{
    [SerializeField] private int maxDepth = 4;
    [SerializeField] private float CubeScale = 1;
    [SerializeField] private Vector3Int RootSize;
    [SerializeField] private Material CubeMaterial;
    [SerializeField] private float paintStrength = 0.2f;
    private Bounds RootBounds;
    private OctreeNode Root;

    private SDFObject SceneTree;

    private GameObject PaintTool;

    InputAction addPaintAction, removePaintAction;

    // https://iquilezles.org/articles/distfunctions/
    static float Dot2(Vector3 a) { return Vector3.Dot(a, a); }

    // Boolean operations
    // OR
    private static float UnionSDF(float sdf1, float sdf2)
    {
        return Mathf.Min(sdf1, sdf2);
    }
    // AND
    private static float IntersectionSDF(float sdf1, float sdf2)
    {
        return Mathf.Max(sdf1, sdf2);
    }
    // XOR
    private static float XorSDF(float sdf1, float sdf2)
    {
        return IntersectionSDF(UnionSDF(sdf1, sdf2), -IntersectionSDF(sdf1, sdf2));
    }
    // Substract ( AND NOT )
    private static float SubstractSDF(float sdf1, float sdf2)
    {
        return IntersectionSDF(sdf1, -sdf2);
    }

    float SceneSDF(Vector3 p)
    {
        return SceneTree.SDF(p);
    }

    public enum OperationsOnSDF { Union, Intersection, Substraction, Xor };

    public abstract class SDFObject
    {
        public abstract float SDF(Vector3 p);
        public abstract Bounds GetBounds();
        public abstract void DrawGizmos();
    }

    public class SphereSDF : SDFObject
    {
        private Vector3 center;
        private float radius;

        public SphereSDF(Vector3 center, float radius) 
        {
            this.center = center;
            this.radius = radius;
        }

        public override float SDF(Vector3 p)
        {
            return Mathf.Sqrt(Dot2(p - center)) - radius;
        }

        public override Bounds GetBounds()
        {
            return new Bounds(center, Vector3.one * radius * 2.0f);
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawWireSphere(center, radius);
        }
    }

    public class BoxSDF : SDFObject
    {
        private Vector3 center;
        private Vector3 halfsize;
        public BoxSDF(Vector3 center, Vector3 halfsize)
        {
            this.center = center;
            this.halfsize = halfsize;
        }

        public override float SDF(Vector3 p)
        {
            Vector3 diff = p - center;
            Vector3 q = new Vector3(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z)) - halfsize;
            return Vector3.Magnitude(Vector3.Max(q, Vector3.zero)) + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0.0f);
        }

        public override Bounds GetBounds()
        {
            return new Bounds(center, halfsize * 2.0f);
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawWireCube(center, halfsize * 2.0f);
        }
    }

    public class CombinationSDF : SDFObject
    {
        private SDFObject a, b;
        private OperationsOnSDF operation;
        private Bounds? bounds = null;

        public CombinationSDF(SDFObject a, SDFObject b, OperationsOnSDF operation)
        {
            this.a = a;
            this.b = b;
            this.operation = operation;
            this.bounds = GetBounds();
        }

        public override float SDF(Vector3 p)
        {
            switch (operation)
            {
                case OperationsOnSDF.Union: return UnionSDF(a.SDF(p), b.SDF(p));
                case OperationsOnSDF.Intersection: return IntersectionSDF(a.SDF(p), b.SDF(p));
                case OperationsOnSDF.Substraction: return SubstractSDF(a.SDF(p), b.SDF(p));
                case OperationsOnSDF.Xor: return XorSDF(a.SDF(p), b.SDF(p));
                default: return Mathf.Infinity;
            }
        }
        public override Bounds GetBounds()
        {
            if (bounds != null) return bounds.Value;
            // On substraction, the substacted object shouldn't be used for bounds
            else if (operation == OperationsOnSDF.Substraction) 
            {
                bounds = a.GetBounds();
                return bounds.Value;
            }
            else
            {
                Bounds ba, bb;
                ba = a.GetBounds();
                bb = b.GetBounds();
                Vector3 min = Vector3.Min(a.GetBounds().min, b.GetBounds().min);
                Vector3 max = Vector3.Max(a.GetBounds().max, b.GetBounds().max);
                bounds = new Bounds((min + max) / 2.0f, max - min);
                return bounds.Value;
            }
        }

        public override void DrawGizmos()
        {
            a.DrawGizmos();
            b.DrawGizmos();
        }
    }

    class OctreeNode
    {
        public Bounds bounds;
        public int depth;
        public OctreeNode[] children;
        public float value;
        public bool IsLeaf()
        {
            return (children == null) || (depth <= 0);
        }
        public bool IsFilled() { return value > 7.5; }
        public OctreeNode(Bounds bounds, int depth, int value = 0)
        {
            //Debug.Log("Creating node at depth " + depth + " with bounds center " + bounds.center + " and size " + bounds.size);
            this.bounds = bounds;
            this.depth = depth;
            this.value = value;
            if(depth <= 0) children = null;
            else
            {
                children = new OctreeNode[8];
                for(int i = 0; i < children.Length; i++)
                {
                    Bounds new_b = bounds;
                    new_b.size /= 2;
                    new_b.center += new Vector3(
                        ((i & 1) == 0) ? -new_b.size.x / 2 : new_b.size.x / 2,
                        ((i & 2) == 0) ? -new_b.size.y / 2 : new_b.size.y / 2,
                        ((i & 4) == 0) ? -new_b.size.z / 2 : new_b.size.z / 2
                    );
                    children[i] = new OctreeNode(new_b, depth - 1, 0);
                }
            }
        }
    }

    void FillOctree(OctreeNode node)
    {
        if(node.IsLeaf()) return;
        else
        {
            node.value = 0;
            if (node.depth == 1)
            {
                node.value = (SceneSDF(node.bounds.center) <= 0) ? 8 : 0;
            }
            else
            {
                for (int i = 0; i < node.children.Length; i++)
                {
                    FillOctree(node.children[i]);
                    node.value += node.children[i].IsFilled() ? 1 : 0;
                }
            }
        }
    }

    void ModelOctree(OctreeNode node)
    {
        if (node.IsLeaf()) return;
        else
        {;
            if (!node.IsFilled()) // Check children
            {
                for (int i = 0; i < node.children.Length; i++) ModelOctree(node.children[i]);
            }
            else // Fill this node
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                
                cube.transform.position = node.bounds.center;
                cube.transform.localScale = node.bounds.size * CubeScale;
                cube.transform.parent = this.transform;

                //////////////// Debug color for cube depth ////////////////
                MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
                renderer.material = new(CubeMaterial);
                if (node.value - (int)node.value == 0) renderer.material.color = Color.white;
                ////renderer.material.color = Color.HSVToRGB(360.0f * node.depth / (float) maxDepth, node.depth / (float) maxDepth, 1.0f);
                //renderer.material.color = Color.green * node.depth / (float) maxDepth;
            }
        }
    }

    private void _PropagateValue(OctreeNode node, float value, Vector3 position)
    {
        if (node != null && node.bounds.Contains(position))
        {
            if (node.depth == 1)
            {
                node.value += value;
                node.value = Mathf.Min(node.value, 8);
            }
            else
            {
                node.value = 0;
                foreach (OctreeNode n in node.children)
                {
                    _PropagateValue(n, value, position);
                    node.value += n.IsFilled() ? 1 : 0;
                }
            }
        }
    }

    void UpdateOctree(OctreeNode node, float value, Vector3 position)
    {
        _PropagateValue(node, value, position);
        foreach (Transform child in transform) Destroy(child.gameObject);
        ModelOctree(node);
    }

    void InitOctree()
    {
        //RootBounds = new Bounds(transform.position, RootSize);
        RootBounds = SceneTree.GetBounds();
        Root = new OctreeNode(RootBounds, maxDepth);
        FillOctree(Root);
        ModelOctree(Root);
    }

    void InitScene()
    {
        // Unique sphere in center 
        SphereSDF sphereA = new SphereSDF(transform.position, 0.5f);
        SceneTree = sphereA;

        // Unique box in center
        //BoxSDF boxA = new BoxSDF(transform.position, new(.4f, .4f, .4f));
        //SceneTree = boxA;

        // Intersection of 2 spheres
        //SphereSDF sphereB = new SphereSDF(transform.position + new Vector3( .2f, 0, 0), .4f);
        //SphereSDF sphereC = new SphereSDF(transform.position + new Vector3(-.2f, 0, 0), .4f);
        //SceneTree = new CombinationSDF(sphereB, sphereC, OperationsOnSDF.Intersection);


        // Box - Sphere

        //SphereSDF sphereD = (new SphereSDF(transform.position + new Vector3(0, 0, -0.5f), .5f));
        //BoxSDF box = (new BoxSDF(transform.position, new Vector3(.5f, .5f, .5f)));
        //SceneTree = new CombinationSDF(box, sphereD, OperationsOnSDF.Substraction);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        addPaintAction = InputSystem.actions.FindAction("AddPaint");
        removePaintAction = InputSystem.actions.FindAction("RemovePaint");

        InitScene();
        InitOctree();
        PaintTool = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        PaintTool.transform.localScale = Vector3.one / 10.0f;
        PaintTool.GetComponent<Collider>().enabled = false;
        PaintTool.GetComponent<MeshRenderer>().material = CubeMaterial;
    }

    private void Update()
    {
        // shoot ray from camera to mouse going into the scene
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        //Ray ray = new Ray(Camera.main.transform.position, Vector3.Normalize(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - Camera.main.transform.position));
        // cast ray
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, 10))
        {
            PaintTool.transform.position = hit.point;
        }

        Debug.DrawRay(ray.origin, ray.direction * 10, Color.red);

        if (addPaintAction.IsPressed())
        {
            // add matter
            UpdateOctree(Root, paintStrength, PaintTool.transform.position);
            Debug.Log("Drawing");
        }
        else if(removePaintAction.IsPressed())
        {
            // remove matter
            UpdateOctree(Root, -paintStrength, PaintTool.transform.position);
            Debug.Log("Erasing");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(RootBounds.center, RootBounds.size);
        Gizmos.color = Color.cyan;
        if (SceneTree != null) SceneTree.DrawGizmos();
    }
}
