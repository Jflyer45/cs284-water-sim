using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

/*
 Since we might implement different buoyancy algorithms, we need a common way to read back the water height.
 The added benifit is that it's cached for all buoyant objects!
 - Jeremy Fischer
 */

[DefaultExecutionOrder(-100)] // read back before Buoyancy.FixedUpdate
public class WaterSurface : MonoBehaviour
{
    public static WaterSurface Instance { get; private set; }

    private Water water;
    private int textureSize;
    private float tile1;

    float[] heights; // CPU cache of heights

    void Awake()
    {
        if (Instance != null) Destroy(this);
        else
        {
            Instance = this;
            water = FindFirstObjectByType<Water>();
            textureSize = water.textureSize;
            tile1 = water.tile1;
            heights = new float[water.textureSize * textureSize];

        }
    }

    void LateUpdate()
    {
        AsyncGPUReadback.Request(
            water.GetBuoyancyTexture(),
            0,
            OnReadbackDone
        );
    }

    void OnReadbackDone(AsyncGPUReadbackRequest req)
    {
        if (req.hasError)
        {
            Debug.LogError("WaterSurface readback failed");
            return;
        }
        var data = req.GetData<float>();
        data.CopyTo(heights);
    }

    public float GetHeight(float worldX, float worldZ)
    {
        // convert to uv
        Vector2 uv = new Vector2(worldX, worldZ) * tile1;
        float u = uv.x - Mathf.Floor(uv.x);
        float v = uv.y - Mathf.Floor(uv.y);

        // map to texel coords
        float fx = u * (textureSize - 1);
        float fy = v * (textureSize - 1);
        int x0 = Mathf.FloorToInt(fx);
        int y0 = Mathf.FloorToInt(fy);
        int x1 = (x0 + 1) % textureSize;
        int y1 = (y0 + 1) % textureSize;
        float tx = fx - x0;
        float ty = fy - y0;

        // fetch four neighbors
        float h00 = heights[y0 * textureSize + x0];
        float h10 = heights[y0 * textureSize + x1];
        float h01 = heights[y1 * textureSize + x0];
        float h11 = heights[y1 * textureSize + x1];

        // bilerp
        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(h0, h1, ty);
    }

    public Vector3 GetNormal(float worldX, float worldZ, float offset = 0.1f)
    {
        // Get the gradient
        float hL = GetHeight(worldX - offset, worldZ);
        float hR = GetHeight(worldX + offset, worldZ);
        float hD = GetHeight(worldX, worldZ - offset);
        float hU = GetHeight(worldX, worldZ + offset);

        Vector3 n = new Vector3(hL - hR, 2f * offset, hD - hU);
        return n.normalized;
    }

}
