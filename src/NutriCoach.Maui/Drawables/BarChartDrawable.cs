using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

public record Bar3DData(string DayAbbrev, double HeightRatio, bool IsHighlighted);

/// <summary>
/// Schlichtes, "Apple-artiges" Balkendiagramm: vollständig abgerundete (kapselförmige) Säulen in
/// einer ruhigen Grundfarbe, nur der heutige Tag farblich hervorgehoben, sehr dezenter Schatten,
/// großzügiger Abstand. Die Wochentag-Beschriftung wird direkt in dieselbe Zeichenfläche
/// gezeichnet wie die Balken - dadurch sitzt sie garantiert exakt unter der jeweiligen Säule,
/// unabhängig von Schriftbreite oder Layout-Rundungsfehlern einer separaten Liste darunter.
/// </summary>
public class BarChartDrawable : IDrawable
{
    private readonly List<Bar3DData> _bars;
    private readonly Color _normalColor;
    private readonly Color _highlightColor;
    private readonly Color _labelColor;
    private readonly Color _highlightLabelColor;

    public BarChartDrawable(List<Bar3DData> bars, Color normalColor, Color highlightColor, Color labelColor, Color highlightLabelColor)
    {
        _bars = bars;
        _normalColor = normalColor;
        _highlightColor = highlightColor;
        _labelColor = labelColor;
        _highlightLabelColor = highlightLabelColor;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_bars.Count == 0) return;

        const float spacing = 14f;
        const float labelAreaHeight = 20f;

        var barWidth = Math.Min(28f, (dirtyRect.Width - spacing * (_bars.Count - 1)) / _bars.Count);
        var totalContentWidth = barWidth * _bars.Count + spacing * (_bars.Count - 1);
        var startX = (dirtyRect.Width - totalContentWidth) / 2f; // zentriert, falls Balken schmaler als verfügbar

        var maxBarHeight = dirtyRect.Height - labelAreaHeight - 6f;
        var baseline = dirtyRect.Height - labelAreaHeight;
        var cornerRadius = barWidth / 2f;

        for (var i = 0; i < _bars.Count; i++)
        {
            var bar = _bars[i];
            var color = bar.IsHighlighted ? _highlightColor : Blend(_normalColor, _highlightColor, (float)Math.Clamp(bar.HeightRatio, 0.15, 1));
            var barHeight = (float)Math.Max(barWidth, bar.HeightRatio * maxBarHeight);
            var x = startX + i * (barWidth + spacing);
            var yTop = baseline - barHeight;
            var rect = new RectF(x, yTop, barWidth, barHeight);

            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 5, Color.FromRgba(0, 0, 0, bar.IsHighlighted ? 45 : 20));
            canvas.FillColor = color;
            canvas.FillRoundedRectangle(rect, cornerRadius);
            canvas.RestoreState();

            // Ganz dezenter Glanz am oberen Rand (nur ein schmaler heller Streifen, keine starke Bänderung)
            canvas.SaveState();
            var clip = new PathF();
            clip.AppendRoundedRectangle(rect, cornerRadius);
            canvas.ClipPath(clip);
            canvas.FillColor = Lighten(color, 0.18f);
            canvas.FillRectangle(x, yTop, barWidth, Math.Min(10f, barHeight * 0.3f));
            canvas.RestoreState();

            // Wochentag-Beschriftung, exakt unter dem Balken zentriert
            canvas.FontColor = bar.IsHighlighted ? _highlightLabelColor : _labelColor;
            canvas.FontSize = 11;
            canvas.DrawString(bar.DayAbbrev, x - 6, dirtyRect.Height - labelAreaHeight + 4, barWidth + 12, labelAreaHeight,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static Color Lighten(Color c, float f) => Color.FromRgba(
        c.Red + (1 - c.Red) * f, c.Green + (1 - c.Green) * f, c.Blue + (1 - c.Blue) * f, c.Alpha);

    /// <summary>Mischt zwei Farben nach einem Anteil (0 = ganz "from", 1 = ganz "to") - für den Höhen-Farbverlauf.</summary>
    private static Color Blend(Color from, Color to, float t) => Color.FromRgba(
        from.Red + (to.Red - from.Red) * t,
        from.Green + (to.Green - from.Green) * t,
        from.Blue + (to.Blue - from.Blue) * t,
        1f);
}
