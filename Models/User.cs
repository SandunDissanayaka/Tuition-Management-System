namespace TuitionManagementSystem.Models
{
    /// <summary>Represents a login account. Role is either "Admin" or "User".</summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public string CreatedDate { get; set; }
    }
}
