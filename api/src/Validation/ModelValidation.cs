using api.Models;

namespace api.Validation
{
    public static class ModelValidation
    {
        public static List<string> ValidateRegisterModel(RegisterModel model)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(model.Email))
            {
                errors.Add("Email is required");
            }
            else if (!IsValidEmail(model.Email))
            {
                errors.Add("Invalid email format");
            }

            if (string.IsNullOrEmpty(model.FullName))
            {
                errors.Add("Full name is required");
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                errors.Add("Password is required");
            }

            if (model.Password != model.ConfirmPassword)
            {
                errors.Add("Passwords do not match");
            }

            return errors;
        }

        public static List<string> ValidateLoginModel(LoginModel model)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(model.Email))
            {
                errors.Add("Email is required");
            }
            else if (!IsValidEmail(model.Email))
            {
                errors.Add("Invalid email format");
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                errors.Add("Password is required");
            }

            return errors;
        }

        public static bool IsValidEmail(string email)
        {
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
    }
}
