using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int score = 0;
    public TMP_Text scoreText;

    public GameObject startPanel;
    public GameObject gameOverPanel;

    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;    // 👈 THÊM DÒNG NÀY

    private bool gameStarted = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        score = 0;
        scoreText.text = "0";

        Time.timeScale = 0f;

        startPanel.SetActive(true);
        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!gameStarted && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        Time.timeScale = 1f;
        startPanel.SetActive(false);
    }

    public void AddScore()
    {
        score++;
        scoreText.text = score.ToString();
    }

    public void GameOver()
    {
        Time.timeScale = 0f;

        gameOverPanel.SetActive(true);
        finalScoreText.text = "Score: " + score;

        // 🎯 LẤY ĐIỂM CAO NHẤT
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        // 🎯 NẾU CAO HƠN → LƯU
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        // 🎯 HIỂN THỊ
        highScoreText.text = "Best: " + highScore;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}