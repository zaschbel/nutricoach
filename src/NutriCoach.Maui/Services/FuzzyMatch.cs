namespace NutriCoach.App.Services;

/// <summary>
/// Einfache Tippfehler-Korrektur per Levenshtein-Distanz: findet den ähnlichsten Namen aus einer
/// Liste bekannter Namen (z. B. Übungen oder Lebensmittel), falls die exakte/Teilstring-Suche
/// nichts gefunden hat - z. B. "Banddrücken" eingetippt, aber "Bankdrücken" gemeint.
/// </summary>
public static class FuzzyMatch
{
    /// <summary>Findet den ähnlichsten Namen aus einer Liste per Levenshtein-Distanz, wenn kein exakter Treffer existiert.</summary>
    public static string? FindClosest(string input, IEnumerable<string> candidates, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var normalizedInput = input.Trim().ToLowerInvariant();

        string? best = null;
        var bestDistance = int.MaxValue;
        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate, input, StringComparison.OrdinalIgnoreCase)) return candidate; // exakter Treffer geht immer vor
            var distance = Levenshtein(normalizedInput, candidate.Trim().ToLowerInvariant());
            if (distance < bestDistance) { bestDistance = distance; best = candidate; }
        }
        return bestDistance <= maxDistance ? best : null;
    }

    private static int Levenshtein(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return dp[a.Length, b.Length];
    }
}
