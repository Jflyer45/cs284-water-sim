using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(PointFloatersBuoyantObject))]
public class ShipHealth : MonoBehaviour
{
    [Tooltip("Amount to subtract from buoyancyScale per hit")]
    public float buoyancyReductionAmount = 0.1f;
    [Tooltip("When buoyancyScale ≤ this, force it to zero")]
    public float sinkThreshold = 5;

    PointFloatersBuoyantObject floater;

    void Awake()
    {
        floater = GetComponent<PointFloatersBuoyantObject>();
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Cannonball")) return;

        floater.ReduceBouyancy(buoyancyReductionAmount);

        if (floater.buoyancyStrength <= sinkThreshold)
            floater.buoyancyStrength = 0f;
    }
}
