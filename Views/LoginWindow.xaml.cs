using System.Windows;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel(this);
            DataContext = _viewModel;

            // PasswordBox.Password cannot be bound in XAML for security reasons,
            // so we push its value into the ViewModel manually here.
            PasswordBox.PasswordChanged += (s, e) => _viewModel.Password = PasswordBox.Password;
        }
    }
}
