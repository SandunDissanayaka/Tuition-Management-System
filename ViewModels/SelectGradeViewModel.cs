using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Views;

namespace TuitionManagementSystem.ViewModels
{
    public class SelectGradeViewModel : ViewModelBase
    {
        public string AdminName => AppSession.CurrentUser?.FullName ?? AppSession.CurrentUser?.Username;
        public string AdminRole => AppSession.CurrentUser?.Role;

        public int Grade10Count => DatabaseHelper.CountStudentsByGrade("Grade 10");
        public int Grade11Count => DatabaseHelper.CountStudentsByGrade("Grade 11");

        public RelayCommand OpenGrade10Command { get; }
        public RelayCommand OpenGrade11Command { get; }
        public RelayCommand LogoutCommand { get; }

        private readonly Window _ownerWindow;

        public SelectGradeViewModel(Window ownerWindow)
        {
            _ownerWindow = ownerWindow;
            OpenGrade10Command = new RelayCommand(_ => OpenDashboard("Grade 10"));
            OpenGrade11Command = new RelayCommand(_ => OpenDashboard("Grade 11"));
            LogoutCommand = new RelayCommand(_ => Logout());
        }

        private void OpenDashboard(string grade)
        {
            AppSession.CurrentGrade = grade;
            var dashboard = new MainDashboardWindow();
            dashboard.Show();
            _ownerWindow.Close();
        }

        private void Logout()
        {
            AppSession.CurrentUser = null;
            var login = new LoginWindow();
            login.Show();
            _ownerWindow.Close();
        }
    }
}
