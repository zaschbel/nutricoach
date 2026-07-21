using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class AddFoodPage : ContentPage
{
    private readonly AddFoodViewModel _viewModel;
    public event Action? CancelRequested;

    public AddFoodPage(AddFoodViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) =>
        {
            try
            {
                await _viewModel.LoadRecentFoodsAsync();
            }
            catch (Exception ex)
            {
                // Unbehandelte Exceptions in async-void Event-Handlern stuerzen die ganze App ab,
                // ohne dass ein try/catch an der Aufrufstelle (z.B. AddFoodCommand) das je sieht -
                // deshalb hier separat absichern und den echten Fehler sichtbar machen.
                await DisplayAlert("Fehler beim Laden", ex.ToString(), "OK");
            }
        };
    }

    private void OnCancelTapped(object? sender, EventArgs e) => CancelRequested?.Invoke();
    private void OnModeSearch(object? sender, EventArgs e) => _viewModel.Mode = "Suche";
    private void OnModeBarcode(object? sender, EventArgs e) => _viewModel.Mode = "Barcode";
    private void OnModeManual(object? sender, EventArgs e) => _viewModel.Mode = "Manuell";
    private void OnModePhoto(object? sender, EventArgs e) => _viewModel.Mode = "Foto";

    private async void OnTakePhotoClicked(object? sender, EventArgs e)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            _viewModel.PhotoErrorMessage = "Kamera auf diesem Gerät nicht verfügbar.";
            return;
        }

        var photo = await MediaPicker.Default.CapturePhotoAsync();
        if (photo is not null) await AnalyzePhoto(photo);
    }

    private async void OnPickPhotoClicked(object? sender, EventArgs e)
    {
        var photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo is not null) await AnalyzePhoto(photo);
    }

    private async Task AnalyzePhoto(FileResult photo)
    {
        using var stream = await photo.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        // photo.ContentType ist bei frisch aufgenommenen Kamerafotos auf iOS oft leer/unzuverlaessig
        // (im Gegensatz zu Galerie-Auswahl) - deshalb das tatsaechliche Format direkt an den Magic
        // Bytes der Bilddaten erkennen, statt der Metadaten zu vertrauen und ggf. das falsche
        // mime_type an die Gemini-API zu schicken (die das dann als "nicht unterstuetzt" ablehnt).
        var mimeType = DetectImageMimeType(imageBytes) ?? photo.ContentType;
        if (string.IsNullOrWhiteSpace(mimeType)) mimeType = "image/jpeg";

        await _viewModel.AnalyzePhotoAsync(imageBytes, mimeType);
    }

    private static string? DetectImageMimeType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";
        if (bytes.Length >= 12 && bytes[8] == 0x48 && bytes[9] == 0x45 && bytes[10] == 0x49 && bytes[11] == 0x43)
            return "image/heic";
        return null;
    }

    private async void OnScanBarcodeClicked(object? sender, EventArgs e)
    {
        var scannerPage = new BarcodeScannerPage();
        var tcs = new TaskCompletionSource<string?>();

        scannerPage.BarcodeScanned += code => tcs.TrySetResult(code);
        scannerPage.Cancelled += () => tcs.TrySetResult(null);

        await Navigation.PushModalAsync(scannerPage);
        var code = await tcs.Task;
        await Navigation.PopModalAsync();

        if (!string.IsNullOrWhiteSpace(code))
        {
            _viewModel.BarcodeText = code;
            _viewModel.LookupBarcodeCommand.Execute(null);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
