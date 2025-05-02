using UnityEngine;

/// <summary>
/// Controls ship movement with WASD keys while working with buoyancy physics.
/// This script should be attached to the root object with a Rigidbody and buoyancy forces.
/// </summary>
public class ShipMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Forward movement force multiplier")]
    public float forwardForce = 10f;
    [Tooltip("Turning force multiplier")]
    public float turnForce = 50f; // Significantly increased from 5f to 50f
    [Tooltip("Strafe force multiplier (left/right movement)")]
    public float strafeForce = 3f;
    [Tooltip("Reverse force multiplier (typically lower than forward)")]
    public float reverseForce = 5f;

    [Header("Control Options")]
    [Tooltip("Enable/disable ship movement controls")]
    public bool movementEnabled = true;
    [Tooltip("Apply forces at this position relative to the ship")]
    public Transform forceApplicationPoint;

    [Header("Physics Tuning")]
    [Tooltip("Reduces rotation from external forces when moving")]
    public float stabilizationTorque = 1f;
    [Tooltip("Limits maximum velocity to prevent excessive speed")]
    public float maxVelocityMagnitude = 15f;

    // References
    private Rigidbody shipRigidbody;

    private void Awake()
    {
        // Get the Rigidbody component
        shipRigidbody = GetComponent<Rigidbody>();

        // If no force application point is set, use the transform's position
        if (forceApplicationPoint == null)
        {
            // Create a new GameObject as a child to apply forces at
            GameObject forcePoint = new GameObject("ForceApplicationPoint");
            forcePoint.transform.parent = transform;

            // Position it slightly below the center of mass and toward the back
            forcePoint.transform.localPosition = new Vector3(0, -0.5f, -1f);
            forceApplicationPoint = forcePoint.transform;
        }
    }

    private void Update()
    {
        // We'll check inputs in Update for better responsiveness
        // But we'll apply forces in FixedUpdate
    }

    private void FixedUpdate()
    {
        if (!movementEnabled || shipRigidbody == null)
            return;

        // Get direct key inputs instead of using axes
        float forwardInput = 0f;
        float turnInput = 0f;
        float strafeInput = 0f;

        // Forward/Backward with W/S
        if (Input.GetKey(KeyCode.W))
            forwardInput = 1f;
        else if (Input.GetKey(KeyCode.S))
            forwardInput = -1f;

        // Turning with A/D
        if (Input.GetKey(KeyCode.A))
            turnInput = -1f;  // Turn left
        else if (Input.GetKey(KeyCode.D))
            turnInput = 1f;   // Turn right

        // Strafing with Q/E
        if (Input.GetKey(KeyCode.Q))
            strafeInput = -1f;
        else if (Input.GetKey(KeyCode.E))
            strafeInput = 1f;

        // Debug output to verify key detection
        if (turnInput != 0)
            Debug.Log("Turn input: " + turnInput);

        // Apply forward/backward movement
        Vector3 forwardForceVector = transform.forward * forwardInput *
            (forwardInput > 0 ? forwardForce : reverseForce);

        // Apply strafe (sideways) force
        Vector3 strafeForceVector = transform.right * strafeInput * strafeForce;

        // Apply movement forces
        if (forceApplicationPoint != null)
        {
            // Apply movement forces at the specified position
            shipRigidbody.AddForceAtPosition(forwardForceVector + strafeForceVector,
                forceApplicationPoint.position, ForceMode.Force);
        }
        else
        {
            // Fallback to applying forces at center of mass
            shipRigidbody.AddForce(forwardForceVector + strafeForceVector, ForceMode.Force);
        }

        // Apply turning torque - Modified for pure rotation
        if (turnInput != 0)
        {
            // Apply turning torque around the up axis for proper rotation
            Vector3 turnTorque = transform.up * turnInput * turnForce;

            // Use ForceMode.Impulse for more immediate response
            shipRigidbody.AddTorque(turnTorque, ForceMode.Impulse);

            // Remove the side force that was causing sideways movement
            // Vector3 bowPosition = transform.position + transform.forward * 2f;
            // Vector3 sideForce = transform.right * turnInput * turnForce * 0.5f;
            // shipRigidbody.AddForceAtPosition(sideForce, bowPosition, ForceMode.Impulse);
        }

        // Apply stabilization when moving to reduce unwanted tilting
        if (Mathf.Abs(forwardInput) > 0.1f || Mathf.Abs(strafeInput) > 0.1f)
        {
            Vector3 stabilization = -shipRigidbody.angularVelocity * stabilizationTorque;
            shipRigidbody.AddTorque(stabilization, ForceMode.Force);
        }

        // Limit maximum velocity to prevent excessive speeds
        LimitVelocity();
    }

    /// <summary>
    /// Limits the ship's velocity to prevent excessive speeds.
    /// </summary>
    private void LimitVelocity()
    {
        if (shipRigidbody.linearVelocity.magnitude > maxVelocityMagnitude)
        {
            shipRigidbody.linearVelocity = shipRigidbody.linearVelocity.normalized * maxVelocityMagnitude;
        }
    }

    /// <summary>
    /// Enables or disables the ship movement controls.
    /// Can be called from other scripts or UI buttons.
    /// </summary>
    /// <param name="enabled">Whether to enable or disable movement</param>
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    /// <summary>
    /// Toggles the movement controls on/off.
    /// </summary>
    public void ToggleMovement()
    {
        movementEnabled = !movementEnabled;
    }
}