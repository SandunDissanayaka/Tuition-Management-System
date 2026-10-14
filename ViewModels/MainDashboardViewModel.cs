using System;
using System.Windows;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Views;

namespace TuitionManagementSystem.ViewModels
{
    public class MainDashboardViewModel : ViewModelBase
    {
        public string InstituteName => "Jayagath Institute";
        public string Grade => AppSession.CurrentGrade;
        public string TodayDate => DateTime.Now.ToString("dddd, dd MMMM yyyy");
        public string CurrentUserName => AppSession.CurrentUser?.Username;
        public string CurrentUserRole => AppSession.CurrentUser?.Role;
        public bool IsAdmin => AppSession.CurrentUser?.Role == "Admin";

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetField(ref _currentView, value);
        }

        private string _activeMenu = "Home";
        public string ActiveMenu
        {
            get => _activeMenu;
            set => SetField(ref _activeMenu, value);
        }

        public RelayCommand ShowHomeCommand { get; }
        public RelayCommand ShowStudentsCommand { get; }
        public RelayCommand ShowAttendanceFeesCommand { get; }
        public RelayCommand ShowPendingFeesCommand { get; }
        public RelayCommand ShowDailySummaryCommand { get; }
        public RelayCommand ShowReportsCommand { get; }
        public RelayCommand ShowUserManagementCommand { get; }
        public RelayCommand ChangeClassCommand { get; }
        public RelayCommand LogoutCommand { get; }

        private readonly Window _ownerWindow;

        public MainDashboardViewModel(Window ownerWindow)
        {
            _ownerWindow = ownerWindow;

            ShowHomeCommand = new RelayCommand(_ => NavigateHome());
            ShowStudentsCommand = new RelayCommand(_ => Navigate("Students", new StudentsView()));
            ShowAttendanceFeesCommand = new RelayCommand(_ => Navigate("AttendanceFees", new AttendanceFeesView()));
            ShowPendingFeesCommand = new RelayCommand(_ => Navigate("PendingFees", new PendingFeesView()));
            ShowDailySummaryCommand = new RelayCommand(_ => Navigate("DailySummary", new DailySummaryView()));
            ShowReportsCommand = new RelayCommand(_ => Navigate("Reports", new ReportsView()));
            ShowUserManagementCommand = new RelayCommand(_ => { if (IsAdmin) Navigate("UserManagement", new UserManagementView()); });
            ChangeClassCommand = new RelayCommand(_ => ChangeClass());
            LogoutCommand = new RelayCommand(_ => Logout());

            NavigateHome();
        }

        private void NavigateHome() => Navigate("Home", new HomeView());

        private void Navigate(string menuKey, UIElement view)
        {
            ActiveMenu = menuKey;
            CurrentView = view;
        }

        private void ChangeClass()
        {
            var selectGrade = new SelectGradeWindow();
            selectGrade.Show();
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
