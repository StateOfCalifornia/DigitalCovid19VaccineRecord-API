
using Application.Common.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace Infrastructure
{
    public class Base64UrlUtility : IBase64UrlUtility
    {

        public string Encode(byte[] arg)
        {
            var s = WebEncoders.Base64UrlEncode(arg);
            return s;
        }

        public byte[] Decode(string arg)
        {
            var s = WebEncoders.Base64UrlDecode(arg);
            return s;
        }
    }
}
