using TpLink.Api.Helpers;
using Xunit;

namespace TpLink.UnitTest;

public class StringUtilsTest
{
    [Fact]
    public void GetAuthorizationBuildsTheEscapedBasicCookie()
    {
        // pins the exact cookie value the adapter expects: "Basic {login}:{md5(password)}", URL-escaped
        Assert.Equal("Basic%20ethereum%3Acd5b1e4947e304476c788cd474fb579a",
            StringUtils.GetAuthorization("ethereum", "bitcoin"));
    }

    [Fact]
    public void HashPasswordIsLowerCaseHexMd5()
    {
        Assert.Equal("cd5b1e4947e304476c788cd474fb579a", StringUtils.HashPassword("bitcoin"));
    }

    [Fact]
    public void TrailingWhitespaceChangesTheHash()
    {
        // the device hashes the password exactly as typed; a trailing space is a different password
        Assert.NotEqual(StringUtils.HashPassword("bitcoin"), StringUtils.HashPassword("bitcoin "));
    }
}
