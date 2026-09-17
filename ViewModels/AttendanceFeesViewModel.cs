using System;
using System.Collections.ObjectModel;
using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.ViewModels
{
    /// <summary>One row on the Attendance & Fees checklist. Wraps AttendanceRecord with
    /// UI-friendly properties for a ComboBox-driven Attendance/Fee status (not RadioButtons -
    /// RadioButton "GroupName" grouping is unreliable inside a virtualizing WPF DataGrid and
    /// was the cause of clicks not sticking).</summary>
    public class AttendanceRowViewModel : ViewModelBase
    {
        public AttendanceRecord Record { get; }
        public string StudentCode => Record.StudentCode;
        public string StudentName => Record.StudentName;

        /// <summary>"Not Marked", "Present" or "Absent". Starts as "Not Marked" until the
        /// admin/user explicitly picks one - nothing is pre-selected for them.</summary>
        public string Attendance
        {
            get => Record.Attendance;
            set { Record.Attendance = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); OnPropertyChanged(nameof(IsPending)); }
        }

        public string FeeStatus
        {
            get => Record.FeeStatus;
            set { Record.FeeStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); OnPropertyChanged(nameof(IsPending)); }
        }

        // Reads the CURRENT daily fee from the parent screen (not a snapshot taken when the
        // row was created), so the preview amount always matches what will actually be saved.
        private readonly Func<decimal> _getCurrentDailyFee;

        /// <summary>
        /// The fee-collection rule for the whole app lives here:
        ///   Present + Paid    -> the fee IS collected (this session's daily fee)
        ///   Present + Pending -> owed but not yet collected -> shows as Rs. 0 collected, and
        ///                        the student appears on the Pending Fees screen
        ///   Present + Unpaid  -> same as Pending: owed, Rs. 0 collected, appears as pending
        ///   Absent  (any fee status) -> the student didn't attend, so nothing is owed or
        ///                        collected for this session at all -> always Rs. 0
        ///   Not Marked        -> nothing decided yet -> always Rs. 0
        /// </summary>
        public decimal Amount => (Attendance == "Present" && FeeStatus == "Paid") ? _getCurrentDailyFee() : 0;

        /// <summary>True when this student attended but hasn't paid - i.e. they will show up
        /// on the Pending Fees screen once saved. Used to give a visual hint in the checklist.</summary>
        public bool IsPending => Attendance == "Present" && FeeStatus != "Paid";

        public AttendanceRowViewModel(AttendanceRecord record, Func<decimal> getCurrentDailyFee)
        {
            Record = record;
            _getCurrentDailyFee = getCurrentDailyFee;
        }

        /// <summary>Called by the parent ViewModel when the Daily Fee box changes, so every
        /// row's Amount preview stays in sync even before Save is clicked.</summary>
        public void RefreshAmount() => OnPropertyChanged(nameof(Amount));
    }

    public class AttendanceFeesViewModel : ViewModelBase
    {
        public string Grade => AppSession.CurrentGrade;

        private DateTime _sessionDate = DateTime.Today;
        public DateTime SessionDate
        {
            get => _sessionDate;
            set { SetField(ref _sessionDate, value); LoadSession(); }
        }

        private string _lessonName;
        public string LessonName
        {
            get => _lessonName;
            set => SetField(ref _lessonName, value);
        }

        private decimal _dailyFee = 500;
        public decimal DailyFee
        {
            get => _dailyFee;
            set
            {
                SetField(ref _dailyFee, value);
                // Live-refresh every row's Amount preview to match the new fee.
                foreach (var row in Rows) row.RefreshAmount();
            }
        }

        public ObservableCollection<AttendanceRowViewModel> Rows { get; set; } = new();

        public RelayCommand SaveCommand { get; }

        private ClassSession _currentSession;

        public AttendanceFeesViewModel()
        {
            SaveCommand = new RelayCommand(_ => SaveChecklist());
            LoadSession();
        }

        private void LoadSession()
        {
            _currentSession = DatabaseHelper.GetOrCreateSession(Grade, SessionDate,
                LessonName ?? "New Lesson", DailyFee);

            LessonName = _currentSession.LessonName;
            DailyFee = _currentSession.DailyFee;

            Rows.Clear();
            foreach (var record in DatabaseHelper.GetAttendanceForSession(_currentSession.Id))
                Rows.Add(new AttendanceRowViewModel(record, () => DailyFee));
        }

        private void SaveChecklist()
        {
            DatabaseHelper.UpdateSessionDetails(_currentSession.Id, LessonName, DailyFee);

            foreach (var row in Rows)
            {
                // Same rule as AttendanceRowViewModel.Amount: only a PRESENT + PAID student
                // actually has money collected. An absent student owes nothing regardless of
                // whatever FeeStatus their dropdown happens to show.
                row.Record.Amount = (row.Attendance == "Present" && row.FeeStatus == "Paid") ? DailyFee : 0;
                DatabaseHelper.SaveAttendanceRecord(row.Record);
            }

            MessageBox.Show("Attendance & fee checklist saved successfully.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);

            LoadSession();
        }
    }
}
