using System.Windows.Controls;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class AttendanceFeesView : UserControl
    {
        public AttendanceFeesView()
        {
            InitializeComponent();
            DataContext = new AttendanceFeesViewModel();
        }
    }
}
