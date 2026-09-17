namespace TuitionManagementSystem.Models
{
    /// <summary>Represents one class held on a specific date for a specific grade
    /// (e.g. "Grade 10" on "2026-08-10" covering the lesson "Quadratic Equations").</summary>
    public class ClassSession
    {
        public int Id { get; set; }
        public string Grade { get; set; }
        public string SessionDate { get; set; }   // stored as yyyy-MM-dd
        public string LessonName { get; set; }
        public decimal DailyFee { get; set; }
        public decimal PrintingExpense { get; set; }
        public decimal VenueExpense { get; set; }
    }
}
