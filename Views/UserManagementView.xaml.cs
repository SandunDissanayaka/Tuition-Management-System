using System.Windows.Controls;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class UserManagementView : UserControl
    {
        private readonly UserManagementViewModel _viewModel;

        public UserManagementView()
        {
            InitializeComponent();
            _viewModel = new UserManagementViewModel();
            DataContext = _viewModel;

            // PasswordBox.Password cannot be data-bound directly, so wire it up manually.
            NewPasswordBox.PasswordChanged += (s, e) => _viewModel.NewPassword = NewPasswordBox.Password;
            ConfirmNewPasswordBox.PasswordChanged += (s, e) => _viewModel.ConfirmNewPassword = ConfirmNewPasswordBox.Password;
        }
    }
}
