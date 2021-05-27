using System;
using System.Threading.Tasks;
using TpLink.Api;
using TpLink.Api.Helpers;
using Xunit;
using Xunit.Sdk;

namespace TpLink.UnitTest
{
    public class StringUtilsTest
    {
        [Theory]
        [InlineData("ethereum", "bitcoin")]
        [InlineData("ethereum", "bitcoin ", Skip = "Miss match")]
        public void TestHashHashMethod(string userName, string password)
        {
            Assert.Equal("Basic%20ethereum%3Acd5b1e4947e304476c788cd474fb579a",
                StringUtils.GetAuthorization(userName, password));
        }
        
        // todo: 
        // - test login
        // - check if not on vpn
        // - test reboot (after reboot waiting 3 sec at least and recheck for connection to router, (maybe ping)
    }
}