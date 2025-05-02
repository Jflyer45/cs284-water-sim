using UnityEngine;

/// <summary>
/// Camera controller that allows looking around only when right mouse button is held.
/// This script does not move the camera's position, only its rotation.
/// </summary>
public class LookAroundCamera : MonoBehaviour
{
    [Header("Mouse Settings")]
    [Tooltip("Mouse sensitivity for horizontal rotation")]
    public float horizontalSensitivity = 2.0f;
    [Tooltip("Mouse sensitivity for vertical rotation")]
    public float verticalSensitivity = 2.0f;
    [Tooltip("Invert vertical mouse input")]
    public bool invertY = false;

    [Header("View Constraints")]
    [Tooltip("Minimum vertical angle (looking down)")]
    public float minVerticalAngle = -80f;
    [Tooltip("Maximum vertical angle (looking up)")]
    public float maxVerticalAngle = 80f;

    [Header("Control Settings")]
    [Tooltip("Enable/disable camera rotation")]
    public bool rotationEnabled = true;

    // Internal variables
    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isRightMouseDown = false;

    private void Start()
    {
        // Initialize rotation values based on current camera orientation
        Vector3 currentRotation = transform.rotation.eulerAngles;
        rotationY = currentRotation.y;
        rotationX = currentRotation.x;

        // Adjust rotationX to be in the -180 to 180 range for proper clamping
        if (rotationX > 180f)
            rotationX -= 360f;
    }

    private void Update()
    {
        // Check if right mouse button is being held down
        isRightMouseDown = Input.GetMouseButton(1);

        // Only process rotation when enabled and right mouse button is pressed
        if (rotationEnabled && isRightMouseDown)
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;

            // Apply inversion if needed
            if (invertY)
                mouseY = -mouseY;
            else
                mouseY = -mouseY; // Default behavior is inverted for most camera controls

            // Update rotation values
            rotationY += mouseX;
            rotationX += mouseY;

            // Clamp vertical rotation
            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

            // Apply rotation to the camera
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
    }

    /// <summary>
    /// Enables or disables camera rotation.
    /// </summary>
    /// <param name="enabled">Whether rotation should be enabled</param>
    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
    }

    /// <summary>
    /// Toggles camera rotation on/off.
    /// </summary>
    public void ToggleRotation()
    {
        rotationEnabled = !rotationEnabled;
    }

    /// <summary>
    /// Resets the camera to look forward.
    /// </summary>
    public void ResetCameraRotation()
    {
        rotationX = 0f;
        // Optional: Reset Y rotation too if desired
        // rotationY = 0f;
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
    }
}