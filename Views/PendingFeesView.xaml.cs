using System.Windows.Controls;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class PendingFeesView : UserControl
    {
        public PendingFeesView()
        {
            InitializeComponent();
            DataContext = new PendingFeesViewModel();
        }
    }
}
