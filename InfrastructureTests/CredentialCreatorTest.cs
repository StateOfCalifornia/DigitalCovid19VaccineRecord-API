namespace InfrastructureTests
{
    public class CredentialCreatorTest
    {

        //[Fact]
        //public void GivenNameArrayLength1IfNoMiddleInitial()
        //{

        //    var request = new GetVaccineCredentialQuery
        //    {
        //        FirstName = "First Name",
        //        LastName = "ABC",
        //        FirstDoseLotNumber = "1",
        //        DateOfBirth = DateTime.Now,
        //        FirstDoseVaccineType = "Pfizer"

        //    };

        //    var creator = new CredentialCreator(new Application.Options.KeySettings()
        //    {
        //        Issuer = "http://fake"
        //    });

        //    var cred = creator.GetCredential(request);

        //    Assert.Single(cred.vc.credentialSubject.fhirBundle.entry[0].resource.name[0].given);
        //}

        //[Fact]
        //public void GivenNameArrayLength2IfMiddleInitial()
        //{
        //    var request = new GetVaccineCredentialQuery
        //    {
        //        FirstName = "First Name",
        //        LastName = "ABC",
        //        MiddleInitial = "B",
        //        FirstDoseLotNumber = "1",
        //        DateOfBirth = DateTime.Now,
        //        FirstDoseVaccineType = "Pfizer"
        //    };

        //    var creator = new CredentialCreator(new Application.Options.KeySettings()
        //    {
        //        Issuer = "http://fake"
        //    });

        //    var cred = creator.GetCredential(request);

        //    Assert.Equal(2,cred.vc.credentialSubject.fhirBundle.entry[0].resource.name[0].given.Count);

        //}
    }
}
