using UnityEngine;
using System.Collections;

public class Cannon : MonoBehaviour
{
    public GameObject cannonBallPrefab;
    public GameObject smokePrefab;  // New smoke prefab variable
    public Transform spawnPoint;
    public Transform aimPoint;
    public float muzzleVelocity = 30f;
    public float maxDelay = 1f;  // random delay between 0 and maxDelay

    // Recoil parameters
    public float recoilDistance = 0.5f;  // How far the cannon moves back
    public float recoilSpeed = 5f;       // How quickly the cannon recoils
    public float resetMinTime = 0.5f;    // Minimum time before resetting
    public float resetMaxTime = 2f;      // Maximum time before resetting
    public float resetSpeed = 2f;        // How quickly the cannon resets

    // Reference to particle system
    public ParticleSystem fireEffect;

    private Vector3 originalLocalPosition;  // Store the original local position
    private bool isResetting = false;

    void Start()
    {
        // Store the initial local position of the cannon
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isResetting)
        {
            float delay = Random.Range(0f, maxDelay);
            StartCoroutine(FireWithDelay(delay));
        }
    }

    IEnumerator FireWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Get the firing direction from spawnPoint to aimPoint
        Vector3 firingDirection = aimPoint.position - spawnPoint.position;
        Fire(firingDirection);

        // Start the recoil animation
        StartCoroutine(RecoilAndReset());
    }

    IEnumerator RecoilAndReset()
    {
        isResetting = true;

        // Calculate the actual firing direction
        Vector3 firingDirection = (aimPoint.position - spawnPoint.position).normalized;

        // Get the exact opposite direction for recoil (backward direction)
        Vector3 recoilDirection = -firingDirection;

        // Calculate recoil target position in world space
        Vector3 recoilTargetWorld = transform.position + (recoilDirection * recoilDistance);

        // Convert target to local space
        Vector3 recoilTargetLocal = transform.parent ?
            transform.parent.InverseTransformPoint(recoilTargetWorld) :
            recoilTargetWorld;

        Vector3 startPosition = transform.localPosition;

        // Quick backward jolt
        float elapsed = 0;
        while (elapsed < 1 / recoilSpeed)
        {
            transform.localPosition = Vector3.Lerp(startPosition, recoilTargetLocal, elapsed * recoilSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we reached the full recoil position
        transform.localPosition = recoilTargetLocal;

        // Random wait time before resetting
        float resetDelay = Random.Range(resetMinTime, resetMaxTime);
        yield return new WaitForSeconds(resetDelay);

        // Smooth reset to original position
        elapsed = 0;
        Vector3 recoilPosition = transform.localPosition;

        while (elapsed < 1 / resetSpeed)
        {
            transform.localPosition = Vector3.Lerp(recoilPosition, originalLocalPosition, elapsed * resetSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we're back at the EXACT original position
        transform.localPosition = originalLocalPosition;

        isResetting = false;
    }

    public void Fire(Vector3 direction)
    {
        if (!cannonBallPrefab || !spawnPoint) return;

        // Play particle effect if assigned
        if (fireEffect != null)
        {
            fireEffect.Play();
        }
        else
        {
            Debug.LogWarning("Fire effect not assigned to cannon.");
        }

        // Instantiate the cannonball
        var ball = Instantiate(cannonBallPrefab, spawnPoint.position, Quaternion.LookRotation(direction));
        var rb = ball.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = direction.normalized * muzzleVelocity;

        // Instantiate smoke in world space
        if (smokePrefab != null)
        {
            // Create smoke at the spawn point in world space
            // By not specifying a parent, the smoke stays in world space and won't follow the ship
            Instantiate(smokePrefab, spawnPoint.position, Quaternion.LookRotation(direction));
        }
        else
        {
            Debug.LogWarning("Smoke prefab not assigned to cannon.");
        }
    }
}