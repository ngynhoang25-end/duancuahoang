using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Waiting,
        Playing,
        Dead
    }

    public float initialGameSpeed = 5f;
    public float gameSpeedIncrease = 0.1f;
    public float gameSpeed { get; private set; }
    public GameState State { get; private set; } = GameState.Waiting;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hiscoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button retryButton;

    [SerializeField] private float dayNightCycleDuration = 45f;
    [SerializeField] private Color dayBackground = new Color(0.8396226f, 0.8396226f, 0.8396226f, 1f);
    [SerializeField] private Color nightBackground = new Color(0.149f, 0.188f, 0.298f, 1f);
    [SerializeField] private Color dayAmbient = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color nightAmbient = new Color(0.35f, 0.38f, 0.48f, 1f);

    private Player player;
    private Spawner spawner;

    private float score;
    private float dayNightTimer;
    private Camera mainCamera;
    public float Score => score;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }

        mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        player = FindObjectOfType<Player>();
        spawner = FindObjectOfType<Spawner>();

        PrepareNewGame();
    }

    public void NewGame()
    {
        PrepareNewGame();
        StartGame();
    }

    public void GameOver()
    {
        if (State == GameState.Dead)
        {
            return;
        }

        State = GameState.Dead;
        gameSpeed = 0f;

        if (player != null)
        {
            player.SetDead();
        }

        spawner.gameObject.SetActive(false);
        gameOverText.text = "GAME OVER\nCHẠM ĐỂ CHƠI LẠI";
        gameOverText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);

        UpdateHiscore();
    }

    private void Update()
    {
        if (State == GameState.Playing)
        {
            gameSpeed += gameSpeedIncrease * Time.deltaTime;
            score += gameSpeed * Time.deltaTime;
            dayNightTimer += Time.deltaTime;
            ApplyDayNightCycle();
            scoreText.text = Mathf.FloorToInt(score).ToString("D5");
        }
        else if (State == GameState.Waiting || State == GameState.Dead)
        {
            if (HasStartInput())
            {
                StartGame();
            }
        }
    }

    public void StartGame()
    {
        PrepareNewGame();
        State = GameState.Playing;
        gameOverText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        spawner.gameObject.SetActive(true);
        player.BeginRun();
        ApplyDayNightCycle();
    }

    private void PrepareNewGame()
    {
        score = 0f;
        gameSpeed = initialGameSpeed;
        dayNightTimer = 0f;
        State = GameState.Waiting;

        if (player != null)
        {
            player.ResetPlayer();
            player.gameObject.SetActive(true);
        }

        if (spawner != null)
        {
            spawner.ResetSpawner();
            spawner.gameObject.SetActive(false);
        }

        gameOverText.text = "CHẠM ĐỂ CHƠI";
        gameOverText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(false);

        UpdateHiscore();
        ApplyDayNightCycle();
        scoreText.text = "00000";
    }

    private void UpdateHiscore()
    {
        float hiscore = PlayerPrefs.GetFloat("hiscore", 0);

        if (score > hiscore)
        {
            hiscore = score;
            PlayerPrefs.SetFloat("hiscore", hiscore);
        }

        hiscoreText.text = Mathf.FloorToInt(hiscore).ToString("D5");
    }

    private bool HasStartInput()
    {
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.touchSupported && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase == TouchPhase.Began;
        }

        return false;
    }

    private void ApplyDayNightCycle()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        float cycleProgress = dayNightCycleDuration <= 0f ? 0f : Mathf.PingPong(dayNightTimer / dayNightCycleDuration, 1f);
        Color backgroundColor = Color.Lerp(dayBackground, nightBackground, cycleProgress);
        Color ambientColor = Color.Lerp(dayAmbient, nightAmbient, cycleProgress);

        mainCamera.backgroundColor = backgroundColor;
        RenderSettings.ambientLight = ambientColor;
    }

}
