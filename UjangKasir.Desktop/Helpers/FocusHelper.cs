using System.Windows;
using System.Windows.Controls;

namespace UjangKasir.Desktop.Helpers;

public static class FocusHelper
{
    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused",
            typeof(bool),
            typeof(FocusHelper),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsFocusedChanged));

    public static bool GetIsFocused(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }

    private static void OnIsFocusedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || dependencyObject is not Control control)
        {
            return;
        }

        control.Dispatcher.BeginInvoke(() =>
        {
            control.Focus();

            if (control is TextBox textBox)
            {
                textBox.SelectAll();
            }

            SetIsFocused(control, false);
        });
    }
}
