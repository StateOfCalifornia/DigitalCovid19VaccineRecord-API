using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface IBase64UrlUtility
    {
        string Encode(byte[] arg);
        byte[] Decode(string arg);
    }
}
