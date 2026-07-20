using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

public record FoodAssessment(string Rating, string RatingIcon, string Explanation, string? AlternativeHint);

/// <summary>
/// Bewertet ein Lebensmittel in Bezug auf das Ziel des Nutzers - eine regelbasierte erste Stufe
/// des "KI-Coach"-Gedankens aus der ursprünglichen Anforderung. Eine echte KI-Anbindung (Claude API)
/// kann das später ersetzen/ergänzen und deutlich differenziertere Einschätzungen liefern.
/// </summary>
public static class FoodAssessmentService
{
    public static FoodAssessment Assess(FoodItem food, FitnessGoal goal, double amountGrams)
    {
        var factor = amountGrams / 100.0;
        var kcal = food.KcalPer100 * factor;
        var protein = food.ProteinPer100 * factor;
        var sugar = food.SugarPer100 * factor;
        var fiber = food.FiberPer100 * factor;
        var satFat = food.SaturatedFatPer100 * factor;

        var proteinPercentOfKcal = kcal > 0 ? (protein * 4 / kcal) * 100 : 0;
        var sugarPercentOfKcal = kcal > 0 ? (sugar * 4 / kcal) * 100 : 0;

        var reasons = new List<string>();
        var concerns = new List<string>();

        if (proteinPercentOfKcal >= 25) reasons.Add("guter Eiweißanteil");
        if (fiber >= 3) reasons.Add("liefert ordentlich Ballaststoffe");
        if (food.KcalPer100 <= 50) reasons.Add("kalorienarm");

        if (sugarPercentOfKcal >= 40) concerns.Add("großer Teil der Kalorien kommt aus Zucker");
        if (satFat >= 5) concerns.Add("recht viel gesättigtes Fett");
        if (food.SaltPer100 >= 1.2) concerns.Add("relativ salzhaltig");

        // Zielbezogene Gewichtung
        var goalNote = goal switch
        {
            FitnessGoal.Abnehmen when food.KcalPer100 > 250 && proteinPercentOfKcal < 15 =>
                "Für dein Abnehmziel ist das eher kalorienreich, ohne viel Eiweiß zu liefern, das lange sattmacht.",
            FitnessGoal.Abnehmen when proteinPercentOfKcal >= 25 =>
                "Passt gut zu deinem Abnehmziel - Eiweiß hält länger satt.",
            FitnessGoal.MuskelAufbau when proteinPercentOfKcal < 15 =>
                "Für den Muskelaufbau eher wenig Eiweiß im Verhältnis zu den Kalorien.",
            FitnessGoal.MuskelAufbau when proteinPercentOfKcal >= 25 =>
                "Gut für den Muskelaufbau - solide Eiweißquelle.",
            _ => null
        };
        if (goalNote is not null)
        {
            if (goalNote.StartsWith("Passt") || goalNote.StartsWith("Gut"))
                reasons.Add(goalNote);
            else
                concerns.Add(goalNote);
        }

        // Gesamturteil
        string rating;
        string icon;
        if (concerns.Count == 0 && reasons.Count > 0)
        {
            rating = "Gute Wahl";
            icon = "✅";
        }
        else if (concerns.Count > reasons.Count)
        {
            rating = "Nicht ideal für dein Ziel";
            icon = "⚠️";
        }
        else
        {
            rating = "Neutral";
            icon = "➖";
        }

        var explanationParts = new List<string>();
        if (reasons.Count > 0) explanationParts.Add(string.Join(", ", reasons));
        if (concerns.Count > 0) explanationParts.Add(string.Join(", ", concerns));

        var joined = string.Join(". ", explanationParts);
        var explanation = joined.Length > 0
            ? char.ToUpper(joined[0]) + joined[1..] + "."
            : "Keine Besonderheiten bei diesem Lebensmittel.";

        string? alternative = BuildConcreteAlternative(food, goal, proteinPercentOfKcal, sugarPercentOfKcal, satFat);

        return new FoodAssessment(rating, icon, explanation, alternative);
    }

    /// <summary>
    /// Baut einen möglichst konkreten Alternativ-Vorschlag statt nur allgemeinem Rat -
    /// erkennt grob die Lebensmittel-Kategorie am Namen und schlägt einen passenden Ersatz vor.
    /// </summary>
    private static string? BuildConcreteAlternative(FoodItem food, FitnessGoal goal,
        double proteinPercentOfKcal, double sugarPercentOfKcal, double satFat)
    {
        var name = food.Name.ToLower();
        var lowProteinHighKcal = food.KcalPer100 > 200 && proteinPercentOfKcal < 15;

        // Kategoriebezogene, konkrete Vorschläge zuerst versuchen
        if (lowProteinHighKcal || sugarPercentOfKcal >= 40 || satFat >= 5)
        {
            if (name.Contains("toast") || name.Contains("brot") || name.Contains("brötchen"))
                return "Probier stattdessen Vollkorn- oder Eiweißbrot, oder kombiniere es mit magerem Quark/Aufschnitt für mehr Eiweiß.";
            if (name.Contains("käse") || name.Contains("gouda") || name.Contains("cheddar"))
                return "Probier eine fettärmere Käsesorte (z. B. körniger Frischkäse, Harzer oder ein leichter Schnittkäse).";
            if (name.Contains("wurst") || name.Contains("salami") || name.Contains("schinken"))
                return "Probier magere Geflügelwurst oder gekochten Schinken statt fettreicher Wurstsorten.";
            if (name.Contains("melone") || name.Contains("obst") || name.Contains("frucht"))
                return "Kombiniere es mit etwas Eiweiß (z. B. griechischer Joghurt) oder Nüssen, um den Zuckeranstieg abzufedern.";
            if (name.Contains("schokolade") || name.Contains("keks") || name.Contains("kuchen"))
                return "Probier dunkle Schokolade (hoher Kakaoanteil) oder eine kleinere Portion mit etwas Eiweiß dazu.";
            if (name.Contains("pizza") || name.Contains("pommes") || name.Contains("fritt"))
                return "Falls verfügbar: eine Variante mit mehr Gemüse/magerem Belag statt viel Käse und Fett.";
            if (name.Contains("limonade") || name.Contains("cola") || name.Contains("saft"))
                return "Probier Wasser mit etwas Frucht, ungesüßten Tee, oder stark verdünnten Saft.";

            // Genereller Fallback ohne erkannte Kategorie
            if (goal == FitnessGoal.Abnehmen || goal == FitnessGoal.MuskelAufbau)
                return "Achte auf eine eiweißreichere Alternative in der gleichen Kategorie (z. B. Magerquark, Hüttenkäse, mageres Geflügel), das hält länger satt.";
            if (sugarPercentOfKcal >= 40)
                return "Achte auf eine zuckerärmere Variante, oder kombiniere es mit Eiweiß/Ballaststoffen.";
            if (satFat >= 5)
                return "Achte auf eine Variante mit weniger gesättigtem Fett.";
        }

        return null;
    }
}
