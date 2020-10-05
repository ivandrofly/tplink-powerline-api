using TpLink.Api.Helpers;
using Xunit;

namespace TpLink.UnitTest
{
    public class StringUtilsTest
    {
        [Theory]
        [InlineData("admin", "2207psp")]
        public void TestHashHashMethod(string userName, string password)
        {
            // username: admin
            // password: 2207psp
            Assert.Equal("Basic%20admin%3A656a351961b7552d9bb35a0201b6d6fd", StringUtils.GetAuthorization(userName, password));
        }
    }
}
