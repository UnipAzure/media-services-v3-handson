using System;
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
    public static class delete_container
    {
        private static readonly string _storageAccountName = Environment.GetEnvironmentVariable("MediaServicesStorageAccountName");
        private static readonly string _storageAccountKey = Environment.GetEnvironmentVariable("MediaServicesStorageAccountKey");

        [FunctionName("delete_container")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            if (data.containerName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass the \"containerName\" property in the input object" });
            try
            {
                CloudBlobContainer container = BlobStorageHelper.GetCloudBlobContainer(_storageAccountName, _storageAccountKey, data.containerName.ToString());
                container.Delete();
            }
            catch (System.Exception ex)
            {
                log.Error("Error when trying to delete the container", ex);
                throw;
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
