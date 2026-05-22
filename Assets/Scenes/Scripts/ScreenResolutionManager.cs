using UnityEngine;

public class ScreenResolutionManager : MonoBehaviour
{
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;

    private static ScreenResolutionManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstScene()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(ScreenResolutionManager));
        instance = managerObject.AddComponent<ScreenResolutionManager>();
        DontDestroyOnLoad(managerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ApplyPcResolution();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private static void ApplyPcResolution()
    {
        Application.targetFrameRate = 60;

        if (Screen.fullScreenMode != FullScreenMode.FullScreenWindow ||
            Screen.width != DefaultWidth ||
            Screen.height != DefaultHeight)
        {
            Screen.SetResolution(DefaultWidth, DefaultHeight, FullScreenMode.FullScreenWindow);
        }
    }
}
