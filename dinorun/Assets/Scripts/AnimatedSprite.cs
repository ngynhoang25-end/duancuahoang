using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSprite : MonoBehaviour
{
    public Sprite[] sprites;
    public float baseFrameRate = 10f;
    private SpriteRenderer spriteRenderer;
    private int frame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Invoke(nameof(Animate), 0f);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Animate()
    {
        if (sprites == null || sprites.Length == 0)
        {
            return;
        }

        frame++;

        if (frame >= sprites.Length) {
            frame = 0;
        }

        if (frame >= 0 && frame < sprites.Length) {
            spriteRenderer.sprite = sprites[frame];
        }

        float frameRate = baseFrameRate;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.State == GameManager.GameState.Playing)
            {
                frameRate = Mathf.Max(baseFrameRate, GameManager.Instance.gameSpeed * 1.5f);
            }
            else if (GameManager.Instance.State == GameManager.GameState.Dead)
            {
                frameRate = baseFrameRate * 0.75f;
            }
        }

        Invoke(nameof(Animate), 1f / Mathf.Max(frameRate, 1f));
    }

}
