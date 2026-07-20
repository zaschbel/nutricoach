# NutriCoach – Fundament (Schritt 1 von mehreren)

Das ist das **Datengerüst + Onboarding-Fragebogen** für deine Ernährungs-/Fitness-App.
Alles läuft lokal auf deinem Windows-Rechner (WPF, .NET 8, SQLite) – noch keine Cloud, noch keine KI, das kommt in den nächsten Schritten.

## Was schon funktioniert

- **Vollständiges Datenmodell** für: Nutzerprofil, Verletzungen/Einschränkungen, Körpermaße-Verlauf, Lebensmittel + Nährwerte, Ernährungstagebuch, Trainingseinheiten (Kraft + Cardio), Wasseraufnahme, Übungsbibliothek
- **Lokale SQLite-Datenbank** (`%AppData%/NutriCoach/nutricoach.db`) – wird beim ersten Start automatisch angelegt. Das ist die geforderte "Memory-Funktion": alles, was einmal eingegeben wird, bleibt dauerhaft gespeichert
- **9-stufiger Onboarding-Fragebogen** (WPF-Fenster), fragt genau das ab, was du beschrieben hast:
  1. Name, Geburtsdatum, Geschlecht
  2. Größe, aktuelles Gewicht (BMI wird live berechnet und angezeigt)
  3. Hauptziel (Abnehmen, Muskelaufbau, Kraft, Ausdauer, Gesundheit, Halten)
  4. Zielgewicht + Wunschdatum ("wie schnell willst du dein Ziel erreichen")
  5. Trainingserfahrung + seit wann
  6. Letzte Entwicklung (zu-/abgenommen, pausiert, neu gestartet) + sonstige Aktivitäten
  7. Verletzungen/Schmerzen/Einschränkungen (beliebig viele, mit Körperbereich, Schweregrad, Beschreibung)
  8. Alltag (Bürojob bis Baustelle) + allgemeines Aktivitätslevel
  9. Zusammenfassung
- **Grundumsatz-/Kalorienbedarfs-Berechnung** (Mifflin-St Jeor + Aktivitäts- + Job-Faktor), inkl. automatischer Ableitung des Tagesziels aus Zielgewicht + Wunschdatum
- Ein minimales Dashboard-Fenster, das nach dem Onboarding zeigt: Alter, BMI, Grundumsatz, Gesamtumsatz, Kalorienziel, Gewichtsveränderung der letzten 30 Tage (Beweis, dass die Memory-Funktion arbeitet)

## Projekt öffnen & starten

Voraussetzung: **Visual Studio 2022** (Community reicht) mit der Workload ".NET Desktop Development", oder das .NET 8 SDK + `dotnet` CLI.

```
NutriCoach.sln öffnen → Startprojekt ist NutriCoach.App → F5
```

Beim ersten Start (`dotnet build` bzw. Visual Studio) lädt NuGet automatisch:
- `Microsoft.EntityFrameworkCore.Sqlite` (lokale Datenbank)
- `Microsoft.Extensions.Configuration.*` (Einstellungen/API-Key)
- `Microsoft.Extensions.DependencyInjection`
- `System.Net.Http.Json` (für spätere API-Calls)

Das benötigt Internetzugriff auf nuget.org – im Sandbox-Environment, in dem ich den Code geschrieben habe, war das nicht möglich, deshalb konnte ich es nicht selbst kompilieren. Falls beim ersten Build etwas nicht passt (z. B. Namespace-Kleinigkeit), sag mir einfach die Fehlermeldung, dann fixe ich es sofort.

## Wichtige Design-Entscheidungen, die ich getroffen habe

- **Alter und BMI werden nie gespeichert, sondern immer live berechnet** – so bleiben sie automatisch korrekt, auch wenn Zeit vergeht oder sich das Gewicht ändert
- **Gewichtsverlauf statt nur aktuellem Wert** – dadurch sind Aussagen wie "4 kg in 3 Monaten abgenommen" eine einfache Datenbankabfrage, keine manuelle Rechnung
- **Verletzungen als eigene Liste** statt Freitext-Feld – damit der spätere Trainingsplan-Generator gezielt einzelne Übungen ausschließen/anpassen kann
- **appsettings.json mit Platzhalter-API-Key** – trage dort deinen eigenen Anthropic API-Key ein, sobald wir den KI-Coach anbinden (nie in den Sourcecode selbst)

## Offene Fragen für den nächsten Schritt

Bevor ich weiterbaue, wäre es gut zu wissen:

1. **Ernährungstagebuch zuerst oder Trainingsplan-Editor zuerst?** (KI-Coach kommt danach, braucht aber im Grunde beides als Datenbasis)
2. Für den Barcode-/Foto-Scan: Soll ich mit **Open Food Facts** (kostenlose Datenbank, aber Lücken bei deutschen Nischenprodukten) starten, oder direkt mit einer **KI-Fotoanalyse** (Foto vom Teller → Claude schätzt Zutaten + Makros), oder beides parallel anbieten?
3. Die Screenshots, die du geschickt hast (dunkles Design, orange Akzentfarbe wie bei MCI) – soll ich mich optisch stark daran orientieren, oder war das nur zur groben Orientierung gemeint?

## Projektstruktur

```
NutriCoach/
├── NutriCoach.sln
└── src/NutriCoach.App/
    ├── Models/       ← Datenmodell (UserProfile, FoodItem, TrainingSession, ...)
    ├── Data/          ← EF Core DbContext + DB-Initialisierung
    ├── Services/       ← BmrCalculator, UserProfileService
    ├── ViewModels/     ← OnboardingViewModel, RelayCommand
    ├── Views/          ← OnboardingWindow, MainWindow
    ├── App.xaml(.cs)   ← Einstiegspunkt, entscheidet Onboarding vs. Dashboard
    └── appsettings.json ← Platzhalter für deinen Claude API-Key
```
