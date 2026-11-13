using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, Interactable
{
    [Header("Door Components")]
    [Tooltip("The moving slab child transform")]
    [SerializeField] private Transform doorSlab;

    [Header("Movement Settings")]
    [Tooltip("Magnitude of the swing angle in degrees (e.g. 90)")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float minOpenSpeed = 2.5f;
    [SerializeField] private float maxOpenSpeed = 7.5f;
    [SerializeField] private float minOpenSpeedVelocity = 5f;
    [SerializeField] private float maxOpenSpeedVelocity = 10f;

    [Tooltip("Check this if the door opens toward you instead of pushing away")]
    [SerializeField] private bool invertSwingDirection = false;

    [Header("Auto-Close Settings")]
    [Tooltip("Toggle whether the door automatically closes after opening")]
    [SerializeField] private bool autoClose = true;
    [Tooltip("Seconds to wait before the door begins automatically closing")]
    [SerializeField] private float autoCloseDelay = 8f;
    [SerializeField] private float closeSpeed = 5f;

    [Header("Mutant Interactions")]
    [Tooltip("What layer mutants are on")]
    [SerializeField] private LayerMask mutantLayer;
    [Tooltip("What layer environment objects are on")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("The distance from a mutant a door automatically opens")]
    [SerializeField] private float mutantOpenDistance = 3f;
    [Tooltip("The height at which an obstruction-checking raycast begins")]
    [SerializeField] private float raycastHeight = 1f;
    [Tooltip("TVision blocker collider to prevent dog from seeing thru small gaps between door & walls")]
    [SerializeField] private Collider visionBlocker;
    // Door must open this far before vision through the doorway is allowed.
    [SerializeField] private float visionClearAngle = 10f;

    [Header("SFX")]
    [SerializeField] private AudioClip doorSound;
    

    private Quaternion openRotation;
    private Quaternion closedRotation;
    private Coroutine rotateCoroutine;
    private Coroutine autoCloseCoroutine;
    private bool isOpen = false;
    private bool isForcedOpen = false;    
    private bool isFullyClosed = true;

    private float openSpeed;

    private Transform frameTransform;

    private void Awake()
    {
        if (doorSlab != null)
        {
            closedRotation = doorSlab.localRotation;
        }
    }

    private void Start()
    {
        frameTransform = transform.Find("Frame");
    }

    private void Update()
    {
        DetectMutants();

        if (isForcedOpen)
        {
            CancelAutoClose();
        }
        else if (isOpen &&
                rotateCoroutine == null &&
                autoClose &&
                autoCloseCoroutine == null)
        {
            autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
        }

        if (visionBlocker != null && doorSlab != null)
        {
            float currentAngle = Quaternion.Angle(
                closedRotation,
                doorSlab.localRotation
            );

            visionBlocker.enabled = currentAngle < visionClearAngle;
        }
    }

    private void CancelAutoClose()
    {
        if (autoCloseCoroutine == null) return;

        StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = null;
    }

    public void Interact()
    {
        if (doorSlab == null || isForcedOpen) return;

        // Cancel pending timer if player manually interacts
        CancelAutoClose();

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        if (isOpen)
        {
            // Close the door
            isOpen = false;
            AudioManager.Instance.PlaySfxAtPosition(doorSound, transform.position, 1.5f);
            rotateCoroutine = StartCoroutine(AnimateDoor(closedRotation));
        }
        else
        {
            // Open away from the player using transform.right (perpendicular doorway vector)
            Transform playerTransform = GameManager.PlayerInstance.transform;
            float playerSpeed = playerTransform.gameObject.GetComponent<FirstPersonController>().getSpeed();
            float adjustedSpeed = Mathf.Clamp(playerSpeed, minOpenSpeedVelocity, maxOpenSpeedVelocity);
            openSpeed = 
            ((adjustedSpeed - minOpenSpeedVelocity) 
            / (maxOpenSpeedVelocity - minOpenSpeedVelocity)
            * (maxOpenSpeed - minOpenSpeed)) + minOpenSpeed;

            // Maybe make dogs also create a sound? And have volume based on dist to player?
            AudioManager.Instance.PlaySfxAtPosition(doorSound, transform.position, 1.5f);
            Open(playerTransform);
           
        }
    }

    /// <summary>
    /// Automatically open itself if a mutant is in its vicinity
    /// </summary>
    private void DetectMutants()
    {
        isForcedOpen = false;
        if (doorSlab == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(
            frameTransform.position,
            mutantOpenDistance,
            mutantLayer
        );
        // Debug.Log($"{name}: found {hitColliders.Length} mutant colliders");

        foreach (Collider hitCollider in hitColliders)
        {
            Transform mutant = hitCollider.transform;

            Vector3 startPos = frameTransform.position + Vector3.up * raycastHeight;
            Vector3 targetPos = hitCollider.bounds.center;
            Vector3 toMutant = targetPos - startPos;

            bool blocked = false;

            RaycastHit[] hits = Physics.RaycastAll(
                startPos,
                toMutant.normalized,
                toMutant.magnitude,
                obstacleLayer,
                QueryTriggerInteraction.Ignore
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<Door>() == this)
                    continue;

                // Debug.Log(
                //     $"{name}: detection of {hitCollider.name} " +
                //     $"blocked by {hit.collider.name}",
                //     hit.collider
                // );

                blocked = true;
                break;
            }

            if (blocked) continue;

            isForcedOpen = true;
            CancelAutoClose();

            if (!isOpen)
            {
                if (rotateCoroutine != null)
                {
                    StopCoroutine(rotateCoroutine);
                    rotateCoroutine = null;
                }
                float mutantSpeed = mutant.gameObject.GetComponent<EnemyMover>().getSpeed();
                float adjustedSpeed = Mathf.Clamp(mutantSpeed, minOpenSpeedVelocity, maxOpenSpeedVelocity);
                openSpeed = 
                ((adjustedSpeed - minOpenSpeedVelocity) 
                / (maxOpenSpeedVelocity - minOpenSpeedVelocity)
                * (maxOpenSpeed - minOpenSpeed)) + minOpenSpeed;
                Open(mutant);
            }

            return;
        }
    }

    /// <summary>
    /// Open the door away from entity.
    /// </summary>
    private void Open(Transform entity)
    {
        // Choose a direction only when opening from fully closed.
        if (isFullyClosed)
        {
            Vector3 directionToEntity = entity.position - transform.position;

            float dot = Vector3.Dot(transform.right, directionToEntity);
            float sign = dot >= 0f ? 1f : -1f;

            if (invertSwingDirection)
                sign = -sign;

            openRotation = closedRotation *
                Quaternion.Euler(0f, 0f, openAngle * sign);
        }

        isOpen = true;
        isFullyClosed = false;

        rotateCoroutine = StartCoroutine(AnimateDoor(openRotation));
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        autoCloseCoroutine = null;

        if (isForcedOpen || !isOpen || doorSlab == null)
            yield break;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        isOpen = false;
        rotateCoroutine = StartCoroutine(AnimateDoor(closedRotation));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation)
    {
        // Ensure StartCoroutine assigns its handle before this
        // routine can finish and clear that handle.
        yield return null;
        float rotationSpeed = targetRotation == openRotation ? openSpeed : closeSpeed;

        while (Quaternion.Angle(
            doorSlab.localRotation,
            targetRotation) > 0.5f)
        {
            doorSlab.localRotation = Quaternion.Slerp(
                doorSlab.localRotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );

            yield return null;
        }

        doorSlab.localRotation = targetRotation;

        if (!isOpen)
            isFullyClosed = true;

        rotateCoroutine = null;
    }
}
