using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Views;

namespace TuitionManagementSystem.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set => SetField(ref _fullName, value);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }

        // Set from code-behind since PasswordBox.Password cannot be bound directly
        public string Password { private get; set; }
        public string ConfirmPassword { private get; set; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set => SetField(ref _successMessage, value);
        }

        public RelayCommand RegisterCommand { get; }
        public RelayCommand BackToLoginCommand { get; }

        private readonly Window _ownerWindow;

        public RegisterViewModel(Window ownerWindow)
        {
            _ownerWindow = ownerWindow;
            RegisterCommand = new RelayCommand(_ => DoRegister());
            BackToLoginCommand = new RelayCommand(_ => GoToLogin());
        }

        private void DoRegister()
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Please fill in every field.";
                return;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            if (DatabaseHelper.UsernameExists(Username.Trim()))
            {
                ErrorMessage = "That username is already taken.";
                return;
            }

            // New self-registered accounts get the "User" role by default.
            // An Admin can promote/manage them later from the User Management screen.
            DatabaseHelper.RegisterUser(Username.Trim(), Password, FullName.Trim(), "User");

            SuccessMessage = "Registration successful! Redirecting to login...";

            // Small delay so the user sees the success message, then go back to Login.
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1.2) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                GoToLogin();
            };
            timer.Start();
        }

        private void GoToLogin()
        {
            var login = new LoginWindow();
            login.Show();
            _ownerWindow.Close();
        }
    }
}
