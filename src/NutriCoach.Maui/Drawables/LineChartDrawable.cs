using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

public record LinePoint(string DayAbbrev, string DayNumber, double? Value);

/// <summary>
/// Verlaufsgrafik (Apple-Health-Look: weiche Flächenfüllung unter der Linie, Schatten, Punkte mit
/// Wertebeschriftung). Tage ohne Wert (Value == null) werden übersprungen - kein Punkt, keine
/// Verbindungslinie durch die Lücke, aber der Wochentag wird trotzdem angezeigt.
/// </summary>
public class LineChartDrawable : IDrawable
{
    private readonly List<LinePoint> _points;
    private readonly Color _lineColor;
    private readonly string _valueFormat;
    private readonly bool _useTrendColors;
    private readonly double? _targetValue;

    public LineChartDrawable(List<LinePoint> points, Color lineColor, Color labelColor, string valueFormat = "{0:0.0}", bool useTrendColors = false, double? targetValue = null)
    {
        _points = points;
        _lineColor = lineColor;
        _valueFormat = valueFormat;
        _useTrendColors = useTrendColors;
        _targetValue = targetValue;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_points.Count == 0) return;

        const float topPadding = 22f;
        const float bottomLabelHeight = 34f;
        var chartTop = topPadding;
        var chartBottom = dirtyRect.Height - bottomLabelHeight;
        var chartHeight = Math.Max(10, chartBottom - chartTop);

        var known = _points.Where(p => p.Value.HasValue).Select(p => p.Value!.Value).ToList();
        if (known.Count == 0) known.Add(0);
        var minValue = known.Min();
        var maxValue = known.Max();
        if (_targetValue.HasValue)
        {
            minValue = Math.Min(minValue, _targetValue.Value);
            maxValue = Math.Max(maxValue, _targetValue.Value);
        }
        if (Math.Abs(maxValue - minValue) < 0.01) { maxValue += 1; minValue -= 1; }

        var stepX = _points.Count > 1 ? dirtyRect.Width / (_points.Count - 1) : 0;
        float YFor(double value) => (float)(chartBottom - (value - minValue) / (maxValue - minValue) * chartHeight);
        float XFor(int i) => i * stepX;

        // Ziel-Linie: dezent gestrichelt, ganz unten gezeichnet, damit Balken/Durchschnittsfläche
        // und die eigentliche Datenlinie unveraendert obenauf bleiben und nichts verdeckt wird.
        if (_targetValue.HasValue)
        {
            canvas.SaveState();
            var targetY = YFor(_targetValue.Value);
            canvas.StrokeColor = Color.FromArgb("#717786");
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            canvas.DrawLine(0, targetY, dirtyRect.Width, targetY);
            canvas.FontColor = Color.FromArgb("#717786");
            canvas.FontSize = 10;
            canvas.DrawString(string.Format(_valueFormat, _targetValue.Value) + " Ziel", dirtyRect.Width - 64, targetY - 16, 64, 14,
                HorizontalAlignment.Right, VerticalAlignment.Bottom);
            canvas.RestoreState();
        }

        // Weiche Flächenfüllung unter der Linie, pro zusammenhängendem Abschnitt (überspringt Lücken)
        canvas.SaveState();
        var fillPath = new PathF();
        var inSegment = false;
        int? segStart = null;
        for (var i = 0; i < _points.Count; i++)
        {
            if (_points[i].Value is null) { inSegment = false; continue; }
            if (!inSegment) { fillPath.MoveTo(XFor(i), chartBottom); fillPath.LineTo(XFor(i), YFor(_points[i].Value!.Value)); inSegment = true; segStart ??= i; }
            else fillPath.LineTo(XFor(i), YFor(_points[i].Value!.Value));

            var isLastKnownInSegment = i == _points.Count - 1 || _points[i + 1].Value is null;
            if (isLastKnownInSegment) fillPath.LineTo(XFor(i), chartBottom);
        }
        canvas.ClipPath(fillPath);
        const int bands = 12;
        for (var b = 0; b < bands; b++)
        {
            var t = b / (float)(bands - 1);
            canvas.FillColor = _lineColor.WithAlpha(0.22f * (1 - t));
            var bandY = chartTop + (chartBottom - chartTop) * b / bands;
            var bandHeight = (chartBottom - chartTop) / bands + 1;
            canvas.FillRectangle(0, bandY, dirtyRect.Width, bandHeight);
        }
        canvas.RestoreState();

        // Verbindungslinie mit weichem Schatten - überspringt Lücken, optional nach Trend eingefärbt
        canvas.SaveState();
        canvas.SetShadow(new SizeF(0, 2), 4, Color.FromRgba(0, 0, 0, 40));
        canvas.StrokeSize = 3.5f;
        canvas.StrokeLineJoin = LineJoin.Round;
        for (var i = 1; i < _points.Count; i++)
        {
            if (_points[i - 1].Value is not double prevValue || _points[i].Value is not double currValue) continue;

            canvas.StrokeColor = !_useTrendColors ? _lineColor
                : currValue < prevValue - 0.05 ? Color.FromArgb("#1E9E5A")
                : currValue > prevValue + 0.05 ? Color.FromArgb("#E08A2E")
                : _lineColor;

            canvas.DrawLine(XFor(i - 1), YFor(prevValue), XFor(i), YFor(currValue));
        }
        canvas.RestoreState();

        // Punkte + Beschriftung (nur für Tage mit Wert)
        for (var i = 0; i < _points.Count; i++)
        {
            var x = XFor(i);

            if (_points[i].Value is double value)
            {
                var y = YFor(value);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(x, y, 5);
                canvas.StrokeColor = _lineColor;
                canvas.StrokeSize = 2.5f;
                canvas.DrawCircle(x, y, 5);

                canvas.FontColor = _lineColor;
                canvas.FontSize = 11;
                canvas.DrawString(string.Format(_valueFormat, value), x - 30, y - 20, 60, 16,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            canvas.FontColor = Color.FromArgb("#717786");
            canvas.FontSize = 11;
            canvas.DrawString(_points[i].DayAbbrev, x - 25, dirtyRect.Height - bottomLabelHeight + 4, 50, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
            canvas.DrawString(_points[i].DayNumber, x - 25, dirtyRect.Height - bottomLabelHeight + 18, 50, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
