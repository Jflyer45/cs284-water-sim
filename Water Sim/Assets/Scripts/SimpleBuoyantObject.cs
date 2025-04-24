using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleBuoyantObject : MonoBehaviour
{
    Rigidbody rb;
    public float buoyancyStrength = 10f;
    public float damping = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 wp = transform.position;

        float waterH = WaterSurface.Instance.GetHeight(wp.x, wp.z);

        float depth = waterH - wp.y;
        if (depth <= 0f) return;

        // push up
        Vector3 Fbuoy = Vector3.up * buoyancyStrength * depth;
        rb.AddForce(Fbuoy, ForceMode.Force);

        // damp
        float vY = Vector3.Dot(rb.linearVelocity, Vector3.up);
        Vector3 Fdamp = Vector3.up * (-damping * vY);
        rb.AddForce(Fdamp, ForceMode.Force);
    }
}
