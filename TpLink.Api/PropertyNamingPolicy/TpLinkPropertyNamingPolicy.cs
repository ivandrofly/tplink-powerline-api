using System;
using System.Text.RegularExpressions;

namespace TpLink.Api.PropertyNamingPolicy
{
    public class TpLinkPropertyNamingPolicy : System.Text.Json.JsonNamingPolicy
    {
        private readonly Regex _regexCasing = new Regex("[a-z][A-Z]", RegexOptions.Compiled);
        public override string ConvertName(string name)
        {
            return name;

            // ignore for now
            var match = _regexCasing.Match(name);
            if (match.Success)
            {
                name = name.Insert(match.Index + 1, "_");
            }
            name = name.ToLower();
            return name;
        }
    }
}
