using System;

namespace IntegrationTester
{
    using Application.Common;
    using Application.VaccineCredential.Queries.GetVaccineCredential;
    using Infrastructure;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading.Tasks;

    namespace IntegerationTester
    {


        public class Program
        {
            static string logFile = "";
            public static async Task Main(string[] argc)
            {
                var apiUrl = "";
                var numberOfLoops = 1;
                var inLogFile = "";
                var inFirstName = "";
                var inLastName = "";
                var inEmail = "";
                var inDOB = "";
                var inId = "";
                var inPin = "";
                var inLTCFDir = "";
                var inUseInflater = true;

                var validArgs = GetArgs(argc, ref inLogFile, ref apiUrl, ref numberOfLoops, ref inFirstName, ref inLastName, ref inEmail, ref inDOB, ref inId, ref inPin, ref inLTCFDir, ref inUseInflater);
                if (!validArgs)
                {
                    return;
                }
                logFile = inLogFile;
                LogAndDisplay(logFile, $"log={logFile} apiUrl={apiUrl} loops={numberOfLoops}");
                HttpClient client = new HttpClient();
                var tasks = new List<Task<HttpResponseMessage>>();
                var timer = new Stopwatch();
                if (!string.IsNullOrWhiteSpace(inLTCFDir))
                {
                    Console.Write("Enter SecretCode: ");
                    var secretCode = ReadPassword();
                    timer.Start();
                    var files = (new DirectoryInfo(inLTCFDir)).GetFiles();
                    foreach (var fileInfo in files)
                    {
                        var ltcfRecipFileName = fileInfo.FullName;
                        var destDir = $"{inLTCFDir}\\{Path.GetFileNameWithoutExtension(fileInfo.Name)}";
                        Directory.CreateDirectory(destDir);
                        await ProcessQrFile(ltcfRecipFileName, inPin, secretCode, apiUrl, destDir, inUseInflater);
                    }
                    LogAndDisplay(logFile, $"Total time is {timer.Elapsed} ");
                    return;
                }
                else if (!string.IsNullOrWhiteSpace(inFirstName))
                {
                    timer.Start();
                    StatusLoops(numberOfLoops, inFirstName, inLastName, inDOB, inEmail, apiUrl, logFile, client, tasks);
                }
                else
                {
                    timer.Start();
                    QrLoops(numberOfLoops, inId, inPin, apiUrl, logFile, client, tasks);
                }
                try
                {
                    var tasksTemp = new List<Task>();
                    tasks.ForEach(t => tasksTemp.Add(t));
                    while (tasksTemp.Count > 0)
                    {
                        int completedIndex = Task.WaitAny(tasksTemp.ToArray());
                        LogAndDisplay(logFile, $"completed {completedIndex}");
                        tasksTemp.RemoveAt(completedIndex);
                    }
                }
                catch (Exception e)
                {
                    LogAndDisplay(logFile, $"Warning: {e.Message}");
                }
                timer.Stop();
                //check each for success....
                int successCount = 0;
                for (var i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];
                    try
                    {
                        if (!t.Result.IsSuccessStatusCode)
                        {
                            LogAndDisplay(logFile, $"Warning, task {i} status:{t.Result.StatusCode} failed.");
                        }
                        else
                        {
                            successCount++;
                        }
                    }
                    catch (Exception e)
                    {
                        LogAndDisplay(logFile, $"Error {e}");
                    }

                }

                LogAndDisplay(logFile, $"Total time is {timer.Elapsed}  successCount:{successCount}");
            }


            private static readonly Random random = new((int)(DateTime.Now.Ticks % Int32.MaxValue));
            public static string RandomString(int length)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var retstring = new string(Enumerable.Repeat(chars, length)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
                if (retstring.Length > 20)
                {
                    retstring = retstring.Substring(0, 20);
                }
                return retstring;
            }
            public static int RandomInteger(string chars)
            {
                var numString = new string(Enumerable.Repeat(chars, 2)
                  .Select(s => s[random.Next(s.Length)]).ToArray());

                return Int32.Parse(numString);
            }

            private static async Task ProcessQrFile(string inLTCFRecipidsFileName, string inPin, SecureString secretCode, string apiUrl, string inDestDir, bool useInflater)
            {
                Directory.CreateDirectory($"{inDestDir}\\qrcodes");
                Directory.CreateDirectory($"{inDestDir}\\models");
                var header = $"RECIP_ID\tFIRST NAME\tLAST NAME\tDOB\tDOSE 1 TYPE\tDOSE 1 LOT\tDOSE 1 PROVIDER\tDOSE 1 DATE\tDOSE 2 TYPE\tDOSE 2 LOT\tDOSE 2 PROVIDER\tDOSE 2 DATE\tDOSE 3 TYPE\tDOSE 3 LOT\tDOSE 3 PROVIDER\tDOSE 3 DATE\n";
                System.IO.File.WriteAllText($"{inDestDir}\\DoseInfo.txt", header);
 
                var encService = new AesEncryptionService(new Base64UrlUtility());
                var recipids = await File.ReadAllLinesAsync(inLTCFRecipidsFileName);
                var cnt = 1;
                foreach(var recipid in recipids)
                {
                    if(recipid.Trim().Length == 0)
                    {
                        continue;
                    }
                                       
                    var precode = DateTime.Now.Ticks + "~" + inPin + "~" + recipid.Trim();
                    var code = encService.EncryptGcm(precode, ToNormalString(secretCode));
                    await QrGenerate(code, recipid, inPin, apiUrl, inDestDir, true, useInflater);
                    LogAndDisplay(logFile,$"Processed {cnt++}/{recipids.Length}");                   
                }

            }

            private static bool GetArgs(string[] argc, ref string logFile, ref string apiUrl, ref int loops, ref string inFirstName, ref string inLastName, ref string inEmail, ref string inDOB, ref string inId, ref string inPin, ref string inLTCFDir, ref bool inUseInflater)
            {
                var validArgs = true;
                try
                {
                    logFile = argc[0];
                    apiUrl = argc[1];
                    loops = argc[2].Length > 0 ? Int32.Parse(argc[2]) : 0;
                    inFirstName = argc.Length > 3 ? argc[3] : "";
                    inLastName = argc.Length > 4 ? argc[4] : "";
                    inEmail = argc.Length > 5 ? argc[5] : "";
                    inDOB = argc.Length > 6 ? argc[6] : inDOB;
                    inId = argc.Length > 7 ? argc[7] : "";
                    inPin = argc.Length > 8 ? argc[8] : "";
                    inLTCFDir = argc.Length > 9 ? argc[9] : "";
                    inUseInflater = argc.Length > 10 && argc[10] == "1";
                }
                catch (Exception)
                {
                    validArgs = false;
                }
                if (!validArgs)
                {
                    Console.WriteLine("Usage:  IntegrationTester logFile apiUrl #loops [firstName lastName email dob Id Pin LCTSDestinationDir LCTSRecipFileName UseInflaterHttpClient");
                    Console.WriteLine("example(random data):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net 100");
                    Console.WriteLine("example(fixed data):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net 100 chris smith chris.smith@state.ca.gov 1990-10-23");
                    Console.WriteLine(@"example(fixed data for page2):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net 100 """" """" """" """" A34jkfda 1122");
                    Console.WriteLine(@"example(LTCF QR (note you will be prompted for secretcode):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net """" """" """" """" """" """" 1122 c:\\temp\\LTCF\\  0");
                    Console.WriteLine("         Warning: make sure your configs are setup so that emails are not sent if you don't want them.( ie SendGridSettings:SandBox, true");
                }
                return validArgs;
            }

            private static void LogAndDisplay(string logFile, string message)
            {
                Console.WriteLine(message);
                File.WriteAllText(logFile, message);

            }

            private static void StatusLoops(int numberOfLoops, string inFirstName, string inLastName, string inDOB, string inEmail, string apiUrl, string logFile, HttpClient client, List<Task<HttpResponseMessage>> tasks)
            {
                for (var i = 0; i < numberOfLoops; i++)
                {
                    //generate a valid pin
                    var validPin = $"{RandomInteger("123456789")}{RandomInteger("123456789")}";
                    while (Utils.ValidatePin(validPin) != 0)
                    {
                        validPin = $"{RandomInteger("123456789")}{RandomInteger("123456789")}";
                    }

                    var firstName = string.IsNullOrWhiteSpace(inFirstName) ? RandomString(RandomInteger("123456789")) : inFirstName;
                    var lastName = string.IsNullOrWhiteSpace(inLastName) ? RandomString(RandomInteger("123456789")) : inLastName;
                    var dateOfBirth = string.IsNullOrWhiteSpace(inDOB) ? $"19{RandomInteger("123456789")}-01-01" : inDOB;
                    var email = string.IsNullOrWhiteSpace(inEmail) ? $"fake.{RandomString(5)}@a{RandomString(5)}.com" : inEmail;
                    var pin = validPin;
                    var lang = "en";

                    var data = new Data
                    {
                        DateOfBirth = dateOfBirth,
                        EmailAddress = email,
                        FirstName = firstName,
                        LastName = lastName,
                        PhoneNumber = "",
                        Pin = pin,
                        Language = lang
                    };
                    var url = $"{apiUrl}/vaccineCredentialStatus";
                    if (!string.IsNullOrWhiteSpace(inEmail))
                    {
                        data.EmailAddress = inEmail;
                    }
                    LogAndDisplay(logFile, $"{i} url={url}  {JsonConvert.SerializeObject(data) }");
                    StringContent content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
                    var responseTask = client.PostAsync(url, content);
                    tasks.Add(responseTask);

                    if (i % 10 == 9)
                    {
                        LogAndDisplay(logFile, $"Started up {i + 1} connections");
                    }
                };



            }
            public static byte[] Decompress(byte[] data)
            {
                using var compressedStream = new MemoryStream(data);
                using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var resultStream = new MemoryStream();
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }

            private async static Task QrGenerate(string code, string recipId, string inPin, string apiUrl, string destDir, bool useGivenNameForQRFileName, bool useInflater)
            {
                //generate a valid pin
                var data = new Qr
                {
                    Id = code,
                    Pin = inPin,
                };
                var url = $"{apiUrl}/vaccineCredential";
                var dataJson = JsonConvert.SerializeObject(data);
                StringContent content = new StringContent(dataJson, System.Text.Encoding.UTF8, "application/json");
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.28.4");// "web api client");
                client.DefaultRequestHeaders.Add("Host", apiUrl.Replace("https://", ""));
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                if (useInflater)
                {
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
                }
                var httpResponseMessge = await client.PostAsync(url, content);
                var jsonBytes = await httpResponseMessge.Content.ReadAsByteArrayAsync();
                string jsonString;
                if (useInflater)
                {
                    var jsonBytesDecompressed = Decompress(jsonBytes);
                    jsonString = System.Text.Encoding.UTF8.GetString(jsonBytesDecompressed);
                }
                else
                {
                    jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                }
                LogAndDisplay(logFile, $"jsonString={jsonString}");
                var model = JsonConvert.DeserializeObject<VaccineCredentialViewModel>(jsonString);
                var qrBase64 = model.FileContentQr;
                var qrPng = Convert.FromBase64String(qrBase64);
                if (useGivenNameForQRFileName)
                {
                    var filename = $"{model.LastName}_{model.FirstName}_{recipId}".Replace(" ", "_");
                    System.IO.File.WriteAllBytes($"{destDir}\\qrcodes\\{filename}.png", qrPng);
                }
                else
                {
                    System.IO.File.WriteAllBytes($"{destDir}\\qrcodes\\{recipId}.png", qrPng);
                }
                System.IO.File.WriteAllText($"{destDir}\\models\\{recipId}.json", Newtonsoft.Json.JsonConvert.SerializeObject(model));
                var dataLine = $"{recipId}\t{model.FirstName}\t{model.LastName}\t{model.DOB}";
                foreach (var dose in model.Doses)
                {
                    dataLine += $"\t{dose.Type}\t{dose.LotNumber}\t{dose.Provider}\t{dose.Doa}";
                }
                dataLine += "\n";
                File.AppendAllText($"{destDir}\\DoseInfo.txt", dataLine);
            }

           
            private static void QrLoops(int numberOfLoops, string inId, string inPin, string apiUrl, string logFile, HttpClient client, List<Task<HttpResponseMessage>> tasks)
            {
                for (var i = 0; i < numberOfLoops; i++)
                {
                    //generate a valid pin

                    var data = new Qr
                    {
                        Id = inId,
                        Pin = inPin,
                    };
                    var url = $"{apiUrl}/vaccineCredential";
                    LogAndDisplay(logFile, $"{i} url={url}  {JsonConvert.SerializeObject(data) }");
                    StringContent content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
                    var responseTask = client.PostAsync(url, content);
                    tasks.Add(responseTask);

                    if (i % 10 == 9)
                    {
                        LogAndDisplay(logFile, $"Started up {i + 1} connections");
                    }
                };
            }
            public class Qr
            {
                public string Id { get; set; }
                public string Pin { get; set; }
            }


            public class Data
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string EmailAddress { get; set; }
                public string PhoneNumber { get; set; }
                public string Pin { get; set; }
                public string DateOfBirth { get; set; }
                public string Language { get; set; }
            }

            // StackOverflow question https://stackoverflow.com/questions/3404421/password-masking-console-application
            // Based on answer from https://stackoverflow.com/a/52592030
            //  by user https://stackoverflow.com/users/1412807/sven-vranckx
            //changed string to securestrings
            public static SecureString ReadPassword()
            {
                SecureString password = new SecureString();
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            return null;
                        case ConsoleKey.Enter:
                            return password;
                        case ConsoleKey.Backspace:
                            if (password.Length > 0)
                            {
                                password.RemoveAt(password.Length - 1);
                                Console.Write("\b \b");
                            }
                            break;
                        default:
                            password.AppendChar(key.KeyChar);
                            Console.Write("*");
                            break;
                    }
                }
            }

            // StackOverflow question https://stackoverflow.com/questions/818704/how-to-convert-securestring-to-system-string
            // Based on answer from https://stackoverflow.com/a/56799584
            //  by user https://stackoverflow.com/users/11644199/eric-alexander-silveira
            public static string ToNormalString(SecureString input)
            {
                IntPtr strptr = Marshal.SecureStringToBSTR(input);
                string normal = Marshal.PtrToStringBSTR(strptr);
                Marshal.ZeroFreeBSTR(strptr);
                return normal;
            }
        }
    }
}
