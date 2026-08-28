using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>Zeichnet einen vollen Ring aus drei Segmenten (Proteine/Kohlenhydrate/Fette), proportional
/// zu ihrem jeweiligen Kalorienanteil, inkl. Anschluss-Linie ("Leader Line") mit Unterstreichung zu
/// Wert+Bezeichnung je Segment - analog zur MCI-App-Vorlage. Wert und Linie werden hier bewusst auf
/// derselben Zeichenfläche erzeugt (statt als separat positionierte XAML-Labels), damit die Linie
/// IMMER exakt zur zugehörigen Beschriftung zeigt, auch wenn sich die Segment-Winkel durch andere
/// Nährwert-Verhältnisse verschieben.</summary>
public class MacroRingDrawable : IDrawable
{
    private readonly double _proteinKcal;
    private readonly double _carbsKcal;
    private readonly double _fatKcal;
    private readonly string _proteinValue;
    private readonly string _carbsValue;
    private readonly string _fatValue;

    public MacroRingDrawable(double proteinG, double carbsG, double fatG)
    {
        _proteinKcal = Math.Max(0, proteinG) * 4;
        _carbsKcal = Math.Max(0, carbsG) * 4;
        _fatKcal = Math.Max(0, fatG) * 9;
        _proteinValue = $"{proteinG:0.#} g Proteine";
        _carbsValue = $"{carbsG:0.#} g Carbs";
        _fatValue = $"{fatG:0.#} g Fette";
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
        // Ring bewusst kleiner als der verfuegbare Platz, damit rundum Raum fuer die Anschluss-
        // Linien zu den Makro-Beschriftungen aussenrum bleibt (wie im Vorbild).
        var radius = maxRadius * 0.55f;

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
        DrawSegmentWithLeaderLine(canvas, cx, cy, radius, maxRadius, start, proteinSweep, gapDeg, ProteinColor, _proteinValue);
        start += proteinSweep;
        DrawSegmentWithLeaderLine(canvas, cx, cy, radius, maxRadius, start, carbsSweep, gapDeg, CarbsColor, _carbsValue);
        start += carbsSweep;
        DrawSegmentWithLeaderLine(canvas, cx, cy, radius, maxRadius, start, fatSweep, gapDeg, FatColor, _fatValue);
    }

    /// <summary>Zeichnet ein Ring-Segment plus eine geknickte Anschluss-Linie: erst radial vom Ring
    /// nach aussen, dann ein kurzes horizontales Stueck, das gleichzeitig als Unterstreichung für den
    /// direkt darueber gesetzten Wert/Beschriftungs-Text dient - der Wert "sitzt" also sichtbar auf der Linie.</summary>
    private static void DrawSegmentWithLeaderLine(ICanvas canvas, float cx, float cy, float radius, float maxRadius,
        double startDeg, double sweepDeg, double gapDeg, Color color, string text)
    {
        if (sweepDeg <= 0) return;

        DrawArc(canvas, cx, cy, radius, startDeg, startDeg + Math.Max(0, sweepDeg - gapDeg), color);

        var midDeg = startDeg + sweepDeg / 2.0;
        var rad = Math.PI / 180.0 * midDeg;
        var cos = (float)Math.Cos(rad);
        var sin = (float)Math.Sin(rad);

        var innerX = cx + (radius + 10) * cos;
        var innerY = cy + (radius + 10) * sin;

        var elbowRadius = radius + (maxRadius - radius) * 0.7f;
        var elbowX = cx + elbowRadius * cos;
        var elbowY = cy + elbowRadius * sin;

        const float underlineLength = 46f;
        var dir = cos >= 0 ? 1f : -1f;
        var endX = elbowX + dir * underlineLength;

        canvas.StrokeSize = 2;
        canvas.StrokeColor = color;
        canvas.DrawLine(innerX, innerY, elbowX, elbowY);
        canvas.DrawLine(elbowX, elbowY, endX, elbowY);

        var boxX = dir > 0 ? elbowX : endX;
        var hAlign = dir > 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;

        canvas.FontColor = Color.FromArgb("#F0F1F3");
        canvas.FontSize = 12;
        canvas.DrawString(text, boxX, elbowY - 18, underlineLength + 4, 16, hAlign, VerticalAlignment.Bottom);

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
