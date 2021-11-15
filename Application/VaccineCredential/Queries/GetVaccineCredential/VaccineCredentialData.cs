using Newtonsoft.Json;
using System.Collections.Generic;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class Name
    {
        [JsonProperty("family")]
        public string Family { get; set; }
        [JsonProperty("given")]
        public List<string> Given { get; set; }
    }

    public class Coding
    {
        [JsonProperty("system")]
        public string System { get; set; }
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    public class VaccineCode
    {
        [JsonProperty("coding")]
        public List<Coding> Coding { get; set; }
    }

    public class Patient
    {
        [JsonProperty("reference")]
        public string Reference { get; set; }
    }

    public class Actor
    {
        [JsonProperty("display")]
        public string Display { get; set; }
    }

    public class Performer
    {
        [JsonProperty("actor")]
        public Actor Actor { get; set; }
    }

    public class Resource
    {
        [JsonProperty("resourceType")]
        public string ResourceType { get; set; }
        [JsonProperty("name")]
        public List<Name> Name { get; set; }
        [JsonProperty("birthDate")]
        public string BirthDate { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("vaccineCode")]
        public VaccineCode VaccineCode { get; set; }
        [JsonProperty("patient")]
        public Patient Patient { get; set; }
        [JsonProperty("occurrenceDateTime")]
        public string OccurrenceDateTime { get; set; }
        [JsonProperty("lotNumber")]
        public string LotNumber { get; set; }
        [JsonProperty("performer")]
        public List<Performer> Performer { get; set; }
    }

    public class Entry
    {
        [JsonProperty("fullUrl")]
        public string FullUrl { get; set; }
        [JsonProperty("resource")]
        public Resource Resource { get; set; }
    }

    public class FhirBundle
    {
        [JsonProperty("resourceType")]
        public string ResourceType { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("entry")]
        public List<Entry> Entry { get; set; }
    }

    public class CredentialSubject
    {
        [JsonProperty("fhirVersion")]
        public string FhirVersion { get; set; }
        [JsonProperty("fhirBundle")]
        public FhirBundle FhirBundle { get; set; }
    }

    public class Vc
    {
        [JsonProperty("type")]
        public List<string> Type { get; set; }
        [JsonProperty("credentialSubject")]
        public CredentialSubject CredentialSubject { get; set; }
    }

    public class Vci
    {
        [JsonProperty("iss")]
        public string Iss { get; set; }
        [JsonProperty("nbf")]
        public long Nbf { get; set; }
        [JsonProperty("vc")]
        public Vc Vc { get; set; }
    }

    public class VerifiableCredentials
    {
        [JsonProperty("verifiableCredential")]
        public List<string> VerifiableCredential { get; set; }
    }
}
