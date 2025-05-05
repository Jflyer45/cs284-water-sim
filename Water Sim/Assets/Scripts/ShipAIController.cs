// ShipAIController.cs
using UnityEngine;

[RequireComponent(typeof(ShipMovement))]
public class ShipAIController : MonoBehaviour
{
    [Tooltip("How often (in seconds) the AI picks a new direction")]
    public float changeDirInterval = 5f;

    [Header("Forward Bias")]
    [Tooltip("Minimum forward thrust (0–1)")]
    [Range(0f, 1f)] public float minForward = 0.8f;
    [Tooltip("Maximum forward thrust (0–1)")]
    [Range(0f, 1f)] public float maxForward = 1f;

    [Header("Lateral & Turn Bias")]
    [Tooltip("Max strafe magnitude")]
    [Range(0f, 1f)] public float maxStrafe = 0.2f;
    [Tooltip("Max turn magnitude")]
    [Range(0f, 1f)] public float maxTurn = 0.3f;

    ShipMovement sm;
    float timer;

    void Awake()
    {
        sm = GetComponent<ShipMovement>();
        sm.useAIInput = true;
        sm.SetMovementEnabled(true);
        PickNewDirection();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= changeDirInterval)
        {
            timer = 0f;
            PickNewDirection();
        }
    }

    void PickNewDirection()
    {
        // Bias so the boat almost always moves forward
        sm.aiForwardInput = Random.Range(minForward, maxForward);
        // Small random strafing and turning
        sm.aiStrafeInput = Random.Range(-maxStrafe, maxStrafe);
        sm.aiTurnInput = Random.Range(-maxTurn, maxTurn);
    }
}
