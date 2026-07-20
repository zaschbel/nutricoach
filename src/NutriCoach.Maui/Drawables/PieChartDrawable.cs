using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>
/// Modernes Donut-Diagramm: dicker Ring statt vollem Kreis, jedes Segment mit weichem Verlauf
/// (heller Rand -> Grundfarbe), Schlagschatten und feiner weißer Trennlinie. Bewusst KEINE
/// extrudierten "Wände" mehr (das war die alte Excel-Chart-Optik) - Tiefe entsteht stattdessen
/// über Licht und Schatten, wie bei modernen Fitness-Apps (z. B. Apple Health-Ringe).
/// </summary>
public class PieChartDrawable : IDrawable
{
    private readonly List<(double Percent, Color Color)> _segments;

    public PieChartDrawable(List<(double Percent, Color Color)> segments)
    {
        _segments = segments;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var total = _segments.Sum(s => s.Percent);
        if (total <= 0) return;

        float cx = dirtyRect.Width / 2f;
        float cy = dirtyRect.Height / 2f;
        float outerRadius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 10f;
        float ringThickness = outerRadius * 0.42f;
        float innerRadius = outerRadius - ringThickness;

        double angle = -90; // oben beginnen

        canvas.SaveState();
        canvas.SetShadow(new SizeF(0, 4), 10, Color.FromRgba(0, 0, 0, 60));

        foreach (var (percent, color) in _segments)
        {
            if (percent <= 0) continue;
            var sweep = percent / total * 360.0;
            var end = angle + sweep;

            var path = BuildRingSegment(cx, cy, innerRadius, outerRadius, angle, end);

            canvas.SaveState();
            canvas.ClipPath(path);
            DrawRadialGloss(canvas, cx, cy, outerRadius, color);
            canvas.RestoreState();

            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2f;
            canvas.DrawPath(path);

            angle = end;
        }
        canvas.RestoreState();
    }

    /// <summary>Simuliert einen weichen Verlauf über mehrere ineinander verschachtelte, leicht aufgehellte Kreise.</summary>
    private static void DrawRadialGloss(ICanvas canvas, float cx, float cy, float outerRadius, Color baseColor)
    {
        const int bands = 8;
        for (var i = bands; i >= 0; i--)
        {
            var t = i / (float)bands;
            var r = outerRadius * (0.4f + 0.6f * t);
            canvas.FillColor = Lighten(baseColor, 0.35f * (1 - t));
            canvas.FillEllipse(cx - r, cy - r, r * 2, r * 2);
        }
    }

    private static PathF BuildRingSegment(float cx, float cy, float innerRadius, float outerRadius, double startDeg, double endDeg)
    {
        var path = new PathF();
        var stepCount = Math.Max(2, (int)(60 * (endDeg - startDeg) / 360.0));

        for (var i = 0; i <= stepCount; i++)
        {
            var t = startDeg + (endDeg - startDeg) * i / stepCount;
            var rad = Math.PI / 180.0 * t;
            var x = cx + outerRadius * (float)Math.Cos(rad);
            var y = cy + outerRadius * (float)Math.Sin(rad);
            if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
        }
        for (var i = stepCount; i >= 0; i--)
        {
            var t = startDeg + (endDeg - startDeg) * i / stepCount;
            var rad = Math.PI / 180.0 * t;
            var x = cx + innerRadius * (float)Math.Cos(rad);
            var y = cy + innerRadius * (float)Math.Sin(rad);
            path.LineTo(x, y);
        }
        path.Close();
        return path;
    }

    private static Color Lighten(Color c, float f) => Color.FromRgba(
        c.Red + (1 - c.Red) * f, c.Green + (1 - c.Green) * f, c.Blue + (1 - c.Blue) * f, c.Alpha);
}
