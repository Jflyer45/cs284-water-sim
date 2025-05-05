// ShipMovement.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardForce = 10f;
    public float turnForce = 50f;
    public float strafeForce = 3f;
    public float reverseForce = 5f;

    [Header("Control Options")]
    [Tooltip("Toggle player vs AI input")]
    public bool useAIInput = false;
    [Range(-1f, 1f)] public float aiForwardInput;
    [Range(-1f, 1f)] public float aiStrafeInput;
    [Range(-1f, 1f)] public float aiTurnInput;

    [Tooltip("Enable player control when not using AI")]
    public bool movementEnabled = true;
    public Transform forceApplicationPoint;

    [Header("Physics Tuning")]
    public float stabilizationTorque = 1f;
    public float maxVelocityMagnitude = 15f;

    Rigidbody shipRigidbody;

    void Awake()
    {
        shipRigidbody = GetComponent<Rigidbody>();
        if (forceApplicationPoint == null)
        {
            var go = new GameObject("ForceApplicationPoint");
            go.transform.parent = transform;
            go.transform.localPosition = new Vector3(0, -0.5f, -1f);
            forceApplicationPoint = go.transform;
        }
    }

    void FixedUpdate()
    {
        if (!movementEnabled || shipRigidbody == null) return;

        float forwardInput = useAIInput
            ? aiForwardInput
            : (Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f);
        float strafeInput = useAIInput
            ? aiStrafeInput
            : (Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f);
        float turnInput = useAIInput
            ? aiTurnInput
            : (Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f);

        // move
        Vector3 moveForce = transform.forward * forwardInput * (forwardInput > 0f ? forwardForce : reverseForce)
                          + transform.right * strafeInput * strafeForce;
        if (forceApplicationPoint != null)
            shipRigidbody.AddForceAtPosition(moveForce, forceApplicationPoint.position, ForceMode.Force);
        else
            shipRigidbody.AddForce(moveForce, ForceMode.Force);

        // turn
        if (turnInput != 0f)
            shipRigidbody.AddTorque(transform.up * turnInput * turnForce, ForceMode.Impulse);

        // stabilize
        if (Mathf.Abs(forwardInput) + Mathf.Abs(strafeInput) > 0.1f)
            shipRigidbody.AddTorque(-shipRigidbody.angularVelocity * stabilizationTorque, ForceMode.Force);

        // clamp speed
        if (shipRigidbody.linearVelocity.magnitude > maxVelocityMagnitude)
            shipRigidbody.linearVelocity = shipRigidbody.linearVelocity.normalized * maxVelocityMagnitude;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }
}
