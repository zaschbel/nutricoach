using Microsoft.EntityFrameworkCore;
using NutriCoach.App.Models;
using System.IO;

namespace NutriCoach.App.Data;


/// <summary>
/// Zentrale Datenbank der App (lokale SQLite-Datei im App-eigenen Speicherbereich des Geräts).
/// Alles, was der Nutzer einmal eingibt, landet hier und wird nie wieder abgefragt –
/// das ist die geforderte "Memory-Funktion".
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<InjuryRecord> Injuries => Set<InjuryRecord>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<NutritionEntry> NutritionEntries => Set<NutritionEntry>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<StrengthSetEntry> StrengthSets => Set<StrengthSetEntry>();
    public DbSet<CardioEntry> CardioEntries => Set<CardioEntry>();
    public DbSet<WaterEntry> WaterEntries => Set<WaterEntry>();
    public DbSet<StepsEntry> StepsEntries => Set<StepsEntry>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<RestDay> RestDays => Set<RestDay>();
    public DbSet<TrainingPlanDay> TrainingPlanDays => Set<TrainingPlanDay>();

    /// <summary>
    /// Plattformunabhängiger App-Datenordner: auf iOS liegt das im sandboxed Documents-Bereich der App,
    /// auf Android im internen App-Speicher - FileSystem.AppDataDirectory (MAUI Essentials) löst das
    /// automatisch richtig auf, ganz ohne plattformspezifischen Code hier.
    /// </summary>
    public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, "nutricoach.db");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.Injuries)
            .WithOne()
            .HasForeignKey(i => i.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.BodyMeasurements)
            .WithOne()
            .HasForeignKey(b => b.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.NutritionEntries)
            .WithOne()
            .HasForeignKey(n => n.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.TrainingSessions)
            .WithOne()
            .HasForeignKey(t => t.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingSession>()
            .HasMany(t => t.StrengthSets)
            .WithOne()
            .HasForeignKey(s => s.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingSession>()
            .HasMany(t => t.CardioEntries)
            .WithOne()
            .HasForeignKey(c => c.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NutritionEntry>()
            .HasOne(n => n.FoodItem)
            .WithMany()
            .HasForeignKey(n => n.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enums als lesbare Strings statt Zahlen speichern -> Datenbank bleibt debugbar
        modelBuilder.Entity<UserProfile>().Property(u => u.Gender).HasConversion<string>();
        modelBuilder.Entity<UserProfile>().Property(u => u.Goal).HasConversion<string>();
        modelBuilder.Entity<UserProfile>().Property(u => u.Experience).HasConversion<string>();
        modelBuilder.Entity<UserProfile>().Property(u => u.RecentTrend).HasConversion<string>();
        modelBuilder.Entity<UserProfile>().Property(u => u.JobActivity).HasConversion<string>();
        modelBuilder.Entity<UserProfile>().Property(u => u.ActivityLevel).HasConversion<string>();
        modelBuilder.Entity<InjuryRecord>().Property(i => i.Severity).HasConversion<string>();
        modelBuilder.Entity<NutritionEntry>().Property(n => n.Meal).HasConversion<string>();
        modelBuilder.Entity<CardioEntry>().Property(c => c.Type).HasConversion<string>();
        modelBuilder.Entity<CardioEntry>().Property(c => c.Location).HasConversion<string>();
        modelBuilder.Entity<Exercise>().Property(e => e.InputType).HasConversion<string>();
        modelBuilder.Entity<Exercise>().Property(e => e.IndoorMetric).HasConversion<string>();
        modelBuilder.Entity<TrainingPlanDay>().Property(p => p.DayOfWeek).HasConversion<string>();
    }
}
