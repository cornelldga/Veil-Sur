using UnityEngine;

/// <summary>
/// Window from Level One. Opens after the Save Terminal is interacted with.
/// </summary>
public class Window : MonoBehaviour, Interactable
{
    [Tooltip("Pivot object at the top edge of the window which the actual guard is a child of.")]
    [SerializeField] private Transform guard;

    [Tooltip("Y scale the guard shrinks to open opening.")]
    [SerializeField] private float openScaleY = 0.1f;

    [Tooltip("How fast the guard rolls up.")]
    [SerializeField] private float speed = 1f;

    private Vector3 openScale;
    private bool isOpen;

    private void Start()
    {
        openScale = new Vector3(guard.localScale.x, openScaleY, guard.localScale.z);
    }

    public void Interact()
    {
        if (isOpen)
        {
            return;
        }

        OpenWindow();
    }

    private void OpenWindow()
    {
        isOpen = true;
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        // Window guard opens by squishing based on the position of the guard "pivot" object.
        guard.localScale = Vector3.MoveTowards(guard.localScale, openScale, speed * Time.deltaTime);
    }
}