using UnityEngine;

/// <summary>
/// Manages the player's score. Tracks hits and kills, and provides events for UI updates.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int scorePerHit = 10;
    [SerializeField] private int scorePerCrit = 20;
    [SerializeField] private int scorePerKill = 100;

    private int currentScore = 0;

    /// <summary>
    /// The current player score.
    /// </summary>
    public int CurrentScore => currentScore;

    /// <summary>
    /// Event that is called when the score changes. Passes the new score value.
    /// </summary>
    public System.Action<int> OnScoreChanged;

    /// <summary>
    /// Singleton instance for easy access from other scripts.
    /// </summary>
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple ScoreManager instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds score for a hit. Called when a shot hits an enemy.
    /// </summary>
    public void AddHitScore()
    {
        AddScore(scorePerHit);
    }

    /// <summary>
    /// Adds score for a critical hit. Called when a shot hits an enemy's critical zone.
    /// </summary>
    public void AddCritScore()
    {
        AddScore(scorePerCrit);
    }

    /// <summary>
    /// Adds score for a kill. Called when an enemy dies.
    /// </summary>
    public void AddKillScore()
    {
        AddScore(scorePerKill);
    }

    /// <summary>
    /// Adds the specified amount to the score.
    /// </summary>
    /// <param name="amount">The amount of score to add.</param>
    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log($"Score: {currentScore} (+{amount})");
    }

    /// <summary>
    /// Resets the score to zero.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log("Score reset to 0");
    }

    /// <summary>
    /// Sets the score to a specific value.
    /// </summary>
    /// <param name="score">The score value to set.</param>
    public void SetScore(int score)
    {
        currentScore = Mathf.Max(0, score);
        OnScoreChanged?.Invoke(currentScore);
    }
}

