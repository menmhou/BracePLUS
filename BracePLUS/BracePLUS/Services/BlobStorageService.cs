using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BlobStorageTutorial
{
    class BlobStorageService
    {
        readonly static CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=xamarinstorageaccmhou;AccountKey=Rws7K4rp/H30YwE+98OTtyagugaV42lt1Iev8yGTBoqSkRidAW11zSuW3uwymYzdZkCDQ2q78rWIbNytwMU9/Q==;EndpointSuffix=core.windows.net");
        readonly static CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

        public List<string> names;

        public BlobStorageService()
        {
            names = new List<string>();
        }

        public async Task<List<T>> GetBlobs<T>(string containerName, string prefix = "", int? maxresultsPerQuery = null, BlobListingDetails blobListingDetails = BlobListingDetails.None) where T : ICloudBlob
        {
            // Retrieve reference to container
            var blobContainer = cloudBlobClient.GetContainerReference(containerName);

            var blobList = new List<T>();

            BlobContinuationToken continuationToken = null;

            try
            {
                do
                {
                    var response = await blobContainer.ListBlobsSegmentedAsync(prefix, true, blobListingDetails, maxresultsPerQuery, continuationToken, null, null);

                    continuationToken = response?.ContinuationToken;

                    foreach (var blob in response?.Results?.OfType<T>())
                    {
                        blobList.Add(blob);
                        names.Add(blob.Name);
                        Debug.WriteLine("Downloading blob: {0}", blob.Name);
                    }

                } while (continuationToken != null);
            }
            catch (Exception ex)
            {
                // Handle exception
                Debug.WriteLine("Blow download failed with exception: " + ex.Message);
            }

            return blobList;
        }

        public static async Task<CloudBlockBlob> SaveBlockBlob(string containerName, byte[] blob, string blobTitle)
        {
            var blobContainer = cloudBlobClient.GetContainerReference(containerName);

            var blockBlob = blobContainer.GetBlockBlobReference(blobTitle);
            await blockBlob.UploadFromByteArrayAsync(blob, 0, blob.Length);

            return blockBlob;
        }
    }
}
