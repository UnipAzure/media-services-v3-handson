//
// Azure Media Services REST API v3 - Functions
//
// create_empty_asset - This function creates an empty asset.
//
//  Input:
//      {
//          "assetNamePrefix":  "Name of the asset",
//          "assetStorageAccount":  "Name of attached storage where to create the asset"  // (optional)  
//      }
//  Output:
//      {
//          "assetName":  "Name of the asset",
//          "assetId":  "Id of the asset created"
//      }
//

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace amsv3functions
{
    public static class create_empty_asset
    {
        [FunctionName("create_empty_asset")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - create_empty_asset was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);


            if (data.assetNamePrefix == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass assetNamePrefix in the input object" });
            string assetStorageAccount = null;
            if (data.assetStorageAccount != null)
                assetStorageAccount = data.assetStorageAccount;
            Guid assetGuid = Guid.NewGuid();
            string assetName = data.assetNamePrefix + "-" + assetGuid.ToString();
            string fileNameTemp = "";

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset asset = null;

            try
            {
                fileNameTemp = ((string)data.assetNamePrefix);

                if (fileNameTemp.Length > 20)
                {
                    fileNameTemp = fileNameTemp.Substring(0, 20);
                }

                fileNameTemp = Regex.Replace(fileNameTemp, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
                fileNameTemp = fileNameTemp.Replace(" ", string.Empty).Replace(".", "").Replace("-", "").Replace("_", "");
                fileNameTemp = $"asset-{fileNameTemp}-{assetGuid.ToString()}";

                if(fileNameTemp.Length > 63)
                {
                    fileNameTemp = fileNameTemp.Substring(0, 63);
                }
                fileNameTemp = fileNameTemp.ToLower();

                assetName = fileNameTemp;

                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                Asset assetParams = new Asset(null, assetName, null, assetGuid, DateTime.Now, DateTime.Now, null, assetName, null, assetStorageAccount, AssetStorageEncryptionFormat.None);
                assetParams.Container = fileNameTemp;
                
                asset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, assetParams);
                //asset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, new Asset());
            }
            catch (ApiErrorException e)
            {
                log.Error("Error when trying to create empty asset", e);
                log.Info($"ERROR: AMS API call failed with error code: {e.Body.Error.Code} and message: {e.Body.Error.Message}");
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "AMS API call error: " + e.Message
                });
            }
            catch(Exception ex)
            {
                log.Error("Error when trying to create empty asset", ex);
            }

            return req.CreateResponse(HttpStatusCode.OK, new
            {
                assetName = assetName,
                assetId = asset.AssetId
            });
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
