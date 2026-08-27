using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>Zeichnet ein Kalender-Raster aus kleinen Quadraten (z. B. die letzten 30 Tage),
/// grün wenn an dem Tag ein Eintrag vorliegt, sonst dezent grau - analog zur MCI-App-Vorlage
/// für "Weigh-In"/"Trainings letzte 30 Tage".</summary>
public class HeatmapDrawable : IDrawable
{
    private readonly bool[] _days;
    private readonly int _columns;
    private readonly Color _activeColor;
    private readonly Color _inactiveColor;

    public HeatmapDrawable(bool[] days, Color activeColor, Color inactiveColor, int columns = 10)
    {
        _days = days;
        _columns = columns;
        _activeColor = activeColor;
        _inactiveColor = inactiveColor;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_days.Length == 0) return;

        var rows = (int)Math.Ceiling(_days.Length / (double)_columns);
        const float spacing = 4f;

        var cellWidth = (dirtyRect.Width - spacing * (_columns - 1)) / _columns;
        var cellHeight = (dirtyRect.Height - spacing * (rows - 1)) / rows;
        var cellSize = Math.Min(cellWidth, cellHeight);

        for (var i = 0; i < _days.Length; i++)
        {
            var col = i % _columns;
            var row = i / _columns;
            var x = col * (cellSize + spacing);
            var y = row * (cellSize + spacing);

            canvas.FillColor = _days[i] ? _activeColor : _inactiveColor;
            canvas.FillRoundedRectangle(x, y, cellSize, cellSize, 3);
        }
    }
}
