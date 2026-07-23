namespace NutriCoach.Maui.Behaviors;

/// <summary>
/// Lässt einen ProgressBar-Fortschritt sanft auffüllen statt sofort zu springen, wenn sich der
/// gebundene Wert ändert. Statt direkt an ProgressBar.Progress zu binden, bindet man an
/// AnimatedProgress - das Behavior übernimmt dann per ProgressTo den eigentlichen Übergang.
/// </summary>
public class AnimatedProgressBehavior : Behavior<ProgressBar>
{
    public static readonly BindableProperty AnimatedProgressProperty = BindableProperty.Create(
        nameof(AnimatedProgress), typeof(double), typeof(AnimatedProgressBehavior), 0.0, propertyChanged: OnAnimatedProgressChanged);

    public double AnimatedProgress
    {
        get => (double)GetValue(AnimatedProgressProperty);
        set => SetValue(AnimatedProgressProperty, value);
    }

    private ProgressBar? _progressBar;

    // Behaviors sind KEIN Teil des visuellen Baums und erben deshalb NICHT automatisch den
    // BindingContext des Elements, an das sie angehängt sind - "{Binding X}" auf AnimatedProgress
    // lief dadurch ins Leere (kein Kontext = kein Wert, Balken blieb bei 0). Muss man manuell
    // durchreichen, inkl. Nachziehen, falls sich der BindingContext später nochmal ändert.
    protected override void OnAttachedTo(ProgressBar bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Progress = 0;
        _progressBar = bindable;
        bindable.BindingContextChanged += OnBindableBindingContextChanged;
        // Setzen von BindingContext löst die Bindung sofort auf und ruft dadurch synchron
        // OnAnimatedProgressChanged auf (siehe unten) - das startet die Auffüll-Animation direkt
        // beim ersten Anzeigen. Ein zusätzliches direktes "bindable.Progress = AnimatedProgress"
        // hier würde diese gerade gestartete Animation sofort wieder abschneiden/überschreiben,
        // deshalb bewusst NICHT mehr direkt zuweisen - das war der eigentliche Fehler.
        BindingContext = bindable.BindingContext;
    }

    protected override void OnDetachingFrom(ProgressBar bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.BindingContextChanged -= OnBindableBindingContextChanged;
        _progressBar = null;
    }

    private void OnBindableBindingContextChanged(object? sender, EventArgs e) =>
        BindingContext = _progressBar?.BindingContext;

    private static void OnAnimatedProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var behavior = (AnimatedProgressBehavior)bindable;
        if (behavior._progressBar is null || newValue is not double target) return;

        _ = behavior._progressBar.ProgressTo(target, 500, Easing.CubicOut);
    }
}
