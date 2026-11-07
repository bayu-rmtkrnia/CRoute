using System.Windows;
using System.Windows.Controls;

namespace CRoute.App.Behaviors;

/// <summary>
/// <see cref="PasswordBox.Password"/> bukan DependencyProperty, jadi tidak bisa
/// di-binding langsung. Attached property ini menjembatani supaya password
/// bisa di-binding ke ViewModel:
/// <c>behaviors:PasswordBoxAssistant.BoundPassword="{Binding Password}"</c>.
/// </summary>
public static class PasswordBoxAssistant
{
    // Default null (bukan ""), supaya binding awal ke string kosong tetap
    // memicu OnBoundPasswordChanged dan handler PasswordChanged terpasang.
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                          OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordBoxAssistant));

    public static string? GetBoundPassword(DependencyObject d) => (string?)d.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject d, string? value) => d.SetValue(BoundPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box)
            return;

        box.PasswordChanged -= HandlePasswordChanged;
        if (!(bool)box.GetValue(IsUpdatingProperty))
            box.Password = (string?)e.NewValue ?? string.Empty;
        box.PasswordChanged += HandlePasswordChanged;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(box, box.Password);
        box.SetValue(IsUpdatingProperty, false);
    }
}
