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

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var total = _proteinKcal + _carbsKcal + _fatKcal;
        var cx = dirtyRect.Width / 2f;
        var cy = dirtyRect.Height / 2f;
        var radius = Math.Min(cx, cy) - 10f;

        canvas.StrokeSize = 18;
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
        DrawArc(canvas, cx, cy, radius, start, start + Math.Max(0, proteinSweep - gapDeg), Color.FromArgb("#B7C98A"));
        start += proteinSweep;
        DrawArc(canvas, cx, cy, radius, start, start + Math.Max(0, carbsSweep - gapDeg), Color.FromArgb("#7CB342"));
        start += carbsSweep;
        DrawArc(canvas, cx, cy, radius, start, start + Math.Max(0, fatSweep - gapDeg), Color.FromArgb("#4E5D2E"));
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
