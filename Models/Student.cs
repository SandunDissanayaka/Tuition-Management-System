namespace TuitionManagementSystem.Models
{
    /// <summary>Represents one enrolled student in a Grade 10 or Grade 11 class.</summary>
    public class Student
    {
        public int Id { get; set; }
        public string StudentCode { get; set; }   // e.g. STU-001
        public string Name { get; set; }
        public string Grade { get; set; }          // "Grade 10" or "Grade 11"
        public string School { get; set; }
        public string ParentGuardian { get; set; }
        public string Contact { get; set; }
    }
}
