using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;

    float verticalLookRotation;
    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // mouse look
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mx);

        verticalLookRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        Camera.main.transform.localEulerAngles = Vector3.right * verticalLookRotation;

        // movement
        Vector3 dir = transform.forward * Input.GetAxis("Vertical")
                    + transform.right * Input.GetAxis("Horizontal");
        cc.SimpleMove(dir * speed);
    }
}
