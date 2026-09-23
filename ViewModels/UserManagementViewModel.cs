using System.Collections.ObjectModel;
using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.ViewModels
{
    /// <summary>
    /// Powers the "User Management" screen: a simple list of every login account
    /// (including ones people created themselves via "New User? Register here"),
    /// with buttons to reset a password or enable/disable the account.
    /// Admin-only screen.
    /// </summary>
    public class UserManagementViewModel : ViewModelBase
    {
        public ObservableCollection<User> Users { get; set; } = new();

        public int TotalUsers => Users.Count;
        public int ActiveUsers => System.Linq.Enumerable.Count(Users, u => u.IsActive);

        // ---- Reset password mini-form ----
        private bool _isResetFormVisible;
        public bool IsResetFormVisible
        {
            get => _isResetFormVisible;
            set => SetField(ref _isResetFormVisible, value);
        }

        public string ResetFormTitle => $"Reset password for \"{SelectedUser?.Username}\"";

        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set { SetField(ref _selectedUser, value); OnPropertyChanged(nameof(ResetFormTitle)); }
        }

        public string NewPassword { private get; set; }
        public string ConfirmNewPassword { private get; set; }

        private string _resetMessage;
        public string ResetMessage
        {
            get => _resetMessage;
            set => SetField(ref _resetMessage, value);
        }

        public RelayCommand<User> ResetPasswordCommand { get; }
        public RelayCommand<User> ToggleActiveCommand { get; }
        public RelayCommand<User> DeleteUserCommand { get; }
        public RelayCommand ConfirmResetCommand { get; }
        public RelayCommand CancelResetCommand { get; }

        public UserManagementViewModel()
        {
            ResetPasswordCommand = new RelayCommand<User>(OpenResetForm);
            ToggleActiveCommand = new RelayCommand<User>(ToggleActive);
            DeleteUserCommand = new RelayCommand<User>(DeleteUser);
            ConfirmResetCommand = new RelayCommand(_ => ConfirmReset());
            CancelResetCommand = new RelayCommand(_ => IsResetFormVisible = false);

            Load();
        }

        private void Load()
        {
            Users.Clear();
            foreach (var u in DatabaseHelper.GetAllUsers()) Users.Add(u);
            OnPropertyChanged(nameof(TotalUsers));
            OnPropertyChanged(nameof(ActiveUsers));
        }

        private void OpenResetForm(User user)
        {
            SelectedUser = user;
            ResetMessage = "";
            IsResetFormVisible = true;
        }

        private void ConfirmReset()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                ResetMessage = "New password must be at least 6 characters.";
                return;
            }
            if (NewPassword != ConfirmNewPassword)
            {
                ResetMessage = "Passwords do not match.";
                return;
            }

            DatabaseHelper.ResetPassword(SelectedUser.Id, NewPassword);
            IsResetFormVisible = false;
            MessageBox.Show($"Password for '{SelectedUser.Username}' has been reset.", "Password Reset",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleActive(User user)
        {
            if (user == null) return;
            if (user.Username == "admin")
            {
                MessageBox.Show("The default admin account cannot be disabled.", "Not allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DatabaseHelper.SetUserActive(user.Id, !user.IsActive);
            Load();
        }

        private void DeleteUser(User user)
        {
            if (user == null) return;
            if (user.Username == "admin")
            {
                MessageBox.Show("The default admin account cannot be deleted.", "Not allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show($"Delete user account '{user.Username}'?", "Confirm delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DatabaseHelper.DeleteUser(user.Id);
                Load();
            }
        }
    }
}
