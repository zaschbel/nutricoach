using NutriCoach.App.Models;

namespace NutriCoach.App.Data;

/// <summary>
/// Startbefüllung der Übungsdatenbank. MET-Werte sind grobe, allgemein anerkannte Richtwerte
/// (Compendium of Physical Activities) für die Kalorienschätzung - keine Bilder, siehe README.
/// </summary>
public static class ExerciseSeedData
{
    public static List<Exercise> GetSeedExercises() => new()
    {
        // ---------------- Brust ----------------
        new() { Name = "Bankdrücken", PrimaryMuscleGroup = "Brust", SecondaryMuscleGroup = "Trizeps", Equipment = "Langhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 6.0, Tip = "Schulterblätter zusammenziehen, Stange kontrolliert bis knapp über die Brust senken." },
        new() { Name = "Kurzhantel-Schrägbankdrücken", PrimaryMuscleGroup = "Brust", SecondaryMuscleGroup = "Schulter", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 6.0, Tip = "Trainiert verstärkt den oberen Brustmuskel." },
        new() { Name = "Kabelzug Fliegende", PrimaryMuscleGroup = "Brust", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.0, Tip = "Leichte Ellenbogenbeugung beibehalten, Bewegung kommt aus der Schulter." },
        new() { Name = "Liegestütze", PrimaryMuscleGroup = "Brust", SecondaryMuscleGroup = "Trizeps", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 7.0, Tip = "Körper von Kopf bis Ferse in einer Linie halten." },
        new() { Name = "Butterfly-Maschine", PrimaryMuscleGroup = "Brust", Equipment = "Maschine", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.0, Tip = "Bewegung langsam und kontrolliert, am Endpunkt kurz halten." },

        // ---------------- Rücken ----------------
        new() { Name = "Klimmzüge", PrimaryMuscleGroup = "Rücken", SecondaryMuscleGroup = "Bizeps", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 8.0, Tip = "Schulterblätter aktiv nach unten ziehen, kontrolliert ablassen." },
        new() { Name = "Latzug", PrimaryMuscleGroup = "Rücken", SecondaryMuscleGroup = "Bizeps", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.0, Tip = "Stange zur oberen Brust ziehen, Rücken leicht nach hinten neigen." },
        new() { Name = "Rudern vorgebeugt", PrimaryMuscleGroup = "Rücken", SecondaryMuscleGroup = "Bizeps", Equipment = "Langhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 6.0, Tip = "Rücken gerade halten, Ellenbogen eng am Körper führen." },
        new() { Name = "Kabelrudern sitzend", PrimaryMuscleGroup = "Rücken", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.0, Tip = "Brust raus, Griff zum Bauchnabel ziehen." },
        new() { Name = "Kreuzheben", PrimaryMuscleGroup = "Rücken", SecondaryMuscleGroup = "Beine", Equipment = "Langhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 6.0, Tip = "Rücken während der gesamten Bewegung neutral/gerade halten." },

        // ---------------- Beine ----------------
        new() { Name = "Kniebeugen", PrimaryMuscleGroup = "Beine", SecondaryMuscleGroup = "Gesäß", Equipment = "Langhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 6.0, Tip = "Knie in Richtung der Zehen, Rücken aufrecht." },
        new() { Name = "Beinpresse", PrimaryMuscleGroup = "Beine", Equipment = "Maschine", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.5, Tip = "Füße schulterbreit, Knie nicht durchdrücken am oberen Punkt." },
        new() { Name = "Ausfallschritte", PrimaryMuscleGroup = "Beine", SecondaryMuscleGroup = "Gesäß", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 5.0, Tip = "Oberkörper aufrecht, vorderes Knie nicht über die Zehenspitze." },
        new() { Name = "Beinstrecker", PrimaryMuscleGroup = "Beine", Equipment = "Maschine", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.5, Tip = "Bewegung langsam, am oberen Punkt kurz halten." },
        new() { Name = "Beinbeuger liegend", PrimaryMuscleGroup = "Beine", Equipment = "Maschine", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.5, Tip = "Hüfte während der Bewegung auf der Bank lassen." },
        new() { Name = "Wadenheben stehend", PrimaryMuscleGroup = "Waden", Equipment = "Maschine", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Vollen Bewegungsumfang nutzen, oben kurz halten." },

        // ---------------- Schulter ----------------
        new() { Name = "Schulterdrücken", PrimaryMuscleGroup = "Schulter", SecondaryMuscleGroup = "Trizeps", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.5, Tip = "Rumpf anspannen, nicht ins Hohlkreuz fallen." },
        new() { Name = "Seitheben", PrimaryMuscleGroup = "Schulter", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Leichte Gewichte, Bewegung kommt aus der Schulter, nicht schwingen." },
        new() { Name = "Frontheben", PrimaryMuscleGroup = "Schulter", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Bis auf Schulterhöhe heben, kontrolliert ablassen." },
        new() { Name = "Face Pulls", PrimaryMuscleGroup = "Schulter", SecondaryMuscleGroup = "Rücken", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Gut für die hintere Schulter und Haltung, Ellenbogen hoch führen." },

        // ---------------- Arme ----------------
        new() { Name = "Bizeps-Curls", PrimaryMuscleGroup = "Bizeps", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Ellenbogen fixiert am Körper, kein Schwung aus der Schulter." },
        new() { Name = "Trizepsdrücken am Kabel", PrimaryMuscleGroup = "Trizeps", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Ellenbogen eng am Körper, nur der Unterarm bewegt sich." },
        new() { Name = "Dips", PrimaryMuscleGroup = "Trizeps", SecondaryMuscleGroup = "Brust", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 7.0, Tip = "Nicht zu tief absinken, um die Schulter zu schonen." },
        new() { Name = "Hammer-Curls", PrimaryMuscleGroup = "Bizeps", SecondaryMuscleGroup = "Unterarm", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Neutraler Griff, gut für Unterarm und Bizeps." },

        // ---------------- Bauch/Core ----------------
        new() { Name = "Sit-ups", PrimaryMuscleGroup = "Bauch", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 5.0, Tip = "Bewegung aus der Bauchmuskulatur, nicht am Kopf ziehen." },
        new() { Name = "Plank", PrimaryMuscleGroup = "Bauch", SecondaryMuscleGroup = "Rücken", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 4.0, Tip = "Körper in einer Linie, Becken nicht durchhängen lassen." },
        new() { Name = "Beinheben hängend", PrimaryMuscleGroup = "Bauch", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 5.0, Tip = "Kontrolliert, ohne Schwung ausführen." },
        new() { Name = "Crunches am Kabel", PrimaryMuscleGroup = "Bauch", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.5, Tip = "Bewegung kommt aus der Wirbelsäule, nicht aus den Armen." },

        // ---------------- Cardio ----------------
        new() { Name = "Laufen", PrimaryMuscleGroup = "Beine", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Körpergewicht", InputType = ExerciseInputType.StreckeZeit, MetValue = 9.8, IndoorMetric = IndoorCardioMetric.Steigung, Tip = "Gleichmäßiges Tempo hilft, länger durchzuhalten. Drinnen (Laufband) wird zusätzlich nach der Steigung gefragt." },
        new() { Name = "Radfahren", PrimaryMuscleGroup = "Beine", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Fahrrad / Spinning-Bike", InputType = ExerciseInputType.StreckeZeit, MetValue = 7.5, IndoorMetric = IndoorCardioMetric.Widerstand, Tip = "Bei 'Drinnen' gehen wir von einem Matrix-Spinning-Bike mit Widerstandsstufen aus." },
        new() { Name = "Rudergerät", PrimaryMuscleGroup = "Rücken", SecondaryMuscleGroup = "Beine", Equipment = "Rudergerät", InputType = ExerciseInputType.WiderstandZeit, MetValue = 7.0, Tip = "Kraft kommt zuerst aus den Beinen, dann Rücken, dann Armen." },
        new() { Name = "Walking", PrimaryMuscleGroup = "Beine", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Körpergewicht", InputType = ExerciseInputType.StreckeZeit, MetValue = 4.5, IndoorMetric = IndoorCardioMetric.Steigung, Tip = "Gutes Einstiegs-Cardio, gelenkschonend." },
        new() { Name = "Schwimmen", PrimaryMuscleGroup = "Ganzkörper", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Körpergewicht", InputType = ExerciseInputType.StreckeZeit, MetValue = 8.0, Tip = "Sehr gelenkschonend, trainiert fast alle Muskelgruppen." },
        new() { Name = "Elliptical / Crosstrainer", PrimaryMuscleGroup = "Ganzkörper", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Crosstrainer", InputType = ExerciseInputType.WiderstandZeit, MetValue = 6.5, Tip = "Gelenkschonende Alternative zum Laufen." },
        new() { Name = "Jumping Jacks", PrimaryMuscleGroup = "Ganzkörper", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 8.0, Tip = "Gutes Aufwärmen, bringt schnell den Puls hoch." },
        new() { Name = "Seilspringen", PrimaryMuscleGroup = "Waden", SecondaryMuscleGroup = "Herz-Kreislauf", Equipment = "Springseil", InputType = ExerciseInputType.NurWiederholungen, MetValue = 11.0, Tip = "Locker in den Knien bleiben, auf dem Ballen landen." },

        // ---------------- Gesäß ----------------
        new() { Name = "Hip Thrust", PrimaryMuscleGroup = "Gesäß", SecondaryMuscleGroup = "Beine", Equipment = "Langhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 5.0, Tip = "Am oberen Punkt Gesäß fest anspannen." },
        new() { Name = "Kickbacks am Kabel", PrimaryMuscleGroup = "Gesäß", Equipment = "Kabelzug", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 4.0, Tip = "Kontrolliert, kein Schwung aus dem unteren Rücken." },
        new() { Name = "Bulgarian Split Squats", PrimaryMuscleGroup = "Beine", SecondaryMuscleGroup = "Gesäß", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 6.0, Tip = "Vorderes Bein trägt den Großteil der Last, Oberkörper aufrecht." },

        // ---------------- Mobility ----------------
        new() { Name = "Hüftöffner", PrimaryMuscleGroup = "Hüfte", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 2.5, Tip = "Langsam und kontrolliert, keine ruckartigen Bewegungen." },
        new() { Name = "Katze-Kuh (Wirbelsäulenmobilisation)", PrimaryMuscleGroup = "Rücken", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 2.5, Tip = "Mit der Atmung koppeln - einatmen beim Hohlkreuz, ausatmen beim Rundmachen." },
        new() { Name = "Schulterkreisen", PrimaryMuscleGroup = "Schulter", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 2.0, Tip = "Gut zum Aufwärmen vor dem oberen Körper." },
        new() { Name = "Weltbeste Dehnung (World's Greatest Stretch)", PrimaryMuscleGroup = "Ganzkörper", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 3.0, Tip = "Deckt Hüfte, Brustwirbelsäule und Beinrückseite in einer Bewegung ab." },
        new() { Name = "Nacken-Dehnung", PrimaryMuscleGroup = "Nacken", Equipment = "Körpergewicht", InputType = ExerciseInputType.NurWiederholungen, MetValue = 1.5, Tip = "Sanft dehnen, kein Ziehen mit Schwung." },

        // ---------------- Unterarm/Griffkraft ----------------
        new() { Name = "Handgelenk-Curls", PrimaryMuscleGroup = "Unterarm", Equipment = "Kurzhantel", InputType = ExerciseInputType.GewichtWiederholungen, MetValue = 3.0, Tip = "Kleine, kontrollierte Bewegung aus dem Handgelenk." },
        new() { Name = "Farmers Walk", PrimaryMuscleGroup = "Unterarm", SecondaryMuscleGroup = "Ganzkörper", Equipment = "Kurzhantel", InputType = ExerciseInputType.NurWiederholungen, MetValue = 4.5, Tip = "Aufrechte Haltung, Schultern nach hinten unten." },
    };
}
