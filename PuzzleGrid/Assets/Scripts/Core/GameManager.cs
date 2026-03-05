using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int moves = 20;
    public bool IsGameOver { get; private set; } = false;

    // ensure only one instance of GameManager exists
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartNewGame(int startMoves = 20)
    {
        score = 0;
        moves = startMoves;
        IsGameOver = false;
        var gridManager = GridManager.Instance != null ? GridManager.Instance : null;
        if (gridManager == null)
        {
            Debug.LogError("GameManager: No GridManager found in scene. Cannot recreate grid.");
            return;
        }

        if (GridManager.Instance == null)
            GridManager.Instance = gridManager;

        gridManager.RecreateGrid();
        UIManager.Instance?.RefreshHUD();
        UIManager.Instance?.ShowGameOver(false);
        UIManager.Instance?.ShowHUD(true);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UIManager.Instance?.RefreshHUD();
        if (IsGameOver) return;
    }

    public void UseMove()
    {
        if (IsGameOver) return;

        moves--;
        if (moves < 0) moves = 0;

        UIManager.Instance?.RefreshHUD();

        if (moves == 0)
        {
            IsGameOver = true; // mark it, but do NOT show UI here
        }
    }
}