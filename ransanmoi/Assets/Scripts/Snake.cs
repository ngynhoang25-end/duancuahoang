using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 20f;
    public float speedMultiplier = 1f;
    public int initialSize = 3;
    public bool moveThroughWalls = false;
    public float speedBoostMultiplier = 1.35f;
    public float slowMultiplier = 0.75f;
    public float speedBoostDuration = 5f;
    public float slowDuration = 5f;

    private List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private GameController gameController;
    private float speedBoostEndTime;
    private float slowEndTime;
    private Vector2 swipeStartPosition;
    private bool swipeTracking;

    private float nextUpdate;

    private void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        ResetState();
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleTouchInput();

        if (gameController != null && !gameController.IsGameplayActive)
        {
            return;
        }

        UpdateTemporaryEffects();
    }

    private void HandleKeyboardInput()
    {
        if (direction.x != 0f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                input = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                input = Vector2Int.down;
            }
        }
        else if (direction.y != 0f)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                input = Vector2Int.right;
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                input = Vector2Int.left;
            }
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                swipeStartPosition = touch.position;
                swipeTracking = true;
            }
            else if (swipeTracking && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                ResolveSwipe(touch.position);
                swipeTracking = false;
            }
            else if (swipeTracking && touch.phase == TouchPhase.Moved)
            {
                ResolveSwipe(touch.position);
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPosition = Input.mousePosition;
            swipeTracking = true;
        }
        else if (swipeTracking && Input.GetMouseButtonUp(0))
        {
            ResolveSwipe(Input.mousePosition);
            swipeTracking = false;
        }
    }

    private void ResolveSwipe(Vector2 currentPosition)
    {
        Vector2 delta = currentPosition - swipeStartPosition;

        if (delta.magnitude < 60f)
        {
            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0f && direction.x == 0f)
            {
                input = Vector2Int.right;
            }
            else if (delta.x < 0f && direction.x == 0f)
            {
                input = Vector2Int.left;
            }
        }
        else
        {
            if (delta.y > 0f && direction.y == 0f)
            {
                input = Vector2Int.up;
            }
            else if (delta.y < 0f && direction.y == 0f)
            {
                input = Vector2Int.down;
            }
        }
    }

    private void UpdateTemporaryEffects()
    {
        float effectMultiplier = 1f;

        if (Time.time < speedBoostEndTime)
        {
            effectMultiplier *= speedBoostMultiplier;
        }

        if (Time.time < slowEndTime)
        {
            effectMultiplier *= slowMultiplier;
        }

        speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        temporarySpeedMultiplier = effectMultiplier;
    }

    private void FixedUpdate()
    {
        if (gameController != null && !gameController.IsGameplayActive)
        {
            return;
        }

        if (Time.time < nextUpdate)
        {
            return;
        }

        if (input != Vector2Int.zero)
        {
            direction = input;
        }

        for (int i = segments.Count - 1; i > 0; i--)
        {
            segments[i].position = segments[i - 1].position;
        }

        int x = Mathf.RoundToInt(transform.position.x) + direction.x;
        int y = Mathf.RoundToInt(transform.position.y) + direction.y;
        transform.position = new Vector2(x, y);

        nextUpdate = Time.time + (1f / (speed * speedMultiplier * temporarySpeedMultiplier));
    }

    private float temporarySpeedMultiplier = 1f;

    public void Grow()
    {
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);
    }

    public void ResetState()
    {
        direction = Vector2Int.right;
        transform.position = Vector3.zero;
        input = Vector2Int.zero;
        speedBoostEndTime = 0f;
        slowEndTime = 0f;
        temporarySpeedMultiplier = 1f;

        for (int i = 1; i < segments.Count; i++)
        {
            Destroy(segments[i].gameObject);
        }

        segments.Clear();
        segments.Add(transform);

        for (int i = 0; i < initialSize - 1; i++)
        {
            Grow();
        }
    }

    public bool Occupies(int x, int y)
    {
        foreach (Transform segment in segments)
        {
            if (Mathf.RoundToInt(segment.position.x) == x &&
                Mathf.RoundToInt(segment.position.y) == y)
            {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            if (gameController != null)
            {
                Food food = other.GetComponent<Food>();

                if (food != null)
                {
                    gameController.ConsumeFood(food);
                }
            }
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            if (gameController != null)
            {
                gameController.TriggerGameOver();
            }
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (gameController != null)
            {
                gameController.TriggerGameOver();
            }
        }
    }

    public void ApplyFoodEffect(Food.FoodType foodType)
    {
        if (foodType == Food.FoodType.SpeedBoost)
        {
            speedBoostEndTime = Time.time + speedBoostDuration;
        }
        else if (foodType == Food.FoodType.Slow)
        {
            slowEndTime = Time.time + slowDuration;
        }
    }

    private void Traverse(Transform wall)
    {
        Vector3 position = transform.position;

        if (direction.x != 0f)
        {
            position.x = Mathf.RoundToInt(-wall.position.x + direction.x);
        }
        else if (direction.y != 0f)
        {
            position.y = Mathf.RoundToInt(-wall.position.y + direction.y);
        }

        transform.position = position;
    }
}
