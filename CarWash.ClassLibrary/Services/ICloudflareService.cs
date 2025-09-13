using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Service for managing Cloudflare cache operations.
    /// </summary>
    public interface ICloudflareService
    {
        /// <summary>
        /// Purges the cache for the .well-known/configuration endpoints.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PurgeConfigurationCacheAsync();
    }
}