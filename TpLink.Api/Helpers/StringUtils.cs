using System.Security.Cryptography;
using System.Text;

namespace TpLink.Api.Helpers;

public static class StringUtils
{
    /// <summary>Lower-case hex MD5 of the password, which is what the device's login script computes.</summary>
    public static string HashPassword(string password)
    {
        var hashBuffer = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashBuffer).ToLowerInvariant();
    }

    /// <summary>The value of the <c>Authorization</c> cookie: <c>Basic {login}:{md5(password)}</c>, URL-escaped.</summary>
    public static string GetAuthorization(string login, string password)
    {
        return Uri.EscapeDataString($"Basic {login}:{HashPassword(password)}");
    }
}
