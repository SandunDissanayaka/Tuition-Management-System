using System.Windows.Controls;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class DailySummaryView : UserControl
    {
        public DailySummaryView()
        {
            InitializeComponent();
            DataContext = new DailySummaryViewModel();
        }
    }
}
