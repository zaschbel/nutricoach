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
        Loaded += async (_, _) => await _viewModel.LoadRecentFoodsAsync();
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

        var mimeType = photo.ContentType;
        if (string.IsNullOrWhiteSpace(mimeType)) mimeType = "image/jpeg";

        await _viewModel.AnalyzePhotoAsync(memoryStream.ToArray(), mimeType);
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
