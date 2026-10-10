using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Put this on a Button in the Settings screen. Each click moves one binding of one action
/// to the next key in the Options list, wrapping around at the end.
/// </summary>
[RequireComponent(typeof(Button))]
public class KeyBinding : MonoBehaviour
{
    [Tooltip("Drag the action from your PlayerControls .inputactions asset (e.g. PlayerMovement/Sprint).")]
    [SerializeField] private InputActionReference actionReference;
 
    [Tooltip("Which binding on that action. Use 0 for most actions.")]
    [SerializeField] private int bindingIndex = 0;
 
    [Tooltip("Text that shows the current key.")]
    [SerializeField] private TMP_Text keyLabel;
 
    [Tooltip("Keys to cycle through, in order. Include the action's default key, e.g. <Keyboard>/leftShift")]
    [SerializeField] private string[] options;
 
    private Button button;
    private InputAction action;
 
    private void Awake()
    {
        button = GetComponent<Button>();
    }
 
    private void OnEnable()
    {
        // The reference points at the project asset. The game uses GameManager's shared copy,
        // so find the same action (by id) in that copy.
        action = GameManager.Controls.asset.FindAction(actionReference.action.id.ToString());
        button.onClick.AddListener(CycleToNext);
        RefreshLabel();
    }
 
    private void OnDisable()
    {
        button.onClick.RemoveListener(CycleToNext);
    }
 
    private void CycleToNext()
    {
        if (options == null || options.Length == 0) return;
 
        string current = action.bindings[bindingIndex].effectivePath;
 
        // Find where we are in the list. If the current key isn't in it, start from the first option.
        int currentIndex = -1;
        for (int i = 0; i < options.Length; i++)
        {
            if (string.Equals(options[i], current, StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = i;
                break;
            }
        }
 
        string next = options[(currentIndex + 1) % options.Length];
 
        action.ApplyBindingOverride(bindingIndex, next);
        GameManager.SaveBindings();
        RefreshLabel();
    }
 
    public void RefreshLabel()
    {
        keyLabel.text = InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
}