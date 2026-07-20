using System.Linq;
using Microsoft.EntityFrameworkCore;
using NutriCoach.App.Data;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Verwaltet den persönlichen Wochenplan: welcher Trainings-Fokus (z. B. "Push", "Ruhetag")
/// an welchem Wochentag ansteht. Entweder selbst zusammengestellt oder automatisch vorgeschlagen.
/// </summary>
public class TrainingPlanService
{
    private static readonly DayOfWeek[] WeekOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public async Task<bool> HasPlanAsync(int userProfileId)
    {
        await using var context = new AppDbContext();
        return await context.TrainingPlanDays.AnyAsync(p => p.UserProfileId == userProfileId);
    }

    /// <summary>Liefert den Plan als Wochentag -> Fokus-Name, für alle 7 Tage (Standard "Frei" falls nichts hinterlegt).</summary>
    public async Task<Dictionary<DayOfWeek, string>> GetPlanAsync(int userProfileId)
    {
        await using var context = new AppDbContext();
        var entries = await context.TrainingPlanDays
            .Where(p => p.UserProfileId == userProfileId)
            .ToListAsync();

        var result = WeekOrder.ToDictionary(d => d, _ => "Frei");
        foreach (var entry in entries) result[entry.DayOfWeek] = entry.PlanName;
        return result;
    }

    public async Task<string> GetPlanForDayAsync(int userProfileId, DayOfWeek day)
    {
        var plan = await GetPlanAsync(userProfileId);
        return plan.TryGetValue(day, out var name) ? name : "Frei";
    }

    /// <summary>Speichert den kompletten Wochenplan (überschreibt bisherige Einträge).</summary>
    public async Task SavePlanAsync(int userProfileId, Dictionary<DayOfWeek, string> plan)
    {
        await using var context = new AppDbContext();

        var existing = await context.TrainingPlanDays
            .Where(p => p.UserProfileId == userProfileId)
            .ToListAsync();
        context.TrainingPlanDays.RemoveRange(existing);

        foreach (var (day, name) in plan)
        {
            context.TrainingPlanDays.Add(new TrainingPlanDay
            {
                UserProfileId = userProfileId,
                DayOfWeek = day,
                PlanName = string.IsNullOrWhiteSpace(name) ? "Frei" : name
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Schlägt einen einfachen, für Anfänger geeigneten Wochenplan vor, basierend auf der
    /// gewünschten Trainingshäufigkeit. Bei niedriger Frequenz: Ganzkörper. Bei höherer: Splits.
    /// </summary>
    public Dictionary<DayOfWeek, string> GenerateAutoPlan(int goalDaysPerWeek)
    {
        var plan = WeekOrder.ToDictionary(d => d, _ => "Frei");

        List<(int dayIndex, string name)> assignments = goalDaysPerWeek switch
        {
            <= 2 => new() { (0, "Ganzkörper"), (3, "Ganzkörper") },
            3 => new() { (0, "Ganzkörper A"), (2, "Ganzkörper B"), (4, "Ganzkörper C") },
            4 => new() { (0, "Oberkörper"), (1, "Unterkörper"), (3, "Oberkörper"), (4, "Unterkörper") },
            5 => new() { (0, "Push"), (1, "Pull"), (2, "Beine"), (3, "Push"), (4, "Pull") },
            6 => new() { (0, "Push"), (1, "Pull"), (2, "Beine"), (3, "Push"), (4, "Pull"), (5, "Beine") },
            _ => new() { (0, "Push"), (1, "Pull"), (2, "Beine"), (3, "Push"), (4, "Pull"), (5, "Beine"), (6, "Mobility") }
        };

        foreach (var (dayIndex, name) in assignments)
            plan[WeekOrder[dayIndex]] = name;

        return plan;
    }
}
