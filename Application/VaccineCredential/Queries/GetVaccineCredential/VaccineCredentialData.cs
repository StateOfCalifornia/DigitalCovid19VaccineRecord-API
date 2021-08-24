using System.Collections.Generic;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class Name
    {
        public string family { get; set; }
        public List<string> given { get; set; }
    }

    public class Coding
    {
        public string system { get; set; }
        public string code { get; set; }
    }

    public class VaccineCode
    {
        public List<Coding> coding { get; set; }
    }

    public class Patient
    {
        public string reference { get; set; }
    }

    public class Actor
    {
        public string display { get; set; }
    }

    public class Performer
    {
        public Actor actor { get; set; }
    }

    public class Resource
    {
        public string resourceType { get; set; }
        public List<Name> name { get; set; }
        public string birthDate { get; set; }
        public string status { get; set; }
        public VaccineCode vaccineCode { get; set; }
        public Patient patient { get; set; }
        public string occurrenceDateTime { get; set; }
        public string lotNumber { get; set; }
        public List<Performer> performer { get; set; }
    }

    public class Entry
    {
        public string fullUrl { get; set; }
        public Resource resource { get; set; }
    }

    public class FhirBundle
    {
        public string resourceType { get; set; }
        public string type { get; set; }
        public List<Entry> entry { get; set; }
    }

    public class CredentialSubject
    {
        public string fhirVersion { get; set; }
        public FhirBundle fhirBundle { get; set; }
    }

    public class Vc
    {
        public List<string> type { get; set; }
        public CredentialSubject credentialSubject { get; set; }
    }

    public class Vci
    {
        public string iss { get; set; }
        public long nbf { get; set; }
        public Vc vc { get; set; }
    }

    public class VerifiableCredentials
    {
        public List<string> verifiableCredential { get; set; }
    }
}
