namespace TpLink.Api.Models
{
    public class EndpointAuth
    {
        public EndpointAuth(string login, string passoword, string endpoint)
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
