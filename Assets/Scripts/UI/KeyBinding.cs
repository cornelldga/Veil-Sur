using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Put this on a Button in the Settings screen. Clicking it greys out the button and shows
/// "Waiting for input..."; the next key pressed becomes the binding. If another row in Settings
/// already used that key, that row is left with no key.
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

    [Header("While waiting for input")]
    [SerializeField] private string waitingText = "Waiting for input...";
    [SerializeField] private Color waitingColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField] private float waitingFontSize = 18f;
    private float normalFontSize;

    // Every row currently on screen. Used to find rows that share a key and to refresh all labels.
    private static readonly List<KeyBinding> Rows = new List<KeyBinding>();

    private Button button;
    private Image buttonImage;
    private Color normalColor = Color.white;
    private InputAction action;
    private InputActionRebindingExtensions.RebindingOperation rebindOp;
    private bool actionWasEnabled;
    private readonly List<Selectable> lockedControls = new List<Selectable>();

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = button.image;
        normalFontSize = keyLabel.fontSize;
        if (buttonImage != null) normalColor = buttonImage.color;
    }

    private void OnEnable()
    {
        // The reference points at the project asset. The game uses GameManager's shared copy,
        // so find the same action (by id) in that copy.
        action = GameManager.Controls.asset.FindAction(actionReference.action.id.ToString());

        Rows.Add(this);
        button.onClick.AddListener(StartRebind);
        RefreshLabel();
    }

    private void OnDisable()
    {
        Rows.Remove(this);
        button.onClick.RemoveListener(StartRebind);

        // Screen closed mid-wait: undo everything so the action isn't left disabled.
        bool wasRebinding = rebindOp != null;
        EndOperation();
        if (wasRebinding) GameManager.IsRebinding = false;
    }

    private void StartRebind()
    {
        if (rebindOp != null || GameManager.IsRebinding) return;

        GameManager.IsRebinding = true;
        ShowWaiting(true);
        LockOtherControls(true);

        // An action must be disabled while it is rebound. Remember if it was on so we can restore it.
        actionWasEnabled = action.enabled;
        if (actionWasEnabled) action.Disable();

        var op = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithControlsExcluding("<Keyboard>/anyKey")  // otherwise a blocked key falls back to "any key"
            .WithCancelingThrough("<Keyboard>/escape")   // Esc cancels
            .OnMatchWaitForAnother(0.1f)                 // avoids catching the click that started it
            .OnComplete(o => Finish(applied: true))
            .OnCancel(o => Finish(applied: false));

        // Keys used by actions that are NOT in Settings (movement, look, notebook, aim...) can't be
        // chosen, so a player can't unbind something they have no way to rebind.
        foreach (string path in ProtectedPaths())
            op.WithControlsExcluding(path);

        rebindOp = op.Start();
    }

    private void Finish(bool applied)
    {
        EndOperation();

        if (applied)
        {
            UnbindOtherRowsUsingSameKey();
            GameManager.SaveBindings();
        }
        RefreshAll();

        // Esc is also the pause key in GameManager. Keep the flag up for one more frame so the
        // same Esc press that cancelled the rebind doesn't also toggle pause.
        StartCoroutine(EndRebindNextFrame());
    }

    /// <summary>
    /// Disposes the rebind, restores the action, the button's look, and the other controls.
    /// </summary>
    private void EndOperation()
    {
        bool wasRebinding = rebindOp != null;
        rebindOp?.Dispose();
        rebindOp = null;

        if (wasRebinding && actionWasEnabled) action.Enable();

        ShowWaiting(false);
        LockOtherControls(false);
    }

    private void ShowWaiting(bool waiting)
    {
        if (buttonImage != null) buttonImage.color = waiting ? waitingColor : normalColor;
        keyLabel.fontSize = waiting ? waitingFontSize : normalFontSize;
        if (waiting) keyLabel.text = waitingText;
    }

    /// <summary>
    /// While one row waits, grey out and disable every other control on the Settings screen
    /// so nothing else can be clicked (including starting a second rebind).
    /// </summary>
    private void LockOtherControls(bool locked)
    {
        if (locked)
        {
            Settings settings = GetComponentInParent<Settings>(true);
            Transform scope = settings != null ? settings.transform : transform.root;

            foreach (Selectable s in scope.GetComponentsInChildren<Selectable>(true))
            {
                if (s == button || !s.interactable) continue;
                s.interactable = false;
                lockedControls.Add(s);
            }
        }
        else
        {
            foreach (Selectable s in lockedControls)
                if (s != null) s.interactable = true;
            lockedControls.Clear();
        }
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

    private static IEnumerable<string> ProtectedPaths()
    {
        foreach (InputAction other in GameManager.Controls.asset)
        {
            for (int i = 0; i < other.bindings.Count; i++)
            {
                InputBinding b = other.bindings[i];
                if (b.isComposite || HasRow(other, i)) continue;   // composite container, or has a row
                string path = b.effectivePath;
                if (!string.IsNullOrEmpty(path)) yield return path;
            }
        }
    }

    private static bool HasRow(InputAction a, int index)
    {
        foreach (KeyBinding row in Rows)
            if (row.action == a && row.bindingIndex == index) return true;
        return false;
    }

    private IEnumerator EndRebindNextFrame()
    {
        yield return null;
        GameManager.IsRebinding = false;
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
}