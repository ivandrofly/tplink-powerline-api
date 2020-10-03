using System;
using System.Collections.Generic;
using System.Text;

namespace TpLink.Api.Models
{
    public class ApiConnection
    {
        public ApiConnection(string login, string passoword, string endpoint)
        {
            Login = login;
            Passoword = passoword;
            Endpoint = endpoint;
        }

        public string Login { get; set; }
        public string Passoword { get; set; }
        public string Endpoint { get; set; }
    }
}
