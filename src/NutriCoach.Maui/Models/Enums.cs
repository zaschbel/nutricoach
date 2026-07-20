namespace NutriCoach.App.Models;

public enum Gender
{
    Männlich,
    Weiblich,
    Divers
}

/// <summary>Das primäre Ziel des Nutzers – steuert Kalorienbedarf, Makro-Split und Trainingsplan-Logik.</summary>
public enum FitnessGoal
{
    Abnehmen,
    MuskelAufbau,
    GewichtHalten,
    KraftSteigern,
    AusdauerVerbessern,
    AllgemeineGesundheit
}

/// <summary>Trainingserfahrung – beeinflusst, wie stark die KI Anfänger-Erklärungen vs. Detail-Zahlen liefert.</summary>
public enum ExperienceLevel
{
    Anfänger,       // < 6 Monate
    Fortgeschritten, // 6 Monate - 2 Jahre
    Erfahren        // > 2 Jahre
}

/// <summary>Wie hat sich das Gewicht/Training in den letzten Wochen entwickelt.</summary>
public enum RecentTrend
{
    KeineVeränderung,
    Zugenommen,
    Abgenommen,
    PauseGehabt,
    NeuGestartet
}

/// <summary>Alltagsaktivität abseits des Trainings – wichtig für NEAT/Grundumsatz-Schätzung.</summary>
public enum DailyJobActivity
{
    ÜberwiegendSitzend,      // Büro, Homeoffice
    ÜberwiegendStehend,      // Verkauf, Service
    KörperlichAktiv,         // Handwerk, Lager
    SehrKörperlichAnstrengend // Baustelle, Landwirtschaft
}

/// <summary>Allgemeines Aktivitätslevel für die Grundumsatz-Formel (PAL-Faktor).</summary>
public enum ActivityLevel
{
    Sitzend,        // PAL ~1.2
    LeichtAktiv,    // PAL ~1.375
    MäßigAktiv,     // PAL ~1.55
    SehrAktiv,      // PAL ~1.725
    ExtremAktiv     // PAL ~1.9
}

public enum InjurySeverity
{
    LeichtesZiehen,
    ChronischerSchmerz,
    AkuteVerletzung,
    NachOperation,
    Eingeschränkt
}

public enum MealType
{
    Frühstück,
    Mittagessen,
    Abendessen,
    Snack
}

public enum CardioType
{
    Laufen,
    Radfahren,
    SpinningBike,
    Schwimmen,
    Rudern,
    Walking,
    Sonstiges
}

public enum CardioLocation
{
    Drinnen,
    Draußen
}

/// <summary>Steuert, welche Eingabefelder beim Eintragen einer Übung sinnvoll sind.</summary>
public enum ExerciseInputType
{
    GewichtWiederholungen,   // klassisches Krafttraining: Gewicht x Wiederholungen (Standard)
    NurWiederholungen,       // Körpergewichtsübungen wie Liegestütze, Klimmzüge
    StreckeZeit,             // Laufen, Radfahren draußen: Distanz + Zeit
    WiderstandZeit           // Spinning-Bike, Rudergerät: Widerstand + Zeit
}

/// <summary>
/// Bei "Drinnen" ausgeführten Strecke/Zeit-Übungen: welcher zusätzliche Parameter ist sinnvoll?
/// Radfahren drinnen (Spinning-Bike) -> Widerstand, Laufen drinnen (Laufband) -> Steigung.
/// </summary>
public enum IndoorCardioMetric
{
    Keine,
    Widerstand,
    Steigung
}
