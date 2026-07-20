using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

public record StatBarPoint(string DayAbbrev, string DayNumber, double Value);

/// <summary>
/// Balkendiagramm für die Statistik-Karten (Kalorien, Proteine, Kohlenhydrate, Fette): Säulen +
/// gestrichelte Ziel-Linie mit Flagge, Wochentag/Datum unter jeder Säule - nach der vom Nutzer
/// vorgegebenen Vorlage.
/// </summary>
public class WeeklyStatBarDrawable : IDrawable
{
    private readonly List<StatBarPoint> _points;
    private readonly double _target;
    private readonly Color _barColor;

    public WeeklyStatBarDrawable(List<StatBarPoint> points, double target, Color barColor)
    {
        _points = points;
        _target = target;
        _barColor = barColor;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_points.Count == 0) return;

        const float topPadding = 22f;
        const float bottomLabelHeight = 34f;
        const float spacing = 10f;

        var chartTop = topPadding;
        var chartBottom = dirtyRect.Height - bottomLabelHeight;
        var chartHeight = Math.Max(10, chartBottom - chartTop);

        var maxValue = Math.Max(_points.Max(p => p.Value), _target) * 1.1;
        if (maxValue <= 0) maxValue = 1;

        var barWidth = Math.Min(26f, (dirtyRect.Width - spacing * (_points.Count - 1)) / _points.Count);
        var totalWidth = barWidth * _points.Count + spacing * (_points.Count - 1);
        var startX = (dirtyRect.Width - totalWidth) / 2f;
        var cornerRadius = barWidth / 2.5f;

        // Gestrichelte Ziel-Linie mit kleiner Flagge rechts
        if (_target > 0)
        {
            var goalY = (float)(chartBottom - _target / maxValue * chartHeight);
            canvas.StrokeColor = Color.FromArgb("#B7B7BE");
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            canvas.DrawLine(0, goalY, dirtyRect.Width - 14, goalY);
            canvas.FontColor = Color.FromArgb("#B7B7BE");
            canvas.FontSize = 12;
            canvas.DrawString("🚩", dirtyRect.Width - 16, goalY - 8, 16, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        for (var i = 0; i < _points.Count; i++)
        {
            var point = _points[i];
            var barHeight = (float)Math.Max(barWidth * 0.6, point.Value / maxValue * chartHeight);
            var x = startX + i * (barWidth + spacing);
            var yTop = chartBottom - barHeight;
            var rect = new RectF(x, yTop, barWidth, barHeight);

            // Weicher Schlagschatten, dann dezenter Glanz oben - gleiche Technik wie beim Dashboard-Diagramm
            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 5, Color.FromRgba(0, 0, 0, 22));
            canvas.FillColor = _barColor;
            canvas.FillRoundedRectangle(rect, cornerRadius);
            canvas.RestoreState();

            canvas.SaveState();
            var clip = new PathF();
            clip.AppendRoundedRectangle(rect, cornerRadius);
            canvas.ClipPath(clip);
            canvas.FillColor = Lighten(_barColor, 0.22f);
            canvas.FillRectangle(x, yTop, barWidth, Math.Min(8f, barHeight * 0.3f));
            canvas.RestoreState();

            canvas.FontColor = Color.FromArgb("#717786");
            canvas.FontSize = 11;
            canvas.DrawString(point.DayAbbrev, x - 10, dirtyRect.Height - bottomLabelHeight + 4, barWidth + 20, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
            canvas.DrawString(point.DayNumber, x - 10, dirtyRect.Height - bottomLabelHeight + 18, barWidth + 20, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static Color Lighten(Color c, float f) => Color.FromRgba(
        c.Red + (1 - c.Red) * f, c.Green + (1 - c.Green) * f, c.Blue + (1 - c.Blue) * f, c.Alpha);
}
