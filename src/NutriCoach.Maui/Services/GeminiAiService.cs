using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using NutriCoach.App.Models;

namespace NutriCoach.Maui.Services;

/// <summary>Ergebnis der Zukunftsbild-Generierung: entweder die rohen Bilddaten + Mimetype, oder ein Fehlergrund.</summary>
public record AiImageResult(byte[]? ImageBytes, string? MimeType, string? Error);

/// <summary>
/// Ruft Googles Gemini API auf (statt Anthropic) - Google bietet für die Gemini-API eine echte
/// kostenlose Stufe ohne Kreditkarte an (über Google AI Studio), was das Zahlungs-Problem bei
/// Anthropic umgeht. Gleicher Aufbau wie ClaudeAiService, damit der Rest der App (MainViewModel,
/// SettingsPage) sich kaum ändern musste.
/// </summary>
public class GeminiAiService
{
    // Google hat gemini-2.5-flash im Juli 2026 überraschend früher abgeschaltet als ursprünglich
    // angekündigt (offiziell erst für Oktober 2026 geplant) - deshalb hier mehrere Modelle probieren,
    // falls Google das nochmal macht: das aktuelle Flaggschiff zuerst, dann zwei bekannte Rückfallebenen.
    private static readonly string[] ModelsToTry = { "gemini-3.5-flash", "gemini-3-flash-preview", "gemini-2.5-flash" };
    private const string LastWorkingModelKey = "gemini_last_working_model";

    // Bild-GENERIERUNG/-Bearbeitung braucht eine eigene Modell-Familie (responseModalities inkl. IMAGE) -
    // die normalen Text-Modelle oben können das nicht, deshalb komplett getrennte Liste + eigener
    // "zuletzt funktioniert"-Preference-Key, damit sich Text- und Bild-Fallback nicht gegenseitig stören.
    private static readonly string[] ImageModelsToTry = { "gemini-3-flash-image", "gemini-2.5-flash-image", "gemini-2.0-flash-exp-image-generation" };
    private const string LastWorkingImageModelKey = "gemini_last_working_image_model";

    /// <summary>
    /// Merkt sich das zuletzt erfolgreich genutzte Modell und probiert es beim nächsten Mal zuerst -
    /// spart Kontingent (jeder Fehlschlag durch ein nicht mehr verfügbares Modell zählt sonst
    /// mit als eigene Anfrage). Nur relevant, falls sich Googles Modell-Angebot mal wieder ändert.
    /// </summary>
    private static IEnumerable<string> GetOrderedModels()
    {
        var lastWorking = Preferences.Default.Get(LastWorkingModelKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(lastWorking) && ModelsToTry.Contains(lastWorking))
        {
            yield return lastWorking;
            foreach (var m in ModelsToTry.Where(m => m != lastWorking)) yield return m;
        }
        else
        {
            foreach (var m in ModelsToTry) yield return m;
        }
    }

    private static void RememberWorkingModel(string model) => Preferences.Default.Set(LastWorkingModelKey, model);

    /// <summary>Analoge Fallback-Reihenfolge wie GetOrderedModels(), aber für die separate Bild-Modell-Liste.</summary>
    private static IEnumerable<string> GetOrderedImageModels()
    {
        var lastWorking = Preferences.Default.Get(LastWorkingImageModelKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(lastWorking) && ImageModelsToTry.Contains(lastWorking))
        {
            yield return lastWorking;
            foreach (var m in ImageModelsToTry.Where(m => m != lastWorking)) yield return m;
        }
        else
        {
            foreach (var m in ImageModelsToTry) yield return m;
        }
    }

    private static void RememberWorkingImageModel(string model) => Preferences.Default.Set(LastWorkingImageModelKey, model);
    private static string ApiUrl(string model, string apiKey) =>
        $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    private static readonly HttpClient Http = new();

    public static async Task<string?> GetApiKeyAsync() => await SecureStorage.Default.GetAsync("gemini_api_key");
    public static async Task SetApiKeyAsync(string key) => await SecureStorage.Default.SetAsync("gemini_api_key", key);
    public static void ClearApiKey() => SecureStorage.Default.Remove("gemini_api_key");

    public async Task<AiRequestResult> GetMealSwapSuggestionAsync(string recentFoodsSummary, string goalText, double remainingKcal, string planContext)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiRequestResult(null, "Kein Gemini-API-Key hinterlegt. In den Einstellungen einrichten.");

        var prompt =
            $"Du bist ein Ernährungscoach in einer Fitness-App. Der Nutzer hat heute bereits gegessen: " +
            $"{recentFoodsSummary}. Sein Ziel: {goalText}. Ihm verbleiben noch {remainingKcal:0} kcal für heute. " +
            $"Zusätzlicher Kontext zu seinem Plan: {planContext} " +
            "Prüfe ehrlich, ob es einen wirklich sinnvollen, konkreten Tausch gibt, der ihn seinem Ziel " +
            "spürbar näher bringt (z. B. bei einem Kalorien- oder Makro-Ungleichgewicht). " +
            "WICHTIG: Erfinde KEINEN Vorschlag, nur um irgendetwas zu liefern - wenn die bisherige " +
            "Ernährung heute schon gut zum Ziel passt und kein Tausch einen echten Unterschied machen " +
            "würde, sag das ehrlich statt einen erzwungenen, unnötigen Vorschlag zu machen. " +
            "Antworte AUSSCHLIESSLICH mit einem JSON-Objekt, ohne Markdown, ohne Erklärung davor oder " +
            "danach. Gibt es einen echten Verbesserungsvorschlag: " +
            "{\"hasImprovement\":true,\"original\":\"...\",\"alternative\":\"...\",\"kcalSaved\":000,\"reason\":\"kurzer Grund in einem Satz, der den Plan-Bezug nennt\"} " +
            "Gibt es KEINEN sinnvollen Vorschlag: " +
            "{\"hasImprovement\":false,\"reason\":\"kurzer, ehrlicher Satz, warum der bisherige Tag schon gut passt\"}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };
        var requestJson = JsonSerializer.Serialize(requestBody);

        string? lastErrorMessage = null;

        foreach (var model in GetOrderedModels())
        {
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(ApiUrl(model, apiKey), content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastErrorMessage = TryExtractErrorMessage(responseJson) ?? $"Fehler {(int)response.StatusCode}";

                    // "Modell nicht mehr verfügbar" (404) -> nächstes Modell aus der Liste probieren,
                    // statt sofort aufzugeben. Andere Fehler (z. B. falscher Key) gleich melden.
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
                    return new AiRequestResult(null, lastErrorMessage);
                }

                using var responseDoc = JsonDocument.Parse(responseJson);
                var text = responseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text)) return new AiRequestResult(null, "Leere Antwort von der KI erhalten.");

                text = text.Trim().Trim('`').Replace("json\n", "").Trim();

                using var suggestionDoc = JsonDocument.Parse(text);
                var root = suggestionDoc.RootElement;
                var hasImprovement = root.TryGetProperty("hasImprovement", out var hasImprovementProp) && hasImprovementProp.GetBoolean();

                var suggestion = new AiSwapSuggestion(
                    hasImprovement,
                    hasImprovement ? root.GetProperty("original").GetString() ?? "" : "",
                    hasImprovement ? root.GetProperty("alternative").GetString() ?? "" : "",
                    hasImprovement && root.TryGetProperty("kcalSaved", out var kcalProp) ? kcalProp.GetInt32() : 0,
                    root.GetProperty("reason").GetString() ?? "");
                RememberWorkingModel(model);
                return new AiRequestResult(suggestion, null);
            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Netzwerk-/Verarbeitungsfehler: {ex.Message}";
            }
        }

        return new AiRequestResult(null, lastErrorMessage ?? "Keines der bekannten Modelle war erreichbar.");
    }

    /// <summary>Beantwortet eine freie, vom Nutzer eingetippte Frage - mit dem Tagesstand als Kontext (was heute gegessen wurde, Ziel, Restkalorien).</summary>
    public async Task<(string? Answer, string? Error)> AskQuestionAsync(string question, string recentFoodsSummary, string goalText, double remainingKcal, string planContext)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (null, "Kein Gemini-API-Key hinterlegt. In den Einstellungen einrichten.");

        var prompt =
            $"Du bist ein Ernährungscoach in einer Fitness-App. Kontext zum Nutzer: heute bereits gegessen: " +
            $"{recentFoodsSummary}. Ziel: {goalText}. Noch {remainingKcal:0} kcal übrig für heute. " +
            $"Zusätzlicher Plan-Kontext: {planContext} " +
            $"Der Nutzer fragt: \"{question}\". " +
            "Beziehe den Plan-Kontext in deine Antwort mit ein, wo es relevant ist. " +
            "Antworte kurz, konkret und freundlich auf Deutsch (2-4 Sätze), direkt bezogen auf seine Frage " +
            "und seinen aktuellen Stand. Kein JSON, keine Markdown-Formatierung, nur normaler Fließtext.";

        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var requestJson = JsonSerializer.Serialize(requestBody);

        string? lastErrorMessage = null;

        foreach (var model in GetOrderedModels())
        {
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(ApiUrl(model, apiKey), content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastErrorMessage = TryExtractErrorMessage(responseJson) ?? $"Fehler {(int)response.StatusCode}";
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
                    return (null, lastErrorMessage);
                }

                using var responseDoc = JsonDocument.Parse(responseJson);
                var text = responseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text)) return (null, "Leere Antwort von der KI erhalten.");
                RememberWorkingModel(model);
                return (text.Trim(), null);
            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Netzwerk-/Verarbeitungsfehler: {ex.Message}";
            }
        }

        return (null, lastErrorMessage ?? "Keines der bekannten Modelle war erreichbar.");
    }

    /// <summary>
    /// Schickt ein Foto eines Lebensmittels/Gerichts an Gemini und lässt es Name + geschätzte
    /// Nährwerte PRO 100g zurückgeben. Gemini kann Bilder direkt verstehen (nicht nur Text),
    /// deshalb reicht ein einzelner Aufruf ohne separate Bilderkennungs-Bibliothek.
    /// </summary>
    public async Task<AiFoodPhotoResult> AnalyzeFoodPhotoAsync(byte[] imageBytes, string mimeType)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiFoodPhotoResult("", 0, 0, 0, 0, "Kein Gemini-API-Key hinterlegt. In den Einstellungen einrichten.");

        var prompt =
            "Du siehst ein Foto von Essen. Identifiziere, was zu sehen ist, und schätze die Nährwerte " +
            "PRO 100 GRAMM (nicht für die gesamte sichtbare Portion - nur pro 100g, wie auf einer " +
            "Lebensmittel-Nährwerttabelle üblich). Bei gemischten Gerichten schätze einen sinnvollen " +
            "Durchschnitt für das Gesamtgericht. Antworte AUSSCHLIESSLICH mit einem JSON-Objekt, ohne " +
            "Markdown, ohne Erklärung davor oder danach, in genau diesem Format: " +
            "{\"name\":\"kurzer Name des Gerichts\",\"kcal\":000,\"protein\":00,\"carbs\":00,\"fat\":00}";

        var base64Image = Convert.ToBase64String(imageBytes);
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = base64Image } }
                    }
                }
            }
        };
        var requestJson = JsonSerializer.Serialize(requestBody);

        string? lastErrorMessage = null;

        foreach (var model in GetOrderedModels())
        {
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(ApiUrl(model, apiKey), content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastErrorMessage = TryExtractErrorMessage(responseJson) ?? $"Fehler {(int)response.StatusCode}";
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
                    return new AiFoodPhotoResult("", 0, 0, 0, 0, lastErrorMessage);
                }

                using var responseDoc = JsonDocument.Parse(responseJson);
                var text = responseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                    return new AiFoodPhotoResult("", 0, 0, 0, 0, "Leere Antwort von der KI erhalten.");

                text = text.Trim().Trim('`').Replace("json\n", "").Trim();

                using var resultDoc = JsonDocument.Parse(text);
                var root = resultDoc.RootElement;
                RememberWorkingModel(model);
                return new AiFoodPhotoResult(
                    root.GetProperty("name").GetString() ?? "Unbekanntes Gericht",
                    root.GetProperty("kcal").GetDouble(),
                    root.GetProperty("protein").GetDouble(),
                    root.GetProperty("carbs").GetDouble(),
                    root.GetProperty("fat").GetDouble(),
                    null);
            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Netzwerk-/Verarbeitungsfehler: {ex.Message}";
            }
        }

        return new AiFoodPhotoResult("", 0, 0, 0, 0, lastErrorMessage ?? "Keines der bekannten Modelle war erreichbar.");
    }

    /// <summary>
    /// Schickt ein Foto einer Person an Gemini und lässt es dasselbe Foto so bearbeiten, dass es zeigt,
    /// wie die Person nach 6 Monaten konsequenter Ernährung/Training realistisch aussehen könnte -
    /// gleiches Bild-Input-Prinzip wie AnalyzeFoodPhotoAsync (inline_data), nur mit responseModalities
    /// TEXT+IMAGE, damit auch ein Bild zurückkommt (nicht nur Text wie bei den anderen Methoden hier).
    /// </summary>
    public async Task<AiImageResult> GenerateFutureBodyImageAsync(byte[] imageBytes, string mimeType, string transformationPrompt)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiImageResult(null, null, "Kein Gemini-API-Key hinterlegt. In den Einstellungen einrichten.");

        var base64Image = Convert.ToBase64String(imageBytes);
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = transformationPrompt },
                        new { inline_data = new { mime_type = mimeType, data = base64Image } }
                    }
                }
            },
            generationConfig = new { responseModalities = new[] { "TEXT", "IMAGE" } }
        };
        var requestJson = JsonSerializer.Serialize(requestBody);

        string? lastErrorMessage = null;

        foreach (var model in GetOrderedImageModels())
        {
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(ApiUrl(model, apiKey), content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastErrorMessage = TryExtractErrorMessage(responseJson) ?? $"Fehler {(int)response.StatusCode}";
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
                    return new AiImageResult(null, null, lastErrorMessage);
                }

                using var responseDoc = JsonDocument.Parse(responseJson);
                var parts = responseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                // Nicht auf einen festen Index verlassen - je nach Modell kann erst der Text-Teil,
                // dann das Bild kommen oder umgekehrt. Deshalb das Array durchsuchen, bis ein Teil
                // mit inlineData/inline_data gefunden wird (Google nutzt hier inkonsistent camelCase
                // in der Response, auch wenn der Request snake_case erwartet).
                foreach (var part in parts.EnumerateArray())
                {
                    JsonElement inlineData;
                    if (part.TryGetProperty("inlineData", out inlineData) || part.TryGetProperty("inline_data", out inlineData))
                    {
                        var resultMimeType = inlineData.TryGetProperty("mimeType", out var mt) ? mt.GetString()
                            : inlineData.TryGetProperty("mime_type", out var mt2) ? mt2.GetString()
                            : "image/png";
                        var base64Data = inlineData.GetProperty("data").GetString();
                        if (string.IsNullOrWhiteSpace(base64Data))
                            return new AiImageResult(null, null, "Leere Bilddaten von der KI erhalten.");

                        RememberWorkingImageModel(model);
                        return new AiImageResult(Convert.FromBase64String(base64Data), resultMimeType ?? "image/png", null);
                    }
                }

                lastErrorMessage = "Die KI hat kein Bild zurückgegeben (nur Text oder leere Antwort).";
            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Netzwerk-/Verarbeitungsfehler: {ex.Message}";
            }
        }

        return new AiImageResult(null, null, lastErrorMessage ?? "Keines der bekannten Bild-Modelle war erreichbar.");
    }

    /// <summary>Übersetzt einen kurzen, vom Nutzer eingegebenen Suchbegriff ins Englische, da TheMealDB
    /// (die externe Rezept-Datenbank) nur englische Suchbegriffe findet.</summary>
    public async Task<(string? Translated, string? Error)> TranslateSearchQueryToEnglishAsync(string germanQuery)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (null, "Kein Gemini-API-Key hinterlegt. In den Einstellungen einrichten.");

        var prompt = "Übersetze diesen Suchbegriff für eine Rezept-Suche ins Englische. Antworte NUR mit " +
            "der Übersetzung (1-3 Wörter, wie ein Suchbegriff), ohne Anführungszeichen, ohne Erklärung: " +
            $"\"{germanQuery}\"";

        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var requestJson = JsonSerializer.Serialize(requestBody);

        string? lastErrorMessage = null;

        foreach (var model in GetOrderedModels())
        {
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(ApiUrl(model, apiKey), content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastErrorMessage = TryExtractErrorMessage(responseJson) ?? $"Fehler {(int)response.StatusCode}";
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
                    return (null, lastErrorMessage);
                }

                using var responseDoc = JsonDocument.Parse(responseJson);
                var text = responseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text)) return (null, "Leere Antwort von der KI erhalten.");
                RememberWorkingModel(model);
                return (text.Trim().Trim('"', '.', ' '), null);
            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Netzwerk-/Verarbeitungsfehler: {ex.Message}";
            }
        }

        return (null, lastErrorMessage ?? "Keines der bekannten Modelle war erreichbar.");
    }

    /// <summary>
    /// Übersetzt eine ganze Liste von Rezepten (Name, Kategorie, Herkunft, Zutaten, Zubereitung) in
    /// EINEM Aufruf komplett ins Deutsche, statt vieler Einzel-Aufrufe - schont das kostenlose
    /// Anfragen-Kontingent. Mutiert die übergebenen Recipe-Objekte direkt (kein separates DTO nötig),
    /// damit die aufrufende Stelle unverändert weiterbinden kann.
    /// Ist <paramref name="goalContext"/> gesetzt (Ziel + Kalorien-/Makro-Ziel des Nutzers), bewertet
    /// derselbe Aufruf zusätzlich, ob jedes Rezept zu diesem Ziel passt (GoalFit/GoalFitReason) -
    /// spart einen zweiten separaten Aufruf gegenüber Übersetzung + Bewertung einzeln.
    /// </summary>
    public async Task<(bool Success, string? Error)> TranslateRecipesToGermanAsync(List<Recipe> recipes, string? goalContext = null)
    {
        if (recipes.Count == 0) return (true, null);

        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "Kein Gemini-API-Key hinterlegt. In den Einstellungen einrichten.");

        var payload = recipes.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            category = r.Category,
            area = r.Area,
            instructions = r.Instructions,
            ingredients = r.IngredientLines
        }).ToList();
        var payloadJson = JsonSerializer.Serialize(payload);

        var assessmentInstruction = string.IsNullOrWhiteSpace(goalContext)
            ? ""
            : $" Bewerte außerdem für jedes Rezept, ob es zum folgenden Nutzerziel passt: {goalContext} " +
              "Ergänze dafür im JSON pro Rezept \"goalFit\" (GENAU einer der drei Werte: \"Gut fürs Ziel\", " +
              "\"Neutral\", \"Nicht ideal\") und \"goalFitReason\" (ein kurzer, konkreter Satz, der erklärt " +
              "warum - bezogen auf Kalorien/Makros/Zutaten des Rezepts im Verhältnis zum Ziel).";

        var jsonFormatFields = string.IsNullOrWhiteSpace(goalContext)
            ? "\"ingredients\":[\"...\"]"
            : "\"ingredients\":[\"...\"],\"goalFit\":\"...\",\"goalFitReason\":\"...\"";

        var prompt =
            "Übersetze die folgenden Rezepte komplett ins Deutsche (Name, Kategorie, Herkunftsregion, " +
            "Zutatenliste, Zubereitungsanleitung). Übersetze NUR den Text, erfinde nichts hinzu, ändere " +
            "keine Mengenangaben oder Zahlen." +
            assessmentInstruction +
            " Antworte AUSSCHLIESSLICH mit einem JSON-Array, ohne Markdown, ohne Erklärung davor oder " +
            "danach, in genau diesem Format (gleiche Reihenfolge, \"id\" unverändert übernehmen): " +
            $"[{{\"id\":\"...\",\"name\":\"...\",\"category\":\"...\",\"area\":\"...\",\"instructions\":\"...\",{jsonFormatFields}}}] " +
            "WICHTIG für gültiges JSON: \"instructions\" muss ein EINZEILIGER JSON-String sein - " +
            "verwende dort NIEMALS echte Zeilenumbrüche, sondern schreibe stattdessen \"\\n\" (Backslash+n) " +
            "als Trennzeichen zwischen Zubereitungsschritten, genau wie im JSON-Standard üblich. " +
            "Hier die Rezepte: " + payloadJson;

        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var requestJson = JsonSerializer.Serialize(requestBody);

        string? lastErrorMessage = null;

        foreach (var model in GetOrderedModels())
        {
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(ApiUrl(model, apiKey), content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastErrorMessage = TryExtractErrorMessage(responseJson) ?? $"Fehler {(int)response.StatusCode}";
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) continue;
                    return (false, lastErrorMessage);
                }

                using var responseDoc = JsonDocument.Parse(responseJson);
                var text = responseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text)) return (false, "Leere Antwort von der KI erhalten.");

                text = text.Trim().Trim('`').Replace("json\n", "").Trim();

                using var translatedDoc = JsonDocument.Parse(text);
                foreach (var translatedItem in translatedDoc.RootElement.EnumerateArray())
                {
                    var id = translatedItem.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var match = recipes.FirstOrDefault(r => r.Id == id);
                    if (match is null) continue;

                    if (translatedItem.TryGetProperty("name", out var nameProp) && nameProp.GetString() is { } n && !string.IsNullOrWhiteSpace(n))
                        match.Name = n;
                    if (translatedItem.TryGetProperty("category", out var catProp))
                        match.Category = catProp.GetString();
                    if (translatedItem.TryGetProperty("area", out var areaProp))
                        match.Area = areaProp.GetString();
                    if (translatedItem.TryGetProperty("instructions", out var instrProp))
                        match.Instructions = instrProp.GetString();
                    if (translatedItem.TryGetProperty("ingredients", out var ingProp) && ingProp.ValueKind == JsonValueKind.Array)
                    {
                        match.IngredientLines = ingProp.EnumerateArray()
                            .Select(e => e.GetString() ?? string.Empty)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
                    }
                    if (translatedItem.TryGetProperty("goalFit", out var goalFitProp))
                        match.GoalFit = goalFitProp.GetString();
                    if (translatedItem.TryGetProperty("goalFitReason", out var goalFitReasonProp))
                        match.GoalFitReason = goalFitReasonProp.GetString();
                }

                RememberWorkingModel(model);
                return (true, null);
            }
            catch (Exception ex)
            {
                lastErrorMessage = $"Netzwerk-/Verarbeitungsfehler: {ex.Message}";
            }
        }

        return (false, lastErrorMessage ?? "Keines der bekannten Modelle war erreichbar.");
    }

    private static string? TryExtractErrorMessage(string responseJson)
    {
        try
        {
            using var errorDoc = JsonDocument.Parse(responseJson);
            return errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
        }
        catch
        {
            return null;
        }
    }
}
