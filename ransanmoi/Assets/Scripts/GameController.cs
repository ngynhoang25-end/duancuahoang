using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const string HighScoreKey = "SnakeHighScore";
    private const string SoundKey = "SnakeSoundEnabled";
    private const string DifficultyKey = "SnakeDifficulty";

    private enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Settings
    }

    private enum DifficultyPreset
    {
        Easy,
        Normal,
        Hard
    }

    public Button playButton;
    public Button pauseButton;
    public Button restartButton;

    public TMP_Text scoreText;

    public int pointsPerLevel = 30;
    public int baseObstacleCount = 1;
    public int obstaclePerLevel = 1;
    public float difficultySpeedStep = 0.08f;

    private Snake snake;
    private Food food;
    private Canvas canvas;
    private GameState gameState = GameState.MainMenu;
    private DifficultyPreset difficultyPreset = DifficultyPreset.Normal;
    private readonly List<GameObject> obstacles = new List<GameObject>();

    private int score;
    private int level = 1;
    private int highScore;
    private bool soundEnabled = true;

    private GameObject overlayRoot;
    private GameObject overlayCard;
    private Text overlayTitle;
    private Text overlayBody;
    private RuntimeButton primaryButton;
    private RuntimeButton secondaryButton;
    private RuntimeButton tertiaryButton;
    private Text levelText;
    private Text highScoreText;

    private bool hasBuiltUi;
    private Sprite runtimeUiSprite;

    private class RuntimeButton
    {
        public GameObject root;
        public Button button;
        public Text label;
    }

    public bool IsGameplayActive => gameState == GameState.Playing;

    private void Awake()
    {
        Time.timeScale = 0f;
    }

    private void Start()
    {
        snake = FindAnyObjectByType<Snake>();
        food = FindAnyObjectByType<Food>();
        canvas = FindAnyObjectByType<Canvas>();

        LoadPersistentSettings();
        BuildRuntimeUi();
        HookSceneButtons();

        StartNewGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (gameState == GameState.Playing)
            {
                PauseGame();
            }
            else if (gameState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    public void PlayGame()
    {
        if (gameState == GameState.Paused)
        {
            ResumeGame();
            return;
        }

        if (gameState == GameState.MainMenu || gameState == GameState.GameOver || gameState == GameState.Settings)
        {
            StartNewGame();
        }
    }

    public void PauseGame()
    {
        if (gameState != GameState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        gameState = GameState.Paused;
        ShowOverlay("Paused", "Take a break and resume when ready.");
        ConfigurePauseButtons();
        UpdateHud();
    }

    public void ResumeGame()
    {
        if (gameState != GameState.Paused && gameState != GameState.MainMenu && gameState != GameState.GameOver)
        {
            return;
        }

        Time.timeScale = 1f;
        gameState = GameState.Playing;
        HideOverlay();
        UpdateHud();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TriggerGameOver()
    {
        if (gameState == GameState.GameOver)
        {
            return;
        }

        gameState = GameState.GameOver;
        Time.timeScale = 0f;

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        ShowOverlay("Game Over", "Score: " + score + "\nHigh score: " + highScore);
        ConfigureGameOverButtons();
        UpdateHud();
    }

    public void ConsumeFood(Food consumedFood)
    {
        if (gameState != GameState.Playing || consumedFood == null)
        {
            return;
        }

        score += GetPointsForFood(consumedFood.currentType);
        snake.Grow();
        snake.ApplyFoodEffect(consumedFood.currentType);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        UpdateProgression();
        consumedFood.RandomizePosition();
        UpdateHud();
    }

    public bool IsCellBlocked(int x, int y, Food ignoreFood = null)
    {
        if (snake != null && snake.Occupies(x, y))
        {
            return true;
        }

        if (food != null && food != ignoreFood && food.Occupies(x, y))
        {
            return true;
        }

        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle == null)
            {
                continue;
            }

            Vector2 position = obstacle.transform.position;
            if (Mathf.RoundToInt(position.x) == x && Mathf.RoundToInt(position.y) == y)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateScoreUI(int value)
    {
        score = value;
        UpdateHud();
    }

    private void StartNewGame()
    {
        ResetMatch(false);
        Time.timeScale = 1f;
        gameState = GameState.Playing;
        HideOverlay();
        UpdateHud();
    }

    private void ResetMatch(bool keepMenuVisible)
    {
        score = 0;
        level = 1;

        if (snake != null)
        {
            snake.initialSize = 3;
            snake.speedMultiplier = GetBaseSpeedMultiplier();
            snake.ResetState();
        }

        RefreshObstacles();

        if (food != null)
        {
            food.RandomizePosition();
        }

        if (keepMenuVisible)
        {
            gameState = GameState.MainMenu;
            Time.timeScale = 0f;
        }

        UpdateHud();
    }

    private void UpdateProgression()
    {
        int computedLevel = Mathf.Max(1, (score / pointsPerLevel) + 1);

        if (computedLevel != level)
        {
            level = computedLevel;
            RefreshObstacles();
        }

        if (snake != null)
        {
            snake.speedMultiplier = GetBaseSpeedMultiplier() * (1f + (level - 1) * difficultySpeedStep);
        }
    }

    private int GetPointsForFood(Food.FoodType foodType)
    {
        return 10;
    }

    private float GetBaseSpeedMultiplier()
    {
        switch (difficultyPreset)
        {
            case DifficultyPreset.Easy:
                return 0.9f;
            case DifficultyPreset.Hard:
                return 1.2f;
            default:
                return 1f;
        }
    }

    private int GetBaseObstacleCount()
    {
        switch (difficultyPreset)
        {
            case DifficultyPreset.Easy:
                return Mathf.Max(0, baseObstacleCount - 1);
            case DifficultyPreset.Hard:
                return baseObstacleCount + 2;
            default:
                return baseObstacleCount;
        }
    }

    private void RefreshObstacles()
    {
        DestroyObstacles();

        if (food == null || food.gridArea == null)
        {
            return;
        }

        Bounds bounds = food.gridArea.bounds;
        int minX = Mathf.RoundToInt(bounds.min.x);
        int maxX = Mathf.RoundToInt(bounds.max.x);
        int minY = Mathf.RoundToInt(bounds.min.y);
        int maxY = Mathf.RoundToInt(bounds.max.y);
        int totalObstacles = GetBaseObstacleCount() + Mathf.Max(0, level - 1) * obstaclePerLevel;
        Sprite obstacleSprite = GetRuntimeUiSprite();

        for (int index = 0; index < totalObstacles; index++)
        {
            Vector2Int position = FindFreeCell(minX, maxX, minY, maxY);
            if (position == Vector2Int.zero && IsCellBlocked(0, 0, null))
            {
                continue;
            }

            GameObject obstacle = new GameObject("Obstacle_" + index);
            obstacle.tag = "Obstacle";
            obstacle.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = obstacleSprite;
            spriteRenderer.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            spriteRenderer.sortingOrder = 1;

            BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            obstacles.Add(obstacle);
        }
    }

    private Vector2Int FindFreeCell(int minX, int maxX, int minY, int maxY)
    {
        int attempts = Mathf.Max(1, (maxX - minX + 1) * (maxY - minY + 1));

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int x = Random.Range(minX, maxX + 1);
            int y = Random.Range(minY, maxY + 1);
            if (!IsCellBlocked(x, y, null))
            {
                return new Vector2Int(x, y);
            }
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!IsCellBlocked(x, y, null))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return Vector2Int.zero;
    }

    private void DestroyObstacles()
    {
        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }

        obstacles.Clear();
    }

    private void HookSceneButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayGame);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void LoadPersistentSettings()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;

        if (PlayerPrefs.HasKey(DifficultyKey))
        {
            difficultyPreset = (DifficultyPreset)PlayerPrefs.GetInt(DifficultyKey, (int)DifficultyPreset.Normal);
        }

        AudioListener.volume = soundEnabled ? 1f : 0f;
    }

    private void BuildRuntimeUi()
    {
        if (hasBuiltUi || canvas == null)
        {
            return;
        }

        hasBuiltUi = true;

        overlayRoot = CreatePanel("RuntimeOverlay", canvas.transform, new Color(0f, 0f, 0f, 0.72f), false);
        overlayRoot.SetActive(false);

        overlayCard = CreatePanel("OverlayCard", overlayRoot.transform, new Color(0.08f, 0.1f, 0.12f, 0.96f), true);
        RectTransform cardRect = overlayCard.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(860f, 620f);
        cardRect.anchoredPosition = Vector2.zero;

        overlayTitle = CreateText("OverlayTitle", overlayCard.transform, "Snake Classic", 46, TextAnchor.MiddleCenter, new Vector2(0f, 210f), new Vector2(760f, 80f));
        overlayBody = CreateText("OverlayBody", overlayCard.transform, "Swipe to move. Eat food. Avoid walls and obstacles.", 26, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(760f, 120f));

        primaryButton = CreateRuntimeButton("PrimaryButton", overlayCard.transform, new Vector2(0f, 0f));
        secondaryButton = CreateRuntimeButton("SecondaryButton", overlayCard.transform, new Vector2(0f, -100f));
        tertiaryButton = CreateRuntimeButton("TertiaryButton", overlayCard.transform, new Vector2(0f, -200f));

        levelText = CreateText("LevelText", canvas.transform, "Level: 1", 24, TextAnchor.UpperLeft, new Vector2(30f, -40f), new Vector2(300f, 40f));
        highScoreText = CreateText("HighScoreText", canvas.transform, "High Score: 0", 24, TextAnchor.UpperRight, new Vector2(-30f, -40f), new Vector2(320f, 40f));
    }

    private GameObject CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        Image image = panel.GetComponent<Image>();
        image.sprite = GetRuntimeUiSprite();
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycastTarget;

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return panel;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        return text;
    }

    private RuntimeButton CreateRuntimeButton(string name, Transform parent, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetRuntimeUiSprite();
        image.type = Image.Type.Simple;
        image.color = new Color(0.85f, 0.88f, 0.92f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text label = CreateText(name + "Label", buttonObject.transform, name, 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520f, 90f));
        label.color = new Color(0.12f, 0.12f, 0.15f, 1f);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(420f, 74f);
        rectTransform.anchoredPosition = anchoredPosition;

        return new RuntimeButton
        {
            root = buttonObject,
            button = button,
            label = label
        };
    }

    private Sprite GetRuntimeUiSprite()
    {
        if (runtimeUiSprite != null)
        {
            return runtimeUiSprite;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[]
        {
            Color.white,
            Color.white,
            Color.white,
            Color.white
        });
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        runtimeUiSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        runtimeUiSprite.name = "RuntimeUiSprite";
        return runtimeUiSprite;
    }

    private void ShowMainMenu()
    {
        gameState = GameState.MainMenu;
        Time.timeScale = 0f;
        ShowOverlay("Snake Classic", "Swipe or use arrow keys / WASD to move.\nEat food, grow longer, and survive as long as possible.");
        ConfigureMainMenuButtons();
        UpdateHud();
    }

    private void ShowOverlay(string title, string body)
    {
        if (overlayRoot == null || overlayTitle == null || overlayBody == null)
        {
            return;
        }

        overlayRoot.SetActive(true);
        overlayTitle.text = title;
        overlayBody.text = body;
    }

    private void HideOverlay()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void ConfigureMainMenuButtons()
    {
        SetupButton(primaryButton, "Play", PlayGame, true);
        SetupButton(secondaryButton, "High Score", ShowHighScore, true);
        SetupButton(tertiaryButton, "Settings", OpenSettings, true);
    }

    private void ConfigurePauseButtons()
    {
        SetupButton(primaryButton, "Resume", ResumeGame, true);
        SetupButton(secondaryButton, "Restart", RestartGame, true);
        SetupButton(tertiaryButton, "Main Menu", ReturnToMainMenu, true);
    }

    private void ConfigureGameOverButtons()
    {
        SetupButton(primaryButton, "Play Again", PlayGame, true);
        SetupButton(secondaryButton, "Main Menu", ReturnToMainMenu, true);
        SetupButton(tertiaryButton, "Settings", OpenSettings, true);
    }

    private void ConfigureSettingsButtons()
    {
        SetupButton(primaryButton, soundEnabled ? "Sound: On" : "Sound: Off", ToggleSound, true);
        SetupButton(secondaryButton, "Difficulty: " + difficultyPreset, CycleDifficulty, true);
        SetupButton(tertiaryButton, "Back", ReturnFromSettings, true);
    }

    private void ShowHighScore()
    {
        ShowOverlay("High Score", "Best run: " + highScore + " points\n\nPress Play to start a new round.");
        SetupButton(primaryButton, "Play", StartNewGame, true);
        SetupButton(secondaryButton, "Settings", OpenSettings, true);
        SetupButton(tertiaryButton, "Back", ShowMainMenu, true);
    }

    private void OpenSettings()
    {
        gameState = GameState.Settings;
        Time.timeScale = 0f;
        ShowOverlay("Settings", "Configure sound and difficulty.");
        ConfigureSettingsButtons();
        UpdateHud();
    }

    private void ReturnFromSettings()
    {
        if (gameState == GameState.Playing)
        {
            Time.timeScale = 1f;
            HideOverlay();
            return;
        }

        ShowMainMenu();
    }

    private void ReturnToMainMenu()
    {
        ResetMatch(true);
        ShowMainMenu();
    }

    private void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        PlayerPrefs.SetInt(SoundKey, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = soundEnabled ? 1f : 0f;
        ConfigureSettingsButtons();
    }

    private void CycleDifficulty()
    {
        difficultyPreset = (DifficultyPreset)(((int)difficultyPreset + 1) % 3);
        PlayerPrefs.SetInt(DifficultyKey, (int)difficultyPreset);
        PlayerPrefs.Save();

        if (gameState == GameState.Playing)
        {
            UpdateProgression();
        }

        ConfigureSettingsButtons();
    }

    private void SetupButton(RuntimeButton runtimeButton, string label, UnityAction action, bool visible)
    {
        if (runtimeButton == null || runtimeButton.button == null || runtimeButton.label == null)
        {
            return;
        }

        runtimeButton.root.SetActive(visible);
        runtimeButton.label.text = label;
        runtimeButton.button.onClick.RemoveAllListeners();
        runtimeButton.button.onClick.AddListener(action);
    }

    private void UpdateHud()
    {
        if (scoreText != null)
        {
            scoreText.SetText("Score: " + score + "  |  Level: " + level);
        }

        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }

        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }
    }
}

public class CanvasFocus : MonoBehaviour
{
    private void Start()
    {
        Focus();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Focus();
        }
    }

    private void Focus()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            GameObject canvasGameObject = canvas.gameObject;
            canvasGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
            canvasGameObject.SetActive(false);
            canvasGameObject.SetActive(true);
        }
    }
}