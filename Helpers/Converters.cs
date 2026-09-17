using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TuitionManagementSystem.Helpers
{
    /// <summary>Picks the "active" or "inactive" sidebar button style depending on
    /// whether this button's key matches the currently selected menu item.</summary>
    public class MenuStyleConverter : IValueConverter
    {
        public static readonly MenuStyleConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string activeMenu = value as string;
            string thisMenu = parameter as string;
            string styleKey = activeMenu == thisMenu ? "SidebarButtonActive" : "SidebarButton";
            return Application.Current.Resources[styleKey];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Simple bool -> Visibility converter (true = Visible, false = Collapsed).</summary>
    public class BoolToVisConverter : IValueConverter
    {
        public static readonly BoolToVisConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Inverse of BoolToVisConverter (true = Collapsed, false = Visible). Used to show a
    /// fallback element - like a "View only" label - exactly when the normal condition is false.</summary>
    public class InverseBoolToVisConverter : IValueConverter
    {
        public static readonly InverseBoolToVisConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Visible when a bound string is non-empty; used to show/hide inline error/success text.</summary>
    public class StringNotEmptyToVisConverter : IValueConverter
    {
        public static readonly StringNotEmptyToVisConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Renders an account's IsActive flag as a light green (Active) or light red (Disabled) pill background.</summary>
    public class BoolToActiveBrushConverter : IValueConverter
    {
        public static readonly BoolToActiveBrushConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7))
                                       : new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Turns IsActive into the words "Active" / "Disabled" for the status pill text.</summary>
    public class BoolToActiveTextConverter : IValueConverter
    {
        public static readonly BoolToActiveTextConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? "Active" : "Disabled";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Matching text color (dark green / dark red) for the status pill.</summary>
    public class BoolToActiveForegroundConverter : IValueConverter
    {
        public static readonly BoolToActiveForegroundConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D))
                                       : new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
