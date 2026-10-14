using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace TuitionManagementSystem.Views
{
    /// <summary>
    /// A password box with a show / hide (eye) button.
    /// It has the same Password property and PasswordChanged event as a normal PasswordBox,
    /// so the existing code-behind of the Login, Register and User Management screens works unchanged.
    /// </summary>
    public partial class PasswordInputBox : UserControl
    {
        private bool _syncing;

        /// <summary>Raised whenever the password text changes (in either mode).</summary>
        public event RoutedEventHandler PasswordChanged;

        public PasswordInputBox()
        {
            InitializeComponent();

            HiddenBox.PasswordChanged += (s, e) =>
            {
                if (_syncing) return;
                _syncing = true;
                VisibleBox.Text = HiddenBox.Password;
                _syncing = false;
                PasswordChanged?.Invoke(this, new RoutedEventArgs());
            };

            VisibleBox.TextChanged += (s, e) =>
            {
                if (_syncing) return;
                _syncing = true;
                HiddenBox.Password = VisibleBox.Text;
                _syncing = false;
                PasswordChanged?.Invoke(this, new RoutedEventArgs());
            };
        }

        /// <summary>The password the user typed (same as PasswordBox.Password).</summary>
        public string Password
        {
            get => HiddenBox.Password;
            set => HiddenBox.Password = value ?? "";
        }

        public void Clear() => Password = "";

        private void EyeButton_Click(object sender, RoutedEventArgs e)
        {
            bool show = EyeButton.IsChecked == true;

            if (show)
            {
                VisibleBox.Visibility = Visibility.Visible;
                HiddenBox.Visibility = Visibility.Collapsed;
                EyeButton.ToolTip = "Hide password";

                VisibleBox.Focus();
                VisibleBox.CaretIndex = VisibleBox.Text.Length;
            }
            else
            {
                HiddenBox.Visibility = Visibility.Visible;
                VisibleBox.Visibility = Visibility.Collapsed;
                EyeButton.ToolTip = "Show password";

                HiddenBox.Focus();
                MoveCaretToEnd(HiddenBox);
            }
        }

        /// <summary>PasswordBox has no public way to place the cursor, so use its internal Select method.
        /// If that is ever unavailable the cursor simply stays where it is.</summary>
        private static void MoveCaretToEnd(PasswordBox box)
        {
            try
            {
                typeof(PasswordBox)
                    .GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(box, new object[] { box.Password.Length, 0 });
            }
            catch (Exception)
            {
                // cosmetic only - ignore
            }
        }
    }
}
