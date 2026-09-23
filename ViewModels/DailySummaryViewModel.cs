using System;
using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.ViewModels
{
    public class DailySummaryViewModel : ViewModelBase
    {
        public string Grade => AppSession.CurrentGrade;

        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set { SetField(ref _date, value); Load(); }
        }

        public string LessonName { get; private set; }
        public int TotalStudents { get; private set; }
        public int Present { get; private set; }
        public int Absent { get; private set; }
        public int Paid { get; private set; }
        public int Pending { get; private set; }
        public decimal TotalFeesCollected { get; private set; }

        private decimal _printingExpense;
        public decimal PrintingExpense
        {
            get => _printingExpense;
            set { SetField(ref _printingExpense, value); OnPropertyChanged(nameof(NetDailyAmount)); }
        }

        private decimal _venueExpense;
        public decimal VenueExpense
        {
            get => _venueExpense;
            set { SetField(ref _venueExpense, value); OnPropertyChanged(nameof(NetDailyAmount)); }
        }

        public decimal NetDailyAmount => TotalFeesCollected - PrintingExpense - VenueExpense;

        public bool HasSession { get; private set; }

        public RelayCommand SaveCommand { get; }

        private ClassSession _session;

        public DailySummaryViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save());
            Load();
        }

        private void Load()
        {
            _session = DatabaseHelper.GetSession(Grade, Date);
            TotalStudents = DatabaseHelper.CountStudentsByGrade(Grade);

            if (_session != null)
            {
                HasSession = true;
                LessonName = _session.LessonName;
                var (total, present, absent, paid, pending, collected) = DatabaseHelper.GetSessionSummary(_session.Id);
                Present = present;
                Absent = absent;
                Paid = paid;
                Pending = pending;
                TotalFeesCollected = collected;
                _printingExpense = _session.PrintingExpense;
                _venueExpense = _session.VenueExpense;
            }
            else
            {
                HasSession = false;
                LessonName = "No class recorded for this date";
                Present = 0;
                Absent = TotalStudents;
                Paid = 0;
                Pending = 0;
                TotalFeesCollected = 0;
                _printingExpense = 0;
                _venueExpense = 0;
            }

            OnPropertyChanged(string.Empty);
        }

        private void Save()
        {
            if (!HasSession)
            {
                MessageBox.Show("There is no attendance session recorded for this date yet. " +
                                 "Record attendance first from the 'Attendance & Fees' screen.",
                                 "Nothing to save", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DatabaseHelper.UpdateSessionExpenses(_session.Id, PrintingExpense, VenueExpense);
            MessageBox.Show("Daily summary saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
