using Unity.Burst;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Octree : MonoBehaviour
{
    [SerializeField] private int maxDepth = 4;
    [SerializeField] private Vector3Int RootSize;
    private Bounds RootBounds;
    private OctreeNode Root;

    // https://iquilezles.org/articles/distfunctions/
    float dot2(Vector3 a) { return Vector3.Dot(a, a); }
    
    // Boolean operations
    // OR
    float sdfUnion(float sdf1, float sdf2)
    {
        return Mathf.Min(sdf1, sdf2);
    }
    // AND
    float sdfIntersection(float sdf1, float sdf2)
    {
        return Mathf.Max(sdf1, sdf2);
    }
    // XOR
    float sdfXOR(float sdf1, float sdf2)
    {
        return Mathf.Max(sdfUnion(sdf1, sdf2), -sdfIntersection(sdf1, sdf2));
    }
    // Substract
    float sdfSubstract(float sdf1, float sdf2)
    {
        return Mathf.Max(-sdf1, sdf2);
    }

    // Primitives

    // Sphere with center and radius
    private float SphereSDF(Vector3 p, Vector3 center, float radius)
    {
        return Mathf.Sqrt(dot2(p - center)) - radius;
    }

    //
    private float BoxSDF(Vector3 p, Vector3 center, Vector3 halfsize)
    {
        Vector3 diff = p - center;  
        Vector3 q = new Vector3(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z)) - halfsize;
        return Vector3.Magnitude(Vector3.Max(q, Vector3.zero)) + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0.0f);
    }

    float SceneSDF(Vector3 p)
    {
        float MAX = Mathf.NegativeInfinity;

        // Unique sphere in center 
        //MAX = Mathf.Max(MAX, SphereSDF(p, transform.position, 0.3f));

        // Unique box in center
        //MAX = Mathf.Max(MAX, BoxSDF(p, transform.position, new Vector3(.4f, .4f, .4f)));

        // Intersection of 2 spheres
        //MAX = Mathf.Max(MAX, sdfIntersection(
        //    SphereSDF(p, transform.position + new Vector3(0.2f, 0.0f, 0.0f), 0.4f),
        //    SphereSDF(p, transform.position + new Vector3(-0.2f, 0.0f, 0.0f), 0.4f)
        //));

        // Box - Sphere
        MAX = Mathf.Max(MAX, sdfSubstract(
            SphereSDF(p, transform.position + new Vector3(0.4f, 0, 0), 0.5f)
            ,
            BoxSDF(p, transform.position + new Vector3(0.0f, 0, 0), new Vector3(.4f, .4f, .4f))
        ));

        return MAX;
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

    bool IsFilled(float value) { return value > 7.9; }

    void FillOctree(OctreeNode node)
    {
        if(node.IsLeaf()) return;
        else
        {
            if (node.depth == 1)
            {
                node.value = (SceneSDF(node.bounds.center) <= 0) ? 8 : 0;
                Debug.Log(node.value);
            }
            else
            {
                node.value = 0.0f;
                for (int i = 0; i < node.children.Length; i++)
                {
                    FillOctree(node.children[i]);
                    node.value += (IsFilled(node.children[i].value) ? 1 : 0);
                }
            }
        }
    }

    void ModelOctree(OctreeNode node)
    {
        Debug.Log($"Depth:{node.depth}, Value:{node.value}");
        if (node.IsLeaf()) return;
        else
        {
            bool shouldFill = IsFilled(node.value);
            if (!shouldFill) // Check children
            {
                for (int i = 0; i < node.children.Length; i++)
                    ModelOctree(node.children[i]);
            }
            else // Fill this node
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<BoxCollider>());
                cube.transform.position = node.bounds.center;
                cube.transform.localScale = node.bounds.size;
                cube.transform.parent = this.transform;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RootBounds = new Bounds(transform.position, RootSize);
        Root = new OctreeNode(RootBounds, maxDepth);
        FillOctree(Root);
        ModelOctree(Root);
    }
}
