using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>Zeichnet einen Fortschritts-Bogen (Halbkreis für die Ernährungs-Übersicht, oder ein
/// nahezu voller Ring für die kompakte Dashboard-Anzeige) - wächst mit dem Fortschritt.</summary>
public class GaugeDrawable : IDrawable
{
    private readonly double _percent;
    private readonly Color _backgroundColor;
    private readonly Color _foregroundColor;
    private readonly double _startDeg;
    private readonly double _sweepDeg;
    private readonly bool _centered;

    public GaugeDrawable(double percent, Color backgroundColor, Color foregroundColor,
        double startDeg = 180, double sweepDeg = 180, bool centered = false)
    {
        _percent = Math.Clamp(percent, 0, 100);
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _startDeg = startDeg;
        _sweepDeg = sweepDeg;
        _centered = centered;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx = dirtyRect.Width / 2f;
        float cy = _centered ? dirtyRect.Height / 2f : dirtyRect.Height - 6f;
        // Im zentrierten Ring-Modus ist der Strich schmaler, damit der Kreis bei gleichem Platz
        // groesser wirken kann, ohne an den Kartenraendern anzustossen.
        var strokeSize = _centered ? 10f : 14f;
        float radius = _centered
            ? Math.Min(cx, cy) - strokeSize / 2f - 2f
            : Math.Min(cx, dirtyRect.Height) - 12f;

        canvas.StrokeSize = strokeSize;
        canvas.StrokeLineCap = LineCap.Round;

        var endDeg = _startDeg + _sweepDeg;
        DrawArcStroke(canvas, cx, cy, radius, _startDeg, endDeg, _backgroundColor);

        var sweepEndDeg = _startDeg + _percent / 100.0 * _sweepDeg;
        if (sweepEndDeg > _startDeg + 0.5)
        {
            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 6, Color.FromRgba(0, 0, 0, 50));
            DrawArcStroke(canvas, cx, cy, radius, _startDeg, sweepEndDeg, _foregroundColor);
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
