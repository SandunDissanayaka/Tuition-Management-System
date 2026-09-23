using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.ViewModels
{
    public class PendingFeesViewModel : ViewModelBase
    {
        public string Grade => AppSession.CurrentGrade;

        private DateTime? _fromDate;
        public DateTime? FromDate { get => _fromDate; set { SetField(ref _fromDate, value); Load(); } }

        private DateTime? _toDate;
        public DateTime? ToDate { get => _toDate; set { SetField(ref _toDate, value); Load(); } }

        private string _searchText;
        public string SearchText { get => _searchText; set { SetField(ref _searchText, value); Load(); } }

        public ObservableCollection<AttendanceRecord> PendingRecords { get; set; } = new();

        public int PendingStudentsCount => PendingRecords.Count;
        public decimal TotalPendingAmount => PendingRecords.Sum(r => GetDue(r));

        public RelayCommand<AttendanceRecord> CollectCommand { get; }

        public PendingFeesViewModel()
        {
            CollectCommand = new RelayCommand<AttendanceRecord>(Collect);
            Load();
        }

        private decimal GetDue(AttendanceRecord r)
        {
            // The amount due equals that session's daily fee; look it up quickly.
            var session = DatabaseHelper.GetSession(r.Grade, DateTime.Parse(r.SessionDate));
            return session?.DailyFee ?? 0;
        }

        private void Load()
        {
            PendingRecords.Clear();
            var records = DatabaseHelper.GetPendingFees(Grade, FromDate, ToDate, SearchText);
            foreach (var r in records) PendingRecords.Add(r);
            OnPropertyChanged(nameof(PendingStudentsCount));
            OnPropertyChanged(nameof(TotalPendingAmount));
        }

        private void Collect(AttendanceRecord record)
        {
            if (record == null) return;
            decimal amountDue = GetDue(record);

            var result = MessageBox.Show(
                $"Collect Rs. {amountDue:N0} from {record.StudentName} for {record.LessonName} ({record.SessionDate})?",
                "Confirm collection", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DatabaseHelper.MarkFeeCollected(record.Id, amountDue);
                Load();
            }
        }
    }
}
