using Microsoft.Maui.Controls.Shapes;
using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;
using NutriCoach.Maui.Services;

namespace NutriCoach.Maui.Views;

public partial class StatistikenPage : ContentView
{
    private readonly StatistikenViewModel _viewModel;

    // Merkt sich die zuletzt gemessene Größe des Vergleichs-Grids, damit der Clip des oberen
    // (Zukunfts-)Bilds sowohl bei Größenänderung als auch bei Schieberegler-Bewegung neu berechnet
    // werden kann, ohne die Größe jedes Mal neu abzufragen.
    private double _comparisonGridWidth;
    private double _comparisonGridHeight;

    public StatistikenPage()
    {
        InitializeComponent();
        _viewModel = new StatistikenViewModel(AppState.ProfileService, new NutritionDiaryService(), new TrainingDiaryService());
        BindingContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }

    /// <summary>Wird beim Wechsel auf diesen Reiter erneut aufgerufen, damit z. B. gerade geändertes
    /// Gewicht oder neu eingetragenes Essen sofort in den Grafiken auftaucht, nicht erst nach Neustart.</summary>
    public async Task RefreshAsync() => await _viewModel.LoadAsync();

    // ---------------- Zukunftsbild: Foto aufnehmen/wählen ----------------

    private async void OnTakeFutureBodyPhotoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await Application.Current!.MainPage!.DisplayAlert("Nicht verfügbar", "Kamera auf diesem Gerät nicht verfügbar.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is not null) await GenerateFutureBodyFromPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            // Genau wie bei AddFoodPage: unbehandelte Exceptions in async-void Event-Handlern würden
            // die ganze App abstürzen lassen, ohne dass irgendwas davon sichtbar wird - deshalb hier
            // separat absichern und den echten Fehler anzeigen statt die Funktion "einfach nichts tun" zu lassen.
            await Application.Current!.MainPage!.DisplayAlert("Fehler bei der Foto-Aufnahme", ex.ToString(), "OK");
        }
    }

    private async void OnPickFutureBodyPhotoClicked(object? sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is not null) await GenerateFutureBodyFromPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Fehler bei der Foto-Auswahl", ex.ToString(), "OK");
        }
    }

    private async Task GenerateFutureBodyFromPhotoAsync(FileResult photo)
    {
        try
        {
            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var mimeType = ImageMimeTypeSniffer.DetectImageMimeType(imageBytes) ?? photo.ContentType;
            if (string.IsNullOrWhiteSpace(mimeType)) mimeType = "image/jpeg";

            await _viewModel.GenerateFutureBodyImageAsync(imageBytes, mimeType);

            // GenerateFutureBodyImageAsync fängt eigene Fehler bereits intern ab und zeigt sie über
            // FutureImageErrorMessage im UI an - hier zusätzlich absichern, falls doch etwas unerwartet
            // durchrutscht (z. B. beim Foto-Einlesen selbst, das oben noch außerhalb des VM-try/catch liegt).
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Fehler bei der Zukunftsbild-Erstellung", ex.ToString(), "OK");
        }
    }

    // ---------------- Zukunftsbild: Vorher/Nachher-Schieberegler ----------------

    private void OnComparisonGridSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement grid) return;
        _comparisonGridWidth = grid.Width;
        _comparisonGridHeight = grid.Height;
        UpdateComparisonClip();
    }

    private void OnCompareSliderValueChanged(object? sender, ValueChangedEventArgs e) => UpdateComparisonClip();

    private void UpdateComparisonClip()
    {
        try
        {
            if (_comparisonGridWidth <= 0 || _comparisonGridHeight <= 0) return;

            var clipWidth = _comparisonGridWidth * CompareSlider.Value;
            GeneratedImage.Clip = new RectangleGeometry(new Rect(0, 0, clipWidth, _comparisonGridHeight));
            var dividerWidth = CompareDivider.Width > 0 ? CompareDivider.Width : 2;
            CompareDivider.TranslationX = clipWidth - (dividerWidth / 2.0);
        }
        catch
        {
            // Rein visuelle Positionierung des Schiebereglers/Trennlinie - ein Fehler hier (z. B.
            // während des allerersten Layout-Durchlaufs) soll nie die Seite crashen, nur die
            // optionale Trennlinie/den Clip in diesem einen Frame nicht aktualisieren.
        }
    }
}
