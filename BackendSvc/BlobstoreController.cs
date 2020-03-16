using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Azure.Storage;
using Microsoft.Azure.Management.Storage;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using Microsoft.Azure.Management.Storage.Models;

namespace BackendSvc
{
    [Route("api/[controller]")]
    public class BlobstoreController : ControllerBase
    {
        IConfiguration _configuration;

        public BlobstoreController (IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("blobssp")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IEnumerable<string> ListContainerSP()
        {
            // access storage and generate a SAS URL with a service principal in the same tenant as the storage account
            var (service, storageAccount, storageContainer) = GetBlobServiceWithServicePrincipal();
            var container = service.GetBlobContainerClient(storageContainer);

            List<string> names = new List<string>();
            foreach (BlobItem blob in container.GetBlobs())
            {
                names.Add(blob.Name);
            }

            string lastBlob = (names.Count > 0) ? names.Last() : "";
            string sasurl = GetContainerSASWithServicePrincipal(service, storageAccount, storageContainer, lastBlob);
            names.Add(sasurl);

            return names;
        }

        [HttpGet("blobs")]
        public async Task<IEnumerable<string>> ListContainer()
        {
            // access storage and generate a SAS URL in an Azure Lighthouse customer's tenant with a service principal 
            // in the Lighthouse Provider tenant
            var (service, storageAccount, storageContainer, credential) = await GetBlobServiceWithLighthouse();
            var container = service.GetBlobContainerClient(storageContainer);

            List<string> names = new List<string>();
            foreach (BlobItem blob in container.GetBlobs())
            {
                names.Add(blob.Name);
            }

            string lastBlob = (names.Count > 0) ? names.Last() : "";
            string sasurl = GetContainerSASWithLighthouse(service, storageAccount, storageContainer, lastBlob, credential);
            names.Add(sasurl);

            return names;
        }

        [HttpGet("user")]
        public JsonResult Get()
        {
            return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
        }

        private (BlobServiceClient service, string account, string container) GetBlobServiceWithServicePrincipal()
        {
            var spMap = _configuration.GetSection("withServicePrincipal");
            string idp = User.Claims.FirstOrDefault(x => (x.Type == "idp" || 
                x.Type.EndsWith("identityprovider", true, null)))?.Value;
            string cfgSec = String.IsNullOrEmpty(idp) ? "default" : idp;
            var spConfig = spMap.GetSection(cfgSec);
            if (!spConfig.Exists())
                spConfig = spMap.GetSection("default");
            
            string tenantid = spConfig["tenantid"];
            string spid = spConfig["spid"];
            string spkeyName = spConfig["spkeyName"];
            string spkey = _configuration[spkeyName];
            Uri aadAuthEndpoint = new Uri("https://login.microsoftonline.com/");
            string storageAccount = spConfig["storageAccount"];
            string storageContainer = spConfig["storageContainer"];
            Uri accountUri = new Uri(String.Format("https://{0}.blob.core.windows.net", storageAccount));

            TokenCredential credential =
                new ClientSecretCredential(
                    tenantid,
                    spid,
                    spkey,
                    new TokenCredentialOptions() { AuthorityHost = aadAuthEndpoint });

            // Create a client that can authenticate using our token credential
            BlobServiceClient service = new BlobServiceClient(accountUri, credential);

            return (service, storageAccount, storageContainer);
        }

        private string GetContainerSASWithServicePrincipal(BlobServiceClient service, 
            string storageAccount, string storageContainer, string blobName)
        {
            DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddHours(2);
            BlobSasBuilder blobSasBuilder = new BlobSasBuilder
            {
                BlobContainerName = storageContainer,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(2)
            };
            blobSasBuilder.SetPermissions(
                BlobContainerSasPermissions.Read |
                BlobContainerSasPermissions.Create |
                BlobContainerSasPermissions.List);

            UserDelegationKey delegationKey = service.GetUserDelegationKey(null, expiresOn).Value;
            var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(delegationKey, storageAccount);
            String uri = String.IsNullOrEmpty(blobName) ? 
                String.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list&{2}", 
                    storageAccount,
                    storageContainer,
                    sasQueryParameters.ToString()) :
                String.Format("https://{0}.blob.core.windows.net/{1}/{2}?{3}", 
                    storageAccount,
                    storageContainer,
                    blobName,
                    sasQueryParameters.ToString());
            return uri;
        }

        private async Task<(BlobServiceClient service, string account, string container, StorageSharedKeyCredential credential)> 
            GetBlobServiceWithLighthouse()
        {
            var lhConfig = _configuration.GetSection("withLighthouse");
            string tenantid = lhConfig["tenantid"];
            string spid = lhConfig["spid"];
            string spkeyName = lhConfig["spkeyName"];
            string spkey = _configuration[spkeyName];

            var spMap = lhConfig.GetSection("userStorageMap");
            string idp = User.Claims.FirstOrDefault(x => (x.Type == "idp" || 
                x.Type.EndsWith("identityprovider", true, null)))?.Value;
            string cfgSec = String.IsNullOrEmpty(idp) ? "default" : idp;
            var spConfig = spMap.GetSection(cfgSec);
            if (!spConfig.Exists())
                spConfig = spMap.GetSection("default");

            string subscriptionId = spConfig["subscriptionId"];
            string resourceGroupName = spConfig["resourceGroupName"];
            string storageAccount = spConfig["storageAccount"];
            string storageContainer = spConfig["storageContainer"];
            Uri accountUri = new Uri(String.Format("https://{0}.blob.core.windows.net", storageAccount));

            // use lighthouse service principal to get customer's storage account key
            var app = ConfidentialClientApplicationBuilder.Create(spid)
                        .WithClientSecret(spkey)
                        .WithTenantId(tenantid)
                        .Build();
            var result = await app.AcquireTokenForClient(new string[] { "https://management.azure.com/.default" })
                                .ExecuteAsync();
            TokenCredentials tokenCredentials = new TokenCredentials(result.AccessToken);

            StorageManagementClient storageManagementClient = 
                new StorageManagementClient(tokenCredentials) {SubscriptionId = subscriptionId };
            IList<StorageAccountKey> acctKeys = 
                storageManagementClient.StorageAccounts.ListKeys(resourceGroupName, storageAccount).Keys;

            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(storageAccount, acctKeys[0].Value);
            BlobServiceClient service = new BlobServiceClient(accountUri, credential);
            return (service, storageAccount, storageContainer, credential);
        }

        private string GetContainerSASWithLighthouse(BlobServiceClient service, string storageAccount,
            string storageContainer, string blobName, StorageSharedKeyCredential credential)
        {
            BlobSasBuilder blobSasBuilder;

            BlobContainerClient container = service.GetBlobContainerClient(storageContainer);
            BlobContainerAccessPolicy policy = container.GetAccessPolicy();
            BlobSignedIdentifier blobSignedIdentifier = policy.SignedIdentifiers.FirstOrDefault(x => x.Id == "expiresapr"); 

            // if stored access policy exists, use it, otherwise, specify permissions
            if (blobSignedIdentifier != null)
            {
                blobSasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = storageContainer,
                    Identifier = blobSignedIdentifier.Id
                };
                /* load test how fast we can generate SAS token
                for (int ii = 0; ii < 100000; ++ii )
                {
                    var blobSas = new BlobSasBuilder
                    {
                        BlobContainerName = storageContainer,
                        BlobName = "abc" + ii, 
                        Identifier = blobSignedIdentifier.Id
                    };
                    var param = blobSas.ToSasQueryParameters(credential).ToString();
                }*/
            }
            else
            {
                DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddHours(2);
                blobSasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = storageContainer,
                    ExpiresOn = expiresOn,
                };
                blobSasBuilder.SetPermissions(
                    BlobContainerSasPermissions.Read |
                    BlobContainerSasPermissions.Create |
                    BlobContainerSasPermissions.List);
            }

            var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(credential).ToString();
            String uri = String.IsNullOrEmpty(blobName) ? 
                String.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list&{2}", 
                    storageAccount,
                    storageContainer,
                    sasQueryParameters.ToString()) :
                String.Format("https://{0}.blob.core.windows.net/{1}/{2}?{3}", 
                    storageAccount,
                    storageContainer,
                    blobName,
                    sasQueryParameters.ToString());

            return uri;
        }
    }
}
