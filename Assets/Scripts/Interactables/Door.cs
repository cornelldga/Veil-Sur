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
    [SerializeField] private float openSpeed = 2.5f;

    [Tooltip("Check this if the door opens toward you instead of pushing away")]
    [SerializeField] private bool invertSwingDirection = false;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Coroutine rotateCoroutine;

    private void Awake()
    {
        if (doorSlab != null)
        {
            closedRotation = doorSlab.localRotation;
        }
    }

    public void Interact()
    {
        if (doorSlab == null) return;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }

        if (isOpen)
        {
            // Close the door
            isOpen = false;
            rotateCoroutine = StartCoroutine(AnimateDoor(closedRotation));
        }
        else
        {
            // Open away from the player using transform.right (perpendicular doorway vector)
            isOpen = true;

            Transform playerTransform = GameManager.PlayerInstance.transform;
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

            float dot = Vector3.Dot(transform.right, directionToPlayer);
            float sign = (dot >= 0f) ? 1f : -1f;

            if (invertSwingDirection)
            {
                sign = -sign;
            }

            Quaternion targetRotation = closedRotation * Quaternion.Euler(0f, 0f, openAngle * sign);
            rotateCoroutine = StartCoroutine(AnimateDoor(targetRotation));
        }
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation)
    {
        while (Quaternion.Angle(doorSlab.localRotation, targetRotation) > 0.5f)
        {
            doorSlab.localRotation = Quaternion.Slerp(doorSlab.localRotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }

        doorSlab.localRotation = targetRotation;
        rotateCoroutine = null;
    }
}
