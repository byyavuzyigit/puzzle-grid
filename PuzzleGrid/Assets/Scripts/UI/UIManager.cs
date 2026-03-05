using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject gridRoot;

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject hudPanel;
    public GameObject highScoresPanel;
    public GameObject gameOverPanel;
    public HighScoresUI highScoresUI;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text movesText;

    [Header("Game Over")]
    public TMP_Text finalScoreText;
    public TMP_InputField nameInput;
    public GameObject submitScoreButton; // optional: to hide/show

    [Header("High Scores")]
    public TMP_Text highScoresText;

    public bool IsInMenu { get; private set; } = true;
    private int pendingScore = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowStartMenu();
    }

    public void ShowStartMenu()
    {
        IsInMenu = true;

        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        highScoresPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // Optional: show latest leaderboard on menu load
        highScoresUI.Refresh();
    }

    public void StartGame()
    {
        IsInMenu = false;

        GameManager.Instance.StartNewGame(20);
        RefreshHUD();
        gridRoot.SetActive(true);

        startPanel.SetActive(false);
        highScoresPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        hudPanel.SetActive(true);
    }

    public void ShowHighScores()
    {
        startPanel.SetActive(false);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        highScoresPanel.SetActive(true);
        gridRoot.SetActive(false);
        highScoresUI.Refresh();
    }

    public void BackToMenu()
    {
        ShowStartMenu();
        gridRoot.SetActive(true);
    }

    public void ShowHUD(bool show)
    {
        if (hudPanel != null) hudPanel.SetActive(show);
    }

    public void ShowGameOver(bool show)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(show);

        if (!show) return;

        pendingScore = GameManager.Instance != null ? GameManager.Instance.score : 0;

        if (finalScoreText != null)
            finalScoreText.text = $"Score: {pendingScore}";

        // Always show the submit button so player can submit any score
        if (nameInput != null)
        {
            nameInput.gameObject.SetActive(true);
            nameInput.text = "";
            nameInput.ActivateInputField();
        }

        if (submitScoreButton != null)
            submitScoreButton.SetActive(true);
    }

    public void RefreshHUD()
    {
        if (GameManager.Instance == null) return;
        scoreText.text = $"Score: {GameManager.Instance.score}";
        movesText.text = $"Moves: {GameManager.Instance.moves}";
    }

    private void UpdateHighScoresText()
    {
        if (highScoresText == null)
        {
            Debug.LogError("highScoresText reference is NULL (assign it in UIManager inspector).");
            return;
        }

        var entries = ScoreManager.LoadTopScores();

        string s = "High Scores\n\n";
        if (entries.Count == 0)
            s += "No scores yet.";
        else
            for (int i = 0; i < entries.Count; i++)
                s += $"{i + 1}. {entries[i].name} — {entries[i].score}\n";

        highScoresText.text = s;
    }

    public void SubmitScoreFromGameOver()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver == false) return;

        string playerName = nameInput != null ? nameInput.text : "Player";
        ScoreManager.SubmitScore(playerName, pendingScore);

        // hide input after saving (prevents double-submit)
        if (nameInput != null) nameInput.gameObject.SetActive(false);
        if (submitScoreButton != null) submitScoreButton.SetActive(false);

        // refresh high scores text so it’s ready if you open the HighScores panel
        highScoresUI.Refresh();
    }

    public void RestartRun()
    {
        // restart gameplay but stay out of menu
        StartGame();
    }

    public void ExitGame()
        {
    #if UNITY_EDITOR
            Debug.Log("ExitGame called (Editor does not quit).");
    #else
            Application.Quit();
    #endif
        }
    }