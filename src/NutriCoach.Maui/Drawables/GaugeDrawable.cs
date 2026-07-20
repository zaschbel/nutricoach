using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>Zeichnet den Kalorien-Bogen (Halbkreis, wächst mit dem Fortschritt) für die Ernährungs-Übersicht.</summary>
public class GaugeDrawable : IDrawable
{
    private readonly double _percent;
    private readonly Color _backgroundColor;
    private readonly Color _foregroundColor;

    public GaugeDrawable(double percent, Color backgroundColor, Color foregroundColor)
    {
        _percent = Math.Clamp(percent, 0, 100);
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx = dirtyRect.Width / 2f;
        float cy = dirtyRect.Height - 6f;
        float radius = Math.Min(cx, dirtyRect.Height) - 12f;

        canvas.StrokeSize = 14;
        canvas.StrokeLineCap = LineCap.Round;

        DrawArcStroke(canvas, cx, cy, radius, 180, 360, _backgroundColor);

        var sweepEndDeg = 180 + _percent / 100.0 * 180.0;
        if (sweepEndDeg > 180.5)
        {
            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 6, Color.FromRgba(0, 0, 0, 50));
            DrawArcStroke(canvas, cx, cy, radius, 180, sweepEndDeg, _foregroundColor);
            canvas.RestoreState();
        }
    }

    private static void DrawArcStroke(ICanvas canvas, float cx, float cy, float radius, double startDeg, double endDeg, Color color)
    {
        var path = new PathF();
        var stepCount = Math.Max(2, (int)(60 * (endDeg - startDeg) / 360.0));

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
