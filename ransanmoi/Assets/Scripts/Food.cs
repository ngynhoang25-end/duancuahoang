using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Food : MonoBehaviour
{
    public enum FoodType
    {
        Normal,
        SpeedBoost,
        Slow
    }

    public Collider2D gridArea;
    public SpriteRenderer spriteRenderer;

    public FoodType currentType = FoodType.Normal;
    public Color normalColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color speedBoostColor = new Color(0.25f, 0.9f, 1f, 1f);
    public Color slowColor = new Color(1f, 0.35f, 0.35f, 1f);
    [Range(0f, 1f)] public float speedBoostChance = 0.15f;
    [Range(0f, 1f)] public float slowChance = 0.15f;

    private Snake snake;
    private GameController gameController;
    private BoxCollider2D foodCollider;

    private void Awake()
    {
        snake = FindAnyObjectByType<Snake>();
        gameController = FindAnyObjectByType<GameController>();
        foodCollider = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        RandomizePosition();
    }

    public void RandomizePosition()
    {
        if (gridArea == null)
        {
            return;
        }

        Bounds bounds = gridArea.bounds;
        ChooseFoodType();

        int minX = Mathf.RoundToInt(bounds.min.x);
        int maxX = Mathf.RoundToInt(bounds.max.x);
        int minY = Mathf.RoundToInt(bounds.min.y);
        int maxY = Mathf.RoundToInt(bounds.max.y);
        int x = minX;
        int y = minY;

        bool foundPosition = false;
        int attempts = Mathf.Max(1, (maxX - minX + 1) * (maxY - minY + 1));

        for (int i = 0; i < attempts; i++)
        {
            x = Random.Range(minX, maxX + 1);
            y = Random.Range(minY, maxY + 1);

            if (!IsBlocked(x, y))
            {
                foundPosition = true;
                break;
            }
        }

        if (!foundPosition)
        {
            for (int row = minY; row <= maxY && !foundPosition; row++)
            {
                for (int column = minX; column <= maxX; column++)
                {
                    if (!IsBlocked(column, row))
                    {
                        x = column;
                        y = row;
                        foundPosition = true;
                        break;
                    }
                }
            }
        }

        transform.position = new Vector2(x, y);
        ApplyVisuals();
    }

    public bool Occupies(int x, int y)
    {
        return Mathf.RoundToInt(transform.position.x) == x &&
               Mathf.RoundToInt(transform.position.y) == y;
    }

    private bool IsBlocked(int x, int y)
    {
        if (snake != null && snake.Occupies(x, y))
        {
            return true;
        }

        if (gameController != null && gameController.IsCellBlocked(x, y, this))
        {
            return true;
        }

        return false;
    }

    private void ChooseFoodType()
    {
        float roll = Random.value;

        if (roll < speedBoostChance)
        {
            currentType = FoodType.SpeedBoost;
        }
        else if (roll < speedBoostChance + slowChance)
        {
            currentType = FoodType.Slow;
        }
        else
        {
            currentType = FoodType.Normal;
        }
    }

    private void ApplyVisuals()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (currentType)
        {
            case FoodType.SpeedBoost:
                spriteRenderer.color = speedBoostColor;
                break;
            case FoodType.Slow:
                spriteRenderer.color = slowColor;
                break;
            default:
                spriteRenderer.color = normalColor;
                break;
        }
    }

}
