using Application.VaccineCredential.Queries.GetVaccineCredential;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface ICredentialCreator
    {
        Vci GetCredential(Vc vc);
        GoogleWallet GetGoogleCredential(Vci data, string shc);
    }
}
