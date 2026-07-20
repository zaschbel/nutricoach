namespace NutriCoach.Maui.Views;

public partial class TrainingPage : ContentView
{
    public TrainingPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
    }
}
