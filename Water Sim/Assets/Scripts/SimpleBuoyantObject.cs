using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

public class SimpleBuoyantObject : MonoBehaviour
{
    Water water;

    void Start()
    {
        water = FindFirstObjectByType<Water>();

    }

    void Update()
    {
        Vector3 worldPos = transform.position;
        Vector2 pos = new Vector2(worldPos.x, worldPos.z);
        Vector2 uv = worldPos * water.tile1;

        int x = Mathf.FloorToInt(frac(uv.x) * (water.textureSize - 1));
        int y = Mathf.FloorToInt(frac(uv.y) * (water.textureSize - 1));

        AsyncGPUReadback.Request(
            water.GetBuoyancyTexture(), // src
            0,                          // mip level
            x, 1,                       // x, width
            y, 1,                       // y, height
            0, 1,                       // z (slice), depth (number of slices)
            OnWaterReadback             // your callback
        );
    }

    void OnWaterReadback(AsyncGPUReadbackRequest req)
    {
        if (req.hasError)
        {
            Debug.LogError("GPU readback error");
            return;
        }

        float height = req.GetData<float>()[0];

        var p = transform.position;
        p.y = height;
        transform.position = p;
    }
}
