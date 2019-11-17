using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace AzureBlobStorageSample.Azure
{
    public class AzureBlobReader
    {
        private readonly AzureBlobSettings _blobSettings;

        public AzureBlobReader(AzureBlobSettings blobSettings)
        {
            _blobSettings = blobSettings;
        }

        public async Task<MemoryStream> ReadFileAsync(string blobFileName)
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_blobSettings.ContainerName);
            var containerExists = await container
                .ExistsAsync()
                .ConfigureAwait(false);

            if (!containerExists)
            {
                throw new InvalidOperationException(
                    $"Container '{_blobSettings.ContainerName}' does not exist."
                );
            }

            var memoryStream = new MemoryStream();

            var cloudBlockBlob = container.GetBlockBlobReference(blobFileName);
            await cloudBlockBlob
                .DownloadToStreamAsync(memoryStream, null, null, null)
                .ConfigureAwait(false);

            memoryStream.Position = 0;

            return memoryStream;
        }

        public async Task<byte[]> ReadFilePartAsync(string blobFileName, int offset, int length)
        {
            var storageAccount = CloudStorageAccount.Parse(_blobSettings.ConnectionString);

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_blobSettings.ContainerName);
            var containerExists = await container
                .ExistsAsync()
                .ConfigureAwait(false);

            if (!containerExists)
            {
                throw new InvalidOperationException(
                    $"Container '{_blobSettings.ContainerName}' does not exist."
                );
            }

            var target = new byte[length];
            var cloudBlockBlob = container.GetBlockBlobReference(blobFileName);
            await cloudBlockBlob
                .DownloadRangeToByteArrayAsync(target, 0, offset, length)
                .ConfigureAwait(false);

            return target;
        }
    }
}
