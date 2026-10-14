using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Views;

namespace TuitionManagementSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Creates the SQLite database file and all tables the first time the app runs,
            // and seeds one default Admin account (see SETUP_GUIDE.md for the initial password).
            DatabaseHelper.InitializeDatabase();

            // ---- Emergency admin password recovery ----
            // If the app is launched with:  TuitionManagementSystem.exe --reset-admin-password=NewPassword123
            // the "admin" account's password is reset immediately, the app tells you it worked,
            // and then exits so you can log in normally with the new password. This does NOT
            // touch any student, attendance or fee data - only the admin login credential.
            string resetArg = e.Args.Length > 0
                ? System.Array.Find(e.Args, a => a.StartsWith("--reset-admin-password="))
                : null;

            if (resetArg != null)
            {
                string newPassword = resetArg.Substring("--reset-admin-password=".Length);

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    MessageBox.Show("The new password must be at least 6 characters.\n\nExample:\nTuitionManagementSystem.exe --reset-admin-password=NewSecurePass1",
                        "Password Reset - Invalid Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    bool success = DatabaseHelper.ResetAdminPasswordDirect(newPassword);
                    MessageBox.Show(success
                        ? $"The admin password has been reset successfully.\n\nYou can now log in with:\nUsername: admin\nPassword: {newPassword}\n\n(Also make sure the account is re-enabled - this reset also re-activates it.)"
                        : "No 'admin' account was found in the database to reset.",
                        "Admin Password Reset", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Shutdown();
                return;
            }

            var login = new LoginWindow();
            login.Show();
        }
    }
}
