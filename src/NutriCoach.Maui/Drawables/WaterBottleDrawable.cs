using Microsoft.Maui.Graphics;

namespace NutriCoach.Maui.Drawables;

/// <summary>Zeichnet eine schlichte, durchgehende Wasserflasche (kein wellig aufgelöster Boden -
/// eine einzige zusammenhängende Form, wie im Referenzbild) mit Deckel, Etikettenband, Glanz-Streifen
/// und einem Füllstand, der von unten nach oben wächst.</summary>
public class WaterBottleDrawable : IDrawable
{
    private readonly double _fillRatio;

    public WaterBottleDrawable(double fillRatio)
    {
        _fillRatio = Math.Clamp(fillRatio, 0, 1);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float scale = Math.Min(dirtyRect.Width / 70f, dirtyRect.Height / 135f);
        canvas.SaveState();
        canvas.Translate(dirtyRect.Center.X - 35 * scale, dirtyRect.Center.Y - 67.5f * scale);
        canvas.Scale(scale, scale);

        var lightBlue = Color.FromArgb("#8FD3F4");
        var midBlue = Color.FromArgb("#4FA9E8");
        var darkBlue = Color.FromArgb("#2E7FC1");

        // Ganze Flasche als EINE zusammenhängende, abgerundete Form (Körper + Schulter), kein
        // ausgefranster Boden - der Boden ist einfach unten sanft abgerundet.
        var body = new PathF();
        body.AppendRoundedRectangle(new RectF(8, 25, 54, 83), 14, 14, 10, 10);
        // Schulter/Hals oben
        body.MoveTo(20, 25);
        body.LineTo(24, 8);
        body.QuadTo(25, 3, 30, 3);
        body.LineTo(40, 3);
        body.QuadTo(45, 3, 46, 8);
        body.LineTo(50, 25);
        body.Close();

        var shoulder = new PathF();
        shoulder.MoveTo(20, 26);
        shoulder.LineTo(24, 8);
        shoulder.QuadTo(25, 3, 30, 3);
        shoulder.LineTo(40, 3);
        shoulder.QuadTo(45, 3, 46, 8);
        shoulder.LineTo(50, 26);
        shoulder.Close();

        canvas.FillColor = lightBlue;
        canvas.FillPath(body);
        canvas.FillPath(shoulder);

        // Füllung von unten - auf beide Teilformen zusammen begrenzt
        if (_fillRatio > 0.02)
        {
            var fillHeight = (float)(_fillRatio * 100);
            var combinedClip = new PathF();
            combinedClip.AppendRoundedRectangle(new RectF(8, 25, 54, 83), 14, 14, 10, 10);

            canvas.SaveState();
            canvas.ClipPath(combinedClip);
            canvas.FillColor = midBlue;
            canvas.FillRectangle(4, 108 - fillHeight, 64, fillHeight + 10);
            canvas.RestoreState();
        }

        // Etikettenband
        canvas.FillColor = darkBlue;
        canvas.FillRectangle(8, 62, 54, 10);
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(8, 65, 54, 4);

        // Deckel
        canvas.FillColor = midBlue;
        var cap = new PathF();
        cap.AppendRoundedRectangle(new RectF(24, 0, 22, 12), 4);
        canvas.FillPath(cap);

        // Glanz-Streifen ("i"-Form: Punkt + Strich)
        canvas.FillColor = Color.FromRgba(255, 255, 255, 130);
        canvas.FillCircle(46, 26, 4);
        var stripe = new PathF();
        stripe.AppendRoundedRectangle(new RectF(43, 35, 6, 24), 3);
        canvas.FillPath(stripe);

        canvas.RestoreState();
    }
}
