using System.Windows;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterWindow()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel(this);
            DataContext = _viewModel;

            PasswordBox.PasswordChanged += (s, e) => _viewModel.Password = PasswordBox.Password;
            ConfirmPasswordBox.PasswordChanged += (s, e) => _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }
}
