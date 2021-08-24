using Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface IJwtSign
    {
        string Signature(byte[] payload);
        long ToUnixTimestamp(DateTime date);

        Thumbprint  GetThumbprint(string certificate);
        string GetKid(Thumbprint thumbprint);
        string SignWithRsaKey(byte[] payload);
    }
}
