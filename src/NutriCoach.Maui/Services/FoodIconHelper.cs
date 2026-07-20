using System.Linq;

namespace NutriCoach.App.Services;

/// <summary>Ordnet einem Lebensmittelnamen ein passendes Emoji-Icon zu (einfache Schlüsselwort-Erkennung).</summary>
public static class FoodIconHelper
{
    private static readonly (string[] Keywords, string Icon)[] Mappings =
    {
        (new[] { "toast", "brot", "brötchen", "baguette", "vollkorn" }, "🍞"),
        (new[] { "käse", "gouda", "cheddar", "mozzarella", "parmesan", "feta" }, "🧀"),
        (new[] { "apfel" }, "🍎"),
        (new[] { "banane" }, "🍌"),
        (new[] { "wassermelone", "melone" }, "🍉"),
        (new[] { "orange", "mandarine" }, "🍊"),
        (new[] { "trauben" }, "🍇"),
        (new[] { "erdbeere" }, "🍓"),
        (new[] { "huhn", "hähnchen", "hühnchen", "geflügel", "pute" }, "🍗"),
        (new[] { "rind", "steak", "beef" }, "🥩"),
        (new[] { "fisch", "lachs", "thunfisch", "hering" }, "🐟"),
        (new[] { "ei", "eier" }, "🥚"),
        (new[] { "milch" }, "🥛"),
        (new[] { "joghurt" }, "🥣"),
        (new[] { "reis" }, "🍚"),
        (new[] { "nudeln", "pasta", "spaghetti" }, "🍝"),
        (new[] { "pizza" }, "🍕"),
        (new[] { "schokolade", "kakao" }, "🍫"),
        (new[] { "kartoffel", "pommes" }, "🥔"),
        (new[] { "salat", "gemüse", "brokkoli", "spinat" }, "🥗"),
        (new[] { "nuss", "nüsse", "mandel", "erdnuss" }, "🥜"),
        (new[] { "wasser" }, "💧"),
        (new[] { "kaffee" }, "☕"),
        (new[] { "tee" }, "🍵"),
    };

    public static string GetIcon(string foodName)
    {
        var lower = foodName.ToLower();
        foreach (var (keywords, icon) in Mappings)
        {
            if (keywords.Any(lower.Contains))
                return icon;
        }
        return "🍽️";
    }
}
