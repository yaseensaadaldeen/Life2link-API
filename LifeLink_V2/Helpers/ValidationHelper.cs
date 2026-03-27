using System.Text.RegularExpressions;

namespace LifeLink_V2.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidSyrianPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Remove any spaces, dashes, or parentheses
            phone = Regex.Replace(phone, @"[\s\-\(\)]", "");

            // Check for Syrian mobile numbers: +9639xxxxxxxx or 009639xxxxxxxx or 09xxxxxxxx
            if (Regex.IsMatch(phone, @"^(?:\+?963|00963|0)?9[0-9]{8}$"))
                return true;

            // Check for Syrian landline numbers
            if (Regex.IsMatch(phone, @"^(?:\+?963|00963|0)?1[1-9][0-9]{6}$"))
                return true;

            return false;
        }

        public static bool IsValidEmail(string email)
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

        public static bool IsValidNationalId(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Length != 11)
                return false;

            return Regex.IsMatch(nationalId, @"^\d{11}$");
        }

        public static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
                age--;

            return age;
        }
    }
}