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
}

/// <summary>Aufsummierte Tageswerte, z. B. für die Kopfzeile des Ernährungstagebuchs.</summary>
public class DailyTotals
{
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public int WaterMl { get; set; }
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
            PhotoPath = food.PhotoPath
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
            WaterMl = waterMl
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
