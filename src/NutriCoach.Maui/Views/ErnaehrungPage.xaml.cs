namespace NutriCoach.Maui.Views;

public partial class ErnaehrungPage : ContentView
{
    public ErnaehrungPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
    }
}
