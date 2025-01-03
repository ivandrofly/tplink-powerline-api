using System;
using System.Text;

namespace TpLink.Api.Helpers
{
    public static class StringUtils
    {
        public static string HashPassword(string password)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBuffer = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            //  A string of hexadecimal pairs separated by hyphens, where each pair represents the corresponding element in value; for example, "7F-2C-4A-00"
            return BitConverter.ToString(hashBuffer).Replace("-", string.Empty).ToLowerInvariant();
        }

        //System.Net.WebUtility.HtmlEncode()
        //System.Uri.EscapeDataString()
        public static string GetAuthorization(string login, string password)
        {
            return $"{Uri.EscapeDataString($"Basic {login}:{HashPassword(password)}")}";
        }
    }
}