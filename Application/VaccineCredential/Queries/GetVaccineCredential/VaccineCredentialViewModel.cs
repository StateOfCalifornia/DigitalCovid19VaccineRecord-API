using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Application.Common.Models;
using Application.VaccineCredential.Queries;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class VaccineCredentialViewModel
    {
        public string FileContentQr { get; set; } //base64 Encoded
        public string FileNameQr { get; set; }
        public string MimeTypeQr { get; set; }
        public string FileContentSmartCard { get; set; }  //base64 Encoded
        public string FileNameSmartCard { get; set; }
        public string WalletContent { get; set; }
        public string MimeTypeSmartCard { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DOB { get; set; }
        public List<Dose> Doses {get;set;}
    }

    public class Dose
    {
        public string Type { get; set; }
        public string Doa { get; set; }
        public string Provider { get; set; }
        public string LotNumber { get; set; }

    }

    public class VaccineCredentialModel
    {
        public RateLimit RateLimit { get; set; }
        public VaccineCredentialViewModel VaccineCredentialViewModel { get; set; }
        public bool CorruptData { get; set; }
    }
}
