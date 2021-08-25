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
                vc = vc,
                iss = _keySettings.Issuer,
                nbf = _jwtSign.ToUnixTimestamp(DateTime.Now)
            };
            return data;
        }        

        public GoogleWallet GetGoogleCredential(Vci cred, string shc)
        {
            var patientDetail = new PatientDetails()
            {
                dateOfBirth = cred.vc.credentialSubject.fhirBundle.entry[0].resource.birthDate,
                identityAssuranceLevel = "IAL1.4",
                patientName = $"{ cred.vc.credentialSubject.fhirBundle.entry[0].resource.name[0].given[0]} {cred.vc.credentialSubject.fhirBundle.entry[0].resource.name[0].family}"
            };

            var vaccinationRecords = new List<VaccinationRecord>();


            for (int inx = 1; inx < cred.vc.credentialSubject.fhirBundle.entry.Count; inx++)
            {
                var dose = cred.vc.credentialSubject.fhirBundle.entry[inx];
                var lotNumber = dose.resource.lotNumber;
                if (string.IsNullOrWhiteSpace(lotNumber)) { lotNumber = null; }

                string provider = null;
                if (dose.resource.performer != null && dose.resource.performer.Count > 0)
                {
                    provider = dose.resource.performer[0].actor.display;
                }
                if (string.IsNullOrWhiteSpace(provider)) { provider = null; }

                var vaccinationRecord = new VaccinationRecord()
                {
                    code = dose.resource.vaccineCode.coding[0].code.ToString(),
                    doseDateTime = dose.resource.occurrenceDateTime,
                    doseLabel = "Dose " + inx,
                    lotNumber = lotNumber,
                    manufacturer = Utils.VaccineTypeNames.GetValueOrDefault(dose.resource.vaccineCode.coding[0].code.ToString()),
                    provider = provider,
                    description = Utils.VaccineTypeNames.GetValueOrDefault(dose.resource.vaccineCode.coding[0].code.ToString())
                };

                vaccinationRecords.Add(vaccinationRecord);
            }



            var vaccinationDetail = new VaccinationDetails()
            {
                vaccinationRecord = vaccinationRecords
            };

            var logo = new Logo()
            {
                sourceUri = new SourceUri()
                {
                    description = "State of California",
                    uri = _keySettings.GoogleWalletLogo
                }
            };

            var cardObject = new CovidCardObject()
            {
                id = _keySettings.GoogleIssuerId + $".{Guid.NewGuid()}",
                issuerId = _keySettings.GoogleIssuerId,
                cardColorHex = "#FFFFFF",
                logo = logo,
                patientDetails = patientDetail,
                title = "COVID-19 Vaccination Card",
                vaccinationDetails = vaccinationDetail,
                barcode = new Barcode
                {
                    type = "qrCode",//Enum.GetName(typeof(BarcodeType), BarcodeType.QR_CODE),
                    value = shc,
                    
                    renderEncoding = Enum.GetName(typeof(BarcodeRenderEncoding), BarcodeRenderEncoding.UTF_8),
                    alternateText = ""
                }
            };

            var cardObjects = new List<CovidCardObject>
            {
                cardObject
            };

            var data = new GoogleWallet()
            {
                iss = _keySettings.GoogleIssuer,
                iat = _jwtSign.ToUnixTimestamp(DateTime.Now),
                aud = "google",
                typ = "savetogooglepay",
                origins = new List<object>(),
                payload = new Payload() { covidCardObjects = cardObjects }
            };
            return data;
        }
    }
}
