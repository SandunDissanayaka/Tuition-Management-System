using System.Windows;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class MainDashboardWindow : Window
    {
        public MainDashboardWindow()
        {
            InitializeComponent();
            DataContext = new MainDashboardViewModel(this);
        }
    }
}
