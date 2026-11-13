using UnityEngine;

/// <summary>
/// Plug that must be interacted with in order to restore power to the printer.
/// </summary>
public class PrinterPlug : MonoBehaviour, Interactable
{
    [Tooltip("The printer for this plug.")]
    [SerializeField] private Printer printer;

    [Tooltip("Where the plug ends up when plugged in.")]
    [SerializeField] private Transform pluggedInPos;

    [Tooltip("Sound that plays when the plug is plugged in.")]
    [SerializeField] private AudioClip pluggedInSound;
    
    private bool isPluggedIn;

    /// <summary>
    /// Called upon interaction with the plug. If already plugged in, does nothing.
    /// </summary>
    public void Interact()
    {
        if (isPluggedIn)
        {
            return;
        }

        isPluggedIn = true;
        transform.SetPositionAndRotation(pluggedInPos.position, pluggedInPos.rotation);

        if (pluggedInSound != null)
        {
            AudioManager.Instance.PlaySfxAtPosition(pluggedInSound, transform.position);
        }

        printer.TurnOn();
    }
}