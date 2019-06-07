using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.IO;
using System.Linq.Expressions;

using Newtonsoft.Json;
using System.Configuration;
using System.Dynamic;
using System.Threading;
using System.Globalization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;
//using Microsoft.Framework.Configuration;

namespace ConsoleAppGetAllSubscription
{
    class Program
    {
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["TenantDomain"];
        private static string clientId = ConfigurationManager.AppSettings["ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["AccessKey"];

        static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
        //
        // To authenticate to the To Do list service, the client needs to know the service's App ID URI.
        // To contact the To Do list service we need it's URL as well.
        //
        private static string managementEndpoint = ConfigurationManager.AppSettings["ManagementEndPoint"];       

        //private static HttpClient httpClient = new HttpClient();
        private static AuthenticationContext authContext = null;
        private static ClientCredential clientCredential = null;
        private static string accessToken = null;
        static void Main(string[] args)
        {
            //Get the AAD token to get authorized to make the call to the Usage API
            //string token = GetOAuthTokenFromAAD();

            string token = null;
            GetOAuthTokenFromAADSilent().Wait();
            token = accessToken;
           
            string billingUsageAPI = ConfigurationManager.AppSettings["BillingUsageAPI"];
            string enrollmentNumber = ConfigurationManager.AppSettings["EnrollmentNumber"];
            string accessKey = ConfigurationManager.AppSettings["AccessKey"];
            int daysToCheck = -3;
            string endDate = DateTime.Now.ToString("yyyy-MM-dd");
            string startDate = DateTime.Now.AddDays(daysToCheck).ToString("yyyy-MM-dd");

            //https://consumption.azure.com/v2/enrollments/{enrollmentNumber}/usagedetailsbycustomdate?startTime={startDate}&endTime={endDate}";
            //https://consumption.azure.com/v2/enrollments/{0}/usagedetails
            //string requestURL = String.Format(CultureInfo.InvariantCulture, billingUsageAPI, enrollmentNumber,startDate, endDate);                       
            string requestURL = "https://management.azure.com/subscriptions?api-version=2016-06-01";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);

            // Add the OAuth Authorization header, and Content Type header
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
            request.ContentType = "application/json";

           
            // Call the Usage API, dump the output to the console window
            try
            {
                // Call the REST endpoint
                Console.WriteLine("Calling Billing Usage API to get all subscriptions...");
                var oRV = new List<SubscriptionEntity>();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(String.Format("Billing Usage API response status: {0}", response.StatusDescription));
                Stream receiveStream = response.GetResponseStream();

                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                dynamic azureResources = Newtonsoft.Json.JsonConvert.DeserializeObject(readStream.ReadToEnd());

                if (azureResources != null && azureResources.data != null)
                {

                    foreach (var r in azureResources.data)
                    {

                        if (r.subscriptionGuid != null || r.subscriptionGuid != string.Empty)
                        {
                            string subId = r.subscriptionId;
                            string dispName = r.displayName;
                            oRV.Add(new SubscriptionEntity(enrollmentNumber, subId, dispName));
                        }
                    }
                }

                //string nextLink = "";
                var subscriptionGUIDs = new List<string>();
                //do
                //{
                //    string usageDetail = "";
                //    if (nextLink == "")
                //    {
                //        WebRequest usageDetailRequest = WebRequest.Create(requestURL);
                //        usageDetailRequest.Headers.Add("Authorization", "bearer " + accessKey);
                //        HttpWebResponse usageDetailResponse = (HttpWebResponse)usageDetailRequest.GetResponse();
                //        StreamReader usageDetailreader = new StreamReader(usageDetailResponse.GetResponseStream());
                //        usageDetail = usageDetailreader.ReadToEnd();
                //    }
                //    else
                //    {
                //        WebRequest usageDetailRequest = WebRequest.Create(nextLink);
                //        usageDetailRequest.Headers.Add("Authorization", "bearer " + accessKey);
                //        HttpWebResponse usageDetailResponse = (HttpWebResponse)usageDetailRequest.GetResponse();
                //        StreamReader usageDetailreader = new StreamReader(usageDetailResponse.GetResponseStream());
                //        usageDetail = usageDetailreader.ReadToEnd();
                //    }                    
                //    dynamic usage = JsonConvert.DeserializeObject(usageDetail);
                //    if(usage != null)
                //    {
                //        nextLink = usage.nextLink;
                //        foreach (dynamic dataEntry in usage.data)
                //        {
                //            if (!subscriptionGUIDs.Contains(dataEntry.subscriptionGuid.ToString()))
                //            {
                //                subscriptionGUIDs.Add(dataEntry.subscriptionGuid.ToString());
                //                string subId = dataEntry.subscriptionId;
                //                string dispName = dataEntry.displayName;
                //                oRV.Add(new SubscriptionEntity(enrollmentNumber, subId, dispName));
                //            }
                //        }
                //    }                    
                //} while (nextLink != null);
                var jsonGUIDs = JsonConvert.SerializeObject(subscriptionGUIDs);


                if (azureResources != null && azureResources.value != null)
                {

                    foreach (var r in azureResources.value)
                    {

                        if (r.subscriptionId != null || r.subscriptionId != string.Empty)
                        {
                            string subId = r.subscriptionId;
                            string dispName = r.displayName;
                            oRV.Add(new SubscriptionEntity(enrollmentNumber, subId, dispName));
                        }

                    }
                }

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureStorageConnectionString"));
                // Create the table client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                // Get a reference to a table named "subscriptionTable"
                CloudTable subscriptionTable = tableClient.GetTableReference(CloudConfigurationManager.GetSetting("storageTableName"));

                // Create the CloudTable if it does not exist
                subscriptionTable.CreateIfNotExistsAsync().Wait();

                // Create the batch operation.
                TableBatchOperation batchOperation = new TableBatchOperation();
                               
                //batchOperation.
                foreach(var ba in oRV)
                {
                    batchOperation.InsertOrReplace(ba);
                }
               
                // Execute the batch operation.
                subscriptionTable.ExecuteBatchAsync(batchOperation).Wait();


                //Console.WriteLine("Usage stream received.  Press ENTER to continue with raw output.");
                //Console.ReadLine();
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(oRV));
                string filePath = System.Environment.CurrentDirectory + "\\" + Guid.NewGuid() + ".json";
                System.IO.File.WriteAllText(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(oRV));
                Console.WriteLine("Raw output complete.  Press ENTER to continue with JSON output.");
                Console.ReadLine();

                
                //response.Close();
                //readStream.Close();               
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""));
                Console.ReadLine();
            }
        }
        public static string GetOAuthTokenFromAAD()
        {
            var authenticationContext = new AuthenticationContext(String.Format("{0}/{1}",
                                                                    ConfigurationManager.AppSettings["ADALServiceURL"],
                                                                    ConfigurationManager.AppSettings["TenantDomain"]));

            //Ask the logged in user to authenticate, so that this client app can get a token on his behalf
            var result = authenticationContext.AcquireToken(String.Format("{0}/", ConfigurationManager.AppSettings["ARMBillingServiceURL"]),
                                                            ConfigurationManager.AppSettings["ClientID"],
                                                            new Uri(ConfigurationManager.AppSettings["ADALRedirectURL"]),
                                                            PromptBehavior.RefreshSession);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
        private static async Task GetOAuthTokenFromAADSilent()
        {
            AuthenticationResult result = null;
            authContext = new AuthenticationContext(authority);
            clientCredential = new ClientCredential(clientId, appKey);
            int retryCount = 0;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    result = await authContext.AcquireTokenAsync(string.Format("{0}/", managementEndpoint), clientCredential);

                    //result = authContext.AcquireToken(todoListResourceId, clientCredential);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    Console.WriteLine(
                        String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
                        DateTime.Now.ToString(),
                        ex.ToString(),
                        retry.ToString()));
                }

            } while ((retry == true) && (retryCount < 3));

            if (result == null)
            {
                Console.WriteLine("Canceling attempt to contact To Do list service.\n");
                return;
            }           
            accessToken = result.AccessToken;
        }
    }

    public class Subscription
    {
        public string subscriptionId { get; set; }
        public string displayName { get; set; }
    }

    public class SubscriptionEntity : TableEntity
    {
        public SubscriptionEntity(string EnrollmentNumber, string SubscriptionId, string DisplayName)
        {
            this.PartitionKey = EnrollmentNumber;
            this.RowKey = SubscriptionId;
            this.subscriptionId = SubscriptionId;
            this.displayName = DisplayName;

        }       
        public string displayName { get; set; }
        public string subscriptionId { get; set; }

        //public DateTime processedDate { get; set; }
    }
}
