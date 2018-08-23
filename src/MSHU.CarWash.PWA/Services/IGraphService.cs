using Microsoft.Graph;

namespace MSHU.CarWash.PWA.Services
{
    /// <summary>
    /// Service to help working with Microsoft Graph
    /// </summary>
    public interface IGraphService
    {
        /// <summary>
        /// Get an authenticated Microsoft Graph Service client
        /// </summary>
        /// <returns>Authenticated Graph service client</returns>
        GraphServiceClient GetAuthenticatedClient();
    }
}
