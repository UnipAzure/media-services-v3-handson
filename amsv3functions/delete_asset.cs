using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;


namespace amsv3functions
{
    public static class delete_asset
    {
        [FunctionName("delete_asset")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.destinationContainer == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass destinationContainer in the input object" });

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset inputAsset = null;

            IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);

            string assetName = data.destinationContainer.ToString();

            inputAsset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, assetName);
            if (inputAsset == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = $"Asset {assetName} not found" });

            client.Assets.Delete(amsconfig.ResourceGroup, amsconfig.AccountName, assetName);

            return req.CreateResponse(HttpStatusCode.OK, $"Asset {assetName} deleted");
        }

        private static IAzureMediaServicesClient CreateMediaServicesClient(MediaServicesConfigWrapper config)
        {
            ArmClientCredentials credentials = new ArmClientCredentials(config.serviceClientCredentialsConfig);

            return new AzureMediaServicesClient(config.serviceClientCredentialsConfig.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }
    }
}
