using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Views;

namespace TuitionManagementSystem.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }

        // Bound to the PasswordBox via code-behind (PasswordBox does not support binding directly)
        public string Password { private get; set; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public RelayCommand LoginCommand { get; }
        public RelayCommand GoToRegisterCommand { get; }

        private readonly Window _ownerWindow;

        public LoginViewModel(Window ownerWindow)
        {
            _ownerWindow = ownerWindow;
            LoginCommand = new RelayCommand(_ => DoLogin());
            GoToRegisterCommand = new RelayCommand(_ => OpenRegister());
        }

        private void DoLogin()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return;
            }

            var user = DatabaseHelper.GetUserByUsername(Username.Trim());
            if (user == null || !PasswordHelper.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage = "This account has been disabled. Contact the administrator.";
                return;
            }

            AppSession.CurrentUser = user;

            var selectGrade = new SelectGradeWindow();
            selectGrade.Show();
            _ownerWindow.Close();
        }

        private void OpenRegister()
        {
            var register = new RegisterWindow();
            register.Show();
            _ownerWindow.Close();
        }
    }
}
