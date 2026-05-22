using UnityEngine;

public class SelectedCharacterSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.14f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int frameIndex;

    public void Configure(Sprite[] animationFrames)
    {
        // This lightweight animator is only used when the selected character does not use the existing AnimatorController.
        frames = animationFrames;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }

        enabled = false;
    }

    public void FreezeToFirstFrame()
    {
        enabled = false;
        timer = 0f;
        frameIndex = 0;

        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer < frameDuration)
        {
            return;
        }

        timer = 0f;
        frameIndex = (frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }
}
