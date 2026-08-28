namespace NutriCoach.App.Services;

/// <summary>Ein Tag in der Wochen-Navigationsleiste des Dashboards.</summary>
public record DayInfo(DateOnly Date, string DayAbbrev, int DayNumber, bool IsSelected, bool IsToday, bool HasEntries, bool IsRestDay, bool IsBlank = false);

/// <summary>Ein Balken im Wochen-Aktivitäts-Diagramm auf dem Dashboard.</summary>
public record WeeklyActivityBar(string DayAbbrev, int Steps, double HeightRatio, bool IsToday);
