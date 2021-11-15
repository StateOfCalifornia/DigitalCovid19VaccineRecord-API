using Application.Common;
using Application.Common.Interfaces;
using Application.Options;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Infrastructure
{
    public class CredentialCreator : ICredentialCreator
    {

        private readonly KeySettings _keySettings;
        private readonly IJwtSign _jwtSign;

        public CredentialCreator(KeySettings keySettings, IJwtSign jwtSign)
        {
            _keySettings = keySettings;
            _jwtSign = jwtSign;
        }
        public Vci GetCredential(Vc vc)
        {
            var data = new Vci()
            {
                Vc = vc,
                Iss = _keySettings.Issuer,
                Nbf = _jwtSign.ToUnixTimestamp(DateTime.Now)
            };
            return data;
        }        

        public GoogleWallet GetGoogleCredential(Vci cred, string shc)
        {
            var patientDetail = new PatientDetails()
            {
                DateOfBirth = cred.Vc.CredentialSubject.FhirBundle.Entry[0].Resource.BirthDate,
                IdentityAssuranceLevel = "IAL1.4",
                PatientName = $"{ cred.Vc.CredentialSubject.FhirBundle.Entry[0].Resource.Name[0].Given[0]} {cred.Vc.CredentialSubject.FhirBundle.Entry[0].Resource.Name[0].Family}"
            };

            var vaccinationRecords = new List<VaccinationRecord>();


            for (int inx = 1; inx < cred.Vc.CredentialSubject.FhirBundle.Entry.Count; inx++)
            {
                var dose = cred.Vc.CredentialSubject.FhirBundle.Entry[inx];
                var lotNumber = dose.Resource.LotNumber;
                if (string.IsNullOrWhiteSpace(lotNumber)) { lotNumber = null; }

                var vaccinationRecord = new VaccinationRecord()
                {
                    Code = dose.Resource.VaccineCode.Coding[0].Code.ToString(),
                    DoseDateTime = dose.Resource.OccurrenceDateTime,
                    DoseLabel = "Dose",
                    LotNumber = lotNumber,
                    Manufacturer = Utils.VaccineTypeNames.GetValueOrDefault(dose.Resource.VaccineCode.Coding[0].Code.ToString()),
                    Description = Utils.VaccineTypeNames.GetValueOrDefault(dose.Resource.VaccineCode.Coding[0].Code.ToString())
                };

                vaccinationRecords.Add(vaccinationRecord);
            }



            var vaccinationDetail = new VaccinationDetails()
            {
                VaccinationRecord = vaccinationRecords
            };

            var logo = new Logo()
            {
                SourceUri = new SourceUri()
                {
                    Description = "State of California",
                    Uri = _keySettings.GoogleWalletLogo
                }
            };

            var cardObject = new CovidCardObject()
            {
                Id = _keySettings.GoogleIssuerId + $".{Guid.NewGuid()}",
                IssuerId = _keySettings.GoogleIssuerId,
                CardColorHex = "#FFFFFF",
                Logo = logo,
                PatientDetails = patientDetail,
                Title = "COVID-19 Vaccination Card",
                VaccinationDetails = vaccinationDetail,
                Barcode = new Barcode
                {
                    Type = "qrCode",//Enum.GetName(typeof(BarcodeType), BarcodeType.QR_CODE),
                    Value = shc,
                    
                    RenderEncoding = Enum.GetName(typeof(BarcodeRenderEncoding), BarcodeRenderEncoding.UTF_8),
                    AlternateText = ""
                }
            };

            var cardObjects = new List<CovidCardObject>
            {
                cardObject
            };

            var data = new GoogleWallet()
            {
                Iss = _keySettings.GoogleIssuer,
                Iat = _jwtSign.ToUnixTimestamp(DateTime.Now),
                Aud = "google",
                Typ = "savetogooglepay",
                Origins = new List<object>(),
                Payload = new Payload() { CovidCardObjects = cardObjects }
            };
            return data;
        }
    }
}
