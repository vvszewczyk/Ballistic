using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LeaderboardService
{
    private const int MaxEntries = 5;
    private const int MaxNameLength = 16;
    private const string DefaultPlayerName = "Player";
    private const string NameKey = "leaderboard_name_";
    private const string ScoreKey = "leaderboard_score_";
    private const string LevelKey = "leaderboard_level_";

    public static void SubmitScore(int score, int level)
    {
        SubmitScore(DefaultPlayerName, score, level);
    }

    public static void SubmitScore(string playerName, int score, int level)
    {
        List<LeaderboardEntry> entries = LoadEntries();

        entries.Add(new LeaderboardEntry(NormalizePlayerName(playerName), score, level));
        entries.Sort(CompareEntries);

        while (entries.Count > MaxEntries)
        {
            entries.RemoveAt(entries.Count - 1);
        }

        SaveEntries(entries);
    }

    public static string GetFormattedLeaderboard()
    {
        List<LeaderboardEntry> entries = LoadEntries();

        if (entries.Count == 0)
        {
            return "No scores yet.";
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < entries.Count; i++)
        {
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(entries[i].playerName);
            builder.Append("   Score: ");
            builder.Append(entries[i].score);
            builder.Append("   Level: ");
            builder.Append(entries[i].level);

            if (i < entries.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    public static string NormalizePlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return DefaultPlayerName;
        }

        string normalized = playerName.Trim()
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('\t', ' ');

        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        if (normalized.Length > MaxNameLength)
        {
            normalized = normalized.Substring(0, MaxNameLength);
        }

        return string.IsNullOrWhiteSpace(normalized) ? DefaultPlayerName : normalized;
    }

    private static int CompareEntries(LeaderboardEntry a, LeaderboardEntry b)
    {
        int scoreCompare = b.score.CompareTo(a.score);

        if (scoreCompare != 0)
        {
            return scoreCompare;
        }

        return b.level.CompareTo(a.level);
    }

    private static List<LeaderboardEntry> LoadEntries()
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        for (int i = 0; i < MaxEntries; i++)
        {
            if (!PlayerPrefs.HasKey(ScoreKey + i))
            {
                continue;
            }

            string playerName = PlayerPrefs.GetString(NameKey + i, DefaultPlayerName);
            int score = PlayerPrefs.GetInt(ScoreKey + i, 0);
            int level = PlayerPrefs.GetInt(LevelKey + i, 0);

            entries.Add(new LeaderboardEntry(playerName, score, level));
        }

        return entries;
    }

    private static void SaveEntries(List<LeaderboardEntry> entries)
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            PlayerPrefs.DeleteKey(NameKey + i);
            PlayerPrefs.DeleteKey(ScoreKey + i);
            PlayerPrefs.DeleteKey(LevelKey + i);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerPrefs.SetString(NameKey + i, entries[i].playerName);
            PlayerPrefs.SetInt(ScoreKey + i, entries[i].score);
            PlayerPrefs.SetInt(LevelKey + i, entries[i].level);
        }

        PlayerPrefs.Save();
    }

    private struct LeaderboardEntry
    {
        public readonly string playerName;
        public readonly int score;
        public readonly int level;

        public LeaderboardEntry(string playerName, int score, int level)
        {
            this.playerName = NormalizePlayerName(playerName);
            this.score = score;
            this.level = level;
        }
    }
}
