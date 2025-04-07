using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Water : MonoBehaviour
{
    public int planeSize = 10;
    public int planeRes = 10;

    private Mesh mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // The unity object for water has a mesh component, this grabs it so we can have reference to it
        CreateMesh();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // The mesh component is empty, this method generates the mesh
    [ContextMenu("Create Mesh")]
    private void CreateMesh()
    {
        mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        GetComponent<MeshFilter>().mesh = mesh;

        int segmentCount = planeSize * planeRes;
        int verticesPerSide = segmentCount + 1;
        float halfSize = planeSize * 0.5f;

        Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 defaultTangent = new Vector4(1f, 0f, 0f, -1f);

        int index = 0;
        for (int x = 0; x < verticesPerSide; x++)
        {
            for (int z = 0; z < verticesPerSide; z++)
            {
                float percentX = (float)x / segmentCount;
                float percentZ = (float)z / segmentCount;

                float posX = percentX * planeSize - halfSize;
                float posZ = percentZ * planeSize - halfSize;

                vertices[index] = new Vector3(posX, 0f, posZ);
                uv[index] = new Vector2(percentX, percentZ);
                tangents[index] = defaultTangent;
                index++;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.tangents = tangents;

        int[] triangles = new int[segmentCount * segmentCount * 6];
        int t = 0;
        for (int x = 0; x < segmentCount; x++)
        {
            for (int z = 0; z < segmentCount; z++)
            {
                int topLeft = x * verticesPerSide + z;
                int topRight = topLeft + 1;
                int bottomLeft = (x + 1) * verticesPerSide + z;
                int bottomRight = bottomLeft + 1;

                // First triangle
                triangles[t++] = topLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomRight;

                // Second triangle
                triangles[t++] = topLeft;
                triangles[t++] = bottomRight;
                triangles[t++] = bottomLeft;
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
