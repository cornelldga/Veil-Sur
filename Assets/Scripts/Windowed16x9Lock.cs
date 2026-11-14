using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

public static class Windowed16x9Lock
{
    public const int Width = 1920;
    public const int Height = 1080;   // use 1280 x 720 if you want a smaller default window

#if !UNITY_EDITOR
    // Runtime: runs automatically in builds only. Play mode is untouched.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyAtLaunch()
    {
        Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
    }
#endif
}

#if UNITY_EDITOR
// Build time: runs before every build, no matter who makes it
public class Windowed16x9BuildEnforcer : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = Windowed16x9Lock.Width;
        PlayerSettings.defaultScreenHeight = Windowed16x9Lock.Height;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.allowFullscreenSwitch = false;
    }
}
#endif
