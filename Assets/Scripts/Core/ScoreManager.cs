using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ScoreManager
{
    private const string Key = "TOP_SCORES_WITH_NAMES_V1";
    private const int MaxScores = 5;

    [System.Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
    }

    [System.Serializable]
    private class ScoreList
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    // loads the top scores from PlayerPrefs.
    public static List<ScoreEntry> LoadTopScores()
    {
        if (!PlayerPrefs.HasKey(Key))
            return new List<ScoreEntry>();

        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json))
            return new List<ScoreEntry>();

        var list = JsonUtility.FromJson<ScoreList>(json);
        return list?.entries ?? new List<ScoreEntry>();
    }

    // submits a new score. sanitizes the player name and adds the score to the top scores list if it qualifies.the list is automatically sorted and trimmed to keep only MaxScores entries.
    public static List<ScoreEntry> SubmitScore(string name, int score)
    {
        name = SanitizeName(name);

        var entries = LoadTopScores();
        entries.Add(new ScoreEntry { name = name, score = score });

        entries = entries
            .OrderByDescending(e => e.score)
            .Take(MaxScores)
            .ToList();

        Save(entries);
        return entries;
    }

    // checks if a score would qualify for the top scores list.
    public static bool WouldQualify(int score)
    {
        var entries = LoadTopScores();
        if (entries.Count < MaxScores) return true;
        return score > entries.Min(e => e.score);
    }

    // saves the score list to PlayerPrefs as JSON.
    private static void Save(List<ScoreEntry> entries)
    {
        var list = new ScoreList { entries = entries };
        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    // validates and cleans up a player name.
    private static string SanitizeName(string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return "Player";
        if (name.Length > 12) name = name.Substring(0, 12);
        return name;
    }

    // clears all saved high scores from PlayerPrefs.
    public static void ClearScores()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}