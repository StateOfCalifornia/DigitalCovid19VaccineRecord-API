using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public enum BarcodeType
    {
        BARCODE_TYPE_UNSPECIFIED,
        AZTEC,
        CODE_39,
        CODE_128,
        CODABAR,
        DATA_MATRIX,
        EAN_8,
        EAN_13,
        ITF_14,
        PDF_417,
        QR_CODE,
        UPC_A,
        TEXT_ONLY
    }
    public enum BarcodeRenderEncoding
    {
        RENDER_ENCODING_UNSPECIFIED,
        UTF_8
    }
    public class TranslatedString
    {
        public string language { get; set; }
        public string value { get; set; }
    }
    public class LocalizedString
    {
        public List<TranslatedString> translatedValues { get; set; }
        public TranslatedString defaultValue { get; set; }
    }
    public class Barcode
    {
        public string alternateText { get; set; }
        public LocalizedString showCodeText { get; set; }
        public string type { get; set; }

        public string renderEncoding {get;set;}
        public string value { get; set; }
    }

    public class SourceUri
    {
        public string description { get; set; }
        public string uri { get; set; }
    }

    public class Logo
    {
        public SourceUri sourceUri { get; set; }
    }

    public class PatientDetails
    {
        public string dateOfBirth { get; set; }
        public string identityAssuranceLevel { get; set; }
        public string patientId { get; set; }
        public string patientName { get; set; }
    }

    public class VaccinationRecord
    {
        public string code { get; set; }
        public string contactInfo { get; set; }
        public string description { get; set; }
        public string doseDateTime { get; set; }
        public string doseLabel { get; set; }
        public string lotNumber { get; set; }
        public string manufacturer { get; set; }
        public string provider { get; set; }
    }

    public class VaccinationDetails
    {
        public List<VaccinationRecord> vaccinationRecord { get; set; }
    }

    public class CovidCardObject
    {
        public string id { get; set; }
        public string issuerId { get; set; }
        public Barcode barcode { get; set; }
        public string cardColorHex { get; set; }
        public string expiration { get; set; }
        public Logo logo { get; set; }
        public PatientDetails patientDetails { get; set; }
        public string title { get; set; }
        public VaccinationDetails vaccinationDetails { get; set; }
    }

    public class Payload
    {
        public List<CovidCardObject> covidCardObjects { get; set; }
    }

    public class GoogleWallet
    {
        public string iss { get; set; }
        public string aud { get; set; }
        public long iat { get; set; }
        public string typ { get; set; }
        public List<object> origins { get; set; }
        public Payload payload { get; set; }
    }
}

