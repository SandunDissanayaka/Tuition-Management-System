using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.Helpers
{
    /// <summary>Tiny static holder that keeps track of who is logged in and which grade
    /// they picked, so every ViewModel in the app can read it without passing it around manually.</summary>
    public static class AppSession
    {
        public static User CurrentUser { get; set; }
        public static string CurrentGrade { get; set; } = "Grade 10";
    }
}
