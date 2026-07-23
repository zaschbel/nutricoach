using ZXing.Net.Maui;

namespace NutriCoach.Maui.Views;

public partial class BarcodeScannerPage : ContentPage
{
    public event Action<string>? BarcodeScanned;
    public event Action? Cancelled;

    private bool _handled;

    public BarcodeScannerPage()
    {
        InitializeComponent();

        // Explizit konfigurieren, welche Formate erkannt werden sollen - Lebensmittel-Barcodes
        // sind so gut wie immer EAN-13, EAN-8 oder UPC-A (eindimensionale Strichcodes). Ohne diese
        // Konfiguration hat der Scanner offenbar gar nichts erkannt.
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_handled) return;

        var result = e.Results?.FirstOrDefault();
        if (result is null || string.IsNullOrWhiteSpace(result.Value)) return;

        _handled = true;
        var value = result.Value;
        MainThread.BeginInvokeOnMainThread(() => BarcodeScanned?.Invoke(value));
    }

    private void OnCancelClicked(object? sender, EventArgs e) => Cancelled?.Invoke();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
