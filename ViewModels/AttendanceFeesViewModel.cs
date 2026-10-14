using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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

    /// <summary>One button in the page-number strip under the attendance list
    /// (e.g. "1", "2", "3" ... or an "..." gap marker that is not clickable).</summary>
    public class PageButtonItem
    {
        public int Number { get; set; }
        public string Label { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsEllipsis { get; set; }
    }

    public class AttendanceFeesViewModel : ViewModelBase
    {
        /// <summary>How many students are shown on one page of the checklist.</summary>
        private const int PageSize = 10;

        public string Grade => AppSession.CurrentGrade;

        /// <summary>The latest date the date picker may show/select (today). Past dates only.</summary>
        public DateTime MaxDate => DateTime.Today;

        private DateTime _sessionDate = DateTime.Today;
        public DateTime SessionDate
        {
            get => _sessionDate;
            set
            {
                // Attendance can only be recorded for today or earlier - never a future date.
                if (value.Date > DateTime.Today)
                {
                    value = DateTime.Today;
                    OnPropertyChanged();   // snap the date picker back to today
                }

                SetField(ref _sessionDate, value.Date);
                _currentPage = 1;          // a different day starts again from page 1
                LoadSession();
            }
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
                // Live-refresh every row's Amount preview (all pages) to match the new fee.
                foreach (var row in _allRows) row.RefreshAmount();
            }
        }

        // ------------------------------------------------------------------
        // SEARCH + PAGING
        // ------------------------------------------------------------------

        /// <summary>EVERY student of the grade for the selected date. Save always goes through
        /// this full list, so students on other pages / hidden by a search are still saved.</summary>
        private readonly List<AttendanceRowViewModel> _allRows = new();

        /// <summary>The students that match the current search (before paging).</summary>
        private List<AttendanceRowViewModel> _filteredRows = new();

        private int _currentPage = 1;

        /// <summary>Only the (max 10) students of the current page - this is what the list binds to.</summary>
        public ObservableCollection<AttendanceRowViewModel> Rows { get; } = new();

        /// <summary>The page-number buttons shown under the list.</summary>
        public ObservableCollection<PageButtonItem> PageNumbers { get; } = new();

        private string _searchText = "";
        /// <summary>Filters the checklist by student ID (e.g. STU-003) or by part of the name.</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value ?? ""))
                {
                    _currentPage = 1;      // a new search always starts at page 1
                    ApplyFilter();
                }
            }
        }

        public int CurrentPage => _currentPage;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(_filteredRows.Count / (double)PageSize));
        public bool ShowPager => TotalPages > 1;
        public bool HasNoResults => _filteredRows.Count == 0;

        public string PageInfoText
        {
            get
            {
                if (_filteredRows.Count == 0) return "No students to show";
                int first = (_currentPage - 1) * PageSize + 1;
                int last = Math.Min(_currentPage * PageSize, _filteredRows.Count);
                return $"Showing {first}-{last} of {_filteredRows.Count} students  (Page {_currentPage} of {TotalPages})";
            }
        }

        public string NoResultsText => string.IsNullOrWhiteSpace(SearchText)
            ? "No students have been added to this grade yet."
            : $"No student matches \"{SearchText.Trim()}\".";

        public RelayCommand SaveCommand { get; }
        public RelayCommand ClearSearchCommand { get; }
        public RelayCommand PreviousPageCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand<PageButtonItem> GoToPageCommand { get; }

        private ClassSession _currentSession;

        public AttendanceFeesViewModel()
        {
            SaveCommand = new RelayCommand(_ => SaveChecklist());
            ClearSearchCommand = new RelayCommand(_ => SearchText = "");
            PreviousPageCommand = new RelayCommand(_ => GoToPage(_currentPage - 1), _ => _currentPage > 1);
            NextPageCommand = new RelayCommand(_ => GoToPage(_currentPage + 1), _ => _currentPage < TotalPages);
            GoToPageCommand = new RelayCommand<PageButtonItem>(p => { if (p != null && !p.IsEllipsis) GoToPage(p.Number); });

            LoadSession();
        }

        private void LoadSession()
        {
            _currentSession = DatabaseHelper.GetOrCreateSession(Grade, SessionDate,
                LessonName ?? "New Lesson", DailyFee);

            LessonName = _currentSession.LessonName;
            DailyFee = _currentSession.DailyFee;

            _allRows.Clear();
            foreach (var record in DatabaseHelper.GetAttendanceForSession(_currentSession.Id))
                _allRows.Add(new AttendanceRowViewModel(record, () => DailyFee));

            ApplyFilter();
        }

        /// <summary>Re-runs the search over all students, keeps the current page if it still
        /// exists, then rebuilds the visible page.</summary>
        private void ApplyFilter()
        {
            string q = (SearchText ?? "").Trim();

            _filteredRows = string.IsNullOrEmpty(q)
                ? _allRows.ToList()
                : _allRows.Where(r =>
                        (r.StudentCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        (r.StudentName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (_currentPage > TotalPages) _currentPage = TotalPages;
            if (_currentPage < 1) _currentPage = 1;

            RefreshPage();
        }

        private void GoToPage(int page)
        {
            if (page < 1) page = 1;
            if (page > TotalPages) page = TotalPages;
            if (page == _currentPage) return;

            _currentPage = page;
            RefreshPage();
        }

        /// <summary>Fills Rows with the (max 10) students of the current page and rebuilds the
        /// page-number strip.</summary>
        private void RefreshPage()
        {
            Rows.Clear();
            foreach (var row in _filteredRows.Skip((_currentPage - 1) * PageSize).Take(PageSize))
                Rows.Add(row);

            BuildPageNumbers();

            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(ShowPager));
            OnPropertyChanged(nameof(HasNoResults));
            OnPropertyChanged(nameof(PageInfoText));
            OnPropertyChanged(nameof(NoResultsText));
            CommandManager.InvalidateRequerySuggested();   // refresh Previous/Next enabled state
        }

        /// <summary>Builds the 1 2 3 ... strip. With many pages it shows
        /// first page, a window around the current page, and the last page, joined by "..." gaps.</summary>
        private void BuildPageNumbers()
        {
            PageNumbers.Clear();
            int total = TotalPages;

            var pages = new SortedSet<int> { 1, total };
            for (int p = _currentPage - 1; p <= _currentPage + 1; p++)
                if (p >= 1 && p <= total) pages.Add(p);

            int previous = 0;
            foreach (int p in pages)
            {
                if (previous != 0 && p - previous > 1)
                    PageNumbers.Add(new PageButtonItem { Label = "...", IsEllipsis = true });

                PageNumbers.Add(new PageButtonItem
                {
                    Number = p,
                    Label = p.ToString(),
                    IsCurrent = p == _currentPage
                });
                previous = p;
            }
        }

        private void SaveChecklist()
        {
            DatabaseHelper.UpdateSessionDetails(_currentSession.Id, LessonName, DailyFee);

            // Saves EVERY student of the grade (all pages, even ones hidden by the search box),
            // not just the 10 currently on screen.
            foreach (var row in _allRows)
            {
                // Same rule as AttendanceRowViewModel.Amount: only a PRESENT + PAID student
                // actually has money collected. An absent student owes nothing regardless of
                // whatever FeeStatus their dropdown happens to show.
                row.Record.Amount = (row.Attendance == "Present" && row.FeeStatus == "Paid") ? DailyFee : 0;
                DatabaseHelper.SaveAttendanceRecord(row.Record);
            }

            MessageBox.Show("Attendance & fee checklist saved successfully.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Reload from the database; the search text and current page are kept.
            LoadSession();
        }
    }
}
