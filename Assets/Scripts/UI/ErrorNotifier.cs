using System;

/// <summary>
/// show error msg
/// any script can call ErrorNotifier.Show("message"); the ErrorBox displays it.
/// </summary>
public static class ErrorNotifier
{
    public static event Action<string> OnError;

    /// <summary>
    /// shows error box 
    /// </summary>
    /// <param name="message">text to display</param>
    public static void Show(string message)
    {
        OnError?.Invoke(message);
    }
}
