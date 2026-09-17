using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.Data
{
    /// <summary>
    /// One simple static class handling every database call in the app.
    /// This keeps the project easy to follow: there is exactly one place that talks to SQLite.
    /// </summary>
    public static class DatabaseHelper
    {
        // The .db file is created next to the .exe, inside a "Data" sub-folder.
        private static readonly string DbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string DbFile = Path.Combine(DbFolder, "TuitionManagement.db");
        public static string ConnectionString => $"Data Source={DbFile}";

        private static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================
        public static void InitializeDatabase()
        {
            Directory.CreateDirectory(DbFolder);

            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    FullName TEXT,
                    Role TEXT NOT NULL DEFAULT 'User',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedDate TEXT
                );

                CREATE TABLE IF NOT EXISTS Students (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentCode TEXT UNIQUE,
                    Name TEXT NOT NULL,
                    Grade TEXT NOT NULL,
                    School TEXT,
                    ParentGuardian TEXT,
                    Contact TEXT
                );

                CREATE TABLE IF NOT EXISTS Sessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Grade TEXT NOT NULL,
                    SessionDate TEXT NOT NULL,
                    LessonName TEXT,
                    DailyFee REAL NOT NULL DEFAULT 0,
                    PrintingExpense REAL NOT NULL DEFAULT 0,
                    VenueExpense REAL NOT NULL DEFAULT 0,
                    UNIQUE(Grade, SessionDate)
                );

                CREATE TABLE IF NOT EXISTS AttendanceRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER NOT NULL,
                    StudentId INTEGER NOT NULL,
                    Attendance TEXT NOT NULL DEFAULT 'Not Marked',
                    FeeStatus TEXT NOT NULL DEFAULT 'Unpaid',
                    Amount REAL NOT NULL DEFAULT 0,
                    UNIQUE(SessionId, StudentId),
                    FOREIGN KEY (SessionId) REFERENCES Sessions(Id),
                    FOREIGN KEY (StudentId) REFERENCES Students(Id)
                );";
            cmd.ExecuteNonQuery();

            SeedDefaultAdmin();
        }

        private static void SeedDefaultAdmin()
        {
            using var conn = GetConnection();
            var check = conn.CreateCommand();
            // Specifically checks for an "admin" account rather than "any users at all",
            // so the default admin always exists even if the Users table already has
            // other rows (e.g. self-registered accounts created before this ran).
            check.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = 'admin' COLLATE NOCASE";
            long count = (long)check.ExecuteScalar();
            if (count > 0) return;

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CreatedDate)
                                 VALUES ($u, $p, $f, 'Admin', 1, $d)";
            cmd.Parameters.AddWithValue("$u", "admin");
            cmd.Parameters.AddWithValue("$p", PasswordHelper.Hash("Jy@Adm2026#Secure"));
            cmd.Parameters.AddWithValue("$f", "Administrator");
            cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        // =====================================================================
        // USERS  (Login, Registration, User Management module)
        // =====================================================================
        public static User GetUserByUsername(string username)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Username = $u COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$u", username);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return ReadUser(reader);
            return null;
        }

        public static bool UsernameExists(string username)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = $u COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$u", username);
            return (long)cmd.ExecuteScalar() > 0;
        }

        public static void RegisterUser(string username, string password, string fullName, string role = "User")
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CreatedDate)
                                 VALUES ($u, $p, $f, $r, 1, $d)";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$p", PasswordHelper.Hash(password));
            cmd.Parameters.AddWithValue("$f", fullName);
            cmd.Parameters.AddWithValue("$r", role);
            cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users ORDER BY Id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(ReadUser(reader));
            return list;
        }

        public static void ResetPassword(int userId, string newPassword)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET PasswordHash = $p WHERE Id = $id";
            cmd.Parameters.AddWithValue("$p", PasswordHelper.Hash(newPassword));
            cmd.Parameters.AddWithValue("$id", userId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Emergency recovery path for a forgotten admin password: resets the "admin"
        /// account's password directly by username, without needing to be logged in.
        /// Used by App.xaml.cs when the app is launched with a --reset-admin-password argument.
        /// Returns true if an admin account was found and updated.
        /// </summary>
        public static bool ResetAdminPasswordDirect(string newPassword)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET PasswordHash = $p, IsActive = 1 WHERE Username = 'admin' COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$p", PasswordHelper.Hash(newPassword));
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public static void SetUserActive(int userId, bool isActive)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsActive = $a WHERE Id = $id";
            cmd.Parameters.AddWithValue("$a", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", userId);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteUser(int userId)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", userId);
            cmd.ExecuteNonQuery();
        }

        private static User ReadUser(SqliteDataReader r) => new User
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Username = r.GetString(r.GetOrdinal("Username")),
            PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
            FullName = r.IsDBNull(r.GetOrdinal("FullName")) ? "" : r.GetString(r.GetOrdinal("FullName")),
            Role = r.GetString(r.GetOrdinal("Role")),
            IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
            CreatedDate = r.IsDBNull(r.GetOrdinal("CreatedDate")) ? "" : r.GetString(r.GetOrdinal("CreatedDate"))
        };

        // =====================================================================
        // STUDENTS
        // =====================================================================
        public static List<Student> GetStudentsByGrade(string grade)
        {
            var list = new List<Student>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Students WHERE Grade = $g ORDER BY Id";
            cmd.Parameters.AddWithValue("$g", grade);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(ReadStudent(reader));
            return list;
        }

        public static int CountStudentsByGrade(string grade)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Students WHERE Grade = $g";
            cmd.Parameters.AddWithValue("$g", grade);
            return Convert.ToInt32((long)cmd.ExecuteScalar());
        }

        public static void AddStudent(Student s)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Students (StudentCode, Name, Grade, School, ParentGuardian, Contact)
                                 VALUES ($c, $n, $g, $s, $p, $ct)";
            cmd.Parameters.AddWithValue("$c", s.StudentCode);
            cmd.Parameters.AddWithValue("$n", s.Name);
            cmd.Parameters.AddWithValue("$g", s.Grade);
            cmd.Parameters.AddWithValue("$s", s.School ?? "");
            cmd.Parameters.AddWithValue("$p", s.ParentGuardian ?? "");
            cmd.Parameters.AddWithValue("$ct", s.Contact ?? "");
            cmd.ExecuteNonQuery();
        }

        public static void UpdateStudent(Student s)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Students SET Name=$n, School=$s, ParentGuardian=$p, Contact=$ct WHERE Id=$id";
            cmd.Parameters.AddWithValue("$n", s.Name);
            cmd.Parameters.AddWithValue("$s", s.School ?? "");
            cmd.Parameters.AddWithValue("$p", s.ParentGuardian ?? "");
            cmd.Parameters.AddWithValue("$ct", s.Contact ?? "");
            cmd.Parameters.AddWithValue("$id", s.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteStudent(int studentId)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Students WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", studentId);
            cmd.ExecuteNonQuery();
        }

        public static string GetNextStudentCode()
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Students";
            long count = (long)cmd.ExecuteScalar();
            return $"STU-{(count + 1):D3}";
        }

        private static Student ReadStudent(SqliteDataReader r) => new Student
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            StudentCode = r.GetString(r.GetOrdinal("StudentCode")),
            Name = r.GetString(r.GetOrdinal("Name")),
            Grade = r.GetString(r.GetOrdinal("Grade")),
            School = r.IsDBNull(r.GetOrdinal("School")) ? "" : r.GetString(r.GetOrdinal("School")),
            ParentGuardian = r.IsDBNull(r.GetOrdinal("ParentGuardian")) ? "" : r.GetString(r.GetOrdinal("ParentGuardian")),
            Contact = r.IsDBNull(r.GetOrdinal("Contact")) ? "" : r.GetString(r.GetOrdinal("Contact"))
        };

        // =====================================================================
        // SESSIONS + ATTENDANCE + FEES
        // =====================================================================

        /// <summary>Read-only lookup - returns null if no session exists yet for that grade+date.</summary>
        public static ClassSession GetSession(string grade, DateTime date)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Sessions WHERE Grade=$g AND SessionDate=$d";
            cmd.Parameters.AddWithValue("$g", grade);
            cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadSession(reader) : null;
        }

        /// <summary>Gets an existing session for that grade+date, or creates a new one.</summary>
        public static ClassSession GetOrCreateSession(string grade, DateTime date, string lessonName, decimal dailyFee)
        {
            string dateStr = date.ToString("yyyy-MM-dd");
            using var conn = GetConnection();

            long sessionId;
            ClassSession session;

            var find = conn.CreateCommand();
            find.CommandText = "SELECT * FROM Sessions WHERE Grade=$g AND SessionDate=$d";
            find.Parameters.AddWithValue("$g", grade);
            find.Parameters.AddWithValue("$d", dateStr);
            ClassSession existing = null;
            using (var reader = find.ExecuteReader())
            {
                if (reader.Read()) existing = ReadSession(reader);
            }

            if (existing != null)
            {
                sessionId = existing.Id;
                session = existing;
            }
            else
            {
                var insert = conn.CreateCommand();
                insert.CommandText = @"INSERT INTO Sessions (Grade, SessionDate, LessonName, DailyFee)
                                        VALUES ($g, $d, $l, $f); SELECT last_insert_rowid();";
                insert.Parameters.AddWithValue("$g", grade);
                insert.Parameters.AddWithValue("$d", dateStr);
                insert.Parameters.AddWithValue("$l", lessonName);
                insert.Parameters.AddWithValue("$f", (double)dailyFee);
                sessionId = (long)insert.ExecuteScalar();
                session = new ClassSession { Id = (int)sessionId, Grade = grade, SessionDate = dateStr, LessonName = lessonName, DailyFee = dailyFee };
            }

            // Make sure every CURRENT student of this grade has an attendance row for this
            // session - this runs every time, not just when the session is first created, so
            // a student added to the class AFTER today's session already existed still shows
            // up here (INSERT OR IGNORE means students who already have a row are untouched).
            foreach (var student in GetStudentsByGrade(grade))
            {
                var ar = conn.CreateCommand();
                ar.CommandText = @"INSERT OR IGNORE INTO AttendanceRecords (SessionId, StudentId, Attendance, FeeStatus, Amount)
                                    VALUES ($sid, $stid, 'Not Marked', 'Unpaid', 0)";
                ar.Parameters.AddWithValue("$sid", sessionId);
                ar.Parameters.AddWithValue("$stid", student.Id);
                ar.ExecuteNonQuery();
            }

            return session;
        }

        public static void UpdateSessionDetails(int sessionId, string lessonName, decimal dailyFee)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Sessions SET LessonName=$l, DailyFee=$f WHERE Id=$id";
            cmd.Parameters.AddWithValue("$l", lessonName);
            cmd.Parameters.AddWithValue("$f", (double)dailyFee);
            cmd.Parameters.AddWithValue("$id", sessionId);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateSessionExpenses(int sessionId, decimal printing, decimal venue)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Sessions SET PrintingExpense=$p, VenueExpense=$v WHERE Id=$id";
            cmd.Parameters.AddWithValue("$p", (double)printing);
            cmd.Parameters.AddWithValue("$v", (double)venue);
            cmd.Parameters.AddWithValue("$id", sessionId);
            cmd.ExecuteNonQuery();
        }

        public static List<AttendanceRecord> GetAttendanceForSession(int sessionId)
        {
            var list = new List<AttendanceRecord>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ar.*, s.StudentCode, s.Name AS StudentName
                FROM AttendanceRecords ar
                JOIN Students s ON s.Id = ar.StudentId
                WHERE ar.SessionId = $sid
                ORDER BY s.Id";
            cmd.Parameters.AddWithValue("$sid", sessionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(ReadAttendance(reader));
            return list;
        }

        public static void SaveAttendanceRecord(AttendanceRecord ar)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE AttendanceRecords SET Attendance=$a, FeeStatus=$f, Amount=$amt
                                 WHERE SessionId=$sid AND StudentId=$stid";
            cmd.Parameters.AddWithValue("$a", ar.Attendance);
            cmd.Parameters.AddWithValue("$f", ar.FeeStatus);
            cmd.Parameters.AddWithValue("$amt", (double)ar.Amount);
            cmd.Parameters.AddWithValue("$sid", ar.SessionId);
            cmd.Parameters.AddWithValue("$stid", ar.StudentId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Every student who was PRESENT but hasn't paid yet, for a grade, optionally
        /// filtered by date range and name search. Absent students never owe a fee for a day
        /// they didn't attend, so they are excluded here regardless of their FeeStatus value.</summary>
        public static List<AttendanceRecord> GetPendingFees(string grade, DateTime? from, DateTime? to, string searchName)
        {
            var list = new List<AttendanceRecord>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ar.*, s.StudentCode, s.Name AS StudentName, sess.SessionDate, sess.LessonName, sess.Grade
                FROM AttendanceRecords ar
                JOIN Students s ON s.Id = ar.StudentId
                JOIN Sessions sess ON sess.Id = ar.SessionId
                WHERE sess.Grade = $g
                  AND ar.Attendance = 'Present'
                  AND ar.FeeStatus IN ('Pending','Unpaid')
                  AND ($from IS NULL OR sess.SessionDate >= $from)
                  AND ($to IS NULL OR sess.SessionDate <= $to)
                  AND ($name IS NULL OR s.Name LIKE $name)
                ORDER BY sess.SessionDate DESC";
            cmd.Parameters.AddWithValue("$g", grade);
            cmd.Parameters.AddWithValue("$from", (object)from?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$to", (object)to?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$name", string.IsNullOrWhiteSpace(searchName) ? (object)DBNull.Value : $"%{searchName}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(ReadAttendance(reader, withSessionInfo: true));
            return list;
        }

        public static void MarkFeeCollected(int attendanceRecordId, decimal amount)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE AttendanceRecords SET FeeStatus='Paid', Amount=$amt WHERE Id=$id";
            cmd.Parameters.AddWithValue("$amt", (double)amount);
            cmd.Parameters.AddWithValue("$id", attendanceRecordId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Aggregated numbers for the Home dashboard and Daily Summary screen.
        /// "Absent" is counted explicitly rather than as "everything that isn't present",
        /// because a record can also be "Not Marked" (not yet decided by the admin/user).
        /// "paid" and "pending" only consider PRESENT students: a student who was absent
        /// never owed a fee for that day in the first place, so they count toward neither.</summary>
        public static (int total, int present, int absent, int paid, int pending, decimal collected) GetSessionSummary(int sessionId)
        {
            var records = GetAttendanceForSession(sessionId);
            int total = records.Count;
            int present = records.FindAll(r => r.Attendance == "Present").Count;
            int absent = records.FindAll(r => r.Attendance == "Absent").Count;
            int paid = records.FindAll(r => r.Attendance == "Present" && r.FeeStatus == "Paid").Count;
            int pending = records.FindAll(r => r.Attendance == "Present" && r.FeeStatus != "Paid").Count;
            decimal collected = 0;
            foreach (var r in records) collected += r.Amount;
            return (total, present, absent, paid, pending, collected);
        }

        private static ClassSession ReadSession(SqliteDataReader r) => new ClassSession
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Grade = r.GetString(r.GetOrdinal("Grade")),
            SessionDate = r.GetString(r.GetOrdinal("SessionDate")),
            LessonName = r.IsDBNull(r.GetOrdinal("LessonName")) ? "" : r.GetString(r.GetOrdinal("LessonName")),
            DailyFee = Convert.ToDecimal(r.GetDouble(r.GetOrdinal("DailyFee"))),
            PrintingExpense = Convert.ToDecimal(r.GetDouble(r.GetOrdinal("PrintingExpense"))),
            VenueExpense = Convert.ToDecimal(r.GetDouble(r.GetOrdinal("VenueExpense")))
        };

        private static AttendanceRecord ReadAttendance(SqliteDataReader r, bool withSessionInfo = false)
        {
            var ar = new AttendanceRecord
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                SessionId = r.GetInt32(r.GetOrdinal("SessionId")),
                StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
                StudentCode = r.GetString(r.GetOrdinal("StudentCode")),
                StudentName = r.GetString(r.GetOrdinal("StudentName")),
                Attendance = r.GetString(r.GetOrdinal("Attendance")),
                FeeStatus = r.GetString(r.GetOrdinal("FeeStatus")),
                Amount = Convert.ToDecimal(r.GetDouble(r.GetOrdinal("Amount")))
            };
            if (withSessionInfo)
            {
                ar.SessionDate = r.GetString(r.GetOrdinal("SessionDate"));
                ar.LessonName = r.IsDBNull(r.GetOrdinal("LessonName")) ? "" : r.GetString(r.GetOrdinal("LessonName"));
                ar.Grade = r.GetString(r.GetOrdinal("Grade"));
            }
            return ar;
        }
    }
}
