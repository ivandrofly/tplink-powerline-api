using System;
using System.Collections.Generic;
using System.Text;

namespace TpLink.Api.Helpers
{
    public static class StringUtils
    {
        public static string HashPassword(string password)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBuffer = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            //string hashedPwd = Encoding.UTF8.GetString));
            return BitConverter.ToString(hashBuffer).Replace("-", string.Empty).ToLowerInvariant();
            // encoding.utf8.getstirng vs bitconvert.tostring
        }
        
        //System.Net.WebUtility.HtmlEncode()
        //System.Uri.EscapeDataString()
        public static string GetAuthorization(string login, string password) => $"{Uri.EscapeDataString($"Basic {login}:{HashPassword(password)}")}";

    }
}
