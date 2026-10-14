using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;

namespace TuitionManagementSystem.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        public string Grade => AppSession.CurrentGrade;

        /// <summary>Stores a picked date, but never a date after today. If a future date gets in
        /// (typed by hand, for example) it is replaced by today and the picker is refreshed.</summary>
        private void SetPastOrToday(ref DateTime field, DateTime value, string propertyName)
        {
            DateTime allowed = value.Date > DateTime.Today ? DateTime.Today : value.Date;
            bool changed = SetField(ref field, allowed, propertyName);
            if (!changed && allowed != value) OnPropertyChanged(propertyName);
        }

        // ------------------------------------------------------------------
        // PENDING FEE REPORT (PDF export)
        // ------------------------------------------------------------------
        private DateTime _fromDate = DateTime.Today.AddDays(-7);
        public DateTime FromDate { get => _fromDate; set => SetPastOrToday(ref _fromDate, value, nameof(FromDate)); }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate { get => _toDate; set => SetPastOrToday(ref _toDate, value, nameof(ToDate)); }

        private string _statusMessage;
        public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

        public RelayCommand ExportPendingFeesPdfCommand { get; }

        // ------------------------------------------------------------------
        // ATTENDANCE HISTORY (search a student -> lessons attended / absent)
        // ------------------------------------------------------------------
        private string _historySearchText = "";
        /// <summary>Student name (or part of it) or student ID to look up. Results update as you type.</summary>
        public string HistorySearchText
        {
            get => _historySearchText;
            set { if (SetField(ref _historySearchText, value ?? "")) LoadHistory(); }
        }

        private DateTime _historyFromDate = DateTime.Today.AddDays(-30);
        public DateTime HistoryFromDate
        {
            get => _historyFromDate;
            set { SetPastOrToday(ref _historyFromDate, value, nameof(HistoryFromDate)); LoadHistory(); }
        }

        private DateTime _historyToDate = DateTime.Today;
        public DateTime HistoryToDate
        {
            get => _historyToDate;
            set { SetPastOrToday(ref _historyToDate, value, nameof(HistoryToDate)); LoadHistory(); }
        }

        /// <summary>Lessons the student(s) attended (marked Present) in the date range.</summary>
        public ObservableCollection<AttendanceHistoryItem> AttendedLessons { get; } = new();

        /// <summary>Lessons the student(s) missed (marked Absent) in the date range.</summary>
        public ObservableCollection<AttendanceHistoryItem> AbsentLessons { get; } = new();

        public int AttendedCount => AttendedLessons.Count;
        public int AbsentCount => AbsentLessons.Count;

        private string _historyMessage;
        /// <summary>Hint / warning / "no records" / "showing results for ..." text above the results.</summary>
        public string HistoryMessage { get => _historyMessage; set => SetField(ref _historyMessage, value); }

        private bool _showHistoryResults;
        /// <summary>True when there is at least one attendance record to show.</summary>
        public bool ShowHistoryResults { get => _showHistoryResults; set => SetField(ref _showHistoryResults, value); }

        public bool NoAttendedLessons => ShowHistoryResults && AttendedLessons.Count == 0;
        public bool NoAbsentLessons => ShowHistoryResults && AbsentLessons.Count == 0;

        public ReportsViewModel()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            ExportPendingFeesPdfCommand = new RelayCommand(_ => ExportPendingFeesPdf());
            LoadHistory();
        }

        private void LoadHistory()
        {
            AttendedLessons.Clear();
            AbsentLessons.Clear();
            ShowHistoryResults = false;

            string query = (HistorySearchText ?? "").Trim();

            if (query.Length == 0)
            {
                HistoryMessage = "Type a student's name or ID above to see the lessons they attended and the lessons they missed.";
            }
            else if (HistoryFromDate.Date > HistoryToDate.Date)
            {
                HistoryMessage = "The From date cannot be later than the To date.";
            }
            else
            {
                var records = DatabaseHelper.GetAttendanceHistory(Grade, query, HistoryFromDate, HistoryToDate);

                foreach (var r in records)
                {
                    var item = new AttendanceHistoryItem
                    {
                        StudentCode = r.StudentCode,
                        StudentName = r.StudentName,
                        LessonName = string.IsNullOrWhiteSpace(r.LessonName) ? "-" : r.LessonName,
                        DateText = DateTime.TryParseExact(r.SessionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var d) ? d.ToString("dd MMM yyyy") : r.SessionDate
                    };

                    if (r.Attendance == "Present") AttendedLessons.Add(item);
                    else if (r.Attendance == "Absent") AbsentLessons.Add(item);
                }

                if (records.Count == 0)
                {
                    HistoryMessage = $"No attendance has been recorded for \"{query}\" between " +
                                     $"{HistoryFromDate:dd MMM yyyy} and {HistoryToDate:dd MMM yyyy}.";
                }
                else
                {
                    var students = records.Select(r => r.StudentName).Distinct().ToList();
                    HistoryMessage = students.Count == 1
                        ? $"Attendance history for {students[0]} ({records[0].StudentCode})"
                        : $"{students.Count} students match \"{query}\" - type more of the name or the full ID to see just one student.";
                    ShowHistoryResults = true;
                }
            }

            OnPropertyChanged(nameof(AttendedCount));
            OnPropertyChanged(nameof(AbsentCount));
            OnPropertyChanged(nameof(NoAttendedLessons));
            OnPropertyChanged(nameof(NoAbsentLessons));
        }

        private void ExportPendingFeesPdf()
        {
            var records = DatabaseHelper.GetPendingFees(Grade, FromDate, ToDate, null);

            var dialog = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = $"{Grade.Replace(" ", "")}_FeeReport_{DateTime.Now:yyyyMMdd}.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"{Grade} - Pending Fee Report ({FromDate:dd MMM yyyy} to {ToDate:dd MMM yyyy})")
                        .SemiBold().FontSize(16);

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Student").SemiBold();
                            header.Cell().Text("Date").SemiBold();
                            header.Cell().Text("Lesson").SemiBold();
                            header.Cell().Text("Status").SemiBold();
                            header.Cell().Text("Amount Due").SemiBold();
                        });

                        foreach (var r in records)
                        {
                            var session = DatabaseHelper.GetSession(r.Grade, DateTime.Parse(r.SessionDate));
                            table.Cell().Text(r.StudentName);
                            table.Cell().Text(r.SessionDate);
                            table.Cell().Text(r.LessonName);
                            table.Cell().Text(r.FeeStatus);
                            table.Cell().Text($"Rs. {session?.DailyFee ?? 0:N0}");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by Jayagath Institute Tuition Management System - ");
                        x.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                    });
                });
            }).GeneratePdf(dialog.FileName);

            StatusMessage = $"Report exported to {dialog.FileName}";
            MessageBox.Show("PDF report generated successfully.", "Export complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>One line in the Attendance History lists on the Reports screen.</summary>
    public class AttendanceHistoryItem
    {
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string LessonName { get; set; }
        public string DateText { get; set; }
    }
}
