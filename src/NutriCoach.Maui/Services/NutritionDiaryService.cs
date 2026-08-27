using System.Linq;
using Microsoft.EntityFrameworkCore;
using NutriCoach.App.Data;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>Ein Tagebuch-Eintrag zusammen mit den daraus berechneten Nährwerten für die angegebene Menge.</summary>
public class NutritionEntryDisplay
{
    public int Id { get; set; }
    public int FoodItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double AmountGrams { get; set; }
    public MealType Meal { get; set; }
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public string? PhotoPath { get; set; }
    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath);
    public string Icon => FoodIconHelper.GetIcon(Name);

    // Zusaetzliche Makros + Mikronaehrstoffe fuer diesen Eintrag (bereits auf die tatsaechlich
    // gegessene Menge hochgerechnet, nicht mehr "je 100g" wie im FoodItem).
    public double Sugar { get; set; }
    public double SaturatedFat { get; set; }
    public double Fiber { get; set; }
    public double Salt { get; set; }
    public double VitaminA { get; set; }
    public double VitaminB1 { get; set; }
    public double VitaminB2 { get; set; }
    public double VitaminB3 { get; set; }
    public double VitaminB5 { get; set; }
    public double VitaminB6 { get; set; }
    public double VitaminB7 { get; set; }
    public double VitaminB9 { get; set; }
    public double VitaminB12 { get; set; }
    public double VitaminC { get; set; }
    public double VitaminD { get; set; }
    public double VitaminE { get; set; }
    public double VitaminK { get; set; }
    public double Calcium { get; set; }
    public double Magnesium { get; set; }
    public double Potassium { get; set; }
    public double Sodium { get; set; }
    public double Phosphorus { get; set; }
    public double Chloride { get; set; }
    public double Sulfur { get; set; }
    public double Iron { get; set; }
    public double Zinc { get; set; }
    public double Selenium { get; set; }
    public double Copper { get; set; }
    public double Manganese { get; set; }
    public double Iodine { get; set; }
    public double Fluoride { get; set; }
    public double Chromium { get; set; }
    public double Molybdenum { get; set; }
    public double Cobalt { get; set; }
    public double Silicon { get; set; }
    public double SugarAlcohols { get; set; }
    public double Alcohol { get; set; }
    public double Omega3 { get; set; }
    public double Omega6 { get; set; }
    public double Omega9 { get; set; }
}

/// <summary>Aufsummierte Tageswerte, z. B. für die Kopfzeile des Ernährungstagebuchs.</summary>
public class DailyTotals
{
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public int WaterMl { get; set; }

    public double Sugar { get; set; }
    public double SaturatedFat { get; set; }
    public double Fiber { get; set; }
    public double Salt { get; set; }
    public double VitaminA { get; set; }
    public double VitaminB1 { get; set; }
    public double VitaminB2 { get; set; }
    public double VitaminB3 { get; set; }
    public double VitaminB5 { get; set; }
    public double VitaminB6 { get; set; }
    public double VitaminB7 { get; set; }
    public double VitaminB9 { get; set; }
    public double VitaminB12 { get; set; }
    public double VitaminC { get; set; }
    public double VitaminD { get; set; }
    public double VitaminE { get; set; }
    public double VitaminK { get; set; }
    public double Calcium { get; set; }
    public double Magnesium { get; set; }
    public double Potassium { get; set; }
    public double Sodium { get; set; }
    public double Phosphorus { get; set; }
    public double Chloride { get; set; }
    public double Sulfur { get; set; }
    public double Iron { get; set; }
    public double Zinc { get; set; }
    public double Selenium { get; set; }
    public double Copper { get; set; }
    public double Manganese { get; set; }
    public double Iodine { get; set; }
    public double Fluoride { get; set; }
    public double Chromium { get; set; }
    public double Molybdenum { get; set; }
    public double Cobalt { get; set; }
    public double Silicon { get; set; }
    public double SugarAlcohols { get; set; }
    public double Alcohol { get; set; }
    public double Omega3 { get; set; }
    public double Omega6 { get; set; }
    public double Omega9 { get; set; }
}

/// <summary>Ein zuletzt verwendetes Lebensmittel mit der Menge, die beim letzten Mal eingetragen wurde.</summary>
public class RecentFoodDisplay
{
    public FoodItem Food { get; set; } = null!;
    public double LastAmountGrams { get; set; }
    public string Icon => FoodIconHelper.GetIcon(Food.Name);
}

public class NutritionDiaryService
{
    private static NutritionEntryDisplay ToDisplay(NutritionEntry entry)
    {
        var food = entry.FoodItem!;
        var factor = entry.AmountGrams / 100.0;
        return new NutritionEntryDisplay
        {
            Id = entry.Id,
            FoodItemId = entry.FoodItemId,
            Name = food.Name,
            AmountGrams = entry.AmountGrams,
            Meal = entry.Meal,
            Kcal = Math.Round(food.KcalPer100 * factor, 0),
            Protein = Math.Round(food.ProteinPer100 * factor, 1),
            Carbs = Math.Round(food.CarbsPer100 * factor, 1),
            Fat = Math.Round(food.FatPer100 * factor, 1),
            PhotoPath = food.PhotoPath,
            Sugar = food.SugarPer100 * factor,
            SaturatedFat = food.SaturatedFatPer100 * factor,
            Fiber = food.FiberPer100 * factor,
            Salt = food.SaltPer100 * factor,
            VitaminA = food.VitaminAPer100 * factor,
            VitaminB1 = food.VitaminB1Per100 * factor,
            VitaminB2 = food.VitaminB2Per100 * factor,
            VitaminB3 = food.VitaminB3Per100 * factor,
            VitaminB5 = food.VitaminB5Per100 * factor,
            VitaminB6 = food.VitaminB6Per100 * factor,
            VitaminB7 = food.VitaminB7Per100 * factor,
            VitaminB9 = food.VitaminB9Per100 * factor,
            VitaminB12 = food.VitaminB12Per100 * factor,
            VitaminC = food.VitaminCPer100 * factor,
            VitaminD = food.VitaminDPer100 * factor,
            VitaminE = food.VitaminEPer100 * factor,
            VitaminK = food.VitaminKPer100 * factor,
            Calcium = food.CalciumPer100 * factor,
            Magnesium = food.MagnesiumPer100 * factor,
            Potassium = food.PotassiumPer100 * factor,
            Sodium = food.SodiumPer100 * factor,
            Phosphorus = food.PhosphorusPer100 * factor,
            Chloride = food.ChloridePer100 * factor,
            Sulfur = food.SulfurPer100 * factor,
            Iron = food.IronPer100 * factor,
            Zinc = food.ZincPer100 * factor,
            Selenium = food.SeleniumPer100 * factor,
            Copper = food.CopperPer100 * factor,
            Manganese = food.ManganesePer100 * factor,
            Iodine = food.IodinePer100 * factor,
            Fluoride = food.FluoridePer100 * factor,
            Chromium = food.ChromiumPer100 * factor,
            Molybdenum = food.MolybdenumPer100 * factor,
            Cobalt = food.CobaltPer100 * factor,
            Silicon = food.SiliconPer100 * factor,
            SugarAlcohols = food.SugarAlcoholsPer100 * factor,
            Alcohol = food.AlcoholPer100 * factor,
            Omega3 = food.Omega3Per100 * factor,
            Omega6 = food.Omega6Per100 * factor,
            Omega9 = food.Omega9Per100 * factor
        };
    }

    public async Task<List<NutritionEntryDisplay>> GetEntriesForDateAsync(int userProfileId, DateOnly date)
    {
        await using var context = new AppDbContext();
        var entries = await context.NutritionEntries
            .Include(e => e.FoodItem)
            .Where(e => e.UserProfileId == userProfileId && e.Date == date)
            .ToListAsync();

        return entries.Select(ToDisplay).ToList();
    }

    /// <summary>
    /// Die zuletzt verwendeten Lebensmittel mit der jeweils zuletzt eingetragenen Menge - für den
    /// "Zuletzt verwendet"-Schnellzugriff beim Hinzufügen, damit man nicht jedes Mal neu suchen muss.
    /// </summary>
    public async Task<List<RecentFoodDisplay>> GetRecentlyUsedFoodsAsync(int userProfileId, int limit = 8)
    {
        await using var context = new AppDbContext();

        var recentEntries = await context.NutritionEntries
            .Include(e => e.FoodItem)
            .Where(e => e.UserProfileId == userProfileId)
            .OrderByDescending(e => e.Id)
            .ToListAsync();

        var seen = new HashSet<int>();
        var result = new List<RecentFoodDisplay>();

        foreach (var entry in recentEntries)
        {
            if (!seen.Add(entry.FoodItemId)) continue;
            result.Add(new RecentFoodDisplay { Food = entry.FoodItem!, LastAmountGrams = entry.AmountGrams });
            if (result.Count >= limit) break;
        }

        return result;
    }

    /// <summary>Tagesgesamtwerte für einen Zeitraum auf einmal - Grundlage für die Wochen-Statistik-Grafiken.</summary>
    public async Task<Dictionary<DateOnly, DailyTotals>> GetDailyTotalsForRangeAsync(int userProfileId, DateOnly start, DateOnly end)
    {
        var result = new Dictionary<DateOnly, DailyTotals>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            result[date] = await GetDailyTotalsAsync(userProfileId, date);
        }
        return result;
    }

    public async Task<DailyTotals> GetDailyTotalsAsync(int userProfileId, DateOnly date)
    {
        var entries = await GetEntriesForDateAsync(userProfileId, date);

        await using var context = new AppDbContext();
        var waterMl = await context.WaterEntries
            .Where(w => w.UserProfileId == userProfileId && w.Date == date)
            .SumAsync(w => (int?)w.AmountMl) ?? 0;

        return new DailyTotals
        {
            Kcal = entries.Sum(e => e.Kcal),
            Protein = entries.Sum(e => e.Protein),
            Carbs = entries.Sum(e => e.Carbs),
            Fat = entries.Sum(e => e.Fat),
            WaterMl = waterMl,
            Sugar = entries.Sum(e => e.Sugar),
            SaturatedFat = entries.Sum(e => e.SaturatedFat),
            Fiber = entries.Sum(e => e.Fiber),
            Salt = entries.Sum(e => e.Salt),
            VitaminA = entries.Sum(e => e.VitaminA),
            VitaminB1 = entries.Sum(e => e.VitaminB1),
            VitaminB2 = entries.Sum(e => e.VitaminB2),
            VitaminB3 = entries.Sum(e => e.VitaminB3),
            VitaminB5 = entries.Sum(e => e.VitaminB5),
            VitaminB6 = entries.Sum(e => e.VitaminB6),
            VitaminB7 = entries.Sum(e => e.VitaminB7),
            VitaminB9 = entries.Sum(e => e.VitaminB9),
            VitaminB12 = entries.Sum(e => e.VitaminB12),
            VitaminC = entries.Sum(e => e.VitaminC),
            VitaminD = entries.Sum(e => e.VitaminD),
            VitaminE = entries.Sum(e => e.VitaminE),
            VitaminK = entries.Sum(e => e.VitaminK),
            Calcium = entries.Sum(e => e.Calcium),
            Magnesium = entries.Sum(e => e.Magnesium),
            Potassium = entries.Sum(e => e.Potassium),
            Sodium = entries.Sum(e => e.Sodium),
            Phosphorus = entries.Sum(e => e.Phosphorus),
            Chloride = entries.Sum(e => e.Chloride),
            Sulfur = entries.Sum(e => e.Sulfur),
            Iron = entries.Sum(e => e.Iron),
            Zinc = entries.Sum(e => e.Zinc),
            Selenium = entries.Sum(e => e.Selenium),
            Copper = entries.Sum(e => e.Copper),
            Manganese = entries.Sum(e => e.Manganese),
            Iodine = entries.Sum(e => e.Iodine),
            Fluoride = entries.Sum(e => e.Fluoride),
            Chromium = entries.Sum(e => e.Chromium),
            Molybdenum = entries.Sum(e => e.Molybdenum),
            Cobalt = entries.Sum(e => e.Cobalt),
            Silicon = entries.Sum(e => e.Silicon),
            SugarAlcohols = entries.Sum(e => e.SugarAlcohols),
            Alcohol = entries.Sum(e => e.Alcohol),
            Omega3 = entries.Sum(e => e.Omega3),
            Omega6 = entries.Sum(e => e.Omega6),
            Omega9 = entries.Sum(e => e.Omega9)
        };
    }

    public async Task AddEntryAsync(int userProfileId, int foodItemId, double amountGrams, MealType meal, DateOnly date)
    {
        await using var context = new AppDbContext();
        context.NutritionEntries.Add(new NutritionEntry
        {
            UserProfileId = userProfileId,
            FoodItemId = foodItemId,
            AmountGrams = amountGrams,
            Meal = meal,
            Date = date
        });
        await context.SaveChangesAsync();
    }

    public async Task RemoveEntryAsync(int entryId)
    {
        await using var context = new AppDbContext();
        var entry = await context.NutritionEntries.FindAsync(entryId);
        if (entry is not null)
        {
            context.NutritionEntries.Remove(entry);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>Ändert nachträglich die Menge eines bestehenden Tagebuch-Eintrags.</summary>
    public async Task UpdateEntryAmountAsync(int entryId, double newAmountGrams)
    {
        await using var context = new AppDbContext();
        var entry = await context.NutritionEntries.FindAsync(entryId);
        if (entry is not null)
        {
            entry.AmountGrams = newAmountGrams;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>Ermittelt, an welchen Tagen im angegebenen Zeitraum bereits Einträge (Essen oder Wasser) existieren.</summary>
    public async Task<HashSet<DateOnly>> GetDatesWithEntriesAsync(int userProfileId, DateOnly start, DateOnly end)
    {
        await using var context = new AppDbContext();

        var foodDates = await context.NutritionEntries
            .Where(e => e.UserProfileId == userProfileId && e.Date >= start && e.Date <= end)
            .Select(e => e.Date)
            .ToListAsync();

        var waterDates = await context.WaterEntries
            .Where(w => w.UserProfileId == userProfileId && w.Date >= start && w.Date <= end)
            .Select(w => w.Date)
            .ToListAsync();

        return foodDates.Concat(waterDates).ToHashSet();
    }

    public async Task AddWaterAsync(int userProfileId, int amountMl, DateOnly date)
    {
        await using var context = new AppDbContext();
        context.WaterEntries.Add(new WaterEntry
        {
            UserProfileId = userProfileId,
            AmountMl = amountMl,
            Date = date,
            Time = TimeOnly.FromDateTime(DateTime.Now)
        });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Setzt die Tages-Wassermenge direkt auf einen festen Wert (für den Schieberegler) -
    /// ersetzt alle bisherigen Einträge des Tages durch einen einzigen, statt sie zu addieren.
    /// </summary>
    public async Task SetWaterForDateAsync(int userProfileId, DateOnly date, int totalMl)
    {
        await using var context = new AppDbContext();

        var existing = await context.WaterEntries
            .Where(w => w.UserProfileId == userProfileId && w.Date == date)
            .ToListAsync();
        context.WaterEntries.RemoveRange(existing);

        if (totalMl > 0)
        {
            context.WaterEntries.Add(new WaterEntry
            {
                UserProfileId = userProfileId,
                AmountMl = totalMl,
                Date = date,
                Time = TimeOnly.FromDateTime(DateTime.Now)
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Liest die für den Tag eingetragene Schrittzahl (0, falls noch nichts eingetragen wurde).</summary>
    /// <summary>Schrittzahlen für einen Zeitraum (z. B. eine Woche) auf einmal - für das Aktivitäts-Diagramm.</summary>
    public async Task<Dictionary<DateOnly, int>> GetStepsForRangeAsync(int userProfileId, DateOnly start, DateOnly end)
    {
        await using var context = new AppDbContext();
        var entries = await context.StepsEntries
            .Where(s => s.UserProfileId == userProfileId && s.Date >= start && s.Date <= end)
            .ToListAsync();
        return entries.ToDictionary(e => e.Date, e => e.Steps);
    }

    public async Task<int> GetStepsForDateAsync(int userProfileId, DateOnly date)
    {
        await using var context = new AppDbContext();
        var entry = await context.StepsEntries.FirstOrDefaultAsync(s => s.UserProfileId == userProfileId && s.Date == date);
        return entry?.Steps ?? 0;
    }

    /// <summary>Setzt/überschreibt die Schrittzahl für den Tag (ein Eintrag pro Tag, wie beim Wasser-Schieberegler).</summary>
    public async Task SetStepsForDateAsync(int userProfileId, DateOnly date, int steps)
    {
        await using var context = new AppDbContext();
        var existing = await context.StepsEntries.FirstOrDefaultAsync(s => s.UserProfileId == userProfileId && s.Date == date);
        if (existing is not null)
        {
            existing.Steps = steps;
        }
        else
        {
            context.StepsEntries.Add(new StepsEntry { UserProfileId = userProfileId, Date = date, Steps = steps });
        }
        await context.SaveChangesAsync();
    }
}
