using System;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;

namespace TuitionManagementSystem.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public string Grade => AppSession.CurrentGrade;
        public string TodayDate => DateTime.Now.ToString("dd MMMM yyyy");
        public string HeaderTitle => $"{Grade} Mathematics Today's Class Summary";
        public string HeaderSubtitle => $"Detailed breakdown of academic parameters, attendance and financial metrics for {TodayDate}.";

        public int TotalStudents { get; private set; }
        public int PresentToday { get; private set; }
        public int AbsentToday { get; private set; }
        public decimal FeesCollected { get; private set; }
        public decimal PendingFeesAmount { get; private set; }

        public bool HasSessionToday { get; private set; }
        public string LessonName { get; private set; } = "No session recorded yet";
        public decimal DailyFee { get; private set; }
        public int EnrolledCount { get; private set; }

        public HomeViewModel()
        {
            Load();
        }

        private void Load()
        {
            TotalStudents = DatabaseHelper.CountStudentsByGrade(Grade);
            EnrolledCount = TotalStudents;

            var session = DatabaseHelper.GetSession(Grade, DateTime.Today);
            if (session != null)
            {
                HasSessionToday = true;
                LessonName = string.IsNullOrWhiteSpace(session.LessonName) ? "(No lesson name set)" : session.LessonName;
                DailyFee = session.DailyFee;

                var (total, present, absent, paid, pending, collected) = DatabaseHelper.GetSessionSummary(session.Id);
                PresentToday = present;
                AbsentToday = absent;
                FeesCollected = collected;
                // "pending" already only counts PRESENT students who haven't paid - an absent
                // student never owed anything for a day they didn't attend, so they're excluded.
                PendingFeesAmount = pending * session.DailyFee;
            }
            else
            {
                HasSessionToday = false;
                PresentToday = 0;
                AbsentToday = TotalStudents;
                FeesCollected = 0;
                PendingFeesAmount = 0;
            }

            OnPropertyChanged(string.Empty); // refresh every bound property
        }
    }
}
