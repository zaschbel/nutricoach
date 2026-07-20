using System.Globalization;

namespace NutriCoach.App.Services;

/// <summary>Ein einzelnes Segment eines Kreisdiagramms, fertig als XAML-Path-Data-String.</summary>
public record PieSlice(string PathData, string Color, string Label, double Percent,
    string? WallPathData, string WallColor, double OffsetX, double OffsetY);

/// <summary>
/// Berechnet die Path-Geometrie für Kreisdiagramm-Segmente inkl. dezentem 3D-Effekt
/// (dunklere "Wand" an der Vorderseite jedes Segments + leichter Explode-Versatz).
/// Wichtig: Zahlen werden IMMER mit Invariant-Culture formatiert, sonst würde bei deutscher
/// Locale ein Komma statt Punkt verwendet werden und die Path-Syntax kaputtgehen.
/// </summary>
public static class PieChartHelper
{
    private static string Fmt(double v) => v.ToString(CultureInfo.InvariantCulture);

    public static string BuildSlice(double startDeg, double endDeg, double radius, double cx, double cy)
    {
        if (endDeg - startDeg >= 359.999) endDeg = startDeg + 359.999;

        var (x1, y1) = PointOnCircle(startDeg, radius, cx, cy);
        var (x2, y2) = PointOnCircle(endDeg, radius, cx, cy);
        var largeArc = (endDeg - startDeg) > 180 ? 1 : 0;

        return $"M {Fmt(cx)},{Fmt(cy)} L {Fmt(x1)},{Fmt(y1)} A {Fmt(radius)},{Fmt(radius)} 0 {largeArc} 1 {Fmt(x2)},{Fmt(y2)} Z";
    }

    /// <summary>
    /// Baut die "Wand" (Seitenfläche) eines Segments für den 3D-Effekt - nur sichtbar für den
    /// vorderen/unteren Bereich des Kreises (dort, wo man bei einem leicht gekippten Kreis
    /// tatsächlich auf die Seite schauen würde). Gibt null zurück, wenn das Segment komplett
    /// auf der Rückseite liegt (dann gibt es nichts zu zeichnen).
    /// </summary>
    private static string? BuildWallSlice(double startDeg, double endDeg, double radius, double cx, double cy, double depth)
    {
        // Vorderer Bereich = Winkel zwischen 0° (rechts) und 180° (links), das ist die untere Hälfte.
        var clippedStart = Math.Max(startDeg, 0);
        var clippedEnd = Math.Min(endDeg, 180);
        if (clippedEnd <= clippedStart) return null;

        var (x1, y1) = PointOnCircle(clippedStart, radius, cx, cy);
        var (x2, y2) = PointOnCircle(clippedEnd, radius, cx, cy);
        var largeArc = (clippedEnd - clippedStart) > 180 ? 1 : 0;

        return $"M {Fmt(x1)},{Fmt(y1)} " +
               $"A {Fmt(radius)},{Fmt(radius)} 0 {largeArc} 1 {Fmt(x2)},{Fmt(y2)} " +
               $"L {Fmt(x2)},{Fmt(y2 + depth)} " +
               $"A {Fmt(radius)},{Fmt(radius)} 0 {largeArc} 0 {Fmt(x1)},{Fmt(y1 + depth)} Z";
    }

    private static (double X, double Y) PointOnCircle(double deg, double radius, double cx, double cy)
    {
        var rad = Math.PI / 180 * deg;
        return (cx + radius * Math.Cos(rad), cy + radius * Math.Sin(rad));
    }

    /// <summary>
    /// Baut einen offenen Bogen (kein gefülltes Segment, nur die Linie) - für Ring-/Gauge-Anzeigen
    /// wie den Kalorien-Fortschritt oben im Dashboard. Wird mit Stroke statt Fill gezeichnet.
    /// </summary>
    public static string BuildArc(double startDeg, double endDeg, double radius, double cx, double cy)
    {
        if (endDeg <= startDeg) endDeg = startDeg + 0.001;
        if (endDeg - startDeg >= 359.999) endDeg = startDeg + 359.999;

        var (x1, y1) = PointOnCircle(startDeg, radius, cx, cy);
        var (x2, y2) = PointOnCircle(endDeg, radius, cx, cy);
        var largeArc = (endDeg - startDeg) > 180 ? 1 : 0;

        return $"M {Fmt(x1)},{Fmt(y1)} A {Fmt(radius)},{Fmt(radius)} 0 {largeArc} 1 {Fmt(x2)},{Fmt(y2)}";
    }

    /// <summary>Verdunkelt eine Hex-Farbe (#RRGGBB) um den angegebenen Faktor, für die 3D-Wandfarbe.</summary>
    private static string Darken(string hex, double factor)
    {
        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);
        r = (int)(r * (1 - factor));
        g = (int)(g * (1 - factor));
        b = (int)(b * (1 - factor));
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Baut die drei Segmente für Kohlenhydrate/Eiweiß/Fett, basierend auf ihrem Kalorienanteil
    /// (Kohlenhydrate & Eiweiß = 4 kcal/g, Fett = 9 kcal/g), inklusive dezentem 3D-Effekt.
    /// </summary>
    public static List<PieSlice> BuildMacroSlices(double carbsG, double proteinG, double fatG,
        double radius = 68, double cx = 80, double cy = 72, double depth = 14)
    {
        var carbsKcal = carbsG * 4;
        var proteinKcal = proteinG * 4;
        var fatKcal = fatG * 9;
        var totalKcal = carbsKcal + proteinKcal + fatKcal;

        var slices = new List<PieSlice>();
        if (totalKcal <= 0)
        {
            var emptyColor = "#2E2E33";
            slices.Add(new PieSlice(
                BuildSlice(0, 359.999, radius, cx, cy), emptyColor, "Keine Daten", 100,
                BuildWallSlice(0, 359.999, radius, cx, cy, depth), Darken(emptyColor, 0.35), 0, 0));
            return slices;
        }

        var data = new (string Label, double Kcal, string Color)[]
        {
            ("Kohlenhydrate", carbsKcal, "#4AC0D9"),
            ("Eiweiß", proteinKcal, "#1E7A8C"),
            ("Fett", fatKcal, "#F5A623"),
        };

        double angle = -90; // oben beginnen, wie im klassischen Kreisdiagramm
        foreach (var (label, kcal, color) in data)
        {
            if (kcal <= 0) continue;
            var percent = kcal / totalKcal * 100;
            var sweep = kcal / totalKcal * 360;
            var end = angle + sweep;

            // Leichter "Explode"-Versatz nach außen, in Richtung der Segment-Mitte
            var midAngle = (angle + end) / 2;
            var midRad = Math.PI / 180 * midAngle;
            const double explodeDistance = 5;
            var offsetX = Math.Cos(midRad) * explodeDistance;
            var offsetY = Math.Sin(midRad) * explodeDistance;

            slices.Add(new PieSlice(
                BuildSlice(angle, end, radius, cx, cy), color, label, percent,
                BuildWallSlice(angle, end, radius, cx, cy, depth), Darken(color, 0.35),
                offsetX, offsetY));

            angle = end;
        }

        return slices;
    }
}
