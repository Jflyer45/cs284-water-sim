using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class InitialSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    public Vector3 spinAxis = Vector3.up; // axis of rotation
    public float spinAmount = 5f;         // angular speed in rad/s

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularVelocity = spinAxis.normalized * spinAmount;
        // or, if you prefer ForceMode:
        // rb.AddTorque(spinAxis.normalized * spinAmount, ForceMode.VelocityChange);
    }
}
