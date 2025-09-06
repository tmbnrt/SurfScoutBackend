using SurfScoutBackend.Models;

namespace SurfScoutBackend.Functions
{
    public static class CheckNewUserData
    {
        public static string IsValidUserData(User user)
        {
            // Check if username is empty or null
            if (string.IsNullOrWhiteSpace(user.Username))
                return "user name is not valid.";

            // Check email
            if (!IsValidEmail(user.Email))
                return "Email is not valid.";

            if (!PasswordIsSave(user.Password_hash))
                return "Password must contain at least 8 digits, numeric and upper/lower case.";

            // Check if password hash is empty or null
            if (string.IsNullOrWhiteSpace(user.Password_hash))
                return "Password must not be empty.";

            var validRoles = new[] { "user", "admin", "unknown" };
            if (!validRoles.Contains(user.Role))
                return "User role is not valid.";

            return "valid";
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool PasswordIsSave(string password)
        {
            if (password.Length < 8)
                return false;

            if (!password.Any(char.IsDigit))
                return false;

            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
                return false;

            return true;
        }
    }
}
