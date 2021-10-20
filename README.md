# What is Digital COVID-19 Vaccine Record (DCVR)?

In June 2021, the California Department of Technology (CDT) launched the Digital COVID-19 Vaccine Record (DCVR) Portal, implementing the SMART Health Card framework. A Digital Vaccine Record is an electronic vaccination record drawn from the data stored in the California immunization registry. The digital record shows the same information as a resident's paper CDC vaccine card: name, date of birth, vaccination dates, and type of vaccine received. The digital record also includes a QR code that, when scanned by a SMART Health Card reader, will display to the reader your name, date of birth, vaccine dates, and vaccine type. The QR code also confirms the vaccine record as an official record of the state of California. 

# Application Architecture

California's implementation of the application is a three-tier, logically and physically separated architecture: 
1.	A web application using the React JavaScript Library, 
2.	A middle tier written in Microsoft .NET Core with a Node.js API, and 
3.	A backend data tier using Snowflake. 

# Twilio SMS and SendGrid API 
- Once the API determines that information submitted matches an existing record in the immunization registry, a 'found' message is sent via Twilio (SMS) or SendGrid (email). 
- If a match is not found, a failure message is sent via Twilio (SMS) or SendGrid (email). 

# Code Repos
There are a total of three code repositories:
1.	DigitalCovid19VaccineRecord-UI
2.	DigitalCovid19VaccineRecord-API
3.	DigitalCovid19VaccineRecord-QR

Please view the readme file in each repo for further details.


# How to run locally
- Deploy UI Code into a local directory.
- Deploy API Code into a local directory.
- Replace any backend return data set with a JSON data set specified in API readme file.
- Use this example string for recipID = "12345" to generate a sample QR code.
- Use expected JSON structure from database:

```
{ "vc": { "credentialSubject": { "fhirBundle": { "entry": [ { "fullUrl": "resource:0", "resource": { "birthDate": "1951-08-15", "name": [ { "family": "Doe", "given": [ "John" ] } ], "resourceType": "Patient" } }, { "fullUrl": "resource:1", "resource": { "occurrenceDateTime": "2021-02-23", "patient": { "reference": "resource:0" }, "performer": [ { "actor": { "display": "KAISER PERMANENTE NCAL" } } ], "resourceType": "Immunization", "status": "completed", "vaccineCode": { "coding": [ { "code": "208", "system": "http://hl7.org/fhir/sid/cvx" } ] } } }, { "fullUrl": "resource:2", "resource": { "lotNumber": "EN6207", "occurrenceDateTime": "2021-03-16", "patient": { "reference": "resource:0" }, "performer": [ { "actor": { "display": "KAISER PERMANENTE NCAL" } } ], "resourceType": "Immunization", "status": "completed", "vaccineCode": { "coding": [ { "code": "208", "system": "http://hl7.org/fhir/sid/cvx" } ] } } } ], "resourceType": "Bundle", "type": "collection" }, "fhirVersion": "4.0.1" }, "type": [ "https://smarthealth.cards#health-card", "https://smarthealth.cards#immunization", "https://smarthealth.cards#covid19" ] } }
```

# Functionality Overview 
The DCVR is most useful as an official, digital copy of an individual's CDC card. It is an official document issued by the State of California and is sufficient as proof of a vaccination record if a person cannot produce their CDC card.  

Users who scan the QR code will see a long string of text, starting with the prefix "shc:/", which indicates that the information to follow is a SMART Health Card. (iPhone users who try scanning this with their camera get the "no usable data found" message because as of this writing, there is no application on the iPhone associated with the "shc:" prefix.) The long string of text is known as an "FHIR bundle" - FHIR stands for Fast Health Interoperability Resources and is an emerging collection of standards designed to improve healthcare data interoperability. More information is at fhir.org. 

The State of California cryptographically signs this bundle; any reader can validate that signature by using the state's public key, published on the My Vaccine Record page at https://myvaccinerecord.cdph.ca.gov/creds/.well-known/jwks.json . The QR code includes the same information presented on the digital vaccine record webpage: your name, date of birth, and the date(s) of your vaccination(s). When a SMART Health Card-compatible reader scans the QR code, it gives the reader the ability to verify the signature, confirming the authenticity of underlying information. (If the FHIR bundle had been modified, the signature verification would fail, resulting in an unsuccessful scan.)

In this way, a reader can know for certain that an individual with the name presented, born on the day indicated, actually received the vaccination(s) mentioned in the QR code. Those vaccination(s) are registered in the California Immunization Registry. 

It's worth mentioning what the SMART Health Card is not: it's not proof of identity, it is a confirmation of a vaccination record belonging to the individual named, born on the date specified. Any venue or organization that wants to rely on the SMART Health Card as proof of vaccination should match the information contained on an identity document (driver's license, for example). 

A final note on the digital vaccine record system design and ongoing improvements to the system. In order to generate a digital vaccine record, we need to match the contact information you provide with contact information already on file with the California Immunization Registry. In most cases, the vaccination provider submitted the resident's contact information with the report of their vaccination. In those cases, the resident will successfully retrieve their digital vaccine record 100% of the time. However, if your contact information is missing from the immunization registry, we cannot match your information with the vaccination record, and therefore cannot send you a link. We worked hard before launch to ensure that we had as much contact information in the system as possible and have made great strides since we launched. By working closely with the vaccination providers, we've added even more contact information for several million California residents; as a result, we have improved the match rate on digital vaccine record requests. As we get more reports from residents who remain unable to retrieve their digital vaccine records, we are in close contact with each provider so that we can continue to make progress. In the meantime, any resident who wants to can reach out to their provider directly and ask that they send updated contact information to the state's immunization registry or can reach out to CDPH and upload a copy of their CDC card so that the state can update their records. (Submitted information will need to be verified and may take a bit of time before it's reflected in the system.)

---

# Introduction
The API, the middle tier between the UI and Backend to verify users’ vaccine credentials are a match in the database.

#Recommended Software & Tools:
- Visual Studio 2019 or greater
- IIS Server 10 or greater
- Web Browsers (Chrome, Edge, Firefox)

#Installation and Setup
- Clone the GitHub repository via command line

- git clone https://github.com/StateOfCalifornia/DigitalVaccineRecord-API.git

- Set VaccineCredentialAPI as start up project
- Update secrets.json

```
{
  "TwilioSettings:FromPhone": "Your sender Phone",
  "TwilioSettings:AuthToken": "Your Auth ID",
  "TwilioSettings:AccountSID": "Your Account SID",
  "TwilioSettings:SandBox": 0,

  "SnowFlakeSettings:UseRelaxed": 1,
  "SnowFlakeSettings:ConnectionString": "",
  "SnowFlakeSettings:IdQuery": "",
  "SnowFlakeSettings:RelaxedEmailQuery": "",
  "SnowFlakeSettings:RelaxedPhoneQuery": "",
  "SnowFlakeSettings:StatusEmailQuery": "",
  "SnowFlakeSettings:StatusPhoneQuery": "",

  "SendGridSettings:SenderEmail": "no-reply@email.com",
  "SendGridSettings:Sender": "Vaccine Credential",
  "SendGridSettings:Key": "Your send grid key SG.....",
  "SendGridSettings:SandBox": 0,

  "KeySettings:PrivateKey": "Your Eliptical Curve Private Key",
  "KeySettings:Issuer": "Your Issuer",
  "KeySettings:Certificate": "Your Eliptical Curve Certificate",

  "AppSettings:WebUrl": "Your localhost url UI app",
  "AppSettings:QrCodeApi": "Your localhost url Function app",
  "AppSettings:DeveloperSms": "",
  "AppSettings:DeveloperEmail": "",
  "AppSettings:CodeSecret": "Your code secret must be HEX Code 32 characters",
  "AppSettings:LinkExpireHours": 2,
  "AppSettings:CacheMinutes": 1,
  "AppSettings:UseMessageQueue": 0,
  "AppSettings:MaxStatusTries": -1,
  "AppSettings:MaxStatusSeconds": 60,
  "AppSettings:MaxQrTries": -1,
  "AppSettings:MaxQrSeconds": 60,
  "AppSettings:RedisConnectionString": "",
  "AppSettings:SendNotFoundEmail": 1,
  "AppSettings:SendNotFoundSms": 1,
  "AppSettings:AppleWalletUrl": "https://redirect.health.apple.com/SMARTHealthCard/",
  "AppSettings:GoogleWalletUrl": "https://pay.google.com/gp/v/save/",
  "AppSettings:UseBatchMode": "0",

  "MessageQueueSettings:ConnectionString": "DefaultEndpointsProtocol=https;AccountName=fake;AccountKey=fake;EndpointSuffix=core.windows.net",
  "MessageQueueSettings:QueueName": "yourqueue",
  "MessageQueueSettings:MaxDequeuePerMinute": 800,
  "MessageQueueSettings:NumberThreads": 500,
  "MessageQueueSettings:SleepSeconds": 3,
  "MessageQueueSettings:AlternateQueueName": "",
  "MessageQueueSettings:MessageExpireHours": 72,
  "MessageQueueSettings:InvisibleSeconds": "600",

  "KeySettings:GooglePrivateKey": "",
  "KeySettings:GoogleCertificate": "",
  "KeySettings:GoogleIssuer": "",
  "KeySettings:GoogleIssuerId": "",
  "KeySettings:GoogleWalletLogo": ""
}
```

- Start the IIS Server from Visual Studio Code 2019 or greater.

- localhost will start, and a Swagger interface will begin.

#API references
GET /HealthCheck

Call to check the status of API

Parameters:
```
{
  "pin": "645321"
}
```
Expected Output:
```
200 OK
```

POST /vaccineCredential
Parameters:

Example:
```
{
  "id": "DFA28931HJA9001229jsd0qw",
  "pin": "1122",
  "walletCode": "G"
}
```
Expected Output:
```
{
  "fileContentQr": "string",
  "fileNameQr": "string",
  "mimeTypeQr": "string",
  "fileContentSmartCard": "string",
  "fileNameSmartCard": "string",
  "walletContent": "string",
  "mimeTypeSmartCard": "string",
  "firstName": "John",
  "lastName": "Doe",
  "dob": "2000-01-22",
  "doses": [
    {
      "type": "Pfizer",
      "doa": "string",
      "provider": "string",
      "lotNumber": "string"
    }
  ]
}
```

POST /vaccineCredentialStatus
Parameters:

note:
	phoneNumber must be empty if emailAddress exists.
	emailAddress must be empty if phoneNumber exists. (example below)
	pin: must be 4 digits with no sequence.

```
{
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "2000-08-12",
  "phoneNumber": "000-000-0000",
  "emailAddress": "",
  "pin": "1122",
  "language": "en"
}
```
Expect Output:
```
200 OK
```

# How to run from localhost
1) Clone the API repository into Visual Studio
2) Open the API project solution.

3) Set ```VaccineCredential.Api``` as "Start up project".

4) Update the ```secrets.json``` with information from API ReadMe above.

5) Edit ``` SnowFlakeService.cs ``` under Infrastructure > SnowFlake
a) edit function as:
```
 public async Task<string> GetVaccineCredentialStatusAsync(GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken)
 {
            return "12345";
 }

```
b) Edit function ```public async Task<Vc> GetVaccineCredentialSubjectAsync(string id, CancellationToken cancellationToken)```

Add these lines under the function

```
Vc vaccineCredential = null;
var jsonString1 = @"{ ""vc"": { ""credentialSubject"": { ""fhirBundle"": { ""entry"": [ { ""fullUrl"": ""resource:0"", ""resource"": { ""birthDate"": ""1951-08-15"", ""name"": [ { ""family"": ""Doe"", ""given"": [""John""] } ], ""resourceType"": ""Patient"" } }, { ""fullUrl"": ""resource:1"", ""resource"": { ""occurrenceDateTime"": ""2021-02-23"", ""patient"": { ""reference"": ""resource:0"" }, ""performer"": [ { ""actor"": { ""display"": ""KAISER PERMANENTE NCAL"" } } ], ""resourceType"": ""Immunization"", ""status"": ""completed"", ""vaccineCode"": { ""coding"": [ { ""code"": ""208"", ""system"": ""http://hl7.org/fhir/sid/cvx"" } ] } } }, { ""fullUrl"": ""resource:2"", ""resource"": { ""lotNumber"": ""EN6207"", ""occurrenceDateTime"": ""2021-03-16"", ""patient"": { ""reference"": ""resource:0"" }, ""performer"": [ { ""actor"": { ""display"": ""KAISER PERMANENTE NCAL"" } } ], ""resourceType"": ""Immunization"", ""status"": ""completed"", ""vaccineCode"": { ""coding"": [ { ""code"": ""208"", ""system"": ""http://hl7.org/fhir/sid/cvx"" } ] } } } ], ""resourceType"": ""Bundle"", ""type"": ""collection"" }, ""fhirVersion"": ""4.0.1"" }, ""type"": [ ""https://smarthealth.cards#health-card"", ""https://smarthealth.cards#immunization"", ""https://smarthealth.cards#covid19"" ] } }";
            
var vaccineCredentialobject1 = JsonConvert.DeserializeObject<VcObject>(jsonString1);
            
vaccineCredential = vaccineCredentialobject1.vc;
            
return vaccineCredential;
```

6) Start IIS Express

7) The `http:localhost` server will start

8) Success!



# API Configuration Parameters

| Name | Description |ExpectedValue|
|--|--|--|
|AppSettings__CDCUrl|Url to CDC for covid19|https://www.cdc.gov/coronavirus/2019-ncov/prevent-getting-sick/prevention.html|
|AppSettings__CodeSecret|Used to encrypt the URL to retrieve QR Code. It is AES Secret(32 hex characters long) |D93CF.....290|
|AppSettings__CovidWebUrl|Url for covid19 pate|https://covid19.yourstate.gov|
|AppSettings__DeveloperEmail|If set, all email messages will be sent to this email instead of using the email address entered by users. This flag should only be used in local development environment. It should be blank when the system deployed in Azure||
|AppSettings__DeveloperSms|If set, all SMS messages will be sent to this phone number instead of using the phone number entered by users. This flag should only be used in local development environment. It should be blank when the system deployed in Azure||
|AppSettings__EmailLogoUrl|url for email logo|https://emaillogourl.gov/imgs/emaillogolg.png|
|AppSettings__GoogleWalletUrl|The Google URL that is used to add a user's vaccine credential info to Google Pay application|https://pay.google.com/gp/v/save/|
|AppSettings__AppleWalletUrl|The Apple Wallet's URL that is used to add a user's vaccine credential info to Apple Wallet application|https://redirect.health.apple.com/SMARTHealthCard/|
|AppSettings__LinkExpireHours|Number of hours the QR link is valid|24|
|AppSettings__QrCodeApi|Url of function app for QR code generator|https://[Your Function App url]/api/QRCreate|
|AppSettings:RedisConnectionString |Redis Cache Connection String|
|AppSettings__MaxQrSeconds|Used for rate limiting the Vaccine Credential endpoint. The value is used in conjunction with MaxQRTries|60|
|AppSettings__MaxQrTries|Used for rate limiting. It contains a number of times a client can call the Vaccine Credential endpoint within a given MaxQrSeconds. Note use -1 for no ratelimiting.|3|
|AppSettings__MaxStatusSeconds|Used for rate limiting the Vaccine Credential Status endpoint. The value is used in conjunction with MaxStatusTries|60|
|AppSettings__MaxStatusTries|Used for rate limiting. It contains a number of times a client can call the Vaccine Credential Status endpoint within a given MaxStatusSeconds.  Note use -1 for no ratelimiting.|1|
|AppSettings__SendNotFoundEmail|Indicates whether to send Not Found email|"0": not send, "1": send|
|AppSettings__SendNotFoundSms|Indicates whether to send Not Found SMS text|"0": not send, "1": send|
|AppSettings__UseMessageQueue|Indicates if requests are put in the Credential Request queue for processing later or in real time|"0": no, "1": yes|
|AppSettings__VaccineFAQUrl|Url for FAQ Page|https://website.gov/faq|
|AppSettings__VirtualAssistantUrl|url for virtual assistant|https://virtaulassistant.gov/?id=17|
|AppSettings__WebUrl|Url of the Vaccine Credential front end application|http://mainsite.gov|
|MessageQueueSettings__ConnectionString|URL to the Credential Request queue|DefaultEndpointsProtocol=https;AccountName=[ACCOUNT NAME];AccountKey=[ACCOUNT KEY];EndpointSuffix=core.windows.net|
|MessageQueueSettings__MaxDequeuePerMinute|Number of items in the Credential Request queue that should be processed per minute|1000|
|MessageQueueSettings__MessageExpireHours|Number of hours that an un-processable item should be removed from the Credential Request queue|72|
|MessageQueueSettings__InvisibleSeconds|Number of seconds that a queue item can be tried to process again|300|
|MessageQueueSettings__NumberThreads|Number of tasks the Credential Request web job should run|50|
|MessageQueueSettings__QueueName|The name of the Credential Request queue in Azure|credentialrequests|
|MessageQueueSettings__SleepSeconds|Number of second to sleep in between retrieval messages from the Credential Request queue|3|
|KeySettings__Certificate|The certificate (Elliptical Curve P-256). It is the public key pair of the private key used to sign the QR Code. Used in the creds|https://[your web url]/creds/.well-known/jwks.json|
|KeySettings__Issuer|The URL of the public key|https://[your web url]/creds|
|Keysettings__PrivateKey|The private key (Elliptical Curve P-256) used to sign the QR Code JWT|-----BEGIN EC PRIVATE KEY-----\nM......Ipa0Q==\n-----END EC PRIVATE KEY-----|
|KeySettings__GoogleCertificate|It is the public key pair of the private key used to sign the Google Health Card||
|KeySettings__GoogleIssuer|The Google service account's issuer that is created under CDT Organization. It is linked to the GoogleIssuerId||
|KeySettings__GoogleIssuerId|The Google Merchant Center account that is used to add the generate Vaccine Credential to the Google Pay application|3388000000013279970|
|KeySettings__GooglePrivateKey|The Google service account's private key (RS256) used in signing the Vaccine Credential for Google Health Card|MIIEo..................Uh+jDJj7GxFMqUk=|
|KeySettings__GoogleWalletLogo|The link to the California Logo that is used in the Google Health Card|https://[Your web url]/imgs/ca-logo-660w.png|
|SendGridSettings__Key|The SendGrid API Key to send emails|SG.Az12........RHcJaS4|
|SendGridSettings__Sender|The email sender friendly name|[State] Department of Public Health|
|SendGridSettings__SenderEmail|The sender email|no-reply@[Department].[State].gov|
|SnowFlakeSettings__ConnectionString|The Snowflake connection string|ACCOUNT=[account url];WAREHOUSE=[Warehouse];ROLE=[role];USER=[YOUR_USER];PASSWORD=[YOUR_PASSWORD];DB=[Database];SCHEMA=[Schema]|
|SnowFlakeSettings__IdQuery|The query string to retrieve Vaccine Credential by a RECIP_ID|select * from table_x where id= ?|
|SnowFlakeSettings__StatusEmailQuery|The query string to call Snowflake function to search by email|select check_credentials_by_email_2(?, ?, ?, ?)|
|SnowFlakeSettings__StatusPhoneQuery|The query string to call Snowflake function to search by phone|select check_credentials_by_phone_2(?, ?, ?, ?)|
|SnowFlakeSettings__RelaxedEmailQuery|The query string to call the Snowflake function to search by email with non-strict matching |select check_credentials_by_email_relaxed_3(?, ?, ?, ?)|
|SnowFlakeSettings__RelaxedPhoneQuery|The query string to call Snowflake function to search by phone with non-strict matching|select check_credentials_by_phone_relaxed_3(?, ?, ?, ?)|
|SnowFlakeSettings__UseRelaxed|Flag to turn on/off relaxed queries|"0": not use, "1": use|
|TwilioSettings__AccountSID|Twilio Account id|AC.......55f|
|TwilioSettings__AuthToken|Twilio Authorized Token|d3a.....cba|
|TwilioSettings__FromPhone|Twilio's sender phone number|+183......29|
|WEBJOBS_RESTART_TIME|The time in seconds to restart the Vaccine Credential web job if it is not running|3|


##

