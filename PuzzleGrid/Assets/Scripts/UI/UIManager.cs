using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text movesText;

    [Header("Panels")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshHUD();
        ShowGameOver(false);
    }

    public void RefreshHUD()
    {
        if (GameManager.Instance == null) return;

        scoreText.text = $"Score: {GameManager.Instance.score}";
        movesText.text = $"Moves: {GameManager.Instance.moves}";
    }

    public void ShowGameOver(bool show)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(show);
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}