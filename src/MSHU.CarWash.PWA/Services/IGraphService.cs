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

        /// <summary>
        /// Save access token to the scoped service for later use in the request
        /// </summary>
        /// <param name="accessToken">the access token</param>
        void SaveAccessToken(string accessToken);
    }
}
