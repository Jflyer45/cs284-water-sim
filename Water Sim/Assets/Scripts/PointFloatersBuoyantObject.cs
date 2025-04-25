using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PointFloatersBuoyantObject : MonoBehaviour
{
    public enum BuoyancyMovement
    {
        VerticalOnly,
        AlongSurfaceNormal
    }

    [Header("Floaters")]
    public Vector3[] localFloatPoints;
    public float[] floatAreas;

    [Header("Buoyancy Settings")]
    public float buoyancyStrength = 10;
    public float dampingStrength = 0.5f;

    [Header("Movement Type")]
    public BuoyancyMovement movementType = BuoyancyMovement.VerticalOnly;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (WaterSurface.Instance == null) return;

        for (int i = 0; i < localFloatPoints.Length; i++)
        {
            Vector3 wp = transform.TransformPoint(localFloatPoints[i]);

            float waterH = WaterSurface.Instance.GetHeight(wp.x, wp.z);
            float depth = waterH - wp.y;
            if (depth <= 0f) continue;

            float area = (i < floatAreas.Length) ? floatAreas[i] : 1f;

            Vector3 dir;
            if (movementType == BuoyancyMovement.AlongSurfaceNormal)
            {
                dir = WaterSurface.Instance.GetNormal(wp.x, wp.z);
            }
            else
            {
                dir = Vector3.up;
            }

            Vector3 Fbuoy = dir * (buoyancyStrength * area * depth);
            rb.AddForceAtPosition(Fbuoy, wp);

            Vector3 vel = rb.GetPointVelocity(wp);
            float vY = Vector3.Dot(vel, Vector3.up);
            Vector3 Fdamp = Vector3.up * (-dampingStrength * vY);
            rb.AddForceAtPosition(Fdamp, wp);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (localFloatPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var p in localFloatPoints)
        {
            Vector3 wp = transform.TransformPoint(p);
            Gizmos.DrawSphere(wp, 0.1f);
        }
    }
}
