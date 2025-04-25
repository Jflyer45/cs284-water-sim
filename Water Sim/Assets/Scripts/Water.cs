/* 4/21/2025 Derivative of Acerola, gasgiant by proxy. Simplified and changed to integrate our features */

using System;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Water : MonoBehaviour {

    [Header("Shader Set Up")]
    public Shader waterShader;
    public ComputeShader computeShader;

    [Header("Mesh Settings")]
    public int planeLength = 10;
    public int planeRes = 10;
    public int textureSize = 1024;

    private Material waterMat;
    private Mesh mesh;

    [Header("Spectrum Settings")]
    SpectrumSettings[] spectrums = new SpectrumSettings[4];
    public int seed = 0;

    [Range(0.0f, 0.1f)]     public float lowCutoff = 0.0001f;
    [Range(0.1f, 9000.0f)]  public float highCutoff = 9000.0f;
    [Range(0.0f, 20.0f)]    public float gravity = 9.81f;
    [Range(2.0f, 20.0f)]    public float depth = 20.0f;
    [Range(0.0f, 200.0f)]   public float repeatTime = 200.0f;
    [Range(0.0f, 5.0f)]     public float speed = 1.0f;
    [Range(0.0f, 10.0f)]    public float displacementDepthFalloff = 1.0f;
    public Vector2 lambda = new Vector2(1.0f, 1.0f);

    [Header("Layer One")]
    [Range(0, 2048)]    public int lengthScale1 = 256;
    [Range(0.01f, 3.0f)]public float tile1 = 8.0f;
    [SerializeField]    public DisplaySpectrumSettings spectrum1;

    [Header("Layer Two")]
    [Range(0, 2048)]    public int lengthScale2 = 256;
    [Range(0.01f, 3.0f)]public float tile2 = 8.0f;
    [SerializeField]    public DisplaySpectrumSettings spectrum2;

    [Header("Layer Three")]
    [Range(0, 2048)]    public int lengthScale3 = 256;
    [Range(0.01f, 3.0f)]public float tile3 = 8.0f;
    [SerializeField]    public DisplaySpectrumSettings spectrum3;

    [Header("Layer Four")]
    [Range(0, 2048)]    public int lengthScale4 = 256;
    [Range(0.01f, 3.0f)]public float tile4 = 8.0f;
    [SerializeField]    public DisplaySpectrumSettings spectrum4;

    [Header("Normal Settings")]
    [Range(0.0f, 20.0f)]    public float normalStrength = 1;
    [Range(0.0f, 10.0f)]    public float normalDepthFalloff = 1.0f;

    [Header("Lighting Settings")]

    public Vector3 sunDirection = new Vector3(-1,-1,0);

    [ColorUsageAttribute(false, true)]  public Color sunIrradiance;
    [ColorUsageAttribute(false, true)]  public Color scatter;
    [ColorUsageAttribute(false, true)]  public Color bubble;

    [Range(0.0f, 1.0f)] public float bubbleDensity = 1.0f;
    [Range(0.0f, 2.0f)] public float roughness = 0.1f;
    [Range(0.0f, 2.0f)] public float foamRoughnessModifier = 1.0f;
    [Range(0.0f, 10.0f)]public float heightModifier = 1.0f;
    [Range(0.0f, 10.0f)]public float wavePeakScatterStrength = 1.0f;
    [Range(0.0f, 10.0f)]public float scatterStrength = 1.0f;
    [Range(0.0f, 10.0f)]public float scatterShadowStrength = 1.0f;
    [Range(0.0f, 2.0f)] public float environmentLightStrength = 1.0f;

    [Header("Foam Settings")]
    [ColorUsageAttribute(false, true)] public Color foam;

    [Range(-2.0f, 2.0f)]    public float foamBias = -0.5f;
    [Range(-10.0f, 10.0f)]  public float foamThreshold = 0.0f;
    [Range(0.0f, 1.0f)]     public float foamAdd = 0.5f;
    [Range(0.0f, 1.0f)]     public float foamDecayRate = 0.05f;
    [Range(0.0f, 10.0f)]    public float foamDepthFalloff = 1.0f;
    [Range(-2.0f, 2.0f)]    public float foamSubtract = 0.0f;

    
    private RenderTexture displacementTextures;
    private RenderTexture slopeTextures;
    private RenderTexture initialSpectrumTextures;
    private RenderTexture spectrumTextures;
    private RenderTexture buoyancyTexture;

    private ComputeBuffer spectrumBuffer;

    private int threadGroupsX, threadGroupsY;
    private const int NUM_LAYERS = 4;
    private const int NUM_SPECTRUMS_PER_LAYERS = 1;

    public RenderTexture GetBuoyancyTexture()
    {
        return buoyancyTexture;
    }

    void Start()
    {
        CreateMesh();
        CreateMaterial();
        CreateTextures();

        UpdateComputeShaderProperties();
        SetSpectrumBuffers();
        InitSpectrum();
    }

    void Update()
    {
        UpdateMaterialProperties();
        UpdateComputeShaderProperties();
        SetSpectrumBuffers();

        ProgressSpectrum();
        InverseFFT(spectrumTextures);

        SetTexturesFromComputeShaders();
    }

    private void CreateMesh()
    {
        mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        GetComponent<MeshFilter>().mesh = mesh;

        int segmentCount = planeLength * planeRes;
        int verticesPerSide = segmentCount + 1;
        float halfSize = planeLength * 0.5f;

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

                float posX = percentX * planeLength - halfSize;
                float posZ = percentZ * planeLength - halfSize;

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

    void CreateMaterial() {
        if (waterShader == null) return;

        waterMat = new Material(waterShader);
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = waterMat;
    }
    void CreateTextures()
    {
        threadGroupsX = Mathf.CeilToInt(textureSize / 8.0f);
        threadGroupsY = Mathf.CeilToInt(textureSize / 8.0f);

        initialSpectrumTextures = CreateRenderTexture(textureSize, textureSize, NUM_LAYERS, RenderTextureFormat.ARGBHalf, true);
        displacementTextures = CreateRenderTexture(textureSize, textureSize, NUM_LAYERS, RenderTextureFormat.ARGBHalf, true);
        slopeTextures = CreateRenderTexture(textureSize, textureSize, NUM_LAYERS, RenderTextureFormat.RGHalf, true);
        spectrumTextures = CreateRenderTexture(textureSize, textureSize, NUM_LAYERS * NUM_SPECTRUMS_PER_LAYERS, RenderTextureFormat.ARGBHalf, true);
        buoyancyTexture = CreateRenderTexture(textureSize, textureSize, 1, RenderTextureFormat.RFloat, false, false);

        spectrumBuffer = new ComputeBuffer(NUM_LAYERS * NUM_SPECTRUMS_PER_LAYERS, 8 * sizeof(float));
    }

    void UpdateMaterialProperties()
    {
        waterMat.SetFloat("_NormalStrength", normalStrength);
        waterMat.SetFloat("_Tile0", tile1);
        waterMat.SetFloat("_Tile1", tile2);
        waterMat.SetFloat("_Tile2", tile3);
        waterMat.SetFloat("_Tile3", tile4);
        waterMat.SetFloat("_Roughness", roughness);
        waterMat.SetFloat("_FoamRoughnessModifier", foamRoughnessModifier);
        waterMat.SetVector("_SunIrradiance", sunIrradiance);
        waterMat.SetVector("_BubbleColor", bubble);
        waterMat.SetVector("_ScatterColor", scatter);
        waterMat.SetVector("_FoamColor", foam);
        waterMat.SetFloat("_BubbleDensity", bubbleDensity);
        waterMat.SetFloat("_HeightModifier", heightModifier);
        waterMat.SetFloat("_DisplacementDepthAttenuation", displacementDepthFalloff);
        waterMat.SetFloat("_NormalDepthAttenuation", normalDepthFalloff);
        waterMat.SetFloat("_FoamDepthAttenuation", foamDepthFalloff);
        waterMat.SetFloat("_WavePeakScatterStrength", wavePeakScatterStrength);
        waterMat.SetFloat("_ScatterStrength", scatterStrength);
        waterMat.SetFloat("_ScatterShadowStrength", scatterShadowStrength);
        waterMat.SetFloat("_EnvironmentLightStrength", environmentLightStrength);
        waterMat.SetFloat("_FoamSubtract", foamSubtract);
        waterMat.SetTexture("_DisplacementTextures", displacementTextures);
        waterMat.SetTexture("_SlopeTextures", slopeTextures);
        waterMat.SetVector("_SunDirection", sunDirection);
    }

    void UpdateComputeShaderProperties() {
        computeShader.SetVector("_Lambda", lambda);
        computeShader.SetFloat("_FrameTime", Time.time * speed);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetFloat("_Gravity", gravity);
        computeShader.SetFloat("_RepeatTime", repeatTime);
        computeShader.SetInt("_TextureSize", textureSize);
        computeShader.SetInt("_Seed", seed);
        computeShader.SetInt("_LengthScale0", lengthScale1);
        computeShader.SetInt("_LengthScale1", lengthScale2);
        computeShader.SetInt("_LengthScale2", lengthScale3);
        computeShader.SetInt("_LengthScale3", lengthScale4);
        computeShader.SetFloat("_FoamThreshold", foamThreshold);
        computeShader.SetFloat("_Depth", depth);
        computeShader.SetFloat("_LowCutoff", lowCutoff);
        computeShader.SetFloat("_HighCutoff", highCutoff);
        computeShader.SetFloat("_FoamBias", foamBias);
        computeShader.SetFloat("_FoamDecayRate", foamDecayRate);
        computeShader.SetFloat("_FoamThreshold", foamThreshold);
        computeShader.SetFloat("_FoamAdd", foamAdd);
    }

    void InverseFFT(RenderTexture spectrumTextures)
    {
        computeShader.SetTexture(3, "_FourierTarget", spectrumTextures);
        computeShader.Dispatch(3, 1, textureSize, 1);
        computeShader.SetTexture(4, "_FourierTarget", spectrumTextures);
        computeShader.Dispatch(4, 1, textureSize, 1);
    }

    void InitSpectrum()
    {
        computeShader.SetTexture(0, "_InitialSpectrumTextures", initialSpectrumTextures);
        computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        computeShader.SetTexture(1, "_InitialSpectrumTextures", initialSpectrumTextures);
        computeShader.Dispatch(1, threadGroupsX, threadGroupsY, 1);
    }

    void ProgressSpectrum()
    {
        computeShader.SetTexture(2, "_InitialSpectrumTextures", initialSpectrumTextures);
        computeShader.SetTexture(2, "_SpectrumTextures", spectrumTextures);
        computeShader.Dispatch(2, threadGroupsX, threadGroupsY, 1);
    }

    void SetTexturesFromComputeShaders()
    {
        computeShader.SetTexture(5, "_DisplacementTextures", displacementTextures);
        computeShader.SetTexture(5, "_SpectrumTextures", spectrumTextures);
        computeShader.SetTexture(5, "_SlopeTextures", slopeTextures);
        computeShader.SetTexture(5, "_BuoyancyTexture", buoyancyTexture);
        computeShader.Dispatch(5, threadGroupsX, threadGroupsY, 1);

        displacementTextures.GenerateMips();
        slopeTextures.GenerateMips();
    }

    float JonswapAlpha(float fetch, float windSpeed)
    {
        return 0.076f * Mathf.Pow(windSpeed, 0.44f) / Mathf.Pow(gravity * fetch, 0.22f);
    }

    float JonswapPeakFrequency(float fetch, float windSpeed)
    {
        return 22 * Mathf.Pow(gravity, 0.66f) / Mathf.Pow(windSpeed * fetch, 0.33f);
    }

    void FillSpectrumStruct(DisplaySpectrumSettings displaySettings, ref SpectrumSettings computeSettings) {
        computeSettings.scale = displaySettings.scale;
        computeSettings.angle = displaySettings.windDirection / 180 * Mathf.PI;
        computeSettings.spreadBlend = displaySettings.spreadBlend;
        computeSettings.swell = Mathf.Clamp(displaySettings.swell, 0.01f, 1);
        computeSettings.alpha = JonswapAlpha(displaySettings.fetch, displaySettings.windSpeed);
        computeSettings.peakOmega = JonswapPeakFrequency(displaySettings.fetch, displaySettings.windSpeed);
        computeSettings.gamma = displaySettings.peakEnhancement;
        computeSettings.shortWavesFade = displaySettings.shortWavesFade;
    }

    void SetSpectrumBuffers() {
        FillSpectrumStruct(spectrum1, ref spectrums[0]);
        FillSpectrumStruct(spectrum2, ref spectrums[1]);
        FillSpectrumStruct(spectrum3, ref spectrums[2]);
        FillSpectrumStruct(spectrum4, ref spectrums[3]);

        spectrumBuffer.SetData(spectrums);
        computeShader.SetBuffer(0, "_Spectrums", spectrumBuffer);

        InitSpectrum();
    }

    RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, bool useMips, bool useTex2DArray = true) {
        RenderTexture rt = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
        rt.dimension = useTex2DArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.volumeDepth = depth;
        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 16;
        rt.Create();

        return rt;
    }

    void OnDestroy() {
        Destroy(displacementTextures);
        Destroy(slopeTextures);
        Destroy(initialSpectrumTextures);
        Destroy(spectrumTextures);
        spectrumBuffer.Dispose();
    }
}

// Settings set by the users in the instructor, before going though processing turning into SpectrumSettings.
[System.Serializable]
public struct DisplaySpectrumSettings
{
    [Range(0, 5)]
    public float scale;
    public float windSpeed;
    [Range(0.0f, 360.0f)]
    public float windDirection;
    public float fetch;
    [Range(0, 1)]
    public float spreadBlend;
    [Range(0, 1)]
    public float swell;
    public float peakEnhancement;
    public float shortWavesFade;
}

// Data snet to the GPU
public struct SpectrumSettings
{
    public float scale;
    public float angle;
    public float spreadBlend;
    public float swell;
    public float alpha;
    public float peakOmega;
    public float gamma;
    public float shortWavesFade;
}