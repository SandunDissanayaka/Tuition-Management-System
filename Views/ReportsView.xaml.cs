using System.Windows.Controls;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
        }
    }
}
