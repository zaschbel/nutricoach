using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>Zeichnet einen vollen Ring aus drei Segmenten (Proteine/Kohlenhydrate/Fette), proportional
/// zu ihrem jeweiligen Kalorienanteil - für die Mahlzeit-Detailansicht, analog zur MCI-App-Vorlage.</summary>
public class MacroRingDrawable : IDrawable
{
    private readonly double _proteinKcal;
    private readonly double _carbsKcal;
    private readonly double _fatKcal;

    public MacroRingDrawable(double proteinG, double carbsG, double fatG)
    {
        _proteinKcal = Math.Max(0, proteinG) * 4;
        _carbsKcal = Math.Max(0, carbsG) * 4;
        _fatKcal = Math.Max(0, fatG) * 9;
    }

    // Drei sichtlich unterschiedliche, aber zueinander passende ("harmonische") Farbtöne statt
    // dreier sehr ähnlicher Grüntöne - je eine Farbe pro Makro, konsistent mit den Legenden-Punkten.
    private static readonly Color ProteinColor = Color.FromArgb("#4FB0AE");   // Teal
    private static readonly Color CarbsColor = Color.FromArgb("#7CB342");    // Grün (App-Akzent)
    private static readonly Color FatColor = Color.FromArgb("#E0A458");      // Amber

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var total = _proteinKcal + _carbsKcal + _fatKcal;
        var cx = dirtyRect.Width / 2f;
        var cy = dirtyRect.Height / 2f;
        var maxRadius = Math.Min(cx, cy) - 6f;
        // Ring bewusst kleiner als der verfuegbare Platz, damit rundum Raum fuer die kurzen
        // Verbindungsstriche zu den Makro-Beschriftungen aussenrum bleibt (wie im Vorbild).
        var radius = maxRadius * 0.62f;

        canvas.StrokeSize = 16;
        canvas.StrokeLineCap = LineCap.Round;

        if (total <= 0)
        {
            DrawArc(canvas, cx, cy, radius, -90, 269.9, Color.FromArgb("#282B31"));
            return;
        }

        // Kleine Luecke (Gap) zwischen den Segmenten, damit sie optisch getrennt bleiben - wie im Vorbild.
        const double gapDeg = 4;
        var proteinSweep = _proteinKcal / total * 360.0;
        var carbsSweep = _carbsKcal / total * 360.0;
        var fatSweep = _fatKcal / total * 360.0;

        var start = -90.0;
        DrawSegmentWithConnector(canvas, cx, cy, radius, maxRadius, start, proteinSweep, gapDeg, ProteinColor);
        start += proteinSweep;
        DrawSegmentWithConnector(canvas, cx, cy, radius, maxRadius, start, carbsSweep, gapDeg, CarbsColor);
        start += carbsSweep;
        DrawSegmentWithConnector(canvas, cx, cy, radius, maxRadius, start, fatSweep, gapDeg, FatColor);
    }

    private static void DrawSegmentWithConnector(ICanvas canvas, float cx, float cy, float radius, float maxRadius,
        double startDeg, double sweepDeg, double gapDeg, Color color)
    {
        if (sweepDeg <= 0) return;

        DrawArc(canvas, cx, cy, radius, startDeg, startDeg + Math.Max(0, sweepDeg - gapDeg), color);

        // Kurzer Verbindungsstrich von der Segmentmitte nach aussen, wo die Beschriftung sitzt.
        var midDeg = startDeg + sweepDeg / 2.0;
        var rad = Math.PI / 180.0 * midDeg;
        var innerX = cx + (radius + 10) * (float)Math.Cos(rad);
        var innerY = cy + (radius + 10) * (float)Math.Sin(rad);
        var outerX = cx + maxRadius * (float)Math.Cos(rad);
        var outerY = cy + maxRadius * (float)Math.Sin(rad);

        canvas.StrokeSize = 2;
        canvas.StrokeColor = color;
        canvas.DrawLine(innerX, innerY, outerX, outerY);
        canvas.StrokeSize = 16;
    }

    private static void DrawArc(ICanvas canvas, float cx, float cy, float radius, double startDeg, double endDeg, Color color)
    {
        if (endDeg <= startDeg) return;

        var path = new PathF();
        var stepCount = Math.Max(2, (int)(80 * (endDeg - startDeg) / 360.0));

        for (var i = 0; i <= stepCount; i++)
        {
            var angle = startDeg + (endDeg - startDeg) * i / stepCount;
            var rad = Math.PI / 180.0 * angle;
            var x = cx + radius * (float)Math.Cos(rad);
            var y = cy + radius * (float)Math.Sin(rad);
            if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
        }

        canvas.StrokeColor = color;
        canvas.DrawPath(path);
    }
}
