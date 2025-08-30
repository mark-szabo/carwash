using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Provides functionality for uploading company logos to Azure Blob Storage from a specified URL.
    /// </summary>
    /// <remarks>This service uploads files to a predefined container and folder within Azure Blob Storage. 
    /// The container name is "static-assets" and the folder name is "logos". The uploaded file is  stored with a ".jpg"
    /// extension. Ensure that the provided URL is accessible and the storage  account connection string is correctly
    /// configured in the application settings.</remarks>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a company logo to a cloud storage container from the specified URL.
        /// </summary>
        /// <remarks>The method retrieves the file from the specified URL and uploads it to a predefined
        /// folder within a cloud storage container. The file is saved with a ".jpg" extension, and any existing file
        /// with the same name will be overwritten.</remarks>
        /// <param name="fileUrl">The URL of the file to be uploaded. Must be a valid, accessible URL.</param>
        /// <param name="fileName">The name to assign to the uploaded file, excluding the file extension.</param>
        /// <returns></returns>
        Task UploadCompanyLogoFromUrlAsync(string fileUrl, string fileName);
    }
}