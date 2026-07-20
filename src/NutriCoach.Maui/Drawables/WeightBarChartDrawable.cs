using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

public record WeightBarPoint(string DayAbbrev, string DayNumber, double? WeightKg);

/// <summary>
/// Balkendiagramm fürs Körpergewicht: höheres Gewicht = höherer Balken, deutlich sichtbar durch
/// Skalierung zwischen dem kleinsten und größten Wert im Zeitraum (nicht ab 0 - bei Gewicht wären
/// die Unterschiede sonst kaum sichtbar). Tage OHNE eigene Messung bleiben bewusst leer, statt
/// einen unsicheren Wert zu erfinden.
/// </summary>
public class WeightBarChartDrawable : IDrawable
{
    private readonly List<WeightBarPoint> _points;
    private readonly Color _barColor;

    public WeightBarChartDrawable(List<WeightBarPoint> points, Color barColor)
    {
        _points = points;
        _barColor = barColor;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_points.Count == 0) return;

        const float topPadding = 26f;
        const float bottomLabelHeight = 34f;
        const float spacing = 10f;

        var chartTop = topPadding;
        var chartBottom = dirtyRect.Height - bottomLabelHeight;
        var chartHeight = Math.Max(10, chartBottom - chartTop);

        var known = _points.Where(p => p.WeightKg.HasValue).Select(p => p.WeightKg!.Value).ToList();
        var min = known.Count > 0 ? known.Min() : 0;
        var max = known.Count > 0 ? known.Max() : 1;
        if (max - min < 1) { max += 0.5; min -= 0.5; } // Mindestspanne, sonst wirken nah beieinanderliegende Werte wie 0

        var barWidth = Math.Min(26f, (dirtyRect.Width - spacing * (_points.Count - 1)) / _points.Count);
        var totalWidth = barWidth * _points.Count + spacing * (_points.Count - 1);
        var startX = (dirtyRect.Width - totalWidth) / 2f;
        var cornerRadius = barWidth / 2.5f;

        for (var i = 0; i < _points.Count; i++)
        {
            var point = _points[i];
            var x = startX + i * (barWidth + spacing);

            if (point.WeightKg is double weight)
            {
                var ratio = (weight - min) / (max - min);
                var barHeight = (float)Math.Max(barWidth * 0.5, ratio * chartHeight);
                var yTop = chartBottom - barHeight;
                var rect = new RectF(x, yTop, barWidth, barHeight);

                canvas.SaveState();
                canvas.SetShadow(new SizeF(0, 2), 5, Color.FromRgba(0, 0, 0, 22));
                canvas.FillColor = _barColor;
                canvas.FillRoundedRectangle(rect, cornerRadius);
                canvas.RestoreState();

                canvas.FontColor = Color.FromArgb("#2E7D32");
                canvas.FontSize = 10;
                canvas.DrawString($"{weight:0.0}", x - 10, yTop - 18, barWidth + 20, 16, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            }
            else
            {
                // Kein Messwert für diesen Tag - dezenter gestrichelter Platzhalter statt eines erfundenen Balkens
                canvas.StrokeColor = Color.FromArgb("#D8D8DE");
                canvas.StrokeSize = 1.5f;
                canvas.StrokeDashPattern = new float[] { 3, 3 };
                canvas.DrawLine(x, chartBottom, x + barWidth, chartBottom);
            }

            canvas.FontColor = Color.FromArgb("#717786");
            canvas.FontSize = 11;
            canvas.DrawString(point.DayAbbrev, x - 10, dirtyRect.Height - bottomLabelHeight + 4, barWidth + 20, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
            canvas.DrawString(point.DayNumber, x - 10, dirtyRect.Height - bottomLabelHeight + 18, barWidth + 20, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
