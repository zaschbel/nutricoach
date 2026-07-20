using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Sucht Übungen live in der offenen, kostenlosen wger.de-Übungsdatenbank (über 800 Einträge,
/// keine Anmeldung/kein Key nötig) - gleiches Prinzip wie die Lebensmittelsuche über Open Food
/// Facts: erst leichtgewichtig suchen, dann bei Auswahl die Details nachladen und lokal
/// zwischenspeichern. Mit Zeitlimit abgesichert, damit eine langsame/nicht erreichbare
/// Internetverbindung die Suche nicht ewig hängen lässt.
/// </summary>
public class ExerciseApiService
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("https://wger.de"), Timeout = TimeSpan.FromSeconds(8) };

    public record OnlineExerciseHit(int WgerId, string Name, string CategoryName);

    public async Task<List<OnlineExerciseHit>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<OnlineExerciseHit>();

        try
        {
            var url = $"/api/v2/exercise/search/?term={Uri.EscapeDataString(query)}&language=german&format=json";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await Http.GetFromJsonAsync<WgerSearchResponse>(url, cts.Token);
            if (response?.Suggestions is null) return new List<OnlineExerciseHit>();

            return response.Suggestions
                .Where(s => s.Data is not null && !string.IsNullOrWhiteSpace(s.Data.Name))
                .Select(s => new OnlineExerciseHit(s.Data!.Id, StripHtml(s.Data.Name), s.Data.Category ?? ""))
                .Take(15)
                .ToList();
        }
        catch
        {
            // Kein Internet, Zeitüberschreitung, wger nicht erreichbar o. Ä. - dann gibt's halt nur
            // die lokalen Treffer, kein Grund, die ganze Suche abstürzen zu lassen.
            return new List<OnlineExerciseHit>();
        }
    }

    /// <summary>Lädt die vollen Details einer wger-Übung nach, für den Import in die lokale Datenbank.</summary>
    public async Task<Exercise?> GetDetailsAsync(int wgerId)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var info = await Http.GetFromJsonAsync<WgerExerciseInfo>($"/api/v2/exerciseinfo/{wgerId}/?format=json", cts.Token);
            if (info is null) return null;

            var translation = info.Translations?.FirstOrDefault(t => t.Language == 1) // 1 = Deutsch bei wger
                               ?? info.Translations?.FirstOrDefault(t => t.Language == 2) // sonst Englisch
                               ?? info.Translations?.FirstOrDefault();
            if (translation is null || string.IsNullOrWhiteSpace(translation.Name)) return null;

            var primaryMuscle = info.Muscles?.FirstOrDefault()?.NameEnglish ?? info.Category?.Name ?? "Sonstiges";
            var secondaryMuscle = info.MusclesSecondary?.FirstOrDefault()?.NameEnglish;
            var equipment = info.Equipment is { Count: > 0 } ? string.Join(", ", info.Equipment.Select(e => e.Name)) : "Körpergewicht";

            return new Exercise
            {
                WgerId = wgerId,
                Name = StripHtml(translation.Name),
                PrimaryMuscleGroup = primaryMuscle,
                SecondaryMuscleGroup = secondaryMuscle,
                Equipment = equipment,
                InputType = GuessInputType(info.Category?.Name, equipment),
                MetValue = 5.0,
                Tip = TrimTip(StripHtml(translation.Description))
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Grobe Einordnung, welche Eingabefelder sinnvoll sind - wger kennt diese Unterscheidung selbst nicht.</summary>
    private static ExerciseInputType GuessInputType(string? categoryName, string equipment)
    {
        var text = $"{categoryName} {equipment}".ToLowerInvariant();
        if (text.Contains("cardio") || text.Contains("laufband") || text.Contains("rad") || text.Contains("ruder"))
            return ExerciseInputType.StreckeZeit;
        if (equipment.Contains("Körpergewicht", StringComparison.OrdinalIgnoreCase) && !text.Contains("cardio"))
            return ExerciseInputType.NurWiederholungen;
        return ExerciseInputType.GewichtWiederholungen;
    }

    private static string? TrimTip(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length > 200 ? text[..200].TrimEnd() + "…" : text;
    }

    private static string StripHtml(string? text) =>
        string.IsNullOrWhiteSpace(text) ? "" : Regex.Replace(text, "<[^>]+>", "").Trim();

    // ---------------- Antwort-Modelle der wger-API (nur die Felder, die wir brauchen) ----------------
    private class WgerSearchResponse
    {
        [JsonPropertyName("suggestions")] public List<WgerSuggestion>? Suggestions { get; set; }
    }
    private class WgerSuggestion
    {
        [JsonPropertyName("data")] public WgerSuggestionData? Data { get; set; }
    }
    private class WgerSuggestionData
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }
    private class WgerExerciseInfo
    {
        [JsonPropertyName("category")] public WgerCategory? Category { get; set; }
        [JsonPropertyName("muscles")] public List<WgerMuscle>? Muscles { get; set; }
        [JsonPropertyName("muscles_secondary")] public List<WgerMuscle>? MusclesSecondary { get; set; }
        [JsonPropertyName("equipment")] public List<WgerEquipment>? Equipment { get; set; }
        [JsonPropertyName("translations")] public List<WgerTranslation>? Translations { get; set; }
    }
    private class WgerCategory
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
    private class WgerMuscle
    {
        [JsonPropertyName("name_en")] public string? NameEnglish { get; set; }
    }
    private class WgerEquipment
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
    private class WgerTranslation
    {
        [JsonPropertyName("language")] public int Language { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}
