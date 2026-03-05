using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HighScoresUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform rowsContainer;
    public GameObject rowPrefab;

    private readonly List<GameObject> spawnedRows = new();

    public void Refresh()
    {
        ClearRows();

        var entries = ScoreManager.LoadTopScores();

        // always show 5 rows (empty rows if not enough scores)
        for (int i = 0; i < 5; i++)
        {
            GameObject rowObj = Instantiate(rowPrefab, rowsContainer);
            spawnedRows.Add(rowObj);

            // find children
            TMP_Text rankText = rowObj.transform.Find("RankText")?.GetComponent<TMP_Text>();
            TMP_Text nameText = rowObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text scoreText = rowObj.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

            if (rankText != null) rankText.text = (i + 1).ToString();

            if (i < entries.Count)
            {
                if (nameText != null) nameText.text = entries[i].name;
                if (scoreText != null) scoreText.text = entries[i].score.ToString();

                // highlight #1
                if (i == 0)
                    scoreText.text = $"<b>{entries[i].score}</b>";
            }
            else
            {
                if (nameText != null) nameText.text = "-";
                if (scoreText != null) scoreText.text = "-";
            }
        }
    }

    private void ClearRows()
    {
        foreach (var go in spawnedRows)
            if (go != null) Destroy(go);

        spawnedRows.Clear();
    }
}