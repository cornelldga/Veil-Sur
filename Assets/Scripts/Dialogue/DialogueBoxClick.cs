using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// makes dialogue go to next line when clicked
/// </summary>
public class DialogueBoxClick : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// happens when the dialogue box is clicked
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        DialogueManager.Instance.NextLine();
    }
}