using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace NutriCoach.Maui.Services;

public record AiSwapSuggestion(bool HasImprovement, string Original, string Alternative, int KcalSaved, string Reason);

/// <summary>Ergebnis eines KI-Aufrufs: entweder ein Vorschlag, oder ein für Menschen lesbarer Fehlergrund.</summary>
public record AiRequestResult(AiSwapSuggestion? Suggestion, string? ErrorMessage);

/// <summary>Ergebnis der Foto-Erkennung: geschätzte Nährwerte PRO 100g (nicht für die fotografierte Menge - die Mengenangabe macht der Nutzer selbst danach, wie bei jedem anderen manuell angelegten Lebensmittel).</summary>
public record AiFoodPhotoResult(string Name, double KcalPer100, double ProteinPer100, double CarbsPer100, double FatPer100, string? ErrorMessage);

/// <summary>
/// Ruft die echte Claude API auf, um einen konkreten Ernährungs-Tausch-Vorschlag für den Tag zu
/// bekommen (ersetzt die bisherige reine Deko-Karte). Der API-Key wird lokal auf dem Gerät im
/// sicheren Speicher abgelegt (SecureStorage) - er verlässt das Gerät nur in Richtung
/// api.anthropic.com, direkt vom Handy aus (kein eigener Server dazwischen). Für eine App mit
/// vielen fremden Nutzern wäre ein eigener Server als Vermittler sicherer (der Schlüssel bliebe
/// dann serverseitig geheim); für den persönlichen Gebrauch hier ist das ein vertretbarer Kompromiss.
/// </summary>
public class ClaudeAiService
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-5";
    private static readonly HttpClient Http = new();

    public static async Task<string?> GetApiKeyAsync() => await SecureStorage.Default.GetAsync("anthropic_api_key");
    public static async Task SetApiKeyAsync(string key) => await SecureStorage.Default.SetAsync("anthropic_api_key", key);
    public static void ClearApiKey() => SecureStorage.Default.Remove("anthropic_api_key");

    /// <summary>Liefert einen konkreten Tausch-Vorschlag - oder eine für Menschen lesbare Fehlermeldung, falls es nicht geklappt hat.</summary>
    public async Task<AiRequestResult> GetMealSwapSuggestionAsync(string recentFoodsSummary, string goalText, double remainingKcal)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiRequestResult(null, "Kein API-Key hinterlegt. In den Einstellungen einrichten.");

        var prompt =
            $"Du bist ein Ernährungscoach in einer Fitness-App. Der Nutzer hat heute bereits gegessen: " +
            $"{recentFoodsSummary}. Sein Ziel: {goalText}. Ihm verbleiben noch {remainingKcal:0} kcal für heute. " +
            "Schlage GENAU EINEN konkreten, realistischen Lebensmittel-Tausch vor, der zum Ziel passt " +
            "(z. B. eine Zutat oder ein ähnliches Gericht gegen eine kalorienärmere/passendere Alternative). " +
            "Antworte AUSSCHLIESSLICH mit einem JSON-Objekt, ohne Markdown, ohne Erklärung davor oder danach, " +
            "in genau diesem Format: " +
            "{\"original\":\"...\",\"alternative\":\"...\",\"kcalSaved\":000,\"reason\":\"kurzer Grund in einem Satz\"}";

        var requestBody = new
        {
            model = Model,
            max_tokens = 300,
            messages = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var response = await Http.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Anthropic liefert bei Fehlern ein JSON mit error.message - das direkt anzeigen,
                // statt nur "hat nicht geklappt" (z. B. "Guthaben zu niedrig", "Key ungültig" ...).
                try
                {
                    using var errorDoc = JsonDocument.Parse(responseJson);
                    var message = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                    return new AiRequestResult(null, message ?? $"Fehler {(int)response.StatusCode}");
                }
                catch
                {
                    return new AiRequestResult(null, $"Fehler {(int)response.StatusCode} von der API.");
                }
            }

            using var responseDoc = JsonDocument.Parse(responseJson);
            var text = responseDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(text)) return new AiRequestResult(null, "Leere Antwort von der KI erhalten.");

            // Falls die Antwort trotz Anweisung in Markdown-Codeblöcke verpackt ist, diese entfernen
            text = text.Trim().Trim('`').Replace("json\n", "").Trim();

            using var suggestionDoc = JsonDocument.Parse(text);
            var root = suggestionDoc.RootElement;
            var suggestion = new AiSwapSuggestion(
                true,
                root.GetProperty("original").GetString() ?? "",
                root.GetProperty("alternative").GetString() ?? "",
                root.TryGetProperty("kcalSaved", out var kcalProp) ? kcalProp.GetInt32() : 0,
                root.GetProperty("reason").GetString() ?? "");
            return new AiRequestResult(suggestion, null);
        }
        catch (Exception ex)
        {
            return new AiRequestResult(null, $"Netzwerk-/Verarbeitungsfehler: {ex.Message}");
        }
    }
}
