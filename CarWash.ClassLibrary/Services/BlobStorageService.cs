using Azure.Storage.Blobs;
using System.Net.Http;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class BlobStorageService(BlobServiceClient blobServiceClient, IHttpClientFactory httpClientFactory) : IBlobStorageService
    {
        private const string CONTAINER_NAME = "static-assets";
        private const string FOLDER_NAME = "logos";
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        /// <inheritdoc />
        public async Task UploadCompanyLogoFromUrlAsync(string fileUrl, string fileName)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var fileStream = await response.Content.ReadAsStreamAsync();

            var containerClient = blobServiceClient.GetBlobContainerClient(CONTAINER_NAME);
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{FOLDER_NAME}/{fileName}.jpg";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, overwrite: true);
        }
    }
}
