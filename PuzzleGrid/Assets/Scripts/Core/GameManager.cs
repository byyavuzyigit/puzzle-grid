using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int moves = 20;

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
        Debug.Log("Score: " + score);
    }

    public void UseMove()
    {
        moves--;
        Debug.Log("Moves left: " + moves);

        if (moves <= 0)
        {
            Debug.Log("Game Over");
        }
    }
}