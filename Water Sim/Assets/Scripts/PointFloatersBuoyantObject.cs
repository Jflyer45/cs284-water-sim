using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PointFloatersBuoyantObject : MonoBehaviour
{
    [Header("Floaters")]
    public Vector3[] localFloatPoints;
    public float[] floatAreas;

    [Header("Buoyancy Settings")]
    public float buoyancyStrength = 1000f;
    public float dampingStrength = 0.5f;

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

            // How deep, if at all, no contribution
            float depth = waterH - wp.y;
            if (depth <= 0f) continue;

            // buoyant force
            float area = (i < floatAreas.Length) ? floatAreas[i] : 1f;
            Vector3 Fbuoy = Vector3.up * buoyancyStrength * area * depth;
            rb.AddForceAtPosition(Fbuoy, wp);

            // damping
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
        for (int i = 0; i < localFloatPoints.Length; i++)
        {
            Vector3 wp = Application.isPlaying
                ? transform.TransformPoint(localFloatPoints[i])
                : transform.TransformPoint(localFloatPoints[i]);
            Gizmos.DrawSphere(wp, 0.1f);
        }
    }
}
