using System;
using UnityEngine;

public class CursorInputRouter : MonoBehaviour
{
    private struct PointerSample
    {
        public bool IsPressed;
        public bool PressedThisFrame;
        public bool ReleasedThisFrame;
        public bool HasPosition;
        public Vector2 ScreenPosition;
    }

    private static CursorInputRouter instance;

    [SerializeField] private Camera worldCamera;

    public static bool HasInstance => instance != null;

    public static CursorInputRouter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CursorInputRouter>();

                if (instance == null)
                {
                    GameObject routerObject = new GameObject(nameof(CursorInputRouter));
                    instance = routerObject.AddComponent<CursorInputRouter>();
                }
            }

            return instance;
        }
    }

    public event Action<Vector3> Pressed;
    public event Action<Vector3> Held;
    public event Action<Vector3> Released;

    public bool IsPressed { get; private set; }
    public Vector3 CurrentWorldPosition { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        Instance.Initialize();
    }

    void Awake()
    {
        Initialize();
    }

    void Update()
    {
        PointerSample pointer;

        if (!TryReadPointer(out pointer))
        {
            return;
        }

        if (pointer.HasPosition)
        {
            CurrentWorldPosition = ScreenToWorld(pointer.ScreenPosition);
        }

        if (pointer.PressedThisFrame && !IsPressed)
        {
            IsPressed = true;

            if (Pressed != null)
                Pressed.Invoke(CurrentWorldPosition);
        }

        if (pointer.IsPressed && IsPressed)
        {
            if (Held != null)
                Held.Invoke(CurrentWorldPosition);
        }

        if (pointer.ReleasedThisFrame && IsPressed)
        {
            ForceRelease();
        }
    }

    public void ForceRelease()
    {
        if (!IsPressed)
        {
            return;
        }

        // Finishing the game can end painting before the physical release happens.
        IsPressed = false;

        if (Released != null)
            Released.Invoke(CurrentWorldPosition);
    }

    private void Initialize()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool TryReadPointer(out PointerSample pointer)
    {
        pointer = default(PointerSample);

        // One polling point keeps mouse and touch handling in one place while
        // the rest of the game reacts to press / hold / release events.
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer.HasPosition = true;
            pointer.ScreenPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    pointer.IsPressed = true;
                    pointer.PressedThisFrame = true;
                    return true;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    pointer.IsPressed = true;
                    return true;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    pointer.ReleasedThisFrame = true;
                    return true;
            }
        }

        bool mouseDown = Input.GetMouseButtonDown(0);
        bool mouseHeld = Input.GetMouseButton(0);
        bool mouseUp = Input.GetMouseButtonUp(0);

        if (!mouseDown && !mouseHeld && !mouseUp)
        {
            return false;
        }

        pointer.HasPosition = true;
        pointer.ScreenPosition = Input.mousePosition;
        pointer.IsPressed = mouseHeld;
        pointer.PressedThisFrame = mouseDown;
        pointer.ReleasedThisFrame = mouseUp;
        return true;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        Camera activeCamera = ResolveCamera();

        if (activeCamera == null)
        {
            return Vector3.zero;
        }

        float distanceToCamera = Mathf.Abs(activeCamera.transform.position.z);
        Vector3 worldPosition = activeCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceToCamera));
        worldPosition.z = 0f;
        return worldPosition;
    }

    private Camera ResolveCamera()
    {
        if (worldCamera != null)
        {
            return worldCamera;
        }

        if (Camera.main != null)
        {
            return Camera.main;
        }

        return FindObjectOfType<Camera>();
    }
}
