using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

public enum TabIconType { Home, Training, Food, Stats }

/// <summary>
/// Zeichnet die vier Navigationsleisten-Icons direkt (wie das Kreis-/Balkendiagramm) statt über
/// Bild-Dateien - GraphicsView hat sich in dieser App bereits als zuverlässig erwiesen, im
/// Gegensatz zum Bild-Ressourcen-Weg (SVG und PNG haben beide nicht funktioniert).
/// </summary>
public class TabIconDrawable : IDrawable
{
    private readonly TabIconType _icon;
    private readonly Color _color;

    public TabIconDrawable(TabIconType icon, Color color)
    {
        _icon = icon;
        _color = color;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = _color;
        canvas.StrokeSize = 2.2f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        // Alle Formen sind auf ein 24x24-Koordinatensystem gezeichnet und werden auf die
        // tatsächliche Icon-Größe skaliert, damit sie auf jeder Bildschirmgröße gleich aussehen.
        var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / 24f;
        canvas.SaveState();
        canvas.Translate(dirtyRect.Center.X - 12 * scale, dirtyRect.Center.Y - 12 * scale);
        canvas.Scale(scale, scale);

        switch (_icon)
        {
            case TabIconType.Home: DrawHome(canvas); break;
            case TabIconType.Training: DrawTraining(canvas); break;
            case TabIconType.Food: DrawFood(canvas); break;
            case TabIconType.Stats: DrawStats(canvas); break;
        }

        canvas.RestoreState();
    }

    private void DrawHome(ICanvas canvas)
    {
        var roof = new PathF();
        roof.MoveTo(2, 11);
        roof.LineTo(12, 3.5f);
        roof.LineTo(22, 11);
        canvas.DrawPath(roof);

        var walls = new PathF();
        walls.MoveTo(5, 10);
        walls.LineTo(5, 20.5f);
        walls.LineTo(19, 20.5f);
        walls.LineTo(19, 10);
        canvas.DrawPath(walls);

        canvas.DrawRectangle(9.5f, 14, 5, 6.5f);
    }

    private void DrawTraining(ICanvas canvas)
    {
        var path = new PathF();
        path.MoveTo(2, 6);
        path.LineTo(6, 6);
        path.LineTo(10, 17);
        path.LineTo(14, 6);
        path.LineTo(18, 6);
        path.LineTo(22, 19);
        canvas.DrawPath(path);

        canvas.FillColor = _color;
        canvas.FillCircle(2, 6, 1.6f);
        canvas.FillCircle(10, 17, 1.6f);
    }

    private void DrawFood(ICanvas canvas)
    {
        canvas.DrawLine(6, 2, 6, 9);
        canvas.DrawLine(8.5f, 2, 8.5f, 9);
        canvas.DrawLine(6, 9, 8.5f, 9);
        canvas.DrawLine(7.2f, 9, 7.2f, 21);

        var knife = new PathF();
        knife.MoveTo(17, 2);
        knife.LineTo(17, 11);
        knife.LineTo(19.5f, 6.5f);
        knife.LineTo(17, 2);
        canvas.DrawPath(knife);
        canvas.DrawLine(17, 11, 17, 21);
    }

    private void DrawStats(ICanvas canvas)
    {
        canvas.DrawLine(5, 20, 5, 13);
        canvas.DrawLine(12, 20, 12, 6);
        canvas.DrawLine(19, 20, 19, 15);
        canvas.StrokeSize = 1.4f;
        canvas.DrawLine(3, 21.5f, 21, 21.5f);
    }
}
