using Newtonsoft.Json;
using System.Collections.Generic;


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
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    public class LocalizedString
    {
        [JsonProperty("translatedValues")]
        public List<TranslatedString> TranslatedValues { get; set; }
        [JsonProperty("defaultValue")]
        public TranslatedString DefaultValue { get; set; }
    }
    public class Barcode
    {
        [JsonProperty("alternateText")]
        public string AlternateText { get; set; }
        [JsonProperty("showCodeText")]
        public LocalizedString ShowCodeText { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("renderEncoding")]
        public string RenderEncoding {get;set;}
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class SourceUri
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class Logo
    {
        [JsonProperty("sourceUri")]
        public SourceUri SourceUri { get; set; }
    }

    public class PatientDetails
    {
        [JsonProperty("dateOfBirth")]
        public string DateOfBirth { get; set; }
        [JsonProperty("identityAssuranceLevel")]
        public string IdentityAssuranceLevel { get; set; }
        [JsonProperty("patientId")]
        public string PatientId { get; set; }
        [JsonProperty("patientName")]
        public string PatientName { get; set; }
    }

    public class VaccinationRecord
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("contactInfo")]
        public string ContactInfo { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("doseDateTime")]
        public string DoseDateTime { get; set; }
        [JsonProperty("doseLabel")]
        public string DoseLabel { get; set; }
        [JsonProperty("lotNumber")]
        public string LotNumber { get; set; }
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
        [JsonProperty("provider")]
        public string Provider { get; set; }
    }

    public class VaccinationDetails
    {
        [JsonProperty("vaccinationRecord")]
        public List<VaccinationRecord> VaccinationRecord { get; set; }
    }

    public class CovidCardObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("issuerId")]
        public string IssuerId { get; set; }
        [JsonProperty("barcode")]
        public Barcode Barcode { get; set; }
        [JsonProperty("cardColorHex")]
        public string CardColorHex { get; set; }
        [JsonProperty("expiration")]
        public string Expiration { get; set; }
        [JsonProperty("logo")]
        public Logo Logo { get; set; }
        [JsonProperty("patientDetails")]
        public PatientDetails PatientDetails { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("vaccinationDetails")]
        public VaccinationDetails VaccinationDetails { get; set; }
    }

    public class Payload
    {
        [JsonProperty("covidCardObjects")]
        public List<CovidCardObject> CovidCardObjects { get; set; }
    }

    public class GoogleWallet
    {
        [JsonProperty("iss")]
        public string Iss { get; set; }
        [JsonProperty("aud")]
        public string Aud { get; set; }
        [JsonProperty("iat")]
        public long Iat { get; set; }
        [JsonProperty("typ")]
        public string Typ { get; set; }
        [JsonProperty("origins")]
        public List<object> Origins { get; set; }
        [JsonProperty("payload")]
        public Payload Payload { get; set; }
    }
}

