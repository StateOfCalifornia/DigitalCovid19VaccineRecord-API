using System;

namespace IntegrationTester
{
    using Application.Common;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    namespace IntegerationTester
    {


        public class Program
        {
            public static void Main(string[] argc)
            {
                var apiUrl = "";
                var numberOfLoops = 1;
                var logFile = "";
                var inFirstName = "";
                var inLastName = "";
                var inEmail = "";
                var inDOB = "";
                var inId = "";
                var InPin = "";

                var validArgs = GetArgs(argc, ref logFile, ref apiUrl, ref numberOfLoops, ref inFirstName, ref inLastName, ref inEmail, ref inDOB, ref inId, ref InPin);
                if (!validArgs)
                {
                    return;
                }

                LogAndDisplay(logFile, $"log={logFile} apiUrl={apiUrl} loops={numberOfLoops}");
                HttpClient client = new HttpClient();
                var tasks = new List<Task<HttpResponseMessage>>();
                var timer = new Stopwatch();
                timer.Start();
                if (!string.IsNullOrWhiteSpace(inFirstName))
                {
                    StatusLoops(numberOfLoops, inFirstName, inLastName, inDOB, inEmail, apiUrl, logFile, client, tasks);
                }
                else
                {
                    QrLoops(numberOfLoops, inId, InPin, apiUrl, logFile, client, tasks);
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


            private static readonly Random random = new Random((int)(DateTime.Now.Ticks % Int32.MaxValue));
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


            private static bool GetArgs(string[] argc, ref string logFile, ref string apiUrl, ref int loops, ref string inFirstName, ref string inLastName, ref string inEmail, ref string inDOB, ref string inId, ref string inPin)
            {
                var validArgs = true;
                try
                {
                    logFile = argc[0];
                    apiUrl = argc[1];
                    loops = Int32.Parse(argc[2]);
                    inFirstName = argc.Length > 3 ? argc[3] : "";
                    inLastName = argc.Length > 4 ? argc[4] : "";
                    inEmail = argc.Length > 5 ? argc[5] : "";
                    inDOB = argc.Length > 6 ? argc[6] : inDOB;
                    inId = argc.Length > 7 ? argc[7] : "";
                    inPin = argc.Length > 8 ? argc[8] : "";
                }
                catch (Exception)
                {
                    validArgs = false;
                }
                if (!validArgs)
                {
                    Console.WriteLine("Usage:  IntegrationTester logFile apiUrl #loops [firstName lastName email dob [Id Pin]");
                    Console.WriteLine("example(random data):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net 100");
                    Console.WriteLine("example(fixed data):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net 100 chris smith chris.smith@state.ca.gov 1990-10-23");
                    Console.WriteLine(@"example(fixed data for page2):  IntegrationTester out.log https://as-cdt-pub-vip-vaccinecredapi-ww-p-002-dev.azurewebsites.net 100 """" """" """" """" A34jkfda 1122");
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

        }
    }
}
