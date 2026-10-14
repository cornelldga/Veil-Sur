using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Put this on a Button in the Settings screen. Clicking it shows a "waiting for input"
/// overlay; the next key pressed becomes the binding. If another row in Settings already
/// used that key, that row is left with no key.
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
 
    [Tooltip("The grey 'Waiting for input' panel. Use the same object on every row. Leave it inactive.")]
    [SerializeField] private GameObject waitingOverlay;
 
    // Every row currently on screen. Used to find rows that share a key and to refresh all labels.
    private static readonly List<KeyBinding> Rows = new List<KeyBinding>();
 
    private Button button;
    private InputAction action;
    private InputActionRebindingExtensions.RebindingOperation rebindOp;
 
    private void Awake()
    {
        button = GetComponent<Button>();
    }
 
    private void OnEnable()
    {
        // The reference points at the project asset. The game uses GameManager's shared copy,
        // so find the same action (by id) in that copy.
        action = GameManager.Controls.asset.FindAction(actionReference.action.id.ToString());
 
        Rows.Add(this);
        button.onClick.AddListener(StartRebind);
        if (waitingOverlay != null) waitingOverlay.SetActive(false);
        RefreshLabel();
    }
 
    private void OnDisable()
    {
        Rows.Remove(this);
        button.onClick.RemoveListener(StartRebind);
        CancelOperation();
        if (waitingOverlay != null) waitingOverlay.SetActive(false);
        GameManager.IsRebinding = false;
    }
 
    private void StartRebind()
    {
        if (rebindOp != null) return;
 
        GameManager.IsRebinding = true;
        if (waitingOverlay != null) waitingOverlay.SetActive(true);
 
        // An action must be disabled while it is rebound. Remember if it was on so we can restore it.
        bool wasEnabled = action.enabled;
        if (wasEnabled) action.Disable();
 
        var op = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithControlsExcluding("<Keyboard>/anyKey") 
            .WithCancelingThrough("<Keyboard>/escape")   // Esc cancels
            .OnMatchWaitForAnother(0.1f)                 // avoids catching the click that started it
            .OnComplete(o => Finish(wasEnabled, applied: true))
            .OnCancel(o => Finish(wasEnabled, applied: false));
 
        // Keys used by actions that are NOT in Settings (movement, look, notebook, aim...) can't be
        // chosen, so a player can't unbind something they have no way to rebind.
        foreach (string path in ProtectedPaths())
            op.WithControlsExcluding(path);
 
        rebindOp = op.Start();
    }
 
    private void Finish(bool wasEnabled, bool applied)
    {
        CancelOperation();
 
        if (applied) UnbindOtherRowsUsingSameKey();
 
        if (wasEnabled) action.Enable();
        if (waitingOverlay != null) waitingOverlay.SetActive(false);
 
        if (applied) GameManager.SaveBindings();
        RefreshAll();
 
        // Esc is also the pause key in GameManager. Keep the flag up for one more frame so the
        // same Esc press that cancelled the rebind doesn't also toggle pause.
        StartCoroutine(EndRebindNextFrame());
    }
 
    /// <summary>
    /// If another row already had the key we just picked, give that row an empty binding.
    /// </summary>
    private void UnbindOtherRowsUsingSameKey()
    {
        string newPath = action.bindings[bindingIndex].effectivePath;
        if (string.IsNullOrEmpty(newPath)) return;
 
        foreach (KeyBinding row in Rows)
        {
            if (row == this) continue;
 
            string otherPath = row.action.bindings[row.bindingIndex].effectivePath;
            if (string.Equals(otherPath, newPath, StringComparison.OrdinalIgnoreCase))
                row.action.ApplyBindingOverride(row.bindingIndex, string.Empty);
        }
    }
 
    /// <summary>
    /// Paths of every binding that belongs to an action without a row in Settings.
    /// </summary>
    private static IEnumerable<string> ProtectedPaths()
    {
        foreach (InputAction other in GameManager.Controls.asset)
        {
            if (HasRow(other)) continue;
 
            foreach (InputBinding b in other.bindings)
            {
                if (b.isComposite) continue;   // the container itself has no key; its parts do
                string path = b.effectivePath;
                if (!string.IsNullOrEmpty(path)) yield return path;
            }
        }
    }
 
    private static bool HasRow(InputAction a)
    {
        foreach (KeyBinding row in Rows)
            if (row.action == a) return true;
        return false;
    }
 
    private IEnumerator EndRebindNextFrame()
    {
        yield return null;
        GameManager.IsRebinding = false;
    }
 
    private void CancelOperation()
    {
        rebindOp?.Dispose();
        rebindOp = null;
    }
 
    public static void RefreshAll()
    {
        foreach (KeyBinding row in Rows)
            row.RefreshLabel();
    }
 
    public void RefreshLabel()
    {
        string path = action.bindings[bindingIndex].effectivePath;
        keyLabel.text = string.IsNullOrEmpty(path)
            ? "None"
            : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void ResetKeyBindings()
    {
        GameManager.ResetBindings();
        KeyBinding.RefreshAll();
    }
}