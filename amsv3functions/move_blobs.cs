using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace amsv3functions
{
    public static class move_blobs
    {
        private static readonly string _storageAccountName = Environment.GetEnvironmentVariable("MediaServicesStorageAccountName");
        private static readonly string _storageAccountKey = Environment.GetEnvironmentVariable("MediaServicesStorageAccountKey");

        [FunctionName("move_blobs")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - move_blob was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            if (data.inputContainer == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass the input container name in the input object" });

            if (data.outputContainer == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass the output container name in the input object" });

            try
            {
                CloudBlobContainer destination = BlobStorageHelper.GetCloudBlobContainer(_storageAccountName, _storageAccountKey, data.inputContainer.ToString());
                CloudBlobContainer source = BlobStorageHelper.GetCloudBlobContainer(_storageAccountName, _storageAccountKey, data.outputContainer.ToString());

                IEnumerable<IListBlobItem> blobs = source.ListBlobs("", useFlatBlobListing: true, blobListingDetails: BlobListingDetails.Copy);

                var fileNames = new List<string>();
                foreach(CloudBlockBlob blob in blobs)
                {
                    fileNames.Add(blob.Name);
                }
                BlobStorageHelper.CopyBlobsAsync(source, destination, fileNames, log);

                source.Delete();
            }
            catch (System.Exception ex)
            {
                log.Error("Error when trying to move blobs", ex);
                throw;
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
