using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Water : MonoBehaviour
{
    [Header("Mesh Settings")]
    public int planeSize = 10;
    public int planeRes = 10;

    [Header("Compute Shader Settings")]
    public ComputeShader waterComputeShader;
    public int textureResolution = 256;
    public float waveAmplitude = 1.0f;
    public float waveFrequency = 10.0f;
    public Vector2 windDirection = new Vector2(1, 0);

    [Header("Material Settings")]
    public Material waterMaterial;

    private Mesh mesh;
    private RenderTexture displacementTexture;
    private int kernelHandle;

    void Start()
    {
        if (waterMaterial == null) throw new System.Exception("Must have material w/ shader on the water object");

        CreateMesh();
        SetupComputeShader();
    }

    void Update()
    {
        UpdateComputeShader();
    }

    // Mesh generation method.
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

    void SetupComputeShader()
    {
        displacementTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RFloat);
        displacementTexture.enableRandomWrite = true;
        displacementTexture.Create();

        // This binds the compute shader to the displacementTexture, so it will change when waterComputeShader.Dispatch() is called. 
        kernelHandle = waterComputeShader.FindKernel("CS_GenerateWaterMap");
        waterComputeShader.SetTexture(kernelHandle, "_DisplacementMap", displacementTexture);
        waterComputeShader.SetInt("_Resolution", textureResolution);
        
        waterMaterial.SetTexture("_MainTex", displacementTexture);
    }
    void UpdateComputeShader()
    {
        waterComputeShader.SetFloat("_Time", Time.time);
        waterComputeShader.SetFloat("_WaveAmplitude", waveAmplitude);
        waterComputeShader.SetFloat("_WaveFrequency", waveFrequency);
        waterComputeShader.SetVector("_WindDirection", windDirection);

        // In the compute shader I have [numthreads(8,8,1)], this is arbitrary, may need to tune later, but this must match the WaterFFT.compute header!!
        int threadGroups = Mathf.CeilToInt(textureResolution / 8.0f);
        waterComputeShader.Dispatch(kernelHandle, threadGroups, threadGroups, 1);
    }
}
