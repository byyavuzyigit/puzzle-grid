using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int moves = 20;
    public bool IsGameOver => moves <= 0;

    // ensure only one instance of GameManager exists
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UIManager.Instance?.RefreshHUD();
    }

    public void UseMove()
    {
        moves--;
        UIManager.Instance?.RefreshHUD();
        if (moves <= 0)
        {
            UIManager.Instance?.ShowGameOver(true);
        }
    }
}