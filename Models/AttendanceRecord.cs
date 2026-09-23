namespace TuitionManagementSystem.Models
{
    /// <summary>One row linking a Student to a ClassSession: their attendance and fee status that day.</summary>
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StudentId { get; set; }

        // Convenience fields filled in by JOIN queries for display in the grids
        public string StudentCode { get; set; }
        public string StudentName { get; set; }

        public string Attendance { get; set; } = "Not Marked";  // "Not Marked", "Present" or "Absent"
        public string FeeStatus { get; set; } = "Unpaid";    // "Paid", "Unpaid" or "Pending"
        public decimal Amount { get; set; }

        // Extra display fields used on the Pending Fees screen
        public string SessionDate { get; set; }
        public string LessonName { get; set; }
        public string Grade { get; set; }
    }
}
