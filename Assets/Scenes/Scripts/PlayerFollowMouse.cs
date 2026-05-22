using UnityEngine;

public class PlayerFollowMouse : MonoBehaviour
{
    public float speed = 5f;
    public bool canMove = true;
    public bool enableAnimatorOnMove = true;
    public bool constrainToPaintArea = true;
    public Vector2 paintAreaMin = new Vector2(-32.24f, 5.05f);
    public Vector2 paintAreaMax = new Vector2(37.1f, 7.45f);

    private Animator animator;
    private SelectedCharacterSpriteAnimator selectedCharacterSpriteAnimator;
    private bool hasStartedMoving;

    void Awake()
    {
        animator = GetComponent<Animator>();
        selectedCharacterSpriteAnimator = GetComponent<SelectedCharacterSpriteAnimator>();

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    void OnEnable()
    {
        CursorInputRouter.Instance.Held += HandleCursorHeld;
    }

    void OnDisable()
    {
        if (!CursorInputRouter.HasInstance)
        {
            return;
        }

        CursorInputRouter.Instance.Held -= HandleCursorHeld;
    }

    void HandleCursorHeld(Vector3 worldPosition)
    {
        if (PauseMenuUI.IsPaused || !canMove || !StartIntroAnimator.GameStarted || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            return;
        }

        if (!hasStartedMoving)
        {
            hasStartedMoving = true;

            if (animator != null && enableAnimatorOnMove)
            {
                animator.enabled = true;
            }

            if (selectedCharacterSpriteAnimator == null)
            {
                selectedCharacterSpriteAnimator = GetComponent<SelectedCharacterSpriteAnimator>();
            }

            if (selectedCharacterSpriteAnimator != null)
            {
                selectedCharacterSpriteAnimator.enabled = true;
            }
        }

        worldPosition.z = 0f;
        Vector2 targetPosition = constrainToPaintArea ? ClampToPaintArea(worldPosition) : (Vector2)worldPosition;
        Vector2 nextPosition = Vector2.Lerp(transform.position, targetPosition, speed * Time.deltaTime);
        transform.position = constrainToPaintArea ? ClampToPaintArea(nextPosition) : nextPosition;
    }

    private Vector2 ClampToPaintArea(Vector2 position)
    {
        // The player is moved directly by script, so clamping keeps them inside the paintable lane reliably.
        return new Vector2(
            Mathf.Clamp(position.x, paintAreaMin.x, paintAreaMax.x),
            Mathf.Clamp(position.y, paintAreaMin.y, paintAreaMax.y)
        );
    }
}
