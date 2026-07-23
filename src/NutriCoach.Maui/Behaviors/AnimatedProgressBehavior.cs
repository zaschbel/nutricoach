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

    protected override void OnAttachedTo(ProgressBar bindable)
    {
        base.OnAttachedTo(bindable);
        _progressBar = bindable;
        bindable.Progress = AnimatedProgress;
    }

    protected override void OnDetachingFrom(ProgressBar bindable)
    {
        base.OnDetachingFrom(bindable);
        _progressBar = null;
    }

    private static void OnAnimatedProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var behavior = (AnimatedProgressBehavior)bindable;
        if (behavior._progressBar is null || newValue is not double target) return;

        _ = behavior._progressBar.ProgressTo(target, 500, Easing.CubicOut);
    }
}
