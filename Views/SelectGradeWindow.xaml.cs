using System.Windows;
using TuitionManagementSystem.ViewModels;

namespace TuitionManagementSystem.Views
{
    public partial class SelectGradeWindow : Window
    {
        public SelectGradeWindow()
        {
            InitializeComponent();
            DataContext = new SelectGradeViewModel(this);
        }
    }
}
